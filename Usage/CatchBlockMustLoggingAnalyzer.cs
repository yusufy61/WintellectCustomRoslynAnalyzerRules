using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Wintellect.Analyzers.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [DebuggerDisplay("Rules={DiagnosticIds.CatchBlockMustLoggingAnalyzer}")]
    public class CatchBlockMustLoggingAnalyzer : DiagnosticAnalyzer
    {
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.CatchBlockMustLoggingAnalyzer,
                                                                              new LocalizableResourceString(nameof(Resources.CatchBlockMustLoggingAnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
                                                                              new LocalizableResourceString(nameof(Resources.CatchBlockMustLoggingAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
                                                                              (new LocalizableResourceString(nameof(Resources.CategoryDesign), Resources.ResourceManager, typeof(Resources))).ToString(),
                                                                              DiagnosticSeverity.Warning,
                                                                              true,
                                                                              new LocalizableResourceString(nameof(Resources.CatchBlockMustLoggingAnalyzerDescription), Resources.ResourceManager, typeof(Resources)),
                                                                              "https://www.medium.com/@yusuf7");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
        }

        private void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
        {
            if(context.IsGeneratedOrNonUserCode())
            {
                return;
            }

            var catchClause = (CatchClauseSyntax)context.Node;
            var block = catchClause.Block;

            if (block == null || !block.Statements.Any())
            {
                Report(context, catchClause);
                return;
            }

            // 4. NLog loglama var mı?
            var logMethods = new[] { "error", "warn", "info", "fatal", "debug", "trace" };
            Boolean hasLog = block.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation =>
                {
                    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                    {
                        var methodName = memberAccess.Name.Identifier.Text.ToLower();
                        if (!logMethods.Contains(methodName))
                        {
                            return false;
                        }

                        var loggerName = memberAccess.Expression.ToString().ToLower();
                        return loggerName.Contains("logger") || loggerName.Contains("logmanager");
                    }
                    return false;
                });

            if (!hasLog)
            {
                Report(context, catchClause);
            }
        }

        private void Report(SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            var diagnostic = Diagnostic.Create(Rule, node.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
