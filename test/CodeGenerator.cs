// هذا الملف مسؤول عن مولد الشيفرة (CodeGenerator)
// يزور شجرة التحليل اللغوي (ANTLR parse tree) وينتج شيفرة تجميع (MASM) للبرنامج
// التعليقات باللغة العربية تشرح كيفية توليد البيانات/الكود، التعامل مع المتغيرات المحلية،
// المعاملات، أعضاء الهياكل (static و instance)، وتحويل الأنواع بين int و double.

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System.Text;
using test.Content;

namespace test
{
    public class CodeGenerator : SimpleBaseVisitor<string>
    {
        // عدّاد لتوليد تسميات فريدة داخل الشيفرة (labels)
        private int labelCounter = 0; // مثال: L_skip_0

        // مجمّع السطور الناتج للشيفرة المجمّعة
        private StringBuilder code = new StringBuilder(); // نضع كل سطر من ASM هنا

        // مرجع إلى جدول الرموز المستخدم لاستعلام أنواع المتغيرات والهياكل
        private SymbolTable symbolTable;

        // مجموعة لمراقبة العقد التي تمّت زيارتها لتجنّب الحلقات اللانهائية
        private HashSet<Antlr4.Runtime.ParserRuleContext> visitedNodes;

        // جداول إزاحات المتغيرات المحلية والباراميترات داخل إطار الدالة
        private Dictionary<string, int> localVarOffsets = new Dictionary<string, int>(); // اسم -> إزاحة من EBP
        private Dictionary<string, int> paramOffsets = new Dictionary<string, int>(); // اسم -> إزاحة إلى EBP

        // البداية الافتراضية لإزاحات المتغيرات المحلية والباراميترات
        private int currentLocalOffset = 4; // عادة نبدأ من4 (أو8 عند ضرورة محاذاة double)
        private int currentParamOffset = 8; // معاملات الدالة تبدأ من [ebp+8]

        // حالة الدالة الحالية أثناء التوليد
        private string currentFunctionName = "";
        private string currentFunctionReturnType = "";

        // تسمية ومؤشر لاستخدام معالجات حالات القسمة على صفر
        private const string DivByZeroLabel = "L_div_by_zero";
        private bool usesDivByZeroHandler = false; // إذا تم توليد فحص قسمة على صفر نضع true

        public CodeGenerator(SymbolTable symbolTable)
        {
            // حفظ مرجع جدول الرموز وتهيئة مجموعة الزيارات
            this.symbolTable = symbolTable;
            visitedNodes = new HashSet<Antlr4.Runtime.ParserRuleContext>();
        }

        // نقطة دخول المولد: نمرّر جذور شجرة البرنامج ونسترجع نص ASM كامل
        public string Generate(SimpleParser.ProgramContext tree)
        {
            // رؤوس ملف ASM: نحدد بنية الهدف وإدراج ملفات MASM الضرورية
            code.AppendLine(".386"); // وضع تعليمات المعالج
            code.AppendLine(".model flat, stdcall"); // نموذج الذاكرة وطريقة الاستدعاء
            code.AppendLine("option casemap :none"); // عدم تغيير حالة الحروف تلقائياً
            code.AppendLine();

            // تضمينات MASM الضرورية للوصول إلى دوال ويندوز و masm32
            code.AppendLine("include \\masm32\\include\\windows.inc");
            code.AppendLine("include \\masm32\\include\\kernel32.inc");
            code.AppendLine("include \\masm32\\include\\masm32.inc");
            code.AppendLine("includelib \\masm32\\lib\\kernel32.lib");
            code.AppendLine("includelib \\masm32\\lib\\masm32.lib");
            code.AppendLine();

            // زيارة جذر البرنامج لإنتاج أقسام .data و .code
            VisitProgram(tree);

            // إذا استُخدمت فحوص لقسمة على صفر في أي مكان، نضيف معالج في نهاية الملف
            if (usesDivByZeroHandler)
            {
                code.AppendLine();
                code.AppendLine($"{DivByZeroLabel}:"); // عنوان المعالج
                // إنهاء البرنامج بحالة1 للإشارة لحدوث قسمة على صفر
                code.AppendLine("invoke ExitProcess,1");
            }

            // إرجاع نص الشيفرة المجمّعة
            return code.ToString();
        }

        // SafeVisit: يضيف حماية ضد الزيارات المتكررة لنفس العقدة في الشجرة
        private string SafeVisit(Antlr4.Runtime.ParserRuleContext context)
        {
            if (context == null)
                return ""; // لا شيء لنفعله إن لم توجد عقدة
            if (visitedNodes.Contains(context))
                return ""; // تجنّب الزيارة المتكررة

            visitedNodes.Add(context); // علامة دخول
            try
            {
                return Visit(context); // تنفيذ الزيارة الفعلية
            }
            finally
            {
                visitedNodes.Remove(context); // إزالة علامة عند الانتهاء
            }
        }

        // زيارة جذر البرنامج: توليد أقسام البيانات والكود وتنظيم نقطة البداية
        public override string VisitProgram(SimpleParser.ProgramContext context)
        {
            string programName = context.IDENTIFIER().GetText(); // اسم البرنامج من الشجرة

            // بداية قسم البيانات
            code.AppendLine(".data");
            foreach (SimpleParser.MemberContext member in context.member())
            {
                if (member.global() != null)
                    SafeVisit(member.global()); // تعريف المتغيرات العامة
                if (member.@struct() != null)
                    SafeVisit(member.@struct()); // تعريف أعضاء static داخل .data
            }

            code.AppendLine();

            // بداية قسم الكود
            code.AppendLine(".code");

            // تعريف العلامة start التي تستدعي main ثم تغلق البرنامج
            code.AppendLine("start:");
            code.AppendLine("call main"); // استدعاء الدالة الرئيسية
            code.AppendLine("invoke ExitProcess, 0"); // خروج نظيف بعد انتهاء main
            code.AppendLine();

            // توليد تعريفات الدوال بعد قسم البيانات
            foreach (SimpleParser.MemberContext member in context.member())
            {
                if (member.function() != null)
                    SafeVisit(member.function()); // زيارة كل تعريف دالة
            }

            // توجيه النهاية للمجمّع
            code.AppendLine("end start");
            return ""; // لا نحتاج لإرجاع قيمة
        }

