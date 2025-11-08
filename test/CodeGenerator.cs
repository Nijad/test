using Antlr4.Runtime.Tree;
using System.Text;
using test.Content;

namespace test
{
    public class CodeGenerator : SimpleBaseVisitor<string>
    {
        private int labelCounter = 0;
        private StringBuilder code = new StringBuilder();
        private SymbolTable symbolTable;
        private HashSet<Antlr4.Runtime.ParserRuleContext> visitedNodes;
        private Dictionary<string, int> localVarOffsets = new Dictionary<string, int>();
        private int currentLocalOffset = 4; // نبدأ من [ebp-4]
        private int currentParamOffset = 8; // الباراميترات تبدأ من [ebp+8]


        public CodeGenerator(SymbolTable symbolTable)
        {
            this.symbolTable = symbolTable;
            visitedNodes = new HashSet<Antlr4.Runtime.ParserRuleContext>();
        }

        // دوال مساعدة لتحديد الأنواع
        private string GetExpressionType(SimpleParser.ExpressionContext context)
        {
            if (context.INTEGER() != null) return "int";
            if (context.REAL() != null) return "double";
            if (context.TRUE() != null || context.FALSE() != null) return "bool";
            if (context.IDENTIFIER() != null)
            {
                Symbol symbol = symbolTable.Lookup(context.IDENTIFIER().GetText());
                return symbol?.DataType ?? "unknown";
            }
            return "unknown";
        }

        public string Generate(SimpleParser.ProgramContext tree)
        {
            // إضافة الهيدر الأساسي
            code.AppendLine(".386");
            code.AppendLine(".model flat, stdcall");
            code.AppendLine("option casemap :none");
            code.AppendLine();
            code.AppendLine("include \\masm32\\include\\windows.inc");
            code.AppendLine("include \\masm32\\include\\kernel32.inc");
            code.AppendLine("include \\masm32\\include\\masm32.inc");
            code.AppendLine("includelib \\masm32\\lib\\kernel32.lib");
            code.AppendLine("includelib \\masm32\\lib\\masm32.lib");
            code.AppendLine();

            VisitProgram(tree);
            return code.ToString();
        }

        // استخدام SafeVisit لمنع التكرار
        private string SafeVisit(Antlr4.Runtime.ParserRuleContext context)
        {
            if (context == null)
                return "";

            if (visitedNodes.Contains(context))
                return ""; // تجنب الزيارة المتكررة

            visitedNodes.Add(context);
            try
            {
                return Visit(context);
            }
            finally
            {
                visitedNodes.Remove(context);
            }
        }

        public override string VisitProgram(SimpleParser.ProgramContext context)
        {
            string programName = context.IDENTIFIER().GetText();

            code.AppendLine(".data");
            // توليد بيانات المتغيرات العالمية
            foreach (SimpleParser.MemberContext? member in context.member())
                if (member.global() != null)
                    SafeVisit(member.global());

            code.AppendLine();
            code.AppendLine(".code");
            code.AppendLine($"start:");

            // توليد دوال البرنامج
            foreach (SimpleParser.MemberContext? member in context.member())
                if (member.function() != null)
                    SafeVisit(member.function());

            code.AppendLine("invoke ExitProcess, 0");
            code.AppendLine("end start");

            return "";
        }

        public override string VisitFunction(SimpleParser.FunctionContext context)
        {
            string functionName = context.IDENTIFIER().GetText();
            string returnType = context.type()?.GetText() ?? "void";

            // إعادة تعيين حالة توليد الكود للدالة الجديدة
            localVarOffsets.Clear();
            currentLocalOffset = 4;
            currentParamOffset = 8;

            code.AppendLine();
            code.AppendLine($"{functionName} PROC");

            // إطار الدالة
            code.AppendLine("push ebp");
            code.AppendLine("mov ebp, esp");

            // معالجة الباراميترات أولاً
            if (context.arguments() != null)
            {
                code.AppendLine("; === معالجة الباراميترات ===");
                SafeVisit(context.arguments());
            }

            // حجز المساحة الإجمالية للمتغيرات المحلية
            if (currentLocalOffset > 4)
                code.AppendLine($"sub esp, {currentLocalOffset - 4} ; حجز مساحة للمتغيرات المحلية");

            // توليد كود الجمل داخل الدالة
            foreach (SimpleParser.StatementContext? stmt in context.statement())
                SafeVisit(stmt);

            // نهاية الدالة
            code.AppendLine("; === نهاية الدالة ===");
            code.AppendLine("mov esp, ebp");
            code.AppendLine("pop ebp");

            // تنظيف الباراميترات من المكدس (لنمط stdcall)
            int paramSize = (currentParamOffset - 8);
            if (paramSize > 0)
                code.AppendLine($"ret {paramSize}");
            else
                code.AppendLine("ret");

            code.AppendLine($"{functionName} ENDP");
            return "";
        }

