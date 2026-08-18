using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class ForeignKeyTargetValidator
{
    public static void Validate(
        AttributeData attr,
        IParameterSymbol param,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        var args = attr.ConstructorArguments;
        if (args.Length < 2 ||
            args[0].Value is not string tableSetName ||
            args[1].Value is not string columnName)
        {
            return;
        }

        ValidateColumn(tableSetName, columnName, param, membersByName, diagnostics);
    }

    public static void ValidateColumn(
        string tableSetName,
        string columnName,
        IParameterSymbol param,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        if (!membersByName.TryGetValue(tableSetName, out var targetRecord) || targetRecord is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyTargetNotFound,
                ParameterLocation(param),
                tableSetName));

            return;
        }

        var targetCtor = SinglePrimaryConstructorResolver.Resolve(targetRecord);
        if (targetCtor is null)
        {
            return;
        }

        var targetParam = targetCtor.Parameters
            .FirstOrDefault(candidate => candidate.Name == columnName);
        if (targetParam is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyTargetColumnNotFound,
                ParameterLocation(param),
                columnName,
                targetRecord.Name));

            return;
        }

        if (targetParam.GetAttributes()
            .Any(targetAttribute => SdpAttributeNames.IsSdpAttribute(targetAttribute) &&
                                    targetAttribute.AttributeClass!.Name == "SingleColumnCollectionAttribute"))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyTargetIsSingleColumnCollection,
                ParameterLocation(param),
                columnName,
                targetRecord.Name));

            return;
        }

        var fkType = TypeClassifier.UnwrapNullable(param.Type);
        var targetType = TypeClassifier.UnwrapNullable(targetParam.Type);
        var targetCollection = TypeClassifier.ClassifyCollection(targetType);
        if (targetCollection is not null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyTargetColumnIsCollection,
                ParameterLocation(param),
                columnName,
                targetRecord.Name));

            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(fkType, targetType))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyColumnTypeMismatch,
                ParameterLocation(param),
                param.Name,
                fkType.ToDisplayString(),
                columnName,
                targetRecord.Name,
                targetType.ToDisplayString()));

            return;
        }

        // FK, Target
        // int, int 통과
        // int?, int 통과
        // int? int? 통과
        // int, int? SDP0211 : 타깃 값이 Fk 타입보다 표현력이 넓음
        if (!TypeClassifier.IsNullable(param.Type) && TypeClassifier.IsNullable(targetParam.Type))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyColumnTypeMismatch,
                ParameterLocation(param),
                param.Name,
                param.Type.ToDisplayString(),
                columnName,
                targetRecord.Name,
                targetParam.Type.ToDisplayString()));
        }
    }

    private static Location ParameterLocation(IParameterSymbol param)
        => param.Locations.FirstOrDefault() ?? Location.None;
}