        // ----------------- دفعة2: VisitFunction و حساب الإزاحات -----------------
        // VisitFunction: توليد إطار الدالة، معالجة المعاملات، حجز المتغيرات المحلية، وتوليد جسم الدالة
        public override string VisitFunction(SimpleParser.FunctionContext context)
        {
            // الحصول على اسم الدالة ونوع الإرجاع
            string functionName = context.IDENTIFIER().GetText();
            currentFunctionName = functionName;
            currentFunctionReturnType = context.type()?.GetText() ?? "void";

            // إعادة تهيئة القواميس والإزاحات لكل دالة
            localVarOffsets.Clear();
            paramOffsets.Clear();
            currentLocalOffset = DetermineInitialLocalOffset(context); // نقرر بداية الإزاحة اعتمادًا على وجود double
            currentParamOffset = 8; // المعاملات تبدأ عند ebp+8

            // إضافة الدالة إلى جدول الرموز (لتعرفها المراحل التالية)
            Symbol functionSymbol = new Symbol(
                functionName,
                "function",
                currentFunctionReturnType,
                context.Start.Line,
                context.Start.Column,
                "global"
            );
            symbolTable.AddSymbol(functionSymbol);

            // توليد مدخل الدالة وإعداد الإطار
            code.AppendLine();
            code.AppendLine($"{functionName} PROC");
            code.AppendLine("push ebp"); // حفظ ebp السابق
            code.AppendLine("mov ebp, esp"); // إنشاء إطار جديد: ebp = esp

            // معالجة تعريف المعاملات: تملأ paramOffsets وتضيف الرموز إلى SymbolTable
            if (context.arguments() != null)
            {
                // زيارة قائمة المعاملات ستدير الإزاحات وإضافة الرموز
                SafeVisit(context.arguments());
            }

            // حساب المساحة اللازمة للمتغيرات المحلية (مع محاذاة مناسبة)
            int localVarsSize = CalculateLocalVariablesSize(context);
            if (localVarsSize > 0)
            {
                // حجز المساحة على الستاك للمتغيرات المحلية
                code.AppendLine($"sub esp, {localVarsSize}");
            }

            // توليد تعليمات جسم الدالة (كل جملة داخل الدالة)
            foreach (SimpleParser.StatementContext stmt in context.statement())
            {
                SafeVisit(stmt);
            }

            // إذا لا يوجد return صريح، نولد عودة افتراضية تتناسب مع نوع الدالة
            bool hasReturn = false;
            foreach (var stmt in context.statement())
            {
                if (stmt.RETURN() != null) { hasReturn = true; break; }
            }

            if (!hasReturn)
            {
                if (currentFunctionReturnType == "double")
                {
                    code.AppendLine("fldz"); // تحميل0.0 على FPU stack كقيمة مرجعية
                }
                else if (currentFunctionReturnType != "void")
                {
                    code.AppendLine("mov eax,0"); // إرجاع0 في eax
                }
                // استعادة الإطار والرجوع
                code.AppendLine("mov esp, ebp");
                code.AppendLine("pop ebp");
                code.AppendLine("ret");
            }

            code.AppendLine($"{functionName} ENDP");
            currentFunctionName = "";
            currentFunctionReturnType = "";
            return "";
        }

        // DetermineInitialLocalOffset: نبدأ من8 إذا وُجد double لضمان محاذاة8 بايت
        private int DetermineInitialLocalOffset(SimpleParser.FunctionContext context)
        {
            foreach (var stmt in context.statement())
            {
                if (stmt.type() != null && stmt.variables() != null)
                {
                    string varType = stmt.type().GetText();
                    if (varType.ToLower() == "double")
                    {
                        return 8; // ابدأ من 8 إذا كان هناك متغير double
                    }
                }
            }
            return 4; // وإلا ابدأ من 4
        }

        // CalculateLocalVariablesSize: يحسب الحجم الكلي للمتغيرات المحلية مع الحشو (padding) للمحاذاة
        private int CalculateLocalVariablesSize(SimpleParser.FunctionContext context)
        {
            int size = 0; // إجمالي الحجم

            foreach (var stmt in context.statement())
            {
                if (stmt.type() != null && stmt.variables() != null)
                {
                    string varType = stmt.type().GetText();
                    int varSize = GetTypeSize(varType); // حجم النوع بالبايت

                    foreach (var variable in stmt.variables().variable())
                    {
                        string varName = variable.IDENTIFIER().GetText();

                        // الإزاحة المؤقتة تعتمد على البداية الحالية + الحجم الحالي
                        int offset = currentLocalOffset + size;

                        // إذا كان النوع double، نحتاج لمحاذاة8 بايت
                        if (varSize == 8 && offset % 8 != 0)
                        {
                            int padding = 8 - (offset % 8);
                            size += padding; // نضيف حشو للمحاذاة
                            offset += padding;
                        }
                        else if (varSize == 4 && offset % 4 != 0)
                        {
                            int padding = 4 - (offset % 4);
                            size += padding;
                            offset += padding;
                        }

                        // تسجيل الإزاحة النهائية لهذا المتغير
                        localVarOffsets[varName] = offset;
                        size += varSize; // إضافة حجم المتغير للإجمالي
                    }
                }
            }

            return size; // قيمة تستخدم في sub esp, size
        }

        // GetTypeSize: يرجع حجم النوع (بايت)
        private int GetTypeSize(string type)
        {
            if (string.IsNullOrEmpty(type)) return 4;
            string t = type.ToLower();
            // handle primitive types
            switch (t)
            {
                case "int": return 4; // int =4 بايت
                case "double": return 8; // double =8 بايت
                case "bool": return 1; // bool =1 بايت
            }

            // if it's a struct type, ask symbolTable for size
            if (symbolTable != null && symbolTable.IsStructType(type))
            {
                int s = symbolTable.GetStructSize(type);
                if (s > 0) return s;
            }

            return 4; // افتراضياً4 بايت
        }

        // زيارة قائمة المعاملات: تزور كل براميتر على حدة
        public override string VisitArguments(SimpleParser.ArgumentsContext context)
        {
            foreach (SimpleParser.ArgumentContext arg in context.argument())
            {
                SafeVisit(arg); // VisitArgument سيعالج كل براميتر ويحدد offset
            }
            return "";
        }