        // التصحيح الحاسم: تجاوز VisitStatement بشكل صحيح
        public override string VisitStatement(SimpleParser.StatementContext context)
        {
            if (context == null)
                return "";

            // تحقق من نوع الجملة وعالجها بشكل مناسب
            if (context.IF() != null)
                return GenerateIfStatement(context);
            else if (context.WHILE() != null)
                return GenerateWhileStatement(context);
            else if (context.FOR() != null)
                return GenerateForStatement(context);
            else if (context.RETURN() != null)
                return GenerateReturnStatement(context);
            else if (context.expression() != null && context.expression().Length > 0)
                return GenerateExpressionStatement(context);
            else if (context.LBRACE() != null)
            {
                foreach (SimpleParser.StatementContext? stmt in context.statement())
                    SafeVisit(stmt);
                return "";
            }
            else if (context.type() != null && context.variables() != null)
            {
                // تعريف متغيرات محلية
                SafeVisit(context.variables());
                return "";
            }

            return "";
        }

        public override string VisitArguments(SimpleParser.ArgumentsContext context)
        {
            foreach (SimpleParser.ArgumentContext? arg in context.argument())
                SafeVisit(arg);
            return "";
        }

        public override string VisitArgument(SimpleParser.ArgumentContext context)
        {
            string argName = context.IDENTIFIER().GetText();
            string type = context.type().GetText();

            // حساب موقع الباراميتر الحالي في المكدس
            int paramStackOffset = currentParamOffset;

            // تحديد موقع التخزين المحلي للباراميتر
            int localOffset = AllocateLocalSpace(argName, type);

            // تسجيل موقع الباراميتر في جدول الرموز (إذا كان مدعوماً)
            code.AppendLine($"; معالجة الباراميتر: {argName} ({type})");
            code.AppendLine($"; الموقع: [ebp+{paramStackOffset}] -> [ebp-{localOffset}]");

            // تحميل الباراميتر من المكدس إلى موقعه المحلي
            GenerateParameterLoad(argName, type, paramStackOffset, localOffset);

            // زيادة عداد الباراميترات للباراميتر التالي
            currentParamOffset += 4;

            return "";
        }

        private int AllocateLocalSpace(string varName, string type)
        {
            int size = GetTypeSize(type);
            int offset = currentLocalOffset;

            // تحديث العداد مع المحاذاة
            currentLocalOffset += size;
            if (currentLocalOffset % 4 != 0)
                currentLocalOffset += 4 - (currentLocalOffset % 4);

            // تسجيل الموقع في القاموس
            localVarOffsets[varName] = offset;

            return offset;
        }

        private int GetTypeSize(string type)
        {
            switch (type)
            {
                case "int": return 4;
                case "double": return 8;
                case "bool": return 1;
                default: return 4; // الأنواع المعرفة من المستخدم
            }
        }

        private void GenerateParameterLoad(string varName, string type, int paramOffset, int localOffset)
        {
            switch (type)
            {
                case "int":
                    code.AppendLine($"mov eax, [ebp+{paramOffset}]");
                    code.AppendLine($"mov [ebp-{localOffset}], eax");
                    break;
                case "double":
                    code.AppendLine($"fld qword ptr [ebp+{paramOffset}]");
                    code.AppendLine($"fstp qword ptr [ebp-{localOffset}]");
                    break;
                case "bool":
                    code.AppendLine($"mov al, byte ptr [ebp+{paramOffset}]");
                    code.AppendLine($"mov [ebp-{localOffset}], al");
                    break;
                default:
                    code.AppendLine($"mov eax, [ebp+{paramOffset}]");
                    code.AppendLine($"mov [ebp-{localOffset}], eax");
                    break;
            }
        }

