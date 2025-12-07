using Antlr4.Runtime;
using test.Content;

namespace test
{
    public class CustomLexer : SimpleLexer
    {
        public List<string> LexicalErrorsList { get; } = new List<string>();

        public bool HasLexicalErrors => LexicalErrorsList.Count > 0;

        public CustomLexer(ICharStream input) : base(input) { }

        public override void Action(RuleContext _localctx, int ruleIndex, int actionIndex)
        {
            switch (ruleIndex)
            {
                case 50:
                    UNKNOWN_CHAR_action();
                    break;
            }
        }

        private void UNKNOWN_CHAR_action()
        {
            string msg = $"Lexical error: Unknown character: '{Text}' at line {Line}, column {Column}";
            LexicalErrorsList.Add(msg);
        }
    }
}