        // VisitArgument: يحدد إزاحة البراميتر ويضيفه إلى جدول الرموز
        public override string VisitArgument(SimpleParser.ArgumentContext context)
        {
            string argName = context.IDENTIFIER().GetText(); // اسم البراميتر
            string type = context.type().GetText(); // نوع البراميتر
            int typeSize = GetTypeSize(type); // حجم النوع

            // For struct parameters we pass pointer (4 bytes)
            int paramStackOffset = currentParamOffset;
            if (symbolTable.IsStructType(type))
            {
                // store as pointer
                paramOffsets[argName] = paramStackOffset;
                currentParamOffset += 4;
            }
            else
            {
                paramOffsets[argName] = paramStackOffset;
                currentParamOffset += typeSize; // تحديث الإزاحة للبراميتر التالي
            }

            // إضافة البراميتر إلى جدول الرموز حتى يكون متاحًا داخل جسم الدالة
            Symbol paramSymbol = new Symbol(
                argName,
                "parameter",
                type,
                context.Start.Line,
                context.Start.Column,
                currentFunctionName
            );

            // Actually add to symbol table so lookups succeed
            if (!symbolTable.AddSymbol(paramSymbol))
            {
                // duplicate name in scope - ignore for now
            }

            // ندرج تعليقًا في شيفرة ASM يوضح مكان البراميتر
            code.AppendLine($"; Parameter: {argName} at [ebp+{paramStackOffset}]");
            return "";
        }

        public override string VisitStatement(SimpleParser.StatementContext context)
        {
            if (context.IF() != null)
                return GenerateIfStatement(context);
            if (context.WHILE() != null)
                return GenerateWhileStatement(context);
            if (context.FOR() != null)
                return GenerateForStatement(context);
            if (context.RETURN() != null)
                return GenerateReturnStatement(context);
            if (context.expression() != null && context.expression().Length > 0)
                return GenerateExpressionStatement(context);
            if (context.LBRACE() != null)
            {
                foreach (SimpleParser.StatementContext stmt in context.statement())
                    SafeVisit(stmt);
                return "";
            }
            if (context.type() != null && context.variables() != null)
            {
                SafeVisit(context.variables());
                return "";
            }
            return "";
        }

        private string GenerateReturnStatement(SimpleParser.StatementContext context)
        {
            if (context.expression() != null && context.expression().Length > 0)
            {
                string resultReg = SafeVisit(context.expression(0));

                // إذا كانت الدالة ترجع double والنتيجة في st(0)، نتركها هناك
                if (currentFunctionReturnType == "double" && resultReg == "eax")
                {
                    // إذا كانت النتيجة في eax ولكن الدالة ترجع double، نحتاج لتحويلها
                    code.AppendLine("push eax");
                    code.AppendLine("fild dword ptr [esp]");
                    code.AppendLine("add esp, 4");
                }
                // إذا كانت الدالة ترجع int والنتيجة في st(0)، نحتاج لتحويلها
                else if (currentFunctionReturnType != "double" && resultReg == "st(0)")
                {
                    code.AppendLine("sub esp, 8");
                    code.AppendLine("fstp qword ptr [esp]");
                    code.AppendLine("fistp dword ptr [esp]");
                    code.AppendLine("mov eax, [esp]");
                    code.AppendLine("add esp, 8");
                }
            }
            else if (currentFunctionReturnType == "double")
            {
                // إرجاع قيمة افتراضية للدوال التي ترجع double
                code.AppendLine("fldz");
            }

            code.AppendLine("mov esp, ebp");
            code.AppendLine("pop ebp");
            code.AppendLine("ret");
            return "";
        }

        private string GenerateExpressionStatement(SimpleParser.StatementContext context)
        {
            if (context.expression().Length > 0)
                SafeVisit(context.expression(0));
            return "";
        }

        public override string VisitExpression(SimpleParser.ExpressionContext context)
        {
            code.AppendLine($"; Expression: {context.GetText()}");

            // ✅ التصحيح: معالجة التعيينات أولاً
            if (context.ASSIGN() != null && context.expression().Length >= 1)
            {
                if (context.IDENTIFIER() != null && context.expression().Length == 1)
                {
                    return GenerateSimpleAssignment(context);
                }
                else if (context.expression(0).IDENTIFIER() != null && context.expression().Length == 2)
                {
                    return GenerateAssignment(context);
                }
            }

            // ✅ التصحيح: معالجة المقارنات في الشروط
            if (context.binaryOp() != null && context.expression().Length == 2)
            {
                string op = context.binaryOp().GetText();
                if (op == ">" || op == "<" || op == ">=" || op == "<=" || op == "==" || op == "!=")
                {
                    return GenerateComparison(context, op);
                }
                else
                {
                    return GenerateBinaryOperation(context);
                }
            }

            if (context.INTEGER() != null)
            {
                code.AppendLine($"mov eax, {context.INTEGER().GetText()}");
                return "eax";
            }
            else if (context.REAL() != null)
            {
                // معالجة الأعداد الحقيقية كـ double
                string realValue = context.REAL().GetText();
                code.AppendLine($"push __float64__({realValue})");
                code.AppendLine($"fld qword ptr [esp]");
                code.AppendLine($"add esp, 8");

                return "st(0)";
            }
            else if (context.IDENTIFIER() != null)
            {
                // معالجة الوصول إلى أعضاء الهياكل الثابتة
                if (context.DOT() != null && context.expression().Length == 1)
                {
                    string baseExpr = context.expression(0).GetText();
                    string memberName = context.IDENTIFIER().GetText();
                    return GenerateStructMemberAccess(baseExpr, memberName);
                }
                else if (context.LPAREN() != null)
                {
                    return GenerateFunctionCall(context);
                }
                else if (context.expression().Length == 0)
                {
                    string varName = context.IDENTIFIER().GetText();
                    return GenerateVariableLoad(varName);
                }
            }
            else if (context.ASSIGN() != null && context.expression().Length >= 1)
            {
                if (context.expression(0).IDENTIFIER() != null && context.expression().Length == 2)
                {
                    return GenerateAssignment(context);
                }
                else if (context.IDENTIFIER() != null && context.expression().Length == 1)
                {
                    return GenerateSimpleAssignment(context);
                }
            }
            else if (context.binaryOp() != null && context.expression().Length == 2)
            {
                return GenerateBinaryOperation(context);
            }
            else if (context.expression().Length == 1)
            {
                return SafeVisit(context.expression(0));
            }
            else if (context.DOT() != null && context.expression().Length == 1)
            {
                string baseExpr = context.expression(0).GetText();
                string memberName = context.IDENTIFIER().GetText();
                return GenerateStructMemberAccess(baseExpr, memberName);
            }

            code.AppendLine("; Unknown expression type - using default");
            code.AppendLine("mov eax, 0");
            return "eax";
        }

