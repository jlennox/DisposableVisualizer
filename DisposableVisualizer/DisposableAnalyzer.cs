using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lennox.DisposableVisualizer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisposableAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor DisposableRule =
            CreateDescriptor("JLD0001", "Disposable object constructed.");

        public override ImmutableArray<DiagnosticDescriptor>
            SupportedDiagnostics => ImmutableArray.Create(DisposableRule);

        private static readonly object[] _emptyMessageArgs = new object[0];
        private static readonly SyntaxKind[] _syntaxKinds = {
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.InvocationExpression,
            SyntaxKind.VariableDeclaration,
            SyntaxKind.FieldDeclaration,
            SyntaxKind.UsingStatement,
            SyntaxKind.ArgumentList
        };

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, _syntaxKinds);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var cancel = context.CancellationToken;

            switch (context.Node)
            {
                case ObjectCreationExpressionSyntax initializer:
                    var type = context.SemanticModel.GetTypeInfo(
                        initializer, cancel);

                    if (IsDisposable(type.Type) &&
                        !IsChildOfUsing(context.Node))
                    {
                        Report(context);
                    }

                    break;
                case InvocationExpressionSyntax invoke:
                    var symbol = context.SemanticModel
                        .GetSymbolInfo(invoke, cancel).Symbol;

                    if (symbol is IMethodSymbol methodInfo &&
                        IsDisposable(methodInfo.ReturnType) &&
                        !IsChildOfUsing(context.Node))
                    {
                        Report(context);
                    }

                    break;
                case VariableDeclarationSyntax variable:
                    //if (IsDisposable(variable.Type.))
                    {
                        //Report(context);
                    }
                    var m = variable.Type as IdentifierNameSyntax;
                    break;
                case FieldDeclarationSyntax field:
                    //if (IsDisposable(field.Declaration.Type.))
                    //{
                    //  Report(context);
                    //}

                    break;
            }
        }

        private static bool IsChildOfUsing(SyntaxNode node)
        {
            for (; node != null; node = node.Parent)
            {
                if (node is ArgumentListSyntax)
                {
                    return false;
                }

                if (node is UsingStatementSyntax)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Report(SyntaxNodeAnalysisContext context)
        {
            var diagnostic = Diagnostic.Create(
                DisposableRule, context.Node.GetLocation(),
                _emptyMessageArgs);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsDisposable(ITypeSymbol info)
        {
            var infoNs = info.ContainingNamespace.ToString();

            switch (infoNs)
            {
                case "System.IO":
                    if (info.Name == "MemoryStream")
                    {
                        return false;
                    }
                    break;
                case "System.Threading.Tasks":
                    if (info.Name == "Task")
                    {
                        return false;
                    }
                    break;
            }

            foreach (var i in info.AllInterfaces)
            {
                if (i.SpecialType == SpecialType.System_IDisposable)
                {
                    return true;
                }
            }

            return false;
        }

        private static DiagnosticDescriptor CreateDescriptor(
            string id, string description)
        {
            return new DiagnosticDescriptor(
                id, description, description,
                "Performance", DiagnosticSeverity.Warning, true);
        }

        private static SyntaxKind[] DebugCreateAllSyntaxKinds()
        {
            const int low = 0x2000;
            const int high = 0x22D9;

            var all = new SyntaxKind[high - low + 1];
            var index = 0;

            for (var i = low; i <= high; ++i, index++)
            {
                all[index] = (SyntaxKind)i;
            }

            return all;
        }

        private static string DebugDumpTree(SyntaxNode rootNode)
        {
            var sb = new StringBuilder();

            var toinspect = new Stack<Tuple<SyntaxNode, int>>();
            toinspect.Push(new Tuple<SyntaxNode, int>(rootNode, 0));

            while (toinspect.Count > 0)
            {
                var tuple = toinspect.Pop();
                var node = tuple.Item1;
                var depth = tuple.Item2;

                sb.Append(' ', depth * 4);
                sb.Append(node.GetType().Name);
                sb.Append('\t');

                var nodeString = node.ToString();

                foreach (var chr in nodeString)
                {
                    switch (chr)
                    {
                        case '\n':
                        case '\r':
                            continue;
                    }

                    sb.Append(chr);
                }

                sb.Append('\n');

                foreach (var kid in node.ChildNodes())
                {
                    toinspect.Push(new Tuple<SyntaxNode, int>(kid, depth + 1));
                }
            }

            var description = sb.ToString();

            return description;
        }
    }
}