        private string GenerateIfStatement(SimpleParser.StatementContext context)
        {
            string elseLabel = $"L_else_{labelCounter}";
            string endLabel = $"L_end_{labelCounter}";
            labelCounter++;

            if (context.expression().Length > 0)
            {
                SafeVisit(context.expression(0));
                code.AppendLine($"cmp eax, 0");
                code.AppendLine($"je {elseLabel}");
            }

            // كود if
            if (context.statement().Length > 0)
                SafeVisit(context.statement(0));

            if (context.ELSE() != null)
                code.AppendLine($"jmp {endLabel}");

            // كود else
            code.AppendLine($"{elseLabel}:");
            if (context.ELSE() != null && context.statement().Length > 1)
                SafeVisit(context.statement(1));

            code.AppendLine($"{endLabel}:");
            return "";
        }

        private string GenerateWhileStatement(SimpleParser.StatementContext context)
        {
            string startLabel = $"L_while_start_{labelCounter}";
            string endLabel = $"L_while_end_{labelCounter}";
            labelCounter++;

            code.AppendLine($"{startLabel}:");

            // توليد كود الشرط
            if (context.expression().Length > 0)
            {
                SafeVisit(context.expression(0));
                code.AppendLine($"cmp eax, 0");
                code.AppendLine($"je {endLabel}");
            }

            // كود جسم while
            if (context.statement().Length > 0)
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

            // التهيئة
            if (context.type() != null && context.variables() != null)
                SafeVisit(context.variables());

            code.AppendLine($"{startLabel}:");

            // الشرط
            if (context.expression().Length > 0)
            {
                SafeVisit(context.expression(0));
                code.AppendLine($"cmp eax, 0");
                code.AppendLine($"je {endLabel}");
            }

            // جسم for
            if (context.statement().Length > 0)
                SafeVisit(context.statement(0));

            // التحديث
            if (context.expression().Length > 1)
                SafeVisit(context.expression(1));

            code.AppendLine($"jmp {startLabel}");
            code.AppendLine($"{endLabel}:");
            return "";
        }

        private string GenerateReturnStatement(SimpleParser.StatementContext context)
        {
            if (context.expression() != null && context.expression().Length > 0)
                SafeVisit(context.expression(0));
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
            string expressionType = GetExpressionType(context);

            if (context.INTEGER() != null)
            {
                code.AppendLine($"mov eax, {context.INTEGER().GetText()}");
                return "eax";
            }
            else if (context.REAL() != null)
            {
                // معالجة الأعداد الحقيقية
                double value = double.Parse(context.REAL().GetText());
                code.AppendLine($"push {BitConverter.ToInt32(BitConverter.GetBytes((float)value), 0)}");
                code.AppendLine($"fld dword ptr [esp]");
                code.AppendLine($"add esp, 4");
                return "st(0)"; // أعلى المكدس العائم
            }
            else if (context.TRUE() != null)
            {
                code.AppendLine($"mov eax, 1");
                return "eax";
            }
            else if (context.FALSE() != null)
            {
                code.AppendLine($"mov eax, 0");
                return "eax";
            }
            else if (context.IDENTIFIER() != null && context.ASSIGN() == null &&
                     context.INCREMENT() == null && context.DECREMENT() == null)
            {
                string varName = context.IDENTIFIER().GetText();
                Symbol symbol = symbolTable.Lookup(varName);
                if (symbol == null)
                    return "eax";
                // التحقق إذا كان المتغير محلياً (له موقع في المكدس)
                if (!localVarOffsets.ContainsKey(varName))
                    // المتغير العام
                    switch (symbol.DataType)
                    {
                        case "int":
                        case "bool":
                            code.AppendLine($"mov eax, {varName}");
                            return "eax";
                        case "double":
                            code.AppendLine($"fld {varName}");
                            return "st(0)";
                        default:
                            code.AppendLine($"mov eax, {varName}");
                            return "eax";
                    }

                int offset = localVarOffsets[varName];
                switch (symbol.DataType)
                {
                    case "int":
                    case "bool":
                        code.AppendLine($"mov eax, [ebp-{offset}]");
                        return "eax";
                    case "double":
                        code.AppendLine($"fld qword ptr [ebp-{offset}]");
                        return "st(0)";
                    default:
                        code.AppendLine($"mov eax, [ebp-{offset}]");
                        return "eax";
                }
            }
            else if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                string varName = context.IDENTIFIER().GetText();
                Symbol symbol = symbolTable.Lookup(varName);
                string varType = symbol?.DataType ?? "int";

                SafeVisit(context.expression(1));

                if (varType == "double")
                    code.AppendLine($"fstp {varName}"); // تخزين من المكدس العائم
                else
                    code.AppendLine($"mov {varName}, eax");
                return varType == "double" ? "st(0)" : "eax";
            }
            else if (context.binaryOp() != null && context.expression().Length == 2)
                return GenerateBinaryOperation(context);
            else if (context.INCREMENT() != null || context.DECREMENT() != null)
                return GenerateIncrementDecrement(context);
            else if (context.expression().Length == 1)
                return SafeVisit(context.expression(0));