        private string GenerateComparison(SimpleParser.ExpressionContext context, string op)
        {
            code.AppendLine($"; Comparison: {op}");

            // Evaluate left, push it, evaluate right, pop left into ebx -> right in eax
            SafeVisit(context.expression(0));
            code.AppendLine("push eax");
            SafeVisit(context.expression(1));
            code.AppendLine("pop ebx");

            // Now ebx = left, eax = right
            code.AppendLine("cmp ebx, eax");

            // إرجاع النتيجة كمبلغ منطقي (1 أو 0)
            switch (op)
            {
                case ">":
                    code.AppendLine("setg al");
                    break;
                case "<":
                    code.AppendLine("setl al");
                    break;
                case ">=":
                    code.AppendLine("setge al");
                    break;
                case "<=":
                    code.AppendLine("setle al");
                    break;
                case "==":
                    code.AppendLine("sete al");
                    break;
                case "!=":
                    code.AppendLine("setne al");
                    break;
            }

            code.AppendLine("movzx eax, al");
            return "eax ";
        }

        private string GenerateVariableLoad(string varName)
        {
            code.AppendLine($"; Load variable: {varName}");

            var symbol = symbolTable.Lookup(varName);
            string varType = symbol?.DataType?.ToLower();

            // إذا لم يكن النوع معروفاً، حاول تحديده من السياق
            if (varType == null)
            {
                varType = "int"; // افتراضي
                //Console.WriteLine($"[WARNING] Unknown type for variable '{varName}', assuming 'int'");
            }

            // البحث في الباراميترات أولاً
            if (paramOffsets.ContainsKey(varName))
            {
                int offset = paramOffsets[varName];
                if (varType == "double")
                {
                    code.AppendLine($"fld qword ptr [ebp+{offset}]");
                    return "st(0)";
                }
                else
                {
                    code.AppendLine($"mov eax, [ebp+{offset}]");
                    return "eax";
                }
            }
            // البحث في المتغيرات المحلية
            else if (localVarOffsets.ContainsKey(varName))
            {
                int offset = localVarOffsets[varName];
                if (varType == "double")
                {
                    code.AppendLine($"fld qword ptr [ebp-{offset}]");
                    return "st(0)";
                }
                else
                {
                    code.AppendLine($"mov eax, [ebp-{offset}]");
                    return "eax";
                }
            }
            // متغير عام
            else
            {
                if (varType == "double")
                {
                    code.AppendLine($"fld {varName}");
                    return "st(0)";
                }
                else
                {
                    code.AppendLine($"mov eax, {varName}");
                    return "eax";
                }
            }
        }

        private string GenerateSimpleAssignment(SimpleParser.ExpressionContext context)
        {
            string varName = context.IDENTIFIER().GetText();

            code.AppendLine($"; Simple assignment: {varName} = ...");

            // معالجة الطرف الأيمن من التعيين
            string resultReg = SafeVisit(context.expression(0));

            // تخزين النتيجة في المتغير
            if (paramOffsets.ContainsKey(varName))
            {
                int offset = paramOffsets[varName];
                var symbol = symbolTable.Lookup(varName);
                if (symbol?.DataType?.ToLower() == "double" && resultReg == "st(0)")
                {
                    code.AppendLine($"fstp qword ptr [ebp+{offset}]");
                }
                else
                {
                    code.AppendLine($"mov [ebp+{offset}], eax");
                }
            }
            else if (localVarOffsets.ContainsKey(varName))
            {
                int offset = localVarOffsets[varName];
                var symbol = symbolTable.Lookup(varName);
                if (symbol?.DataType?.ToLower() == "double" && resultReg == "st(0)")
                {
                    code.AppendLine($"fstp qword ptr [ebp-{offset}]");
                }
                else
                {
                    code.AppendLine($"mov [ebp-{offset}], eax");
                }
            }
            else
            {
                // متغير عام
                var symbol = symbolTable.Lookup(varName);
                if (symbol?.DataType?.ToLower() == "double" && resultReg == "st(0)")
                {
                    code.AppendLine($"fstp {varName}");
                }
                else
                {
                    code.AppendLine($"mov {varName}, eax");
                }
            }

            return resultReg;
        }

        private string GenerateFunctionCall(SimpleParser.ExpressionContext context)
        {
            string functionName = context.IDENTIFIER().GetText();

            code.AppendLine($"; Call function: {functionName}");

            // معالجة المعاملات
            int totalParamSize = 0;

            if (context.expr_list() != null)
            {
                foreach (var expr in context.expr_list().expression())
                {
                    // special-case: passing struct variable by address/value
                    if (expr.IDENTIFIER() != null && expr.expression().Length == 0)
                    {
                        string name = expr.IDENTIFIER().GetText();
                        var sym = symbolTable.Lookup(name);
                        if (sym != null && symbolTable.IsStructType(sym.DataType))
                        {
                            // pass address of struct variable
                            if (localVarOffsets.ContainsKey(name))
                            {
                                int off = localVarOffsets[name];
                                code.AppendLine($"lea eax, [ebp-{off}]");
                                code.AppendLine("push eax");
                            }
                            else if (paramOffsets.ContainsKey(name))
                            {
                                int poff = paramOffsets[name];
                                // parameter that represents pointer already: push dword ptr [ebp+poff]
                                code.AppendLine($"mov eax, [ebp+{poff}]");
                                code.AppendLine("push eax");
                            }
                            else
                            {
                                // global
                                code.AppendLine($"push OFFSET {name}");
                            }
                            totalParamSize += 4;
                            continue;
                        }
                    }

                    string resultReg = SafeVisit(expr);

                    if (resultReg == "st(0)")
                    {
                        // معامل عائم: نخزنه في المكدس كـ double (8 بايت)
                        code.AppendLine("sub esp,8");
                        code.AppendLine("fstp qword ptr [esp]");
                        totalParamSize += 8;
                    }
                    else
                    {
                        // معامل صحيح
                        code.AppendLine("push eax");
                        totalParamSize += 4;
                    }
                }
            }

            code.AppendLine($"call {functionName}");

            // تنظيف المكدس
            if (totalParamSize > 0)
            {
                code.AppendLine($"add esp, {totalParamSize}");
            }

            // تحديد نوع الإرجاع بناءً على الدالة
            var funcSymbol = symbolTable.Lookup(functionName);
            if (funcSymbol?.DataType?.ToLower() == "double")
            {
                return "st(0)";
            }
            else
            {
                return "eax";
            }
        }

