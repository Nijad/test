using Antlr4.Runtime;
namespace test
{
    public class ErrorListener : BaseErrorListener
    {
        public List<string> Errors { get; } = new List<string>();

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e)
        {
            string errorMessage = $"خطأ نحوي في السطر {line}, العمود {charPositionInLine}: {msg}";
            Errors.Add(errorMessage);
        }

        public void AddSemanticError(int line, int column, string message)
        {
            string errorMessage = $"خطأ دلالي في السطر {line}, العمود {column}: {message}";
            Errors.Add(errorMessage);
        }

        public bool HasErrors => Errors.Count > 0;

        public void PrintErrors()
        {
            foreach (var error in Errors)
            {
                Console.WriteLine(error);
            }
        }
    }
}
