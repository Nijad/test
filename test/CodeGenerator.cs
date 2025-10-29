using System.Text;
using test.Content;

namespace test
{
    public class CodeGenerator : SimpleBaseVisitor<string>
    {
        private int labelCounter = 0;
        private StringBuilder code = new StringBuilder();
        private SymbolTable symbolTable;

        public CodeGenerator(SymbolTable symbolTable)
        {
            this.symbolTable = symbolTable;
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

        public override string VisitProgram(SimpleParser.ProgramContext context)
        {
            string programName = context.IDENTIFIER().GetText();

            code.AppendLine(".data");
            // توليد بيانات المتغيرات العالمية
            foreach (var member in context.member())
            {
                if (member.global() != null)
                {
                    Visit(member.global());
                }
            }

            code.AppendLine();
            code.AppendLine(".code");
            code.AppendLine($"start:");

            // توليد دوال البرنامج
            foreach (var member in context.member())
            {
                if (member.function() != null)
                {
                    Visit(member.function());
                }
            }

            code.AppendLine("invoke ExitProcess, 0");
            code.AppendLine("end start");

            return code.ToString();
        }

        public override string VisitFunction(SimpleParser.FunctionContext context)
        {
            string functionName = context.IDENTIFIER().GetText();
            string returnType = context.type()?.GetText() ?? "void";

            code.AppendLine();
            code.AppendLine($"{functionName} PROC");

            // توليد كود الباراميترات والمتغيرات المحلية
            if (context.arguments() != null)
            {
                Visit(context.arguments());
            }

            // توليد كود الجمل
            foreach (var stmt in context.statement())
            {
                Visit(stmt);
            }

            if (returnType == "void")
            {
                code.AppendLine("ret");
            }

            code.AppendLine($"{functionName} ENDP");
            return "";
        }

        public override string VisitGlobal(SimpleParser.GlobalContext context)
        {
            string type = context.type().GetText();
            foreach (var variable in context.variables().variable())
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
            }
            return "";
        }

        public override string VisitStatement(SimpleParser.StatementContext context)
        {
            if (context.IF() != null)
            {
                return GenerateIfStatement(context);
            }
            else if (context.WHILE() != null)
            {
                return GenerateWhileStatement(context);
            }
            else if (context.RETURN() != null)
            {
                return GenerateReturnStatement(context);
            }
            else if (context.expression() != null)
            {
                return Visit(context);
            }

            return "";
        }

        private string GenerateIfStatement(SimpleParser.StatementContext context)
        {
            string elseLabel = $"L_else_{labelCounter}";
            string endLabel = $"L_end_{labelCounter}";
            labelCounter++;

            // توليد كود الشرط
            string conditionCode = Visit(context.expression(0));
            code.AppendLine($"cmp eax, 0");
            code.AppendLine($"je {elseLabel}");

            // كود if
            Visit(context.statement(0));
            code.AppendLine($"jmp {endLabel}");

            // كود else
            code.AppendLine($"{elseLabel}:");
            if (context.ELSE() != null)
            {
                Visit(context.statement(1));
            }

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
            string conditionCode = Visit(context.expression(0));
            code.AppendLine($"cmp eax, 0");
            code.AppendLine($"je {endLabel}");

            // كود جسم while
            Visit(context.statement(0));
            code.AppendLine($"jmp {startLabel}");

            code.AppendLine($"{endLabel}:");
            return "";
        }

        private string GenerateReturnStatement(SimpleParser.StatementContext context)
        {
            if (context.expression() != null)
            {
                string exprCode = Visit(context.expression(0));
                // النتيجة تكون في eax بالفعل
            }
            code.AppendLine("ret");
            return "";
        }

        public override string VisitExpression(SimpleParser.ExpressionContext context)
        {
            if (context.INTEGER() != null)
            {
                code.AppendLine($"mov eax, {context.INTEGER().GetText()}");
                return "eax";
            }
            else if (context.IDENTIFIER() != null && context.ASSIGN() == null)
            {
                string varName = context.IDENTIFIER().GetText();
                code.AppendLine($"mov eax, {varName}");
                return "eax";
            }
            else if (context.ASSIGN() != null)
            {
                string varName = context.IDENTIFIER().GetText();
                string valueCode = Visit(context.expression(0));
                code.AppendLine($"mov {varName}, eax");
                return "eax";
            }
            else if (context.binaryOp() != null)
            {
                return GenerateBinaryOperation(context);
            }

            return "";
        }

        private string GenerateBinaryOperation(SimpleParser.ExpressionContext context)
        {
            Visit(context.expression(0)); // الطرف الأيسر في eax
            code.AppendLine("push eax");
            Visit(context.expression(1)); // الطرف الأيمن في eax
            code.AppendLine("pop ebx");

            string op = context.binaryOp().GetText();

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
                case "==":
                    code.AppendLine("cmp ebx, eax");
                    code.AppendLine("sete al");
                    code.AppendLine("movzx eax, al");
                    break;
                case ">":
                    code.AppendLine("cmp ebx, eax");
                    code.AppendLine("setg al");
                    code.AppendLine("movzx eax, al");
                    break;
                    // أضف باقي العمليات حسب الحاجة
            }

            return "eax";
        }
    }
}