            return "";
        }

        private string GenerateBinaryOperation(SimpleParser.ExpressionContext context)
        {
            string leftType = GetExpressionType(context.expression(0));
            string rightType = GetExpressionType(context.expression(1));
            string op = context.binaryOp().GetText();

            // تحديد نوع النتيجة
            string resultType = GetBinaryResultType(leftType, rightType, op);

            if (resultType == "double")
                return GenerateFloatingPointOperation(context, leftType, rightType, op);
            else if (resultType == "bool")
                return GenerateBooleanOperation(context, leftType, rightType, op);
            else
                return GenerateIntegerOperation(context, leftType, rightType, op);
        }

        private string GetBinaryResultType(string leftType, string rightType, string op)
        {
            // إذا كان أي من المعاملات double، النتيجة double
            if (leftType == "double" || rightType == "double")
                return "double";

            // العمليات المنطقية تنتج bool
            if (op == "&&" || op == "||" || op == "==" || op == "!=" ||
                op == "<" || op == "<=" || op == ">" || op == ">=")
                return "bool";

            // العمليات الحسابية تنتج int
            return "int";
        }

        private string GenerateFloatingPointOperation(SimpleParser.ExpressionContext context, string leftType, string rightType, string op)
        {
            // تحميل المعامل الأيسر
            if (leftType == "double")
            {
                SafeVisit(context.expression(0));
            }
            else
            {
                SafeVisit(context.expression(0));
                code.AppendLine("push eax");
                code.AppendLine("fild dword ptr [esp]");
                code.AppendLine("add esp, 4");
            }

            code.AppendLine("push eax"); // حفظ للمعامل الأيسر

            // تحميل المعامل الأيمن
            if (rightType == "double")
            {
                SafeVisit(context.expression(1));
            }
            else
            {
                SafeVisit(context.expression(1));
                code.AppendLine("push eax");
                code.AppendLine("fild dword ptr [esp]");
                code.AppendLine("add esp, 4");
            }

            code.AppendLine("pop ebx"); // استعادة المعامل الأيسر

            switch (op)
            {
                case "+":
                    code.AppendLine("faddp st(1), st(0)");
                    break;
                case "-":
                    code.AppendLine("fsubp st(1), st(0)");
                    break;
                case "*":
                    code.AppendLine("fmulp st(1), st(0)");
                    break;
                case "/":
                    code.AppendLine("fdivp st(1), st(0)");
                    break;
                case "==":
                    code.AppendLine("fcomip st(0), st(1)");
                    code.AppendLine("fstp st(0)"); // تنظيف المكدس
                    code.AppendLine("sete al");
                    code.AppendLine("movzx eax, al");
                    return "eax";
                case "!=":
                    code.AppendLine("fcomip st(0), st(1)");
                    code.AppendLine("fstp st(0)");
                    code.AppendLine("setne al");
                    code.AppendLine("movzx eax, al");
                    return "eax";
                case "<":
                    code.AppendLine("fcomip st(0), st(1)");
                    code.AppendLine("fstp st(0)");
                    code.AppendLine("setb al");
                    code.AppendLine("movzx eax, al");
                    return "eax";
                case ">":
                    code.AppendLine("fcomip st(0), st(1)");
                    code.AppendLine("fstp st(0)");
                    code.AppendLine("seta al");
                    code.AppendLine("movzx eax, al");
                    return "eax";
                default:
                    code.AppendLine("; عملية غير مدعومة للنوع double: " + op);
                    break;
            }

            return "st(0)";
        }

