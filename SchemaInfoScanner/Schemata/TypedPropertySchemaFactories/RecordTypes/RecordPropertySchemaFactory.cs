using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;
using SchemaInfoScanner.TypeCheckers;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.RecordTypes;

public static class RecordPropertySchemaFactory
{
    public static PropertySchemaBase Create(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeSyntax> attributeList,
        INamedTypeSymbol parentRecordSymbol)
    {
        if (!RecordTypeChecker.IsSupportedRecordType(propertySymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FactoryNotSupportedRecordType,
                propertyName,
                propertySymbol.Name));
        }

        var memberProperties = propertySymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x.DeclaringSyntaxReferences.Length > 0)
            .Where(x => x.Type is INamedTypeSymbol);

        var memberSchemata = new List<PropertySchemaBase>();
        foreach (var member in memberProperties)
        {
            var memberAttributeList = new List<AttributeSyntax>();

            foreach (var syntaxRef in member.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();

                if (syntax is ParameterSyntax parameterSyntax)
                {
                    foreach (var attrList in parameterSyntax.AttributeLists)
                    {
                        memberAttributeList.AddRange(attrList.Attributes);
                    }

                    continue;
                }

                if (syntax is PropertyDeclarationSyntax propertyDeclSyntax)
                {
                    foreach (var attrList in propertyDeclSyntax.AttributeLists)
                    {
                        memberAttributeList.AddRange(attrList.Attributes);
                    }
                }
            }

            var innerSchema = TypedPropertySchemaFactory.Create(
                propertyName,
                (INamedTypeSymbol)member.Type,
                memberAttributeList,
                propertySymbol);

            memberSchemata.Add(innerSchema);
        }

        return new RecordPropertySchema(propertyName, propertySymbol, attributeList, memberSchemata);
    }
}
