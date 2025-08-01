using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Wintellect.Analyzers.Design
{
    /// <summary>
    /// Controller sınıfları içinde bulunan public fonksiyonlar HTTP metodları kullanmalıdır.
    /// Eğer ki controller içerisinde bulunan public olmayan fonksiyon yanlış key değerini default olarak alarak yanlış işlemler yapabilir
    /// Bu sorunun önüne geçmek için bu kuralı yazdık.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [DebuggerDisplay("Rules={DiagnosticIds.PublicControllerMethodMustHaveHttpKeyAnalyzer}")]
    public sealed class PublicControllerMethodMustHaveHttpKeyAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// kuralımızı burada tanımlıyoruz.
        /// hata aldığında resources.resx dosyasında bulunan title, description ve message format bilgilerini kullanarak hata mesajı oluşturuyoruz.
        /// default olarak akfif olarak kullanılsın.
        /// güvenlik düzeyi uyarı olarak uyarı verilsin.
        /// </summary>
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.PublicControllerMethodMustHaveHttpKeyAnalyzer,
                                                                                Resources.PublicControllerMethodMustHaveHttpKeyAnalyzerTitle,
                                                                                Resources.PublicControllerMethodMustHaveHttpKeyAnalyzerMessageFormat,
                                                                                Resources.CategoryDesign,
                                                                                DiagnosticSeverity.Warning,
                                                                                true,
                                                                                Resources.PublicControllerMethodMustHaveHttpKeyAnalyzerDescription,
                                                                                "https://medium.com/@yusuf7");

        /// <summary>
        /// Burada 1 adet kural tanımını kullanacağız. birden fazla da kural tanımı yapabilirdik. ama bu kuralda gerek olmadığı için eklemedik.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodSyntax = (MethodDeclarationSyntax)context.Node;
            
            // burada değerin bir sınıfın içinde olup olmadığını kontrol ediyoruz.
            var classDec1 = methodSyntax.Parent as ClassDeclarationSyntax;


            // Eğer ki methodumuzun gövdesi yoksa, yani bir abstract method ise, analiz etmiyoruz.
            if(classDec1 == null)
            {
                return;
            }

            // Sınıfın ismini alıyoruz.
            var className = classDec1.Identifier.Text;

            // Eğer ki sınıf ismi Controller ile bitmiyorsa, yani bir controller sınıfı değilse, bu kuralı atla.
            if (!className.EndsWith("Controller"))
            {
                return;
            }

            // Eğer ki public olmayan bir method ise , yani private,protected veya internal ise , bu kuralı atla.
            if (!methodSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                return;
            }


            // Eğer methodun gövdesi yoksa, yani bir abstract method ise, analiz etmiyoruz.
            if(methodSyntax.Body == null && methodSyntax.ExpressionBody == null)
            {
                return;
            }


            var attributes = methodSyntax.AttributeLists.SelectMany(a => a.Attributes);
            var sematicModel = context.SemanticModel;

            var allowedHttpAttributes = new[]
            {
                "Microsoft.AspNetCore.Mvc.HttpGetAttribute",
                "Microsoft.AspNetCore.Mvc.HttpPostAttribute"
            };

            Boolean hasValidHttpAttribute = false;

            foreach (var attr in attributes)
            {
                var attrSymbol = sematicModel.GetSymbolInfo(attr).Symbol as IMethodSymbol;
                var attrClass = attrSymbol?.ContainingType;

                if (attrClass == null)
                {
                    continue;
                }

                var fullAttrName = attrClass.ToDisplayString(); // Tam namespace + class adı

                if (allowedHttpAttributes.Contains(fullAttrName))
                {
                    hasValidHttpAttribute = true;
                    break;
                }
            }

            if (!hasValidHttpAttribute)
            {
                var diagnostic = Diagnostic.Create(Rule,
                                                    methodSyntax.Identifier.GetLocation(),
                                                    methodSyntax.Identifier.Text
                                                    );

                context.ReportDiagnostic(diagnostic);
            }

            
        }
    }
}
