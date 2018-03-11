using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
            CreateDescriptor("JLD0001", "Disposable object being constructed.");

        public override ImmutableArray<DiagnosticDescriptor>
            SupportedDiagnostics => ImmutableArray.Create(DisposableRule);

        private static readonly object[] _emptyMessageArgs = new object[0];
        private static readonly SyntaxKind[] _syntaxKinds = {
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.InvocationExpression,
            SyntaxKind.VariableDeclaration,
            SyntaxKind.FieldDeclaration
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

                    if (IsDisposable(type.Type))
                    {
                        Report(context);
                    }

                    break;
                case InvocationExpressionSyntax invoke:
                    var symbol = context.SemanticModel
                        .GetSymbolInfo(invoke, cancel).Symbol;

                    if (symbol is IMethodSymbol methodInfo &&
                        IsDisposable(methodInfo.ReturnType))
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

        private static void Report(SyntaxNodeAnalysisContext context)
        {
            var diagnostic = Diagnostic.Create(
                DisposableRule, context.Node.GetLocation(),
                _emptyMessageArgs);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsDisposable(ITypeSymbol info)
        {
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
    }
}
