// هذا الملف يحتوي على تعريف الرمز (Symbol) المستخدم في جدول الرموز
// كل رمز يمثل تعريفًا في اللغة (متغير، دالة، هيكل، ثابت)

namespace test
{
    public class Symbol
    {
        // اسم العنصر كما يظهر في المصدر
        public string Name { get; set; } 

        // نوع الرمز (مثلاً: "local", "global", "function", "struct", "static", "parameter")
        public string Type { get; set; } 

        // النوع البياناتي: int, double, أو اسم struct
        public string DataType { get; set; }  

        // موضع التعريف في الملف لمساعدة رسائل الخطأ
        public int Line { get; set; }  

        // موقع العمود داخل السطر
        public int Column { get; set; }  

        // النطاق (scope) الذي ينتمي إليه هذا الرمز
        public string Scope { get; set; }  

        // علم يحدد ما إذا كان هذا الرمز عضوًا ثابتًا (static)
        public bool IsStatic { get; set; }  

        // الباني: يهيئ جميع الحقول الأساسية للرمز
        public Symbol(string name, string type, string dataType, int line, int column, string scope, bool isStatic = false)
        {
            Name = name; // تخزين الاسم
            Type = type; // تخزين نوع الرمز
            DataType = dataType; // تخزين نوع البيانات
            Line = line; // سطر التعريف
            Column = column; // عمود التعريف
            Scope = scope; // نطاق التعريف
            IsStatic = isStatic; // علامة الثبات
        }

        // تمثيل نصي مفيد للتصحيح والطباعة
        public override string ToString()
        {
            return $"Symbol(Name={Name}, Type={Type}, DataType={DataType}, Scope={Scope}, Line={Line}, Col={Column}, Static={IsStatic})";
        }
    }
}
