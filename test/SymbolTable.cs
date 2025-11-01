using test;

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
        Console.WriteLine($"الدخول إلى النطاق: {scopeName}");
    }

    public void ExitScope()
    {
        if (scopes.Count > 1) // لا نخرج من النطاق العالمي
        {
            string exitedScope = scopeNames.Pop();
            scopes.Pop();
            Console.WriteLine($"الخروج من النطاق: {exitedScope}");
        }
    }

    public bool AddSymbol(Symbol symbol)
    {
        if (scopes.Peek().ContainsKey(symbol.Name))
        {
            Console.WriteLine($"تحذير: الرمز '{symbol.Name}' موجود مسبقاً في النطاق '{CurrentScope}'");
            return false;
        }

        scopes.Peek()[symbol.Name] = symbol;
        Console.WriteLine($"إضافة الرمز: {symbol.Name} من النوع {symbol.DataType} في النطاق {CurrentScope}");
        return true;
    }

    public Symbol Lookup(string name)
    {
        // البحث من الأعلى إلى الأسفل (من أحدث نطاق إلى أقدم نطاق)
        foreach (var scope in scopes)
        {
            if (scope.ContainsKey(name))
            {
                Console.WriteLine($"تم العثور على الرمز '{name}' في النطاق الحالي");
                return scope[name];
            }
        }
        Console.WriteLine($"الرمز '{name}' غير موجود في أي نطاق");
        return null;
    }

    public Symbol LookupInCurrentScope(string name)
    {
        if (scopes.Peek().ContainsKey(name))
        {
            Console.WriteLine($"تم العثور على الرمز '{name}' في النطاق الحالي '{CurrentScope}'");
            return scopes.Peek()[name];
        }
        Console.WriteLine($"الرمز '{name}' غير موجود في النطاق الحالي '{CurrentScope}'");
        return null;
    }

    public void PrintCurrentScope()
    {
        Console.WriteLine($"=== الرموز في النطاق '{CurrentScope}' ===");
        foreach (var symbol in scopes.Peek().Values)
        {
            Console.WriteLine($"  {symbol.Name} : {symbol.DataType} ({symbol.Type})");
        }
        Console.WriteLine("=======================");
    }

    public void PrintAllScopes()
    {
        Console.WriteLine("=== جميع النطاقات والرموز ===");
        int scopeIndex = 0;
        foreach (var scope in scopes)
        {
            string scopeName = scopeNames.ElementAt(scopeIndex);
            Console.WriteLine($"النطاق: {scopeName}");
            foreach (var symbol in scope.Values)
            {
                Console.WriteLine($"  {symbol.Name} : {symbol.DataType} ({symbol.Type})");
            }
            scopeIndex++;
        }
        Console.WriteLine("===========================");
    }
}