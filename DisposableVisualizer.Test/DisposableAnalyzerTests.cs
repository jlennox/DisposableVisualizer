using Lennox.DisposableVisualizer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DiagnosticVerifier = Lennox.DisposableVisualizer.Test.Verifiers.DiagnosticVerifier;

namespace Lennox.DisposableVisualizer.Test
{
    [TestClass]
    public class DisposableAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        public void VerifyDisposableAnalyzerDetection()
        {
            const string propTest = "/*propTest*/";
            const string fieldTest = "/*fieldTest*/";
            const string localTest = "/*localTest*/";
            const string returnTest = "/*returnTest*/";
            const string methodReturnTest = "/*methodReturnTest*/";

            var code =
                $@"using System.IO;
using System;

public class TestClass
{{
    public MemoryStream PropertyMemoryStream => {propTest}new MemoryStream();
    private readonly MemoryStream _fieldStream = {fieldTest}new MemoryStream();

    public void TestMethod()
    {{
        var methodBodyStream = {localTest}new MemoryStream();
        var notDisposable = new DateTime();
        var returnedStream = {returnTest}ReturnsDisposable();
    }}

    private static MemoryStream ReturnsDisposable()
    {{
        return {methodReturnTest}new MemoryStream();
    }}
}}";

            DiagnosticResult TestFor(string testName, string start)
            {
                return new DiagnosticResult {
                    Id = DisposableAnalyzer.DisposableRule.Id,
                    Locations = new[] { LocationOf(code, testName, start) },
                    Severity = DiagnosticSeverity.Warning,
                    Message = DisposableAnalyzer.DisposableRule.MessageFormat.ToString()
                };
            }

            VerifyCSharpDiagnostic(code, new[]
            {
                TestFor(propTest, "new"),
                TestFor(fieldTest, "new"),
                TestFor(localTest, "new"),
                TestFor(returnTest, "ReturnsDisposable"),
                TestFor(methodReturnTest, "new")
            });
        }

        private static DiagnosticResultLocation LocationOf(
            string code, string large, string start)
        {
            var largeIndex = code.IndexOf(large);

            if (largeIndex == -1)
            {
                return default(DiagnosticResultLocation);
            }

            var startIndex = code.IndexOf(start, largeIndex);

            if (startIndex == -1)
            {
                return default(DiagnosticResultLocation);
            }

            var lineCount = 1;
            var firstLine = true;
            var charCount = 0;

            for (var i = startIndex; i >= 0; --i)
            {
                if (code[i] == '\n')
                {
                    ++lineCount;
                    firstLine = false;
                    continue;
                }

                if (firstLine)
                {
                    ++charCount;
                }
            }

            return new DiagnosticResultLocation(
                "Test0.cs", lineCount, charCount);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DisposableAnalyzer();
        }
    }
}