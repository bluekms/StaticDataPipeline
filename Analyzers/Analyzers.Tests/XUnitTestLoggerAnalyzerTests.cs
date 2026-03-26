using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Analyzers.Tests;

public class XUnitTestLoggerAnalyzerTests
{
    [Fact]
    public async Task XunitTestLoggerTypeAnalyzerTest()
    {
        var test = """
                   using System;

                   public class Foo
                   {
                       [Fact]
                       public void TestMethod(ITestOutputHelper testOutputHelper)
                       {
                           var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
                           if (factory.CreateLogger<Bar>() is not TestOutputLogger<Bar> logger)
                           {
                               throw new InvalidOperationException("Logger creation failed.");
                           }
                       }
                   }

                   public class Bar { }
                   """;

        var expected = CSharpAnalyzerVerifier<XunitTestLoggerTypeAnalyzer, DefaultVerifier>.Diagnostic("SDP1001")
            .WithSpan(9, 48, 9, 76)
            .WithArguments("Bar", "Foo");

        var code = test + XUnitTestPreferenceString;
        await CSharpAnalyzerVerifier<XunitTestLoggerTypeAnalyzer, DefaultVerifier>.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task XunitTestLoggerLogsAnalyzerTest()
    {
        var test = """
                   using System;

                   public class Foo
                   {
                       [Fact]
                       public void TestMethod(ITestOutputHelper testOutputHelper)
                       {
                           var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
                           if (factory.CreateLogger<Foo>() is not TestOutputLogger<Foo> logger)
                           {
                               throw new InvalidOperationException("Logger creation failed.");
                           }

                           // Assert.Empty(logger.Logs);
                       }
                   }
                   """;

        var expected = CSharpAnalyzerVerifier<XunitTestLoggerLogsAnalyzer, DefaultVerifier>.Diagnostic("SDP1002")
            .WithSpan(9, 48, 9, 76);

        var code = test + XUnitTestPreferenceString;
        await CSharpAnalyzerVerifier<XunitTestLoggerLogsAnalyzer, DefaultVerifier>.VerifyAnalyzerAsync(code, expected);
    }

    private const string XUnitTestPreferenceString = """
                                                     public class TestOutputLoggerFactory
                                                     {
                                                         public TestOutputLoggerFactory(object testOutputHelper, LogLevel logLevel) { }
                                                         public object CreateLogger<T>() => new TestOutputLogger<T>();
                                                     }

                                                     public class TestOutputLogger<T>
                                                     {
                                                         public System.Collections.Generic.List<string> Logs { get; } = new System.Collections.Generic.List<string>();
                                                     }

                                                     public enum LogLevel
                                                     {
                                                         Trace,
                                                         Debug,
                                                         Information,
                                                         Warning,
                                                         Error,
                                                         Critical,
                                                         None
                                                     }

                                                     public class FactAttribute : Attribute { }

                                                     public interface ITestOutputHelper { }
                                                     """;
}
