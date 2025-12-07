using Antlr4.Runtime;
namespace test
{
    public class ErrorListener : BaseErrorListener
    {
        public List<string> SyntaxErrorsList { get; } = new List<string>();

        public bool HasSyntaxErrors => SyntaxErrorsList.Count > 0;


        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e)
        {
            SyntaxErrorsList.Add($"Syntax error at line {line}, column {charPositionInLine}: {msg}");
        }

        public void PrintErrors()
        {
            foreach (string error in SyntaxErrorsList)
                Console.WriteLine(error);
        }
    }
}
