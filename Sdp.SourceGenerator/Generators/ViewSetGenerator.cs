using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class ViewSetGenerator
{
    private const string ManagerNamespace = "Sdp.Manager";
    private const string ManagerTypeName = "StaticDataManager";

    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var viewSets = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateManagerClass(node),
                transform: static (syntaxContext, cancellationToken) =>
                    Analyze(syntaxContext, cancellationToken))
            .Where(static analysis => analysis is not null)
            .Collect()
            .SelectMany(static (analyses, _) =>
            {
                var firstTableSetByViewSet = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var reportedConflictsByViewSet = new Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
                var list = new List<ViewSetAnalysis>(analyses.Length);
                foreach (var analysis in analyses)
                {
                    // 같은 ViewSet 을 공유하는 매니저가 둘 이상이면 소스/빌더 셸은 첫 매니저로 한 번만 방출한다.
                    // 진단은 모두 ViewSet 심볼 기준(동일 id·위치·메시지)이라 첫 매니저가 이미 보고하므로,
                    // 두 번째 이후 매니저에서는 비워 중복 보고를 막는다.
                    if (!firstTableSetByViewSet.TryGetValue(analysis!.ViewSetSymbol, out var firstTableSet))
                    {
                        firstTableSetByViewSet[analysis.ViewSetSymbol] = analysis.TableSetSymbol;
                        list.Add(analysis);
                        continue;
                    }

                    // 생성 Build 의 시그니처는 TableSet 에 종속이라 ViewSet 당 한 TableSet 만 지원한다.
                    // 다른 TableSet 매니저가 같은 ViewSet 을 쓰면 두 번째 Build 가 방출되지 않아
                    // 연쇄 컴파일 오류만 남으므로 전용 진단(SDP0307)으로 차단한다.
                    if (!SymbolEqualityComparer.Default.Equals(firstTableSet, analysis.TableSetSymbol))
                    {
                        if (!reportedConflictsByViewSet.TryGetValue(analysis.ViewSetSymbol, out var reportedTableSets))
                        {
                            reportedTableSets = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                            reportedConflictsByViewSet[analysis.ViewSetSymbol] = reportedTableSets;
                        }

                        // 같은 TableSet 으로 충돌하는 매니저가 여럿이어도 진단은 (ViewSet, TableSet)
                        // 조합당 한 번만 보고한다 — 위치·메시지가 동일해 중복 표시만 된다.
                        var isFirstConflictForTableSet = reportedTableSets.Add(analysis.TableSetSymbol);
                        var conflictDiagnostics = ImmutableArray<Diagnostic>.Empty;
                        if (isFirstConflictForTableSet)
                        {
                            conflictDiagnostics = ImmutableArray.Create(Diagnostic.Create(
                                SdpDiagnostics.ViewSetSharedByDifferentTableSets,
                                analysis.ViewSetSymbol.Locations.FirstOrDefault() ?? Location.None,
                                analysis.ViewSetSymbol.ToDisplayString(),
                                firstTableSet.ToDisplayString(),
                                analysis.TableSetSymbol.ToDisplayString()));
                        }

                        list.Add(analysis with
                        {
                            CanEmit = false,
                            CanEmitBuilderShell = false,
                            Diagnostics = conflictDiagnostics,
                        });
                        continue;
                    }

                    list.Add(analysis with
                    {
                        CanEmit = false,
                        CanEmitBuilderShell = false,
                        Diagnostics = ImmutableArray<Diagnostic>.Empty,
                    });
                }

                return list;
            });

        context.RegisterSourceOutput(viewSets, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmitBuilderShell)
            {
                return;
            }

            var source = ViewSetEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.HintName, source);
        });
    }

    private static bool IsCandidateManagerClass(SyntaxNode node)
        => node is ClassDeclarationSyntax classDecl && classDecl.BaseList is not null;

    private static ViewSetAnalysis? Analyze(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
        if (declaredSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = symbol.BaseType;
        if (baseType is null
            || baseType.ContainingNamespace?.ToDisplayString() != ManagerNamespace
            || baseType.Name != ManagerTypeName
            || baseType.TypeArguments.Length != 2)
        {
            return null;
        }

        if (baseType.TypeArguments[0] is not INamedTypeSymbol tableSetType)
        {
            // TableSet 쪽 타입 파라미터 패스스루는 TableSetGenerator 가 SDP0218 로 보고한다(단일 소유).
            return null;
        }

        if (baseType.TypeArguments[1] is not INamedTypeSymbol viewSetType)
        {
            // ViewSet 쪽 타입 파라미터 패스스루는 조용히 탈락하면 Build 미생성 + 연쇄 오류만 남으므로
            // 전용 진단으로 알린다. ViewSetSymbol 은 dedup 키와 HintName(CanEmit=false 라 미사용)에만
            // 쓰이므로 매니저 자신을 넣는다.
            if (baseType.TypeArguments[1] is ITypeParameterSymbol typeParameter)
            {
                var diagnostic = Diagnostic.Create(
                    SdpDiagnostics.ManagerTypeArgumentMustBeClosed,
                    classDecl.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    typeParameter.Name);
                return new ViewSetAnalysis(
                    symbol,
                    tableSetType,
                    ImmutableArray<ViewInfo>.Empty,
                    new[] { diagnostic },
                    false,
                    false);
            }

            return null;
        }

        var diagnostics = new List<Diagnostic>();
        var viewSetPartial = CollectViewSetPartialDiagnostic(viewSetType, diagnostics, out var viewSetIsRecord);
        var viewSetOuterPartial = ContainingTypePartialChecker.Check(viewSetType, diagnostics);

        // record syntax 가 없는 ViewSet(SDP0306)은 syntax 기반의 primary ctor 해석이 항상 실패해
        // 허위 SDP0006 이 따라붙으므로 멤버 수집을 건너뛴다.
        var viewInfos = ImmutableArray<ViewInfo>.Empty;
        var primaryConstructorResolved = false;
        if (viewSetIsRecord)
        {
            viewInfos = CollectViewInfos(
                viewSetType, tableSetType, diagnostics, out primaryConstructorResolved, cancellationToken);
        }

        // primary ctor 미해결 시 viewInfos 가 비어 All(...) 이 vacuous true 가 되므로,
        // 해석 성공 여부를 별도로 반영해 SDP0006 상태에서 Build 가 방출되는 것을 막는다.
        var canEmitBuilderShell = viewSetPartial && viewSetOuterPartial;
        var canEmit = canEmitBuilderShell
            && primaryConstructorResolved
            && viewInfos.All(view => view.IsFullyValid);

        return new ViewSetAnalysis(viewSetType, tableSetType, viewInfos, diagnostics, canEmit, canEmitBuilderShell);
    }

    private static bool CollectViewSetPartialDiagnostic(
        INamedTypeSymbol viewSetType,
        List<Diagnostic> diagnostics,
        out bool isRecordInCurrentCompilation)
    {
        var viewSetSyntax = viewSetType.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<RecordDeclarationSyntax>()
            .FirstOrDefault();

        // record 선언 syntax 가 없으면(클래스로 선언했거나 참조 어셈블리 타입) partial record 방출이
        // 불가능하므로, CS 오류만 남기고 침묵하지 않도록 명시 진단을 보고한다.
        if (viewSetSyntax is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ViewSetMustBeRecordInCurrentCompilation,
                viewSetType.Locations.FirstOrDefault() ?? Location.None,
                viewSetType.ToDisplayString()));
            isRecordInCurrentCompilation = false;
            return false;
        }

        isRecordInCurrentCompilation = true;

        if (viewSetSyntax.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
        {
            return true;
        }

        diagnostics.Add(Diagnostic.Create(
            SdpDiagnostics.ViewSetRecordMustBePartial,
            viewSetSyntax.Identifier.GetLocation(),
            viewSetType.ToDisplayString()));
        return false;
    }

    private static ImmutableArray<ViewInfo> CollectViewInfos(
        INamedTypeSymbol viewSetType,
        INamedTypeSymbol tableSetType,
        List<Diagnostic> diagnostics,
        out bool primaryConstructorResolved,
        CancellationToken cancellationToken)
    {
        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(viewSetType);
        if (primaryCtor is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustHaveSinglePrimaryConstructor,
                viewSetType.Locations.FirstOrDefault() ?? Location.None,
                viewSetType.ToDisplayString()));
            primaryConstructorResolved = false;
            return ImmutableArray<ViewInfo>.Empty;
        }

        primaryConstructorResolved = true;

        var builder = ImmutableArray.CreateBuilder<ViewInfo>(primaryCtor.Parameters.Length);

        foreach (var param in primaryCtor.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isNullable = param.NullableAnnotation == NullableAnnotation.Annotated;
            if (isNullable)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ViewSetMemberMustBeNonNullable,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    param.Type.ToDisplayString()));
            }

            var viewSymbol = param.Type as INamedTypeSymbol;
            var viewBase = viewSymbol is null ? null : StaticDataViewBaseResolver.FindBase(viewSymbol);
            if (viewSymbol is null || viewBase is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ViewSetMemberMustBeStaticDataView,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    param.Type.ToDisplayString()));
                builder.Add(new ViewInfo(
                    param.Name,
                    viewSymbol,
                    param.Type.ToDisplayString(),
                    false,
                    isNullable,
                    false,
                    false,
                    false,
                    TargetsManagerTableSet: true,
                    ViewTableSetName: null));
                continue;
            }

            // View 가 base StaticDataView<,> 에 선언한 TableSet 이 매니저의 TableSet 과 다르면,
            // 팩토리는 View 쪽 TableSet 으로 생성되는데 Build 는 매니저 TableSet 으로 호출돼
            // 생성 코드에서 CS0311 이 새거나(생성자가 매니저 쪽 타입일 때) 런타임에야 실패한다.
            // 컴파일 시점에 전용 진단으로 차단한다. base 타입 인자가 닫힌 타입이 아닌 경우는
            // StaticDataViewGenerator 와 동일하게 여기서 판정하지 않는다.
            INamedTypeSymbol? viewTableSet = null;
            if (viewBase.TypeArguments.Length == 2
                && viewBase.TypeArguments[1] is INamedTypeSymbol viewBaseTableSet)
            {
                viewTableSet = viewBaseTableSet;
            }

            var targetsManagerTableSet = viewTableSet is null
                || SymbolEqualityComparer.Default.Equals(viewTableSet, tableSetType);
            if (!targetsManagerTableSet)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ViewTargetsDifferentTableSet,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    viewSymbol.ToDisplayString(),
                    viewTableSet!.ToDisplayString(),
                    tableSetType.ToDisplayString()));
            }

            // partial 여부는 단일 syntax 가 아니라 View 심볼의 모든 선언을 기준으로 판정한다.
            // SDP0303(view non-partial) · SDP0304(ctor 누락) · SDP0002(outer non-partial) 진단은 모든 view 를
            // 보는 StaticDataViewGenerator 가 단일 소유하므로 여기서는 보고하지 않는다(중복 방지 — 진단은
            // 버리는 리스트로 받는다). isPartial · containingTypesPartial · hasValidCtor 는 아래 deferred-error
            // 경로 판단에만 쓴다.
            var viewDeclarations = viewSymbol.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .ToList();

            var isPartial = viewDeclarations.Count > 0
                && viewDeclarations.All(declaration =>
                    declaration.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));

            var containingTypesPartial = ContainingTypePartialChecker.Check(viewSymbol, new List<Diagnostic>());

            var hasValidCtor = ViewConstructorResolver.HasSingleTableSetConstructor(viewSymbol, tableSetType);

            builder.Add(new ViewInfo(
                param.Name,
                viewSymbol,
                viewSymbol.ToDisplayString(),
                true,
                isNullable,
                isPartial,
                containingTypesPartial,
                hasValidCtor,
                targetsManagerTableSet,
                viewTableSet?.ToDisplayString()));
        }

        return builder.ToImmutable();
    }

    internal sealed record ViewSetAnalysis(
        INamedTypeSymbol ViewSetSymbol,
        INamedTypeSymbol TableSetSymbol,
        ImmutableArray<ViewInfo> Views,
        IReadOnlyList<Diagnostic> Diagnostics,
        bool CanEmit,
        bool CanEmitBuilderShell)
    {
        public string HintName
        {
            get
            {
                var qualified = ViewSetSymbol.ToDisplayString(new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
                return qualified + ".ViewSetBuilder.g.cs";
            }
        }
    }

    internal sealed record ViewInfo(
        string ParameterName,
        INamedTypeSymbol? ViewSymbol,
        string TypeName,
        bool IsView,
        bool IsNullable,
        bool IsPartial,
        bool ContainingTypesPartial,
        bool HasValidCtor,
        bool TargetsManagerTableSet,
        string? ViewTableSetName)
    {
        // canEmit(ViewSetGenerator)과 HasDeferrableErrors(ViewSetEmitter)가 같은 판정을 공유한다.
        // ContainingTypesPartial 은 StaticDataViewGenerator 의 팩토리 방출 조건(CanEmitFactoryShell)과
        // 같은 판정이다. 빠지면 팩토리 없는 View 에 Build 가 방출되어 CS0117 로 새어 나간다.
        // TargetsManagerTableSet 이 빠져도 팩토리(View 쪽 TableSet)와 Build(매니저 TableSet)가 어긋나
        // 같은 방식으로 새어 나간다(SDP0308).
        public bool IsFullyValid => IsView
            && IsPartial
            && ContainingTypesPartial
            && HasValidCtor
            && TargetsManagerTableSet
            && !IsNullable;
    }
}
