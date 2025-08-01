/*------------------------------------------------------------------------------
Wintellect.Analyzers - .NET Compiler Platform ("Roslyn") Analyzers and CodeFixes
Copyright (c) Wintellect. All rights reserved
Licensed under the MIT license
------------------------------------------------------------------------------*/
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Wintellect.Analyzers
{
    // This rule should support Visual Basic, but I can't get any of the test code
    // working in VS 2015 CTP5 for VB.NET. I'll come back to this on the next CTP.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    //[DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    [DebuggerDisplay("Rules={DiagnosticIds.FunctionShouldNotOverMaximumLinesAnalyzer}")]
    public sealed class FunctionShouldNotOverMaximumLinesAnalyzer : DiagnosticAnalyzer
    {
        private const Int32 MaximumLines = 20;

        /// <summary>
        /// Bu alanda kuralımızı tanımlıyoruz
        /// Kural başlığı, açıklaması, mesaj formatı ve kategori gibi bilgileri içerir.
        /// Yani burada yazılan işlemler visual studio'da kuralımızın nasıl görüneceğini belirler.
        /// örneğin bir hata ile karşılaştığımızda FunctionShouldNotOverMaximumLinesAnalyzer hatası ve açıklaması bu şekilde bulunuyor diye hata verir.
        /// </summary>
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.FunctionShouldNotOverMaximumLinesAnalyzer,
            new LocalizableResourceString(nameof(Resources.FunctionShouldNotOverMaximumLinesAnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.FunctionShouldNotOverMaximumLinesAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.CategoryDesign), Resources.ResourceManager, typeof(Resources)).ToString(),
            DiagnosticSeverity.Warning,
            true,
            new LocalizableResourceString(nameof(Resources.FunctionShouldNotOverMaximumLinesAnalyzerDescription), Resources.ResourceManager, typeof(Resources)),
            "http://medium.com/@yusuf7");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // Method declarations için callback kaydet
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);

            // Constructor declarations için callback kaydet
            context.RegisterSyntaxNodeAction(AnalyzeConstructorDeclaration, SyntaxKind.ConstructorDeclaration);

            // Property accessor declarations için callback kaydet
            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            // Generated code'u atla
            //derleyici tarafından oluşturulan veya kullanıcı kodu olmayan kodları atlamak için bu kontrolü yapıyoruz.
            if (context.IsGeneratedOrNonUserCode())
            {
                return;
            }

            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Eğer methodumuzun gövdesi yoksa, yani bir abstract method ise, analiz etmiyoruz.
            if (methodDeclaration == null || (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null))
            {
                return;
            }

            // Methodumuzun gövdesindeki satır sayısını alıyoruz.
            Int32 lineCount = methodDeclaration.Body?.GetText()?.Lines.Count ?? methodDeclaration.ExpressionBody?.GetText()?.Lines.Count ?? 0;


            if (lineCount > MaximumLines)
            {
                var diagnostic = Diagnostic.Create(Rule,
                    methodDeclaration.Identifier.GetLocation(),
                    methodDeclaration.Identifier.ValueText,
                    MaximumLines);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context)
        {
            // Generated code'u atla
            //derleyici tarafından oluşturulan veya kullanıcı kodu olmayan kodları atlamak için bu kontrolü yapıyoruz.
            if (context.IsGeneratedOrNonUserCode())
            {
                return;
            }

            var constructorDeclaration = (ConstructorDeclarationSyntax)context.Node;

            // Eğer constructor'ın gövdesi yoksa, analiz etmiyoruz.
            if (constructorDeclaration.Body == null )
            {
                return;
            }

            // Constructor'ın gövdesindeki satır sayısını alıyoruz.
            Int32 lineCount = CalculateConstructorLines(constructorDeclaration);

            if (lineCount > MaximumLines)
            {
                var diagnostic = Diagnostic.Create(Rule,
                    constructorDeclaration.Identifier.GetLocation(),
                    constructorDeclaration.Identifier.ValueText,
                    MaximumLines);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            // Generated code'u atla
            //derleyici tarafından oluşturulan veya kullanıcı kodu olmayan kodları atlamak için bu kontrolü yapıyoruz.
            if (context.IsGeneratedOrNonUserCode())
            {
                return;
            }

            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

            if (propertyDeclaration.AccessorList == null)
            {
                return;
            }


            foreach (var accessor in propertyDeclaration.AccessorList.Accessors)
            {
                // Auto-property accessor'ları analiz etmiyoruz
                if (accessor.Body == null)
                {
                    continue;
                }

                Int32 lineCount = CalculateAccessorLines(accessor);

                if (lineCount > MaximumLines)
                {
                    var diagnostic = Diagnostic.Create(Rule,
                        accessor.GetLocation(),
                        $"{propertyDeclaration.Identifier.ValueText}.{accessor.Keyword.ValueText}",
                        MaximumLines);

                    context.ReportDiagnostic(diagnostic);
                }
            }


        }

        private static Int32 CalculateMethodLines(MethodDeclarationSyntax method)
        {
            if (method.ExpressionBody != null)
            {
                // Eğer methodumuzun gövdesi bir ifade gövdesi ise, satır sayısını 1 olarak kabul ediyoruz.
                return 1;
            }

            if (method.Body != null)
            {
                // normal gövdeli metot olar için, gövde içindeki satır sayısını alıyoruz.
                var startLine = method.Body.OpenBraceToken.GetLocation().GetLineSpan().StartLinePosition.Line;
                var endLine = method.Body.CloseBraceToken.GetLocation().GetLineSpan().EndLinePosition.Line;

                return Math.Max(1, endLine - startLine + 1);
            }

            return 0;
        }

        private static Int32 CalculateConstructorLines(ConstructorDeclarationSyntax constructor)
        {
            if (constructor.Body != null)
            {
                // normal gövdeli yapıcı metot için, gövde içindeki satır sayısını alıyoruz.
                var startLine = constructor.Body.OpenBraceToken.GetLocation().GetLineSpan().StartLinePosition.Line;
                var endLine = constructor.Body.CloseBraceToken.GetLocation().GetLineSpan().EndLinePosition.Line;

                return Math.Max(1, endLine - startLine + 1);
            }

            return 0;
        }

        private static Int32 CalculateAccessorLines(AccessorDeclarationSyntax accessor)
        {

            if (accessor.Body != null)
            {
                // normal gövdeli erişimci için, gövde içindeki satır sayısını alıyoruz.
                var startLine = accessor.Body.OpenBraceToken.GetLocation().GetLineSpan().StartLinePosition.Line;
                var endLine = accessor.Body.CloseBraceToken.GetLocation().GetLineSpan().EndLinePosition.Line;

                return Math.Max(1, endLine - startLine + 1);
            }

            return 0;
        }

    }
}
