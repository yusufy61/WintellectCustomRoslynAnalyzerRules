using System;

namespace Roslyn.Analyzers
{
    static internal class DiagnosticIds
    {
        public const String FunctionShouldNotOverMaximumLinesAnalyzer = "Wintellect015";
        public const String AsyncMethodsMustEndWithAsyncAnalyzer = "Wintellect016";
        public const String PublicControllerMethodMustHaveHttpKeyAnalyzer = "Wintellect017";
        public const String NoEmptyThrowInCatchAnalyzer = "Wintellect018";
        public const String CatchBlockMustLoggingAnalyzer = "Wintellect019";
        public const String FirstMethodShouldBeInTryCatchAnalyzer = "Wintellect020";
    }
}
