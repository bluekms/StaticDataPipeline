using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.RecordTypes;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.RecordTypes;

public static class RecordPropertySchemaFactory
{
    public static PropertySchemaBase Create(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeInfo> attributeList,
        INamedTypeSymbol parentRecordSymbol,
        SchemaFactoryContext context)
    {
        if (!RecordTypeChecker.IsSupportedRecordType(propertySymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FactoryNotSupportedRecordType,
                propertyName,
                propertySymbol.Name));
        }

        var isFirstVisit = context.VisitingRecords.Add(propertySymbol);
        if (!isFirstVisit)
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.CircularReference,
                new RecordName(propertySymbol).FullName));
        }

        try
        {
            var memberSchemata = CreateMemberSchemata(propertySymbol, context);

            return new RecordPropertySchema(propertyName, propertySymbol, attributeList, memberSchemata);
        }
        finally
        {
            context.VisitingRecords.Remove(propertySymbol);
        }
    }

    private static List<PropertySchemaBase> CreateMemberSchemata(INamedTypeSymbol recordSymbol, SchemaFactoryContext context)
    {
        var recordName = new RecordName(recordSymbol);
        var memberProperties = recordSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x.DeclaringSyntaxReferences.Length > 0)
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .Where(x => x.Type is INamedTypeSymbol);

        var memberSchemata = new List<PropertySchemaBase>();
        foreach (var member in memberProperties)
        {
            var syntax = member.DeclaringSyntaxReferences[0].GetSyntax();
            var memberAttributeList = ReadMemberAttributes(syntax, context.SemanticModel);
            if (AttributeAccessors.HasAttribute<IgnoreAttribute>(memberAttributeList))
            {
                continue;
            }

            var memberPropertyName = new PropertyName(recordName, member.Name, IsNullableTypeSyntax(syntax));
            var innerSchema = TypedPropertySchemaFactory.Create(
                memberPropertyName,
                (INamedTypeSymbol)member.Type,
                memberAttributeList,
                recordSymbol,
                context);

            memberSchemata.Add(innerSchema);
        }

        return memberSchemata;
    }

    private static IReadOnlyList<AttributeInfo> ReadMemberAttributes(SyntaxNode syntax, SemanticModel semanticModel)
    {
        return syntax switch
        {
            ParameterSyntax parameter => AttributeInfoReader.Read(parameter.AttributeLists, semanticModel),
            PropertyDeclarationSyntax property => AttributeInfoReader.Read(property.AttributeLists, semanticModel),
            _ => [],
        };
    }

    private static bool IsNullableTypeSyntax(SyntaxNode syntax)
    {
        return syntax switch
        {
            ParameterSyntax parameter => parameter.Type is NullableTypeSyntax,
            PropertyDeclarationSyntax property => property.Type is NullableTypeSyntax,
            _ => false,
        };
    }
}