        private string GenerateIntegerOperation(SimpleParser.ExpressionContext context, string leftType, string rightType, string op)
        {
            SafeVisit(context.expression(0));
            code.AppendLine("push eax");
            SafeVisit(context.expression(1));
            code.AppendLine("pop ebx");

            switch (op)
            {
                case "+":
                    code.AppendLine("add eax, ebx");
                    break;
                case "-":
                    code.AppendLine("sub eax, ebx");
                    break;
                case "*":
                    code.AppendLine("imul eax, ebx");
                    break;
                case "/":
                    code.AppendLine("xor edx, edx");
                    code.AppendLine("idiv ebx");
                    break;
                case "%":
                    code.AppendLine("xor edx, edx");
                    code.AppendLine("idiv ebx");
                    code.AppendLine("mov eax, edx");
                    break;
                case "==":
                    code.AppendLine("cmp eax, ebx");
                    code.AppendLine("sete al");
                    code.AppendLine("movzx eax, al");
                    break;
                case "!=":
                    code.AppendLine("cmp eax, ebx");
                    code.AppendLine("setne al");
                    code.AppendLine("movzx eax, al");
                    break;
                case "<":
                    code.AppendLine("cmp eax, ebx");
                    code.AppendLine("setl al");
                    code.AppendLine("movzx eax, al");
                    break;
                case "<=":
                    code.AppendLine("cmp eax, ebx");
                    code.AppendLine("setle al");
                    code.AppendLine("movzx eax, al");
                    break;
                case ">":
                    code.AppendLine("cmp eax, ebx");
                    code.AppendLine("setg al");
                    code.AppendLine("movzx eax, al");
                    break;
                case ">=":
                    code.AppendLine("cmp eax, ebx");
                    code.AppendLine("setge al");
                    code.AppendLine("movzx eax, al");
                    break;
                default:
                    code.AppendLine("; عملية غير مدعومة: " + op);
                    break;
            }

            return "eax";
        }

        private string GenerateBooleanOperation(SimpleParser.ExpressionContext context, string leftType, string rightType, string op)
        {
            string endLabel = $"L_bool_end_{labelCounter++}";
            string trueLabel = $"L_bool_true_{labelCounter++}";

            switch (op)
            {
                case "&&": // AND المنطقي
                    SafeVisit(context.expression(0));
                    code.AppendLine("cmp eax, 0");
                    code.AppendLine("je " + endLabel);
                    SafeVisit(context.expression(1));
                    code.AppendLine("cmp eax, 0");
                    code.AppendLine("mov eax, 0");
                    code.AppendLine("je " + endLabel);
                    code.AppendLine("mov eax, 1");
                    code.AppendLine(endLabel + ":");
                    break;

                case "||": // OR المنطقي
                    SafeVisit(context.expression(0));
                    code.AppendLine("cmp eax, 0");
                    code.AppendLine("jne " + trueLabel);
                    SafeVisit(context.expression(1));
                    code.AppendLine("cmp eax, 0");
                    code.AppendLine("mov eax, 0");
                    code.AppendLine("je " + endLabel);
                    code.AppendLine(trueLabel + ":");
                    code.AppendLine("mov eax, 1");
                    code.AppendLine(endLabel + ":");
                    break;

                default:
                    // للمقارنات بين القيم المنطقية
                    return GenerateIntegerOperation(context, leftType, rightType, op);
            }

            return "eax";
        }

        public override string VisitGlobal(SimpleParser.GlobalContext context)
        {
            string type = context.type().GetText();
            foreach (SimpleParser.VariableContext? variable in context.variables().variable())
            {
                string varName = variable.IDENTIFIER().GetText();

                switch (type)
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

                // إذا كانت هناك قيمة ابتدائية
                if (variable.expression() != null)
                {
                    string initialValueType = GetExpressionType(variable.expression());

                    // التحقق من توافق الأنواع
                    if (!AreTypesCompatible(type, initialValueType))
                        code.AppendLine($"; تحذير: عدم توافق الأنواع في التعيين - {type} و {initialValueType}");

                    SafeVisit(variable.expression());

                    if (type == "double" && initialValueType != "double")
                    {
                        code.AppendLine("push eax");
                        code.AppendLine("fild dword ptr [esp]");
                        code.AppendLine("fstp {varName}");
                        code.AppendLine("add esp, 4");
                    }
                    else if (type != "double" && initialValueType == "double")
                        code.AppendLine("fistp {varName}");
                    else if (type == "double")
                        code.AppendLine("fstp {varName}");
                    else
                        code.AppendLine("mov {varName}, eax");
                }
            }
            return "";
        }

