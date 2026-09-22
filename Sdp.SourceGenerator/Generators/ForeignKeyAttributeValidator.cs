using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class ForeignKeyAttributeValidator
{
    public static bool Validate(
        ImmutableArray<TableInfo> tables,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var allValid = true;

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (table.RecordSymbol is null || !visited.Add(table.RecordSymbol))
            {
                continue;
            }

            allValid &= ValidateRecordForeignKeys(table.RecordSymbol, membersByName, diagnostics);
        }

        return allValid;
    }

    private static bool ValidateRecordForeignKeys(
        INamedTypeSymbol recordType,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        var primaryCtor = SymbolResolver.FindPrimaryConstructor(recordType);
        if (primaryCtor is null)
        {
            return true;
        }

        var valid = true;

        foreach (var param in primaryCtor.Parameters)
        {
            var fkAttrs = new List<AttributeData>();
            var switchFkAttrs = new List<AttributeData>();

            foreach (var attr in param.GetAttributes())
            {
                if (!SdpAttributeNames.IsSdpAttribute(attr))
                {
                    continue;
                }

                if (attr.AttributeClass!.Name == "ForeignKeyAttribute")
                {
                    fkAttrs.Add(attr);
                }
                else if (attr.AttributeClass.Name == "SwitchForeignKeyAttribute")
                {
                    switchFkAttrs.Add(attr);
                }
            }

            if (fkAttrs.Count > 0 && switchFkAttrs.Count > 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ForeignKeySwitchConflict,
                    ParameterLocation(param),
                    recordType.Name,
                    param.Name));

                valid = false;
            }

            foreach (var attr in fkAttrs)
            {
                ForeignKeyTargetValidator.Validate(attr, param, membersByName, diagnostics);
            }

            ForeignKeyTargetValidator.ValidateTargetUniqueness(recordType, param, fkAttrs, diagnostics);

            if (switchFkAttrs.Count > 0)
            {
                foreach (var attr in switchFkAttrs)
                {
                    SwitchForeignKeyValidator.ValidateTarget(attr, param, membersByName, diagnostics);
                }

                SwitchForeignKeyValidator.ValidateConditionUniqueness(recordType, param, switchFkAttrs, diagnostics);

                SwitchForeignKeyValidator.ValidateConditionColumnExists(recordType, param, switchFkAttrs, diagnostics);

                var consistent = SwitchForeignKeyValidator
                    .ValidateConditionColumnConsistency(recordType, param, switchFkAttrs, diagnostics);
                valid &= consistent;

                if (consistent)
                {
                    SwitchForeignKeyConditionValueValidator.Validate(recordType, param, switchFkAttrs, diagnostics);

                    SwitchForeignKeyValidator
                        .ValidateConditionValueEquivalence(recordType, param, switchFkAttrs, diagnostics);
                }
            }
        }

        return valid;
    }

    private static Location ParameterLocation(IParameterSymbol param)
        => param.Locations.FirstOrDefault() ?? Location.None;
}