        private string GenerateStructMemberAccess(string baseExpr, string memberName)
        {
            code.AppendLine($"; Struct member access: {baseExpr}.{memberName}");

            // If baseExpr is a struct type name or unknown, treat as static member
            var baseSymbol = symbolTable.Lookup(baseExpr);
            if (baseSymbol == null || baseSymbol.Type == "struct")
            {
                var memberSymbol = symbolTable.Lookup(memberName);
                if (memberSymbol != null && memberSymbol.DataType?.ToLower() == "double")
                {
                    code.AppendLine($"fld {memberName}");
                    return "st(0)";
                }
                else
                {
                    code.AppendLine($"mov eax, {memberName}");
                    return "eax";
                }
            }

            // Instance member access: compute field offset within struct
            string baseType = baseSymbol.DataType;
            if (!symbolTable.IsStructType(baseType))
            {
                code.AppendLine($"; Base {baseExpr} is not a struct (type={baseType})");
                code.AppendLine("mov eax,0");
                return "eax";
            }

            int fieldOffset = symbolTable.GetStructMemberOffset(baseType, memberName);
            string memberType = symbolTable.GetStructMemberType(baseType, memberName);

            bool baseIsLocal = localVarOffsets.ContainsKey(baseExpr);
            if (baseIsLocal)
            {
                int baseOffset = localVarOffsets[baseExpr];
                if (memberType != null && memberType.ToLower() == "double")
                {
                    code.AppendLine($"fld qword ptr [ebp-{baseOffset + fieldOffset}]");
                    return "st(0)";
                }
                else
                {
                    code.AppendLine($"mov eax, [ebp-{baseOffset + fieldOffset}]");
                    return "eax";
                }
            }

            if (paramOffsets.ContainsKey(baseExpr))
            {
                int p = paramOffsets[baseExpr];
                // parameter stored as pointer to struct on stack
                // load address then load member
                code.AppendLine($"mov esi, [ebp+{p}]");
                if (memberType != null && memberType.ToLower() == "double")
                {
                    code.AppendLine($"fld qword ptr [esi+{fieldOffset}]");
                    return "st(0)";
                }
                else
                {
                    code.AppendLine($"mov eax, [esi+{fieldOffset}]");
                    return "eax";
                }
            }

            // Global struct variable (address known)
            if (memberType != null && memberType.ToLower() == "double")
            {
                code.AppendLine($"fld qword ptr [{baseExpr} + {fieldOffset}]");
                return "st(0)";
            }
            else
            {
                code.AppendLine($"mov eax, [{baseExpr} + {fieldOffset}]");
                return "eax";
            }
        }

        private string GenerateAssignment(SimpleParser.ExpressionContext context)
        {
            // Support member assignment like a.x = expr
            if (context.expression(0) != null && context.expression(0).DOT() != null && context.expression(0).expression().Length == 1 && context.expression(0).expression(0).IDENTIFIER() != null)
            {
                string baseName = context.expression(0).expression(0).IDENTIFIER().GetText();
                string memberName = context.expression(0).IDENTIFIER().GetText();
                code.AppendLine($"; Member assignment: {baseName}.{memberName} = ...");

                string resultReg = SafeVisit(context.expression(1));

                // If baseName is actually a struct type name, this is a static member access
                var baseSymbol = symbolTable.Lookup(baseName);
                if (baseSymbol == null || baseSymbol.Type == "struct")
                {
                    var memberSymbol = symbolTable.Lookup(memberName);
                    if (memberSymbol != null && memberSymbol.DataType?.ToLower() == "double")
                    {
                        if (resultReg == "st(0)") code.AppendLine($"fstp {memberName}");
                        else
                        {
                            code.AppendLine("push eax");
                            code.AppendLine("fild dword ptr [esp]");
                            code.AppendLine($"fstp {memberName}");
                            code.AppendLine("add esp,4");
                        }
                    }
                    else if (memberSymbol != null) code.AppendLine($"mov {memberName}, eax");
                    return resultReg;
                }

                // Otherwise base is a variable (instance member)
                string structName = baseSymbol.DataType;
                int fieldOffset = symbolTable.GetStructMemberOffset(structName, memberName);
                string memberType = symbolTable.GetStructMemberType(structName, memberName);

                bool baseIsLocal = localVarOffsets.ContainsKey(baseName);
                int baseOffset = baseIsLocal ? localVarOffsets[baseName] : (paramOffsets.ContainsKey(baseName) ? paramOffsets[baseName] : 0);

                if (memberType != null && memberType.ToLower() == "double")
                {
                    if (baseIsLocal)
                        code.AppendLine($"fstp qword ptr [ebp-{baseOffset + fieldOffset}]");
                    else if (paramOffsets.ContainsKey(baseName))
                    {
                        // param is pointer
                        int p = baseOffset;
                        code.AppendLine($"mov esi, [ebp+{p}]");
                        code.AppendLine($"fstp qword ptr [esi+{fieldOffset}]");
                    }
                    else
                        code.AppendLine($"fstp [{baseName} + {fieldOffset}]");
                }
                else
                {
                    if (baseIsLocal)
                        code.AppendLine($"mov [ebp-{baseOffset + fieldOffset}], eax");
                    else if (paramOffsets.ContainsKey(baseName))
                    {
                        int p = baseOffset;
                        code.AppendLine($"mov esi, [ebp+{p}]");
                        code.AppendLine($"mov [esi+{fieldOffset}], eax");
                    }
                    else
                        code.AppendLine($"mov [{baseName} + {fieldOffset}], eax");
                }

                return resultReg;
            }

            string varName = context.expression(0).IDENTIFIER().GetText();

            code.AppendLine($"; Complex assignment: {varName} = ...");

            // معالجة الطرف الأيمن
            string rhsReg = SafeVisit(context.expression(1));

            // تخزين النتيجة في المتغير
            if (paramOffsets.ContainsKey(varName))
            {
                int offset = paramOffsets[varName];
                var symbol = symbolTable.Lookup(varName);
                if (symbol?.DataType?.ToLower() == "double" && rhsReg == "st(0)")
                {
                    code.AppendLine($"fstp qword ptr [ebp+{offset}]");
                }
                else
                {
                    code.AppendLine($"mov [ebp+{offset}], eax");
                }
            }
            else if (localVarOffsets.ContainsKey(varName))
            {
                int offset = localVarOffsets[varName];
                var symbol = symbolTable.Lookup(varName);
                if (symbol?.DataType?.ToLower() == "double" && rhsReg == "st(0)")
                {
                    code.AppendLine($"fstp qword ptr [ebp-{offset}]");
                }
                else
                {
                    code.AppendLine($"mov [ebp-{offset}], eax");
                }
            }
            else
            {
                // متغير عام
                var symbol = symbolTable.Lookup(varName);
                if (symbol?.DataType?.ToLower() == "double" && rhsReg == "st(0)")
                {
                    code.AppendLine($"fstp {varName}");
                }
                else
                {
                    code.AppendLine($"mov {varName}, eax");
                }
            }

            return rhsReg;
        }

