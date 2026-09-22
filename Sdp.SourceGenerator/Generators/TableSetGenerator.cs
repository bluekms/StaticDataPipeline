using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class TableSetGenerator
{
    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var collected = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateStaticDataManagerClass(node),
                transform: static (syntaxContext, cancellationToken) => Analyze(syntaxContext, cancellationToken))
            .Where(static analysis => analysis is not null)
            .Collect();
        var tableSets = collected
            .SelectMany(static (analyses, _) =>
            {
                var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var list = new List<StaticDataManagerAnalysis>(analyses.Length);
                foreach (var analysis in analyses)
                {
                    var isFirstAnalysisForTableSet = seen.Add(analysis!.TableSetSymbol);
                    if (isFirstAnalysisForTableSet)
                    {
                        list.Add(analysis);
                    }
                    else
                    {
                        list.Add(analysis.BridgeOnly());
                    }
                }

                return list;
            });

        context.RegisterSourceOutput(tableSets, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmit)
            {
                return;
            }

            var source = TableSetEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.HintName, source);
        });

        var bridges = collected
            .SelectMany(static (analyses, _) =>
            {
                var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var list = new List<StaticDataManagerAnalysis>(analyses.Length);
                foreach (var analysis in analyses)
                {
                    if (seen.Add(analysis!.StaticDataManagerSymbol))
                    {
                        list.Add(analysis);
                    }
                }

                return list;
            });

        context.RegisterSourceOutput(bridges, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.StaticDataManagerDiagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmitBridge)
            {
                return;
            }

            var source = StaticDataManagerBridgeEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.BridgeHintName, source);
        });
    }

    private static bool IsCandidateStaticDataManagerClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    private static StaticDataManagerAnalysis? Analyze(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
        if (declaredSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = symbol.BaseType;
        if (baseType is null)
        {
            return null;
        }

        if (baseType.ContainingNamespace?.ToDisplayString() != "Sdp.Manager")
        {
            return null;
        }

        if (baseType.Name != "StaticDataManager")
        {
            return null;
        }

        if (baseType.TypeArguments.Length is not (1 or 2))
        {
            return null;
        }

        if (baseType.TypeArguments[0] is not INamedTypeSymbol tableSetType)
        {
            // 타입 파라미터 패스스루(class MidManager<TS> : StaticDataManager<TS>)는 지원하지 않는다.
            // 조용히 탈락하면 로더 미생성 + 미구현 추상 멤버(CS0534)만 남으므로 전용 진단으로 알린다.
            // ViewSet 쪽 타입 인자는 ViewSetGenerator 가 같은 진단을 단일 소유한다.
            if (baseType.TypeArguments[0] is ITypeParameterSymbol typeParameter)
            {
                var diagnostic = Diagnostic.Create(
                    SdpDiagnostics.StaticDataManagerTypeArgumentMustBeClosed,
                    classDecl.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    typeParameter.Name);

                // TableSetSymbol 은 SelectMany 의 dedup 키와 HintName(CanEmit=false 라 미사용)에만
                // 쓰이므로, TableSet 이 해석되지 않는 이 경로에서는 매니저 자신을 넣는다.
                return new StaticDataManagerAnalysis(
                    StaticDataManagerSymbol: symbol,
                    TableSetSymbol: symbol,
                    ViewSetSymbol: null,
                    ImmutableArray<TableInfo>.Empty,
                    ImmutableArray<RecordFkInfo>.Empty,
                    Array.Empty<Diagnostic>(),
                    new[] { diagnostic },
                    CanEmit: false,
                    CanEmitBridge: false);
            }

            return null;
        }

        // 매니저 partial 여부는 브리지(추상 훅 override) 방출 가능성을 가른다. partial 이 아니면
        // CS0534 만 남아 원인이 드러나지 않으므로 전용 진단으로 알린다.
        var staticDataManagerDiagnostics = new List<Diagnostic>();
        var staticDataManagerIsPartial = classDecl.Modifiers
            .Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        if (!staticDataManagerIsPartial)
        {
            staticDataManagerDiagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.StaticDataManagerMustBePartial,
                classDecl.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        var staticDataManagerContainingPartial =
            ContainingTypePartialChecker.Check(symbol, staticDataManagerDiagnostics);

        // WithView 매니저(타입 인자 2개)는 BuildViewSet 훅도 연결해야 한다. TViewSet 이 타입
        // 파라미터면 SDP0218 을 ViewSetGenerator 가 보고하므로 여기서는 브리지만 막는다.
        INamedTypeSymbol? viewSetType = null;
        var viewSetArgumentValid = true;
        if (baseType.TypeArguments.Length == 2)
        {
            viewSetType = baseType.TypeArguments[1] as INamedTypeSymbol;
            viewSetArgumentValid = viewSetType is not null;
        }

        var diagnostics = new List<Diagnostic>();
        var tableSetPartial = CollectTableSetPartialDiagnostics(tableSetType, diagnostics, out var tableSetIsRecord);

        // record syntax 가 없는 TableSet(SDP0216)은 syntax 기반의 primary ctor 해석이 항상 실패해
        // 허위 SDP0006 이 따라붙으므로 멤버 수집을 건너뛴다.
        var tableInfos = ImmutableArray<TableInfo>.Empty;
        var allParametersValid = false;
        if (tableSetIsRecord)
        {
            tableInfos = AnalyzeTableInfos(tableSetType, diagnostics, out allParametersValid, cancellationToken);
        }

        var tableSetOuterPartial = ContainingTypePartialChecker.Check(tableSetType, diagnostics);

        var membersByName = new Dictionary<string, INamedTypeSymbol?>(StringComparer.Ordinal);
        foreach (var tableInfo in tableInfos)
        {
            membersByName[tableInfo.ParameterName] = tableInfo.RecordSymbol;
        }

        var foreignKeysValid = ForeignKeyAttributeValidator.Validate(
            tableInfos,
            membersByName,
            diagnostics,
            cancellationToken);

        var recordFkInfos = ForeignKeyAnalyzer.CollectRecordFkInfos(tableInfos, membersByName, cancellationToken);

        var canEmit = tableSetPartial
            && tableSetOuterPartial
            && allParametersValid
            && foreignKeysValid
            && tableInfos.All(tableInfo => tableInfo.IsPartial);

        // TableSet 로더가 방출되지 않으면 브리지가 존재하지 않는 정적 진입점을 참조해 연쇄
        // 오류(CS0117)가 나므로, 그 경우 브리지를 막아 CS0534 + SDP 진단으로 원인을 한 곳에 모은다.
        var canEmitBridge = staticDataManagerIsPartial
            && staticDataManagerContainingPartial
            && viewSetArgumentValid
            && canEmit;

        return new StaticDataManagerAnalysis(
            symbol,
            tableSetType,
            viewSetType,
            tableInfos,
            recordFkInfos,
            diagnostics,
            staticDataManagerDiagnostics,
            canEmit,
            canEmitBridge);
    }

    private static bool CollectTableSetPartialDiagnostics(
        INamedTypeSymbol tableSetType,
        List<Diagnostic> diagnostics,
        out bool isRecordInCurrentCompilation)
    {
        var tableSetSyntax = tableSetType.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<RecordDeclarationSyntax>()
            .FirstOrDefault();

        // record 선언 syntax 가 없으면(클래스로 선언했거나 참조 어셈블리 타입) partial 방출 자체가
        // 불가능하므로, CS 오류만 남기고 침묵하지 않도록 명시 진단을 보고한다.
        if (tableSetSyntax is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.TableSetMustBeRecordInCurrentCompilation,
                tableSetType.Locations.FirstOrDefault() ?? Location.None,
                tableSetType.ToDisplayString()));
            isRecordInCurrentCompilation = false;

            return false;
        }

        isRecordInCurrentCompilation = true;

        if (tableSetSyntax.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
        {
            return true;
        }

        diagnostics.Add(Diagnostic.Create(
            SdpDiagnostics.TableSetRecordMustBePartial,
            tableSetSyntax.Identifier.GetLocation(),
            tableSetType.ToDisplayString()));

        return false;
    }

    private static ImmutableArray<TableInfo> AnalyzeTableInfos(
        INamedTypeSymbol tableSetType,
        List<Diagnostic> diagnostics,
        out bool allParametersValid,
        CancellationToken cancellationToken)
    {
        allParametersValid = true;

        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(tableSetType);
        if (primaryCtor is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustHaveSinglePrimaryConstructor,
                tableSetType.Locations.FirstOrDefault() ?? Location.None,
                tableSetType.ToDisplayString()));

            return ImmutableArray<TableInfo>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<TableInfo>(primaryCtor.Parameters.Length);

        foreach (var param in primaryCtor.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableType = TypeClassifier.UnwrapNullable(param.Type);
            if (tableType is not INamedTypeSymbol tableSymbol || !IsStaticDataTableSubclass(tableSymbol))
            {
                allParametersValid = false;
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.TableSetMemberMustBeStaticDataTable,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    param.Type.ToDisplayString()));
                continue;
            }

            var tableSyntax = tableSymbol.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();
            var isPartial = tableSyntax?.Modifiers
                .Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) ?? false;

            // syntax 가 없는 테이블(참조 어셈블리 타입)은 팩토리 partial 을 방출할 수 없다.
            // isPartial=false 로 canEmit 이 막히는 이유를 사용자가 알 수 있도록 명시 진단을 보고한다.
            if (tableSyntax is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.TableMustBeDeclaredInCurrentCompilation,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    tableSymbol.ToDisplayString()));
            }
            else if (!isPartial)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.StaticDataTableMustBePartial,
                    tableSyntax.Identifier.GetLocation(),
                    tableSymbol.ToDisplayString()));
            }

            var isNullable = param.NullableAnnotation == NullableAnnotation.Annotated;
            var recordType = ExtractRecordType(tableSymbol);
            builder.Add(new TableInfo(param.Name, tableSymbol, recordType, isPartial, isNullable));
        }

        return builder.ToImmutable();
    }

    private static INamedTypeSymbol? ExtractRecordType(INamedTypeSymbol tableSymbol)
    {
        for (var type = tableSymbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.ContainingNamespace?.ToDisplayString() == "Sdp.Table"
                && type.Name == "StaticDataTable"
                && type.TypeArguments.Length >= 1)
            {
                return type.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        return null;
    }

    private static bool IsStaticDataTableSubclass(INamedTypeSymbol symbol)
    {
        for (var type = symbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.ContainingNamespace?.ToDisplayString() == "Sdp.Table" && type.Name == "StaticDataTable")
            {
                return true;
            }
        }

        return false;
    }
}
