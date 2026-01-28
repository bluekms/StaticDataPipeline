using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.NameObjects;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest;

public class RecordNameTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void RecordName_FromSymbol_ReturnsFullNamespace()
    {
        var code = """
            namespace Docs.TestRecord;

            public sealed record SimpleRecord(int Id, string Name);
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", [syntaxTree]);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var recordSyntax = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.RecordDeclarationSyntax>()
            .First();

        var symbol = semanticModel.GetDeclaredSymbol(recordSyntax) as INamedTypeSymbol;
        Assert.NotNull(symbol);

        var recordNameFromSyntax = new RecordName(recordSyntax);
        var recordNameFromSymbol = new RecordName(symbol);

        // 두 생성자가 동일한 FullName을 반환해야 함
        Assert.Equal(recordNameFromSyntax.FullName, recordNameFromSymbol.FullName);
        Assert.Equal("Docs.TestRecord", recordNameFromSymbol.Namespace);
        Assert.Equal("SimpleRecord", recordNameFromSymbol.Name);
    }

    [Fact]
    public void RecordName_NestedRecord_FromSymbol_ReturnsFullNamespace()
    {
        var code = """
            namespace Docs.TestRecord;

            public sealed record ParentRecord(int Id, ParentRecord.ChildRecord Child)
            {
                public sealed record ChildRecord(string Name);
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", [syntaxTree]);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var recordSyntaxList = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.RecordDeclarationSyntax>()
            .ToList();

        var childRecordSyntax = recordSyntaxList.First(r => r.Identifier.ValueText == "ChildRecord");
        var childSymbol = semanticModel.GetDeclaredSymbol(childRecordSyntax) as INamedTypeSymbol;
        Assert.NotNull(childSymbol);

        var recordNameFromSyntax = new RecordName(childRecordSyntax);
        var recordNameFromSymbol = new RecordName(childSymbol);

        // 내부 정의 nested record도 동일한 FullName을 반환해야 함
        Assert.Equal(recordNameFromSyntax.FullName, recordNameFromSymbol.FullName);
        Assert.Equal("Docs.TestRecord", recordNameFromSymbol.Namespace);
        Assert.Equal("ChildRecord", recordNameFromSymbol.Name);
    }

    [Fact]
    public void RecordSchemaCatalog_FindsNestedRecord()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<RecordNameTests>() as TestOutputLogger<RecordNameTests>;
        Assert.NotNull(logger);

        var code = """
            using System.Collections.Frozen;
            using Eds.Attributes;

            namespace Docs.TestRecord;

            public sealed record ItemData([Key] int ItemId, string ItemName);

            [StaticDataRecord("Excel2", "DictionarySheet")]
            public sealed record DictionarySheet(
                int Id,
                [Length(2)] FrozenDictionary<int, ItemData> Items);
            """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        // ItemData가 카탈로그에 존재하는지 확인
        var itemDataSchemas = recordSchemaCatalog.FindAll("ItemData");
        Assert.NotEmpty(itemDataSchemas);

        // 에러 없이 ComplianceCheck 통과
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RecordSchemaCatalog_FindsInternalNestedRecord()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<RecordNameTests>() as TestOutputLogger<RecordNameTests>;
        Assert.NotNull(logger);

        var code = """
            using System.Collections.Frozen;
            using Eds.Attributes;

            namespace Docs.TestRecord;

            [StaticDataRecord("Excel2", "DictionarySheet")]
            public sealed record DictionarySheet(
                int Id,
                [Length(2)] FrozenDictionary<int, DictionarySheet.ItemData> Items)
            {
                public sealed record ItemData([Key] int ItemId, string ItemName);
            }
            """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        // 내부 정의 ItemData가 카탈로그에 존재하는지 확인
        var itemDataSchemas = recordSchemaCatalog.FindAll("ItemData");
        Assert.NotEmpty(itemDataSchemas);

        // 에러 없이 ComplianceCheck 통과
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }
}
