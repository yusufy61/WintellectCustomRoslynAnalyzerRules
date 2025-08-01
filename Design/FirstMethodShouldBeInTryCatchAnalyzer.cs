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

namespace Wintellect.Analyzers.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [DebuggerDisplay("Rule={DiagnosticIds.FirstMethodShouldBeInTryCatchAnalyzer}")]
    public class FirstMethodShouldBeInTryCatchAnalyzer : DiagnosticAnalyzer
    {
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.FirstMethodShouldBeInTryCatchAnalyzer,
            new LocalizableResourceString(nameof(Resources.FirstMethodShouldBeInTryCatchAnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.FirstMethodShouldBeInTryCatchAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
            (new LocalizableResourceString(nameof(Resources.CategoryUsage), Resources.ResourceManager, typeof(Resources))).ToString(),
            DiagnosticSeverity.Warning,
            true,
            new LocalizableResourceString(nameof(Resources.FirstMethodShouldBeInTryCatchAnalyzerDescription), Resources.ResourceManager, typeof(Resources)),
            "https://www.medium.com/@yusuf7");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            // Eğer ki kod kullanıcı tarafından yazılmadıysa, yani otomatik olarak oluşturulduysa, analiz etmiyoruz.
            if (context.IsGeneratedOrNonUserCode())
            {
                return;
            }

            var invocation = (InvocationExpressionSyntax)context.Node;

            // Yalnızca First() çağrılarını analiz et
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;

                if (methodName != "First")
                {
                    return;
                }

                var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol as IMethodSymbol;
                if (symbol == null || !symbol.Name.Equals("First") || !symbol.ContainingNamespace.ToString().Contains("System.Linq"))
                {
                    return;
                }

                // Eğer First() bir try bloğu içindeyse uyarma
                if (IsInsideTryBlock(invocation))
                {
                    return;
                }

                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), "First()");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private Boolean IsInsideTryBlock(SyntaxNode node)
        {
            while (node != null)
            {
                if (node is TryStatementSyntax)
                {
                    return true;
                }

                node = node.Parent;
            }
            return false;
        }
    }
}
