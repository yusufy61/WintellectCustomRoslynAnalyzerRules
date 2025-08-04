using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Wintellect.Analyzers.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [DebuggerDisplay("Rule={DiagnosticIds.NestedLoogAnalyzer}")]
    public class NestedLoogAnalyzer : DiagnosticAnalyzer
    {

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.NestedLoogAnalyzer,
            new LocalizableResourceString(nameof(Resources.NestedLoogAnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.NestedLoogAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
            (new LocalizableResourceString(nameof(Resources.CategoryUsage), Resources.ResourceManager, typeof(Resources))).ToString(),
            DiagnosticSeverity.Warning,
            true,
            new LocalizableResourceString(nameof(Resources.NestedLoogAnalyzerDescription), Resources.ResourceManager, typeof(Resources)),
            "https://www.medium.com/@yusuf7");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <summary>
        /// 
        /// Burada tüm döngü türlerini analiz ediyoruz. Temel mantık 
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void Initialize(AnalysisContext context)
        {

            context.RegisterSyntaxNodeAction(AnalyzeLoop, SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(AnalyzeLoop, SyntaxKind.ForEachStatement);
            context.RegisterSyntaxNodeAction(AnalyzeLoop, SyntaxKind.WhileStatement);
            context.RegisterSyntaxNodeAction(AnalyzeLoop, SyntaxKind.DoStatement);
        }

        /// <summary>
        ///  İçerik tekrarına gerek yok tüm döngüler için analiz işlemini gerçekleştiriyoruz.
        /// </summary>
        /// <param name="context"></param>
        private void AnalyzeLoop(SyntaxNodeAnalysisContext context)
        {
            // Eğer kod otomatik olarak oluşturulduysa, analiz etmiyoruz.
            if (context.IsGeneratedOrNonUserCode())
            {
                return;
            }

            // context.node = analiz edilen kod parçasıdır.
            // Yukarıda RegisterSyntaxNodeAction ile hangi türdeki döngüleri analiz edeceğimizi belirledik.
            // Yani LoopNode burada analiz edilen döngü türüdür.
            var LoopNode = context.Node;

            var nestedLoops = LoopNode.DescendantNodes()
                .Where(node => node.IsKind(SyntaxKind.ForStatement) ||
                               node.IsKind(SyntaxKind.ForEachStatement) ||
                               node.IsKind(SyntaxKind.WhileStatement) ||
                               node.IsKind(SyntaxKind.DoStatement))
                .Where(node => node != LoopNode);

            foreach (var nestedLoop in nestedLoops)
            {
                var diagnostic = Diagnostic.Create(Rule,
                    nestedLoop.GetLocation(),
                    nestedLoop.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
