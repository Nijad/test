// هذا الملف يحتوي على مستمع للأخطاء (ErrorListener) خاص بمولد التحليل (ANTLR)
// التعليقات بالعربية توضح أن هذا المكون يقوم بالتقاط وتحويل أخطاء التحليل اللغوي/التركيبي
// ولا يقوم بتغيير منطق التنفيذ.

using Antlr4.Runtime;
namespace test
{
    public class ErrorListener : BaseErrorListener
    {
        // قائمة لتخزين رسائل أخطاء التحليل اللغوي/التركيبي
        public List<string> SyntaxErrorsList { get; } = new List<string>();
        // خاصية للتحقق مما إذا كانت هناك أخطاء تحليل لغوي/تركيبي
        public bool HasSyntaxErrors => SyntaxErrorsList.Count > 0;

        // تجاوز طريقة التقاط أخطاء التحليل اللغوي/التركيبي
        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e)
        {
            SyntaxErrorsList.Add($"Syntax error at line {line}, column {charPositionInLine}: {msg}");
        }
        
        // طريقة لطباعة جميع أخطاء التحليل اللغوي/التركيبي المخزنة
        public void PrintErrors()
        {
            foreach (string error in SyntaxErrorsList)
                Console.WriteLine(error);
        }
    }
}