        private string GenerateBinaryOperation(SimpleParser.ExpressionContext context, string op)
        {
            string opSymbol = context.binaryOp().GetText();
            return GenerateBinaryOperation(context);
        }

        private string GenerateBinaryOperation(SimpleParser.ExpressionContext context)
        {
            string op = context.binaryOp().GetText();

            // تحديد أنواع المعاملات
            bool hasFloatingPoint = HasFloatingPoint(context.expression(0)) || HasFloatingPoint(context.expression(1));

            if (hasFloatingPoint)
            {
                return GenerateFloatingPointOperation(context, op);
            }
            else
            {
                return GenerateIntegerOperation(context, op);
            }
        }

        private bool HasFloatingPoint(SimpleParser.ExpressionContext context)
        {
            if (context == null) return false;

            // كشف الأعداد الحقيقية المباشرة
            if (context.REAL() != null)
            {
                //Console.WriteLine($"[DEBUG] Found REAL: {context.REAL().GetText()}");
                return true;
            }

            // كشف المعرفات البسيطة (متغيرات)
            if (context.IDENTIFIER() != null && context.expression().Length == 0 && context.LPAREN() == null)
            {
                string varName = context.IDENTIFIER().GetText();
                var symbol = symbolTable.Lookup(varName);
                bool isDouble = symbol?.DataType?.ToLower() == "double";
                //Console.WriteLine($"[DEBUG] Identifier '{varName}': symbol={symbol?.Name}, type={symbol?.DataType}, isDouble={isDouble}");
                return isDouble;
            }

            // كشف الوصول إلى أعضاء الهياكل - هذا هو الجزء الأهم!
            if (context.DOT() != null && context.expression().Length == 1 && context.IDENTIFIER() != null)
            {
                string memberName = context.IDENTIFIER().GetText();
                var symbol = symbolTable.Lookup(memberName);
                bool isDouble = symbol?.DataType?.ToLower() == "double";
                //Console.WriteLine($"[DEBUG] Struct member '{memberName}': symbol={symbol?.Name}, type={symbol?.DataType}, isDouble={isDouble}");
                return isDouble;
            }

            // كشف استدعاءات الدوال التي ترجع double
            if (context.IDENTIFIER() != null && context.LPAREN() != null)
            {
                string functionName = context.IDENTIFIER().GetText();
                var symbol = symbolTable.Lookup(functionName);
                bool isDouble = symbol?.DataType?.ToLower() == "double";
                //Console.WriteLine($"[DEBUG] Function call '{functionName}': returns {symbol?.DataType}, isDouble={isDouble}");
                return isDouble;
            }

            // التعبيرات الثنائية - تحقق من كلا الطرفين
            if (context.binaryOp() != null && context.expression().Length == 2)
            {
                bool leftHasFP = HasFloatingPoint(context.expression(0));
                bool rightHasFP = HasFloatingPoint(context.expression(1));
                bool result = leftHasFP || rightHasFP;
                //Console.WriteLine($"[DEBUG] Binary operation: leftHasFP={leftHasFP}, rightHasFP={rightHasFP}, result={result}");
                return result;
            }

            // التعبيرات الأحادية - تحقق من التعبير الداخلي
            if (context.expression().Length == 1 && context.ASSIGN() == null)
            {
                bool hasFP = HasFloatingPoint(context.expression(0));
                //Console.WriteLine($"[DEBUG] Unary expression: hasFP={hasFP}");
                return hasFP;
            }

            // التعيينات - تحقق من الطرف الأيمن
            if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                bool hasFP = HasFloatingPoint(context.expression(1));
                //Console.WriteLine($"[DEBUG] Assignment: hasFP={hasFP}");
                return hasFP;
            }

            //Console.WriteLine($"[DEBUG] Unknown expression type: {context.GetText()}");
            return false;
        }

        private string GenerateIntegerOperation(SimpleParser.ExpressionContext context, string op)
        {
            code.AppendLine($"; Integer operation: {op}");

            // Evaluate left into eax, push it
            SafeVisit(context.expression(0));
            code.AppendLine("push eax");

            // Evaluate right into eax
            SafeVisit(context.expression(1));

            // Pop left into ebx, right already in eax
            code.AppendLine("pop ebx");

            switch (op)
            {
                case "+":
                    // eax = right + left (commutative)
                    code.AppendLine("add eax, ebx");
                    break;
                case "-":
                    // want eax = left - right
                    code.AppendLine("mov ecx, ebx");
                    code.AppendLine("sub ecx, eax");
                    code.AppendLine("mov eax, ecx");
                    break;
                case "*":
                    // imul eax, ebx => eax = eax * ebx (right * left)
                    code.AppendLine("imul eax, ebx");
                    break;
                case "/":
                    // need eax = left / right
                    // currently: ebx = left, eax = right
                    // check division by zero (right) -> ebx is left, eax is right, so check eax
                    code.AppendLine("cmp eax, 0");
                    code.AppendLine($"je {DivByZeroLabel}");
                    code.AppendLine("xchg eax, ebx"); // now eax = left, ebx = right
                    code.AppendLine("cdq");
                    code.AppendLine("idiv ebx");
                    usesDivByZeroHandler = true;
                    break;
                case "%":
                    // modulus: left % right
                    code.AppendLine("cmp eax,0");
                    code.AppendLine($"je {DivByZeroLabel}");
                    code.AppendLine("xchg eax, ebx");
                    code.AppendLine("cdq");
                    code.AppendLine("idiv ebx");
                    code.AppendLine("mov eax, edx");
                    usesDivByZeroHandler = true;
                    break;
                case "==":
                    code.AppendLine("cmp ebx, eax");
                    code.AppendLine("sete al");
                    code.AppendLine("movzx eax, al");
                    break;
                case "!=":
                    code.AppendLine("cmp ebx, eax");
                    code.AppendLine("setne al");
                    code.AppendLine("movzx eax, al");
                    break;
                case "<":
                    code.AppendLine("cmp ebx, eax");
                    code.AppendLine("setl al");
                    code.AppendLine("movzx eax, al");
                    break;
                case ">":
                    code.AppendLine("cmp ebx, eax");
                    code.AppendLine("setg al");
                    code.AppendLine("movzx eax, al");
                    break;
                case "<=":
                    code.AppendLine("cmp ebx, eax");
                    code.AppendLine("setle al");
                    code.AppendLine("movzx eax, al");
                    break;
                case ">=":
                    code.AppendLine("cmp ebx, eax");
                    code.AppendLine("setge al");
                    code.AppendLine("movzx eax, al");
                    break;
                default:
                    code.AppendLine("; Unknown integer operation");
                    code.AppendLine("mov eax, 0");
                    break;
            }

            return "eax";
        }

