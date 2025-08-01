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
using Microsoft.CodeAnalysis.Text;

namespace Wintellect.Analyzers.Design
{
    /// <summary>
    /// 
    /// bu kural catch bloğunun içeriğini analiz eder.
    /// catch {} -> boş catch hata verir
    /// catch { throw;} -> sadece throw hata verir.
    /// catch { throw ex; } -> sadece throw ex hata verir.
    /// 
    /// Bu kural bu işlelmleri engellemek için kullanılır.
    /// 
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [DebuggerDisplay("Rule={DiagnosticIds.NoEmptyThrowInCatchAnalyzer}")]
    public class NoEmptyThrowInCatchAnalyzer : DiagnosticAnalyzer
    {
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.NoEmptyThrowInCatchAnalyzer,
                                                                               new LocalizableResourceString(nameof(Resources.NoEmptyThrowInCatchAnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
                                                                               new LocalizableResourceString(nameof(Resources.NoEmptyThrowInCatchAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
                                                                               (new LocalizableResourceString(nameof(Resources.CategoryDesign), Resources.ResourceManager, typeof(Resources))).ToString(),
                                                                               DiagnosticSeverity.Warning,
                                                                               true,
                                                                               new LocalizableResourceString(nameof(Resources.NoEmptyThrowInCatchAnalyzerDescription), Resources.ResourceManager, typeof(Resources)),
                                                                               "https://www.medium.com/yusuf7");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
        }

        /// <summary>
        /// Bu metod, catch bloğunun içeriğini analiz eder.
        /// 
        /// 
        /// Bu metetda 
        /// catch {} -> boş catch hata verir
        /// catch { throw;} -> sadece throw hata verir.
        /// catch { throw ex; } -> sadece throw ex hata verir.
        /// 
        /// Çünkü catch bloğu, bir hata yakalandığında, bu hatayı loglamak veya kullanıcıya bildirmek için kullanılır.
        /// Veya hata durumunda bir işlem yapmak için kullanılır.
        /// </summary>
        /// <param name="context"></param>
        private void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
        {
            if(context.IsGeneratedOrNonUserCode())
            {
                // Eğer ki kod kullanıcı tarafından yazılmadıysa, yani otomatik olarak oluşturulduysa, analiz etmiyoruz.
                return;
            }

            // Burada catch bloğunun kodunu alıyoruz.
            var catchClause = (CatchClauseSyntax)context.Node;
            var block = catchClause.Block;

            // 1- Catch bloğu kodu yoksa, yani catch bloğu boş ise geri döndürüyoruz.
            if (block == null || !block.Statements.Any())
            {
                Report(context, catchClause);
                return;
            }

            // 2. İçerikte yalnızca throw; varsa (loglama yok)
            if (block.Statements.Count == 1 && block.Statements[0] is ThrowStatementSyntax ts1 && ts1.Expression == null)
            {
                Report(context, catchClause);
                return;
            }

            // 3. İçerikte yalnızca throw ex; varsa
            if (block.Statements.Count == 1 && block.Statements[0] is ThrowStatementSyntax ts2 && ts2.Expression != null)
            {
                Report(context, catchClause);
                return;
            }

        }

        private void Report(SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            var diagnostic = Diagnostic.Create(Rule, node.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

    }
}
