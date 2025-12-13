// Lexer مُخصّص يمكن تخصيص قواعده عبر ANTLR
// هنا مجرد ملف حامل للتوضيح

using Antlr4.Runtime;
using test.Content;

namespace test
{
    public class CustomLexer : SimpleLexer
    {
        // List to store lexical error messages
        public List<string> LexicalErrorsList { get; } = new List<string>();
        // Property to check if there are any lexical errors
        public bool HasLexicalErrors => LexicalErrorsList.Count > 0;
        
        public CustomLexer(ICharStream input) : base(input) { }
        // ANTLR action for handling unknown characters
        public override void Action(RuleContext _localctx, int ruleIndex, int actionIndex)
        {
            switch (ruleIndex)
            {
                // Rule index 50 corresponds to UNKNOWN_CHAR
                case 50:
                    UNKNOWN_CHAR_action();
                    break;
            }
        }
        // Action method to log unknown character errors
        private void UNKNOWN_CHAR_action()
        {
            string msg = $"Lexical error: Unknown character: '{Text}' at line {Line}, column {Column}";
            LexicalErrorsList.Add(msg);
        }
    }
}
