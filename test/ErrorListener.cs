using Antlr4.Runtime;
namespace test
{
    public class ErrorListener : BaseErrorListener
    {
        public List<string> Errors { get; } = new List<string>();

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e)
        {
            string errorMessage;
            if (e != null && msg.StartsWith("Lexical"))
                errorMessage = msg;
            else
                errorMessage = $"Syntax error at line {line}, column {charPositionInLine}: {msg}";
            
            Errors.Add(errorMessage);
        }

        public void AddSemanticError(int line, int column, string message)
        {
            string errorMessage = $"Semantic Error at line {line}, column {column}: {message}";
            Errors.Add(errorMessage);
        }

        public bool HasErrors => Errors.Count > 0;

        public void PrintErrors()
        {
            foreach (string error in Errors)
                Console.WriteLine(error);
        }
    }
}
