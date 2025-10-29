namespace test
{
    using System.Collections.Generic;
    using System.Linq;

    public class SymbolTable
    {
        private Stack<Dictionary<string, Symbol>> scopes = new Stack<Dictionary<string, Symbol>>();
        private Stack<string> scopeNames = new Stack<string>();

        public string CurrentScope => scopeNames.Count > 0 ? scopeNames.Peek() : "global";
        public int ScopeDepth => scopeNames.Count;

        public SymbolTable()
        {
            EnterScope("global");
        }

        public void EnterScope(string scopeName)
        {
            scopes.Push(new Dictionary<string, Symbol>());
            scopeNames.Push(scopeName);
        }

        public void ExitScope()
        {
            if (scopes.Count > 1) // لا نخرج من النطاق العالمي
            {
                scopes.Pop();
                scopeNames.Pop();
            }
        }

        public bool AddSymbol(Symbol symbol)
        {
            if (scopes.Peek().ContainsKey(symbol.Name))
                return false;

            scopes.Peek()[symbol.Name] = symbol;
            return true;
        }

        public bool AddSymbol(string name, string type, string dataType, int line, int column, bool isStatic = false)
        {
            var symbol = new Symbol(name, type, dataType, line, column, CurrentScope, isStatic);
            return AddSymbol(symbol);
        }

        public Symbol Lookup(string name)
        {
            foreach (var scope in scopes)
            {
                if (scope.ContainsKey(name))
                    return scope[name];
            }
            return null;
        }

        public Symbol LookupInCurrentScope(string name)
        {
            return scopes.Peek().ContainsKey(name) ? scopes.Peek()[name] : null;
        }

        public List<Symbol> GetAllSymbols()
        {
            var allSymbols = new List<Symbol>();
            foreach (var scope in scopes)
            {
                allSymbols.AddRange(scope.Values);
            }
            return allSymbols;
        }

        public void PrintSymbolTable()
        {
            Console.WriteLine("=== جدول الرموز ===");
            foreach (var symbol in GetAllSymbols())
            {
                Console.WriteLine(symbol);
            }
            Console.WriteLine("===================");
        }
    }
}
