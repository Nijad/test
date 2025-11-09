namespace test
{
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
            Console.WriteLine($"scope enter: {scopeName}");
        }

        public void ExitScope()
        {
            if (scopes.Count > 1) // لا نخرج من النطاق العالمي
            {
                string exitedScope = scopeNames.Pop();
                scopes.Pop();
                Console.WriteLine($"scope exit: {exitedScope}");
            }
        }

        public bool AddSymbol(Symbol symbol)
        {
            if (scopes.Peek().ContainsKey(symbol.Name))
            {
                Console.WriteLine($"worning: symbole '{symbol.Name}' is already existed in the scope '{CurrentScope}'");
                return false;
            }

            scopes.Peek()[symbol.Name] = symbol;
            Console.WriteLine($"adding symbole: {symbol.Name} with type {symbol.DataType} in the scope {CurrentScope}");
            return true;
        }

        public Symbol Lookup(string name)
        {
            // البحث من الأعلى إلى الأسفل (من أحدث نطاق إلى أقدم نطاق)
            foreach (Dictionary<string, Symbol> scope in scopes)
            {
                if (scope.ContainsKey(name))
                {
                    Console.WriteLine($"symbole '{name}' was found in current scope");
                    return scope[name];
                }
            }
            Console.WriteLine($"symbole '{name}' is not exist in the scope");
            return null;
        }

        public Symbol LookupInCurrentScope(string name)
        {
            if (scopes.Peek().ContainsKey(name))
            {
                Console.WriteLine($"symbole '{name}' was found in current scope '{CurrentScope}'");
                return scopes.Peek()[name];
            }
            Console.WriteLine($"symbole '{name}' is not exist in current scope '{CurrentScope}'");
            return null;
        }

        public void PrintCurrentScope()
        {
            Console.WriteLine($"=== symbole in scope '{CurrentScope}' ===");
            foreach (Symbol symbol in scopes.Peek().Values)
            {
                Console.WriteLine($"  {symbol.Name} : {symbol.DataType} ({symbol.Type})");
            }
            Console.WriteLine("=======================");
        }

        public void PrintAllScopes()
        {
            Console.WriteLine("=== all scopes and symbols ===");
            int scopeIndex = 0;
            foreach (Dictionary<string, Symbol> scope in scopes)
            {
                string scopeName = scopeNames.ElementAt(scopeIndex);
                Console.WriteLine($"scope: {scopeName}");
                foreach (Symbol symbol in scope.Values)
                {
                    Console.WriteLine($"  {symbol.Name} : {symbol.DataType} ({symbol.Type})");
                }
                scopeIndex++;
            }
            Console.WriteLine("===========================");
        }

        public Symbol LookupGlobal(string name)
        {
            Dictionary<string, Symbol> globalScope = scopes.Last(); // النطاق العالمي هو الأول في المكدس
            return globalScope.ContainsKey(name) ? globalScope[name] : null;
        }
    }
}