        private string GenerateFloatingPointOperation(SimpleParser.ExpressionContext context, string op)
        {
            code.AppendLine($"; Floating point operation: {op}");

            // معالجة الطرف الأيسر
            string leftReg = SafeVisit(context.expression(0));

            // ✅ إذا كانت النتيجة في eax ولكن يجب أن تكون عائمة، قم بالتحويل
            if (leftReg == "eax")
            {
                code.AppendLine("push eax");
                code.AppendLine("fild dword ptr [esp]");
                code.AppendLine("add esp, 4");
                leftReg = "st(0)";
            }

            // معالجة الطرف الأيمن  
            string rightReg = SafeVisit(context.expression(1));

            // ✅ نفس التحويل للطرف الأيمن
            if (rightReg == "eax")
            {
                code.AppendLine("push eax");
                code.AppendLine("fild dword ptr [esp]");
                code.AppendLine("add esp, 4");
                rightReg = "st(0)";
            }

            // إذا كانت القيمتان في st(0)، نحتاج لتبديل المواقع
            if (leftReg == "st(0)" && rightReg == "st(0)")
            {
                // هذا لا يمكن أن يحدث عادةً، ولكن للسلامة
                code.AppendLine("fxch st(1)");
            }

            // إجراء العملية العائمة
            switch (op)
            {
                case "*":
                    code.AppendLine("fmulp st(1), st(0)");
                    break;
                case "+":
                    code.AppendLine("faddp st(1), st(0)");
                    break;
                case "-":
                    code.AppendLine("fsubp st(1), st(0)");
                    break;
                case "/":
                    // Note: runtime division-by-zero check for floating point is not trivial here; skip
                    code.AppendLine("fdivp st(1), st(0)");
                    break;
                default:
                    code.AppendLine($"; Unsupported floating point operation: {op}");
                    code.AppendLine("fstp st(0)");
                    code.AppendLine("fstp st(0)");
                    code.AppendLine("mov eax, 0");
                    return "eax";
            }

            return "st(0)";
        }

        public override string VisitExpr_list(SimpleParser.Expr_listContext context)
        {
            foreach (var expr in context.expression())
            {
                SafeVisit(expr);
            }
            return "";
        }

        public override string VisitGlobal(SimpleParser.GlobalContext context)
        {
            string type = context.type().GetText();
            foreach (SimpleParser.VariableContext variable in context.variables().variable())
            {
                string varName = variable.IDENTIFIER().GetText();

                switch (type.ToLower())
                {
                    case "int":
                        if (variable.expression() != null)
                        {
                            string initialValue = variable.expression().GetText();
                            code.AppendLine($"{varName} DWORD {initialValue}");
                        }
                        else
                        {
                            code.AppendLine($"{varName} DWORD 0");
                        }
                        break;
                    case "double":
                        if (variable.expression() != null)
                        {
                            string initialValue = variable.expression().GetText();
                            code.AppendLine($"{varName} REAL8 {initialValue}");
                        }
                        else
                        {
                            code.AppendLine($"{varName} REAL8 0.0");
                        }
                        break;
                    case "bool":
                        code.AppendLine($"{varName} BYTE 0");
                        break;
                    default:
                        code.AppendLine($"{varName} DWORD 0");
                        break;
                }
            }
            return "";
        }

        public override string VisitVariables(SimpleParser.VariablesContext context)
        {
            foreach (SimpleParser.VariableContext variable in context.variable())
            {
                SafeVisit(variable);
            }
            return "";
        }

        public override string VisitVariable(SimpleParser.VariableContext context)
        {
            string varName = context.IDENTIFIER().GetText();

            // ✅ أولاً: تأكد من إضافة المتغير إلى SymbolTable
            if (symbolTable.LookupInCurrentScope(varName) == null && !string.IsNullOrEmpty(currentFunctionName))
            {
                string varType = GetVariableTypeFromContext(context);
                if (!string.IsNullOrEmpty(varType))
                {
                    Symbol varSymbol = new Symbol(
                        varName,
                        "local",
                        varType,
                        context.Start.Line,
                        context.Start.Column,
                        currentFunctionName
                    );
                    symbolTable.AddSymbol(varSymbol);
                }
            }

            if (context.expression() != null)
            {
                string resultReg = SafeVisit(context.expression());

                if (localVarOffsets.ContainsKey(varName))
                {
                    int offset = localVarOffsets[varName];
                    var symbol = symbolTable.Lookup(varName);
                    string varType = symbol?.DataType?.ToLower() ?? GetVariableTypeFromContext(context)?.ToLower();

                    //Console.WriteLine($"[DEBUG] Storing to variable '{varName}': type={varType}, resultReg={resultReg}, offset={offset}");

                    if (varType == "double")
                    {
                        if (resultReg == "st(0)")
                        {
                            code.AppendLine($"fstp qword ptr [ebp-{offset}]");
                        }
                        else if (resultReg == "eax")
                        {
                            // إذا كانت النتيجة في eax ولكن يجب تخزينها كـ double
                            code.AppendLine("push eax");
                            code.AppendLine("fild dword ptr [esp]");
                            code.AppendLine($"fstp qword ptr [ebp-{offset}]");
                            code.AppendLine("add esp, 4");
                        }
                    }
                    else
                    {
                        code.AppendLine($"mov [ebp-{offset}], eax");
                    }
                }
                else
                {
                    // ✅ إذا لم يكن هناك offset مسجل، استخدم offset مناسب للنوع
                    string varType = GetVariableTypeFromContext(context)?.ToLower();
                    int defaultOffset = (varType == "double") ? 8 : 4;

                    if (varType == "double" && resultReg == "st(0)")
                    {
                        code.AppendLine($"fstp qword ptr [ebp-{defaultOffset}] ; Default offset for {varName}");
                    }
                    else
                    {
                        code.AppendLine($"mov [ebp-{defaultOffset}], eax ; Default offset for {varName}");
                    }
                }
            }
            return "";
        }