        private bool AreTypesCompatible(string targetType, string sourceType)
        {
            if (targetType == sourceType) return true;
            if ((targetType == "int" || targetType == "double") &&
                (sourceType == "int" || sourceType == "double")) return true;
            if (targetType == "bool" && sourceType == "int") return true;
            return false;
        }

        // أضف هذه الدوال لمعالجة الهياكل
        public override string VisitStruct(SimpleParser.StructContext context)
        {
            // الهياكل عادةً لا تولد كود تنفيذ مباشر، ولكن قد تحتاج لمعالجة الأعضاء الثابتة
            string structName = context.IDENTIFIER(0).GetText();

            // معالجة الأعضاء الثابتة إذا وجدت
            if (context.struct_members() != null)
                SafeVisit(context.struct_members());

            return "";
        }

        public override string VisitStruct_members(SimpleParser.Struct_membersContext context)
        {
            foreach (IParseTree? child in context.children)
                if (child is SimpleParser.Struct_memberContext memberContext)
                    SafeVisit(memberContext);
            return "";
        }

        public override string VisitStruct_member(SimpleParser.Struct_memberContext context)
        {
            // معالجة الأعضاء الثابتة فقط (static)
            if (context.STATIC() != null)
            {
                string type = context.type().GetText();
                string varName = context.variable().IDENTIFIER().GetText();

                // توليد كود للمتغيرات الثابتة
                switch (type)
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
            return "";
        }

        public override string VisitVariables(SimpleParser.VariablesContext context)
        {
            foreach (SimpleParser.VariableContext? variable in context.variable())
                SafeVisit(variable);
            return "";
        }

        public override string VisitVariable(SimpleParser.VariableContext context)
        {
            string varName = context.IDENTIFIER().GetText();

            // إذا كان هناك تعيين ابتدائي
            if (context.expression() != null)
            {
                SafeVisit(context.expression());
                // المتغيرات المحلية تحتاج إلى تخصيص مساحة في المكدس
                code.AppendLine($"mov [ebp-4], eax"); // مثال بسيط - تحتاج لإدارة إطار المكدس
            }

            return "";
        }

        private string GenerateIncrementDecrement(SimpleParser.ExpressionContext context)
        {
            bool isIncrement = context.INCREMENT() != null;
            bool isPrefix = context.GetChild(0) is ITerminalNode;

            string varName = null;
            string varType = "int";

            if (context.expression(0) != null && context.expression(0).IDENTIFIER() != null)
            {
                varName = context.expression(0).IDENTIFIER().GetText();
                Symbol symbol = symbolTable.Lookup(varName);
                varType = symbol?.DataType ?? "int";
            }

            if (varName != null)
            {
                if (varType == "double")
                {
                    if (isPrefix)
                    {
                        // البادئة: ++x أو --x للنوع double
                        code.AppendLine($"fld {varName}");
                        code.AppendLine($"fld1");
                        if (isIncrement)
                            code.AppendLine($"faddp st(1), st(0)");
                        else
                            code.AppendLine($"fsubp st(1), st(0)");
                        code.AppendLine($"fstp {varName}");
                        code.AppendLine($"fld {varName}");
                    }
                    else
                    {
                        // اللاحقة: x++ أو x-- للنوع double
                        code.AppendLine($"fld {varName}");
                        code.AppendLine($"fld {varName}");
                        code.AppendLine($"fld1");
                        if (isIncrement)
                            code.AppendLine($"faddp st(1), st(0)");
                        else
                            code.AppendLine($"fsubp st(1), st(0)");
                        code.AppendLine($"fstp {varName}");
                    }
                    return "st(0)";
                }
                else
                {
                    // للنوع int أو bool
                    if (isPrefix)
                    {
                        if (isIncrement)
                            code.AppendLine($"inc {varName}");
                        else
                            code.AppendLine($"dec {varName}");
                        code.AppendLine($"mov eax, {varName}");
                    }
                    else
                    {
                        code.AppendLine($"mov eax, {varName}");
                        if (isIncrement)
                            code.AppendLine($"inc {varName}");
                        else
                            code.AppendLine($"dec {varName}");
                    }
                    return "eax";
                }
            }

            return "eax";
        }
    }
}