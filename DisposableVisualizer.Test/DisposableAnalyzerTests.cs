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
        private static DiagnosticResult TestFor(
            string code, string testName, string start)
        {
            return new DiagnosticResult {
                Id = DisposableAnalyzer.DisposableRule.Id,
                Locations = new[] { LocationOf(code, testName, start) },
                Severity = DiagnosticSeverity.Warning,
                Message = DisposableAnalyzer.DisposableRule.MessageFormat.ToString()
            };

        }
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
    public FileStream PropertyStream => {propTest}new FileStream("", FileMode.Open);
    private readonly FileStream _fieldStream = {fieldTest}new FileStream("", FileMode.Open);

    public void TestMethod()
    {{
        var methodBodyStream = {localTest}new FileStream("", FileMode.Open);
        var notDisposable = new DateTime();
        var returnedStream = {returnTest}ReturnsDisposable();
    }}

    private static Stream ReturnsDisposable()
    {{
        return {methodReturnTest}new FileStream("", FileMode.Open);
    }}
}}";

            VerifyCSharpDiagnostic(code, new[]
            {
                TestFor(code, propTest, "new"),
                TestFor(code, fieldTest, "new"),
                TestFor(code, localTest, "new"),
                TestFor(code, returnTest, "ReturnsDisposable"),
                TestFor(code, methodReturnTest, "new")
            });
        }

        [TestMethod]
        public void VerifyMemoryStreamAndTaskAreNotReported()
        {
            var code =
                $@"using System.IO;
using System;
using System.Threading.Tasks;

public class TestClass
{{
    public void TestMethod()
    {{
        var ignored = new MemoryStream();
        var ignoredTask = Task.Run(async () => {{ }});
    }}
}}";
            VerifyCSharpDiagnostic(code, new DiagnosticResult[0]);
        }

        [TestMethod]
        public void VerifyDisposablesInUsingsAreNotReported()
        {
            var code =
                $@"using System.IO;
using System;

public class TestClass
{{
    public void TestMethod()
    {{
        using (var fs = new FileStream("", FileMode.Open)) {{ }}
        using (var fs = File.OpenRead("")) {{ }}
    }}
}}";

            VerifyCSharpDiagnostic(code, new DiagnosticResult[0]);
        }

        [TestMethod]
        public void VerifyChildrenOfUsingsAreReported()
        {
            const string nestedTest = "/*nestedTest*/";
            const string nestedArgumentTest = "/*nestedArgumentTest*/";

            var code =
                $@"using System.IO;
using System;

public class TestClass
{{
    public void TestMethod()
    {{
        using (var shouldIgnore = new FileStream("", FileMode.Open)) {{
            var shouldReport = {nestedTest}new FileStream("", FileMode.Open);
            {nestedArgumentTest}TakesStream(new FileStream("", FileMode.Open));
        }}
    }}

    private void TakesStream(FileStream fs) {{ }}
}}";

            VerifyCSharpDiagnostic(code, new[] {
                TestFor(code, nestedTest, "new"),
                TestFor(code, nestedArgumentTest, "new"),
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