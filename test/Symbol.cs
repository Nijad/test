namespace test
{
    public class Symbol
    {
        public string Name { get; set; } // اسم الرمز
        public string Type { get; set; } // مثل: "variable", "function", "struct", "parameter", إلخ
        public string DataType { get; set; } // مثل: "int", "double", "bool", أو اسم الهيكل
        public int Line { get; set; } // رقم السطر في الكود المصدر
        public int Column { get; set; } // رقم العمود في السطر
        public string Scope { get; set; } // النطاق الذي ينتمي إليه الرمز
        public bool IsStatic { get; set; } // لتحديد ما إذا كان الرمز ثابتًا (static) أم لا

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
            return $"{Name} ({Type}) : {DataType} [Scope: {Scope}, Line: {Line}, Column {Column}]";
        }
    }
}
