using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class ForeignKeyAttributeValidator
{
    public static bool Validate(
        ImmutableArray<TableInfo> tables,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        Dictionary<INamedTypeSymbol, ImmutableArray<ParameterAnalysis>> recordParameters,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var recordSymbols = tables
            .Where(static table => table.RecordSymbol is not null)
            .GroupBy<TableInfo, INamedTypeSymbol>(static table => table.RecordSymbol!, SymbolEqualityComparer.Default)
            .Select(static tablesOfRecord => tablesOfRecord.Key)
            .ToList();

        var validatedRecords = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var validatedBrandings = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        var allValid = true;
        foreach (var recordSymbol in recordSymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var primaryCtor = SymbolResolver.FindPrimaryConstructor(recordSymbol);
            if (primaryCtor is null)
            {
                continue;
            }

            var parameters = ForeignKeyAnalyzer.CollectRecordParameters(recordSymbol, primaryCtor, recordParameters, cancellationToken);
            allValid &= ValidateRecordTree(
                recordSymbol,
                parameters,
                membersByName,
                validatedRecords,
                validatedBrandings,
                diagnostics);
        }

        return allValid;
    }

    private static bool ValidateRecordTree(
        INamedTypeSymbol recordType,
        ImmutableArray<ParameterAnalysis> parameters,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        HashSet<INamedTypeSymbol> validatedRecords,
        HashSet<INamedTypeSymbol> validatedBrandings,
        List<Diagnostic> diagnostics)
    {
        var isFirstVisit = validatedRecords.Add(recordType);
        if (!isFirstVisit)
        {
            return true;
        }

        var valid = ValidateRecordForeignKeys(recordType, membersByName, diagnostics);

        foreach (var parameter in parameters)
        {
            var position = FkNestedPosition.Find(parameter);
            if (position is null)
            {
                continue;
            }

            var nestedType = position.Nested.Symbol;
            if (position.Nested.IsTypeBrandingRecord)
            {
                var isFirstBrandingVisit = validatedBrandings.Add(nestedType);
                if (isFirstBrandingVisit)
                {
                    valid &= ValidateTypeBrandingRecord(nestedType, diagnostics);
                }

                continue;
            }

            valid &= ValidateRecordTree(
                nestedType,
                position.Nested.Parameters,
                membersByName,
                validatedRecords,
                validatedBrandings,
                diagnostics);
        }

        return valid;
    }

    private static bool ValidateTypeBrandingRecord(INamedTypeSymbol brandingType, List<Diagnostic> diagnostics)
    {
        var primaryCtor = SymbolResolver.FindPrimaryConstructor(brandingType);
        if (primaryCtor is null)
        {
            return true;
        }

        var valueParam = primaryCtor.Parameters[0];
        var reportedAttributes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attr in valueParam.GetAttributes())
        {
            if (!SdpAttributeNames.IsSdpAttribute(attr))
            {
                continue;
            }

            var attributeName = attr.AttributeClass!.Name;
            if (attributeName is not (SdpAttributeNames.ForeignKey or SdpAttributeNames.SwitchForeignKey))
            {
                continue;
            }

            var isNewAttribute = reportedAttributes.Add(attributeName);
            if (!isNewAttribute)
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyInsideTypeBrandingRecord,
                ParameterLocation(valueParam),
                brandingType.Name,
                valueParam.Name,
                attributeName.Substring(0, attributeName.Length - "Attribute".Length)));
        }

        return reportedAttributes.Count == 0;
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

                if (attr.AttributeClass!.Name == SdpAttributeNames.ForeignKey)
                {
                    fkAttrs.Add(attr);
                }
                else if (attr.AttributeClass.Name == SdpAttributeNames.SwitchForeignKey)
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
