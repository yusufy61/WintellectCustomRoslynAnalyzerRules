using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Wintellect.Analyzers.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [DebuggerDisplay("Rule={DiagnosticIds.AsyncMethodsMustEndWithAsyncAnalyzer}")]
    public class AsyncMethodsMustEndWithAsyncAnalyzer : DiagnosticAnalyzer
    {

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.AsyncMethodsMustEndWithAsyncAnalyzer,
                                                                            new LocalizableResourceString(nameof(Resources.AsyncMethodsMustEndWithAsyncAnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
                                                                            new LocalizableResourceString(nameof(Resources.AsyncMethodsMustEndWithAsyncAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
                                                                            (new LocalizableResourceString(nameof(Resources.CategoryUsage), Resources.ResourceManager, typeof(Resources))).ToString(),
                                                                            DiagnosticSeverity.Warning,
                                                                            true,
                                                                            new LocalizableResourceString(nameof(Resources.AsyncMethodsMustEndWithAsyncAnalyzerDescription), Resources.ResourceManager, typeof(Resources)),
                                                                            "https://www.github.com/yusufy61");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeMethodDeclaration, SymbolKind.Method);
        }

        private static void AnalyzeMethodDeclaration(SymbolAnalysisContext context)
        {
            // Generated code'u atla
            if (context.IsGeneratedOrNonUserCode())
            {
                return;
            }

            // veri içeriği yerine direk methodları ve geri dönüş tiplerini kontrol ediyoruz.
            var methodSymbol = (IMethodSymbol)context.Symbol;
            var returnType = methodSymbol.ReturnType;

            // Eğer dönüş tipi Task veya Task<T> değilse, zaten Async ile bitmesine gerek yok
            if (!methodSymbol.IsAsync)
            {
                // Eğer method async değilse, zaten Async ile bitmesine gerek yok
                return;
            }

            // Direk geri dönüş tipinin formatına bakar. Hangi format ile yazdırma işlemi yapılıyor.
            if (!(returnType.OriginalDefinition?.ToDisplayString() == "System.Threading.Tasks.Task" || returnType.OriginalDefinition?.ToDisplayString() == "System.Threading.Tasks.Task<TResult>"))
            {
                return;
            }

            // Metod ismi zaten "Async" ile bitiyor mu?
            // Zaten bitiyorsa, bu kuralı ihlal etmiyor
            if (methodSymbol.Name.EndsWith("Async"))
            {
                return;
            }


            var diagnostic = Diagnostic.Create(Rule,
                                                methodSymbol.Locations[0],
                                                methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
            
            

        }
    }
}
