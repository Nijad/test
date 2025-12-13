// جدول الرموز (SymbolTable) يحتفظ بتعريفات الرموز عبر نطاقات متعددة
// إضافة دعم لتخزين هيكل التخطيط (StructLayout) الذي يحتوي على حجم الهيكل وإزاحات الأعضاء

using System.Collections.Generic;
using System.Linq;

namespace test
{
    public class StructLayout
    {
        // الحجم الكلي للهيكل بعد المحاذاة
        public int Size { get; set; }

        // خريطة اسم العضو -> إزاحة داخل الهيكل
        public Dictionary<string, int> MemberOffsets { get; set; } = new Dictionary<string, int>();

        // خريطة اسم العضو -> نوع العضو
        public Dictionary<string, string> MemberTypes { get; set; } = new Dictionary<string, string>();
    }

    public class SymbolTable
    {
        // مكدس من القواميس لتمثيل النطاقات المختلفة
        private Stack<Dictionary<string, Symbol>> scopes
            = new Stack<Dictionary<string, Symbol>>();

        // مكدس لأسماء النطاقات (لأغراض التصحيح)
        private Stack<string> scopeNames
            = new Stack<string>();

        // تخطيطات الهياكل المسجلة
        private Dictionary<string, StructLayout> structLayouts = new Dictionary<string, StructLayout>();

        // اسم النطاق الحالي
        public string CurrentScope
            => scopeNames.Count > 0 ? scopeNames.Peek() : "global";

        public SymbolTable()
        {
            // دخول النطاق العام افتراضياً
            EnterScope("global");
        }

        // إنشاء نطاق جديد (مثل دخول دالة أو كتلة)
        public void EnterScope(string scopeName)
        {
            scopes.Push(new Dictionary<string, Symbol>());
            scopeNames.Push(scopeName);
        }

        // الخروج من النطاق الحالي
        public void ExitScope()
        {
            // لا نخرج من النطاق العالمي
            if (scopes.Count > 1)
            {
                scopeNames.Pop();
                scopes.Pop();
            }
        }

        // إضافة رمز جديد للنطاق الحالي
        public bool AddSymbol(Symbol symbol)
        {
            if (scopes.Peek().ContainsKey(symbol.Name))
                return false; // وجود اسم مكرر في نفس النطاق

            scopes.Peek()[symbol.Name] = symbol;
            return true;
        }

        // البحث عن رمز عبر كل النطاقات (من الأعلى إلى الأدنى)
        public Symbol Lookup(string name)
        {
            foreach (var scope in scopes)
                if (scope.ContainsKey(name))
                    return scope[name];

            return null;
        }

        // البحث عن رمز في النطاق الحالي فقط
        public Symbol LookupInCurrentScope(string name)
        {
            if (scopes.Peek().ContainsKey(name))
                return scopes.Peek()[name];

            return null;
        }

        public void PrintCurrentScope()
        {
            //Console.WriteLine($"=== symbole in scope '{CurrentScope}' ===");
            //foreach (Symbol symbol in scopes.Peek().Values)
            //Console.WriteLine($"  {symbol.Name} : {symbol.DataType} ({symbol.Type})");
            //Console.WriteLine("=======================");
        }

        public void PrintAllScopes()
        {
            //Console.WriteLine("=== all scopes and symbols ===");
            int scopeIndex = 0;
            foreach (Dictionary<string, Symbol> scope in scopes)
            {
                string scopeName = scopeNames.ElementAt(scopeIndex);
                //Console.WriteLine($"scope: {scopeName}");
                foreach (Symbol symbol in scope.Values)
                    //Console.WriteLine($"  {symbol.Name} : {symbol.DataType} ({symbol.Type})");
                    scopeIndex++;
            }
            //Console.WriteLine("===========================");
        }

        public Symbol LookupGlobal(string name)
        {
            Dictionary<string, Symbol> globalScope = scopes.Last(); // النطاق العالمي هو الأول في المكدس
            return globalScope.ContainsKey(name) ? globalScope[name] : null;
        }

        public void PrintAllSymbols()
        {
            //Console.WriteLine("Symbols in the symbol table:");
            //foreach (Dictionary<string, Symbol> scope in scopes)
            //foreach (Symbol symbol in scope.Values)
            //Console.WriteLine($"{symbol.Name} -> {symbol.DataType} ({symbol.Type}) in scope {symbol.Scope}");
        }

        public List<Symbol> GetAllSymbols()
        {
            List<Symbol> allSymbols = new List<Symbol>();
            foreach (Dictionary<string, Symbol> scope in scopes)
                allSymbols.AddRange(scope.Values);

            return allSymbols;
        }

        // إدارة تخطيط الهياكل
        public void AddStructLayout(string structName, StructLayout layout)
        {
            structLayouts[structName] = layout; 
        }

        // استرجاع تخطيط هيكل معين
        public StructLayout GetStructLayout(string structName)
        {
            return structLayouts.ContainsKey(structName) ? structLayouts[structName] : null;
        }

        // التحقق مما إذا كان نوع معين هو هيكل
        public bool IsStructType(string name)
        {
            return structLayouts.ContainsKey(name);
        }

        // استرجاع حجم هيكل معين
        public int GetStructSize(string structName)
        {
            var layout = GetStructLayout(structName);
            return layout != null ? layout.Size : 0;
        }

        // استرجاع إزاحة عضو معين داخل هيكل معين
        public int GetStructMemberOffset(string structName, string memberName)
        {
            var layout = GetStructLayout(structName);
            if (layout != null && layout.MemberOffsets.ContainsKey(memberName))
                return layout.MemberOffsets[memberName];

            return -1;
        }

        // استرجاع نوع عضو معين داخل هيكل معين
        public string GetStructMemberType(string structName, string memberName)
        {
            var layout = GetStructLayout(structName);
            if (layout != null && layout.MemberTypes.ContainsKey(memberName))
                return layout.MemberTypes[memberName];

            return null;
        }
    }
}