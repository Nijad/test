namespace test
{
    public class Symbol
    {
        public string Name { get; set; }
        public string Type { get; set; } // مثل: "variable", "function", "struct", "parameter", إلخ
        public string DataType { get; set; } // مثل: "int", "double", "bool", أو اسم الهيكل
        public int Line { get; set; }
        public int Column { get; set; }
        public string Scope { get; set; }
        public bool IsStatic { get; set; }

        public Symbol(string name, string type, string dataType, int line, int column, string scope, bool isStatic = false)
        {
            Name = name;
            Type = type;
            DataType = dataType;
            Line = line;
            Column = column;
            Scope = scope;
            IsStatic = isStatic;
        }

        public override string ToString()
        {
            return $"{Name} ({Type}) : {DataType} [Scope: {Scope}, Line: {Line}]";
        }
    }
}