        private string GetVariableTypeFromContext(SimpleParser.VariableContext context)
        {
            if (context == null) return null;

            // البحث في الشجرة للعثور على نوع المتغير
            RuleContext parent = context.Parent;
            while (parent != null)
            {
                if (parent is SimpleParser.VariablesContext variablesContext)
                {
                    RuleContext grandParent = variablesContext.Parent;
                    if (grandParent is SimpleParser.StatementContext statementContext && statementContext.type() != null)
                    {
                        return statementContext.type().GetText();
                    }
                    else if (grandParent is SimpleParser.GlobalContext globalContext)
                    {
                        return globalContext.type().GetText();
                    }
                }
                parent = parent.Parent;
            }

            return null;
        }

        private string GenerateIfStatement(SimpleParser.StatementContext context)
        {
            string skipLabel = $"L_skip_{labelCounter}";
            labelCounter++;

            // ✅ التصحيح: معالجة التعبير الشرطي بشكل صحيح
            if (context.expression(0) != null)
            {
                var condition = context.expression(0);

                // ✅ معالجة المقارنة (مثل b > max)
                if (condition.binaryOp() != null && condition.expression().Length == 2)
                {
                    // Evaluate left, push, eval right, pop left into ebx
                    SafeVisit(condition.expression(0));
                    code.AppendLine("push eax");
                    SafeVisit(condition.expression(1));
                    code.AppendLine("pop ebx");

                    code.AppendLine("cmp ebx, eax");

                    string op = condition.binaryOp().GetText();
                    switch (op)
                    {
                        case ">":
                            code.AppendLine($"jle {skipLabel}"); // if not (left > right) jump
                            break;
                        case "<":
                            code.AppendLine($"jge {skipLabel}");
                            break;
                        case ">=":
                            code.AppendLine($"jl {skipLabel}");
                            break;
                        case "<=":
                            code.AppendLine($"jg {skipLabel}");
                            break;
                        case "==":
                            code.AppendLine($"jne {skipLabel}");
                            break;
                        case "!=":
                            code.AppendLine($"je {skipLabel}");
                            break;
                        default:
                            code.AppendLine($"jle {skipLabel}");
                            break;
                    }
                }
                else
                {
                    // fallback: evaluate and test eax !=0
                    SafeVisit(context.expression(0));
                    code.AppendLine("cmp eax, 0");
                    code.AppendLine($"je {skipLabel}");
                }
            }

            // ✅ تنفيذ الجملة داخل IF (مثل max = b)
            if (context.statement(0) != null)
            {
                SafeVisit(context.statement(0));
            }

            code.AppendLine($"{skipLabel}:");

            return "";
        }

        private string GetRegisterForComparison(string register)
        {
            if (register == "st(0)")
            {
                return "st(0)";
            }
            else
            {
                return "eax"; // نستخدم eax كمقارنة افتراضية
            }
        }

        private string GenerateWhileStatement(SimpleParser.StatementContext context)
        {
            string startLabel = $"L_while_start_{labelCounter}";
            string endLabel = $"L_while_end_{labelCounter}";
            labelCounter++;

            code.AppendLine($"{startLabel}:");
            SafeVisit(context.expression(0));
            code.AppendLine($"cmp eax, 0");
            code.AppendLine($"je {endLabel}");

            SafeVisit(context.statement(0));
            code.AppendLine($"jmp {startLabel}");

            code.AppendLine($"{endLabel}:");
            return "";
        }

        private string GenerateForStatement(SimpleParser.StatementContext context)
        {
            string startLabel = $"L_for_start_{labelCounter}";
            string endLabel = $"L_for_end_{labelCounter}";
            labelCounter++;

            if (context.type() != null && context.variables() != null)
                SafeVisit(context.variables());

            code.AppendLine($"{startLabel}:");
            if (context.expression().Length > 0)
            {
                SafeVisit(context.expression(0));
                code.AppendLine($"cmp eax, 0");
                code.AppendLine($"je {endLabel}");
            }

            if (context.statement().Length > 0)
                SafeVisit(context.statement(0));

            if (context.expression().Length > 1)
                SafeVisit(context.expression(1));

            code.AppendLine($"jmp {startLabel}");
            code.AppendLine($"{endLabel}:");
            return "";
        }

        public override string VisitStruct(SimpleParser.StructContext context)
        {
            string structName = context.IDENTIFIER(0).GetText();
            code.AppendLine($"; Struct: {structName}");

            if (context.struct_members() != null)
                SafeVisit(context.struct_members());

            return "";
        }

        public override string VisitStruct_members(SimpleParser.Struct_membersContext context)
        {
            foreach (var child in context.children)
            {
                if (child is SimpleParser.Struct_memberContext member)
                    SafeVisit(member);
            }
            return "";
        }

        public override string VisitStruct_member(SimpleParser.Struct_memberContext context)
        {
            if (context.STATIC() != null)
            {
                string type = context.type().GetText();
                string varName = context.variable().IDENTIFIER().GetText();

                // إضافة العضو الثابت إلى جدول الرموز - هذا مهم!
                Symbol memberSymbol = new Symbol(
                    varName,
                    "static",
                    type,
                    context.Start.Line,
                    context.Start.Column,
                    "global"  // لأنها ثابتة، فهي في النطاق العالمي
                );

                if (!symbolTable.AddSymbol(memberSymbol))
                {
                    //Console.WriteLine($"[WARNING] Failed to add static member '{varName}' to symbol table");
                }
                else
                {
                    //Console.WriteLine($"[SUCCESS] Added static member '{varName}' of type '{type}' to symbol table");
                }

                // الكود الأصلي لإنشاء البيانات
                if (context.variable().expression() != null)
                {
                    string initialValue = context.variable().expression().GetText();

                    switch (type.ToLower())
                    {
                        case "int":
                            code.AppendLine($"{varName} DWORD {initialValue}");
                            break;
                        case "double":
                            code.AppendLine($"{varName} REAL8 {initialValue}");
                            break;
                        case "bool":
                            code.AppendLine($"{varName} BYTE {initialValue}");
                            break;
                        default:
                            code.AppendLine($"{varName} DWORD {initialValue}");
                            break;
                    }
                }
                else
                {
                    switch (type.ToLower())
                    {
                        case "int":
                            code.AppendLine($"{varName} DWORD 0");
                            break;
                        case "double":
                            code.AppendLine($"{varName} REAL8 0.0");
                            break;
                        case "bool":
                            code.AppendLine($"{varName} BYTE 0");
                            break;
                        default:
                            code.AppendLine($"{varName} DWORD 0");
                            break;
                    }
                }
            }
            return "";
        }
    }
}