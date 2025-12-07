using Antlr4.Runtime;
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
        private Dictionary<string, int> paramOffsets = new Dictionary<string, int>();
        private int currentLocalOffset = 4;
        private int currentParamOffset = 8;
        private string currentFunctionName = "";
        private string currentFunctionReturnType = "";

        public CodeGenerator(SymbolTable symbolTable)
        {
            this.symbolTable = symbolTable;
            visitedNodes = new HashSet<Antlr4.Runtime.ParserRuleContext>();
        }

        public string Generate(SimpleParser.ProgramContext tree)
        {
            code.AppendLine(".386");
            code.AppendLine(".model flat, stdcall");
            code.AppendLine("option casemap :none");
            code.AppendLine();
            code.AppendLine("include \\\\masm32\\\\include\\\\windows.inc");
            code.AppendLine("include \\\\masm32\\\\include\\\\kernel32.inc");
            code.AppendLine("include \\\\masm32\\\\include\\\\masm32.inc");
            code.AppendLine("includelib \\\\masm32\\\\lib\\\\kernel32.lib");
            code.AppendLine("includelib \\\\masm32\\\\lib\\\\masm32.lib");
            code.AppendLine();

            VisitProgram(tree);
            return code.ToString();
        }

        private string SafeVisit(Antlr4.Runtime.ParserRuleContext context)
        {
            if (context == null) return "";
            if (visitedNodes.Contains(context)) return "";

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
            foreach (SimpleParser.MemberContext member in context.member())
            {
                if (member.global() != null)
                    SafeVisit(member.global());
                if (member.@struct() != null)
                    SafeVisit(member.@struct());
            }

            code.AppendLine();
            code.AppendLine(".code");

            code.AppendLine("start:");
            code.AppendLine("call main");
            code.AppendLine("invoke ExitProcess, 0");
            code.AppendLine();

            foreach (SimpleParser.MemberContext member in context.member())
            {
                if (member.function() != null)
                    SafeVisit(member.function());
            }

            code.AppendLine("end start");
            return "";
        }

        public override string VisitFunction(SimpleParser.FunctionContext context)
        {
            string functionName = context.IDENTIFIER().GetText();
            currentFunctionName = functionName;
            currentFunctionReturnType = context.type()?.GetText() ?? "void";

            localVarOffsets.Clear();
            paramOffsets.Clear();
            currentLocalOffset = DetermineInitialLocalOffset(context);
            currentParamOffset = 8;

            // ✅ إضافة الدالة إلى SymbolTable
            Symbol functionSymbol = new Symbol(
                functionName,
                "function",
                currentFunctionReturnType,
                context.Start.Line,
                context.Start.Column,
                "global"
            );
            symbolTable.AddSymbol(functionSymbol);

            code.AppendLine();
            code.AppendLine($"{functionName} PROC");
            code.AppendLine("push ebp");
            code.AppendLine("mov ebp, esp");

            // ✅ تحقق و أضف المعاملات إلى SymbolTable
            //Console.WriteLine($"[DEBUG] Processing function: {functionName}");
            // معالجة الباراميترات أولاً
            if (context.arguments() != null)
            {
                foreach (var arg in context.arguments().argument())
                {
                    string argName = arg.IDENTIFIER().GetText();
                    string argType = arg.type().GetText();
                    //Console.WriteLine($"[DEBUG] Function parameter: {argName} : {argType}");
                }
                SafeVisit(context.arguments());
            }

            // حساب المساحة للمتغيرات المحلية
            int localVarsSize = CalculateLocalVariablesSize(context);
            if (localVarsSize > 0)
            {
                code.AppendLine($"sub esp, {localVarsSize}");
            }

            // ✅ تحقق نهائي من إزاحات المتغيرات
            //Console.WriteLine($"[FINAL CHECK] Function {currentFunctionName} local variables:");
            foreach (var entry in localVarOffsets)
            {
                //Console.WriteLine($"[FINAL CHECK]   {entry.Key} at [ebp-{entry.Value}]");
            }

            // توليد كود الجمل داخل الدالة
            foreach (SimpleParser.StatementContext stmt in context.statement())
            {
                SafeVisit(stmt);
            }

            // إذا لم يكن هناك return statement صريح، نضيف العودة التلقائية
            bool hasReturn = false;
            foreach (var stmt in context.statement())
            {
                if (stmt.RETURN() != null)
                {
                    hasReturn = true;
                    break;
                }
            }

            if (!hasReturn)
            {
                if (currentFunctionReturnType == "double")
                {
                    code.AppendLine("fldz"); // تحميل الصفر العائم
                }
                else if (currentFunctionReturnType != "void")
                {
                    code.AppendLine("mov eax, 0");
                }
                code.AppendLine("mov esp, ebp");
                code.AppendLine("pop ebp");
                code.AppendLine("ret");
            }

            code.AppendLine($"{functionName} ENDP");
            currentFunctionName = "";
            currentFunctionReturnType = "";
            return "";
        }

        private int DetermineInitialLocalOffset(SimpleParser.FunctionContext context)
        {
            // ابحث عن أي متغيرات من نوع double
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

        private int CalculateLocalVariablesSize(SimpleParser.FunctionContext context)
        {
            int size = 0;

            foreach (var stmt in context.statement())
            {
                if (stmt.type() != null && stmt.variables() != null)
                {
                    string varType = stmt.type().GetText();
                    int varSize = GetTypeSize(varType);

                    foreach (var variable in stmt.variables().variable())
                    {
                        string varName = variable.IDENTIFIER().GetText();

                        // ✅ استخدام currentLocalOffset كنقطة بداية
                        int offset = currentLocalOffset + size;

                        // محاذاة بناءً على نوع المتغير
                        if (varSize == 8 && offset % 8 != 0)
                        {
                            int padding = 8 - (offset % 8);
                            size += padding;
                            offset += padding;
                        }
                        else if (varSize == 4 && offset % 4 != 0)
                        {
                            int padding = 4 - (offset % 4);
                            size += padding;
                            offset += padding;
                        }

                        localVarOffsets[varName] = offset;
                        size += varSize;

                        //Console.WriteLine($"[DEBUG] Local variable '{varName}' of type '{varType}' at offset {offset}");
                    }
                }
            }

            return size;
        }

        private int GetTypeSize(string type)
        {
            switch (type.ToLower())
            {
                case "int": return 4;
                case "double": return 8;
                case "bool": return 1;
                default: return 4;
            }
        }

        public override string VisitArguments(SimpleParser.ArgumentsContext context)
        {
            foreach (SimpleParser.ArgumentContext arg in context.argument())
            {
                SafeVisit(arg);
            }
            return "";
        }

        public override string VisitArgument(SimpleParser.ArgumentContext context)
        {
            string argName = context.IDENTIFIER().GetText();
            string type = context.type().GetText();
            int typeSize = GetTypeSize(type);

            int paramStackOffset = currentParamOffset;
            paramOffsets[argName] = paramStackOffset;
            currentParamOffset += typeSize;

            // ✅ تأكد من إضافة المعامل إلى SymbolTable
            Symbol paramSymbol = new Symbol(
                argName,
                "parameter",
                type,
                context.Start.Line,
                context.Start.Column,
                currentFunctionName
            );

            if (!symbolTable.AddSymbol(paramSymbol))
            {
                //Console.WriteLine($"[WARNING] Failed to add parameter '{argName}' to symbol table");
            }
            else
            {
                //Console.WriteLine($"[SUCCESS] Added parameter '{argName}' of type '{type}' to symbol table");
            }

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
                if (currentFunctionReturnType == "double" && resultReg == "st(0)")
                {
                    // القيمة بالفعل في المكان الصحيح
                }
                else if (currentFunctionReturnType == "double" && resultReg == "eax")
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
                    return GenerateStructMemberAccess(memberName);
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
                string memberName = context.IDENTIFIER().GetText();
                var symbol = symbolTable.Lookup(memberName);

                if (symbol?.DataType?.ToLower() == "double")
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

            code.AppendLine("; Unknown expression type - using default");
            code.AppendLine("mov eax, 0");
            return "eax";
        }

        private string GenerateComparison(SimpleParser.ExpressionContext context, string op)
        {
            code.AppendLine($"; Comparison: {op}");

            // معالجة الطرف الأيسر
            string leftReg = SafeVisit(context.expression(0));
            if (leftReg == "eax")
            {
                code.AppendLine("push eax"); // حفظ الطرف الأيسر
            }

            // معالجة الطرف الأيمن
            string rightReg = SafeVisit(context.expression(1));

            if (leftReg == "eax")
            {
                code.AppendLine("pop ebx"); // استعادة الطرف الأيسر إلى ebx
            }
            else
            {
                code.AppendLine("mov ebx, eax");
                code.AppendLine($"mov eax, {rightReg}");
            }

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
            return "eax";
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

        private string GenerateStructMemberAccess(string memberName)
        {
            code.AppendLine($"; Struct member access: {memberName}");

            var symbol = symbolTable.Lookup(memberName);
            if (symbol != null && symbol.DataType.ToLower() == "double")
            {
                code.AppendLine($"fld {memberName}");  // تحميل كعدد عائم
                return "st(0)";
            }
            else
            {
                code.AppendLine($"mov eax, {memberName}");
                return "eax";
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
                    string resultReg = SafeVisit(expr);

                    if (resultReg == "st(0)")
                    {
                        // معامل عائم: نخزنه في المكدس كـ double (8 بايت)
                        code.AppendLine("sub esp, 8");
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

        private string GenerateAssignment(SimpleParser.ExpressionContext context)
        {
            string varName = context.expression(0).IDENTIFIER().GetText();

            code.AppendLine($"; Complex assignment: {varName} = ...");

            // معالجة الطرف الأيمن
            string resultReg = SafeVisit(context.expression(1));

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

            // ✅ تحسين: للمقارنات في جمل if، يمكن استخدام القفزات مباشرة
            if (op == ">" || op == "<" || op == ">=" || op == "<=" || op == "==" || op == "!=")
            {
                // معالجة الطرف الأيسر
                SafeVisit(context.expression(0));
                code.AppendLine("push eax");

                // معالجة الطرف الأيمن
                SafeVisit(context.expression(1));
                code.AppendLine("pop ebx");

                code.AppendLine("cmp ebx, eax");

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
                return "eax";
            }

            // معالجة الطرف الأيسر
            SafeVisit(context.expression(0));
            code.AppendLine("push eax");

            // معالجة الطرف الأيمن
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
                    code.AppendLine("xor edx, edx");    // تنظيف edx
                    code.AppendLine("xchg eax, ebx");   // eax = المقسوم, ebx = المقسوم عليه
                    code.AppendLine("cdq");             // تحويل eax إلى edx:eax
                    code.AppendLine("idiv ebx");        // eax = الناتج, edx = الباقي
                                                        // ✅ التصحيح: لا نحتاج mov eax, ebx - النتيجة بالفعل في eax
                    break;
                case "%":
                    code.AppendLine("xor edx, edx");
                    code.AppendLine("xchg eax, ebx");
                    code.AppendLine("cdq");
                    code.AppendLine("idiv ebx");
                    code.AppendLine("mov eax, edx");    // الباقي في edx
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
                    var leftExpr = condition.expression(0);
                    var rightExpr = condition.expression(1);
                    string op = condition.binaryOp().GetText();

                    // ✅ تحميل الطرف الأيسر (b)
                    string leftReg = SafeVisit(leftExpr);
                    if (leftReg == "eax")
                    {
                        code.AppendLine("push eax"); // حفظ الطرف الأيسر في المكدس
                    }

                    // ✅ تحميل الطرف الأيمن (max)
                    string rightReg = SafeVisit(rightExpr);

                    if (leftReg == "eax")
                    {
                        code.AppendLine("pop ebx"); // استعادة الطرف الأيسر إلى ebx
                    }
                    else
                    {
                        code.AppendLine("mov ebx, eax"); // إذا لم يكن في eax، انقله إلى ebx
                        code.AppendLine($"mov eax, {rightReg}"); // ثم انقل الطرف الأيمن إلى eax
                    }

                    // ✅ إجراء المقارنة الصحيحة
                    code.AppendLine("cmp ebx, eax");

                    // ✅ استخدام القفز المناسب بناءً على العملية
                    switch (op)
                    {
                        case ">":
                            code.AppendLine($"jle {skipLabel}"); // إذا كان ebx <= eax، اقفز
                            break;
                        case "<":
                            code.AppendLine($"jge {skipLabel}"); // إذا كان ebx >= eax، اقفز
                            break;
                        case ">=":
                            code.AppendLine($"jl {skipLabel}");  // إذا كان ebx < eax، اقفز
                            break;
                        case "<=":
                            code.AppendLine($"jg {skipLabel}");  // إذا كان ebx > eax، اقفز
                            break;
                        case "==":
                            code.AppendLine($"jne {skipLabel}"); // إذا كان ebx != eax، اقفز
                            break;
                        case "!=":
                            code.AppendLine($"je {skipLabel}");  // إذا كان ebx == eax، اقفز
                            break;
                        default:
                            code.AppendLine($"jle {skipLabel}");
                            break;
                    }
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