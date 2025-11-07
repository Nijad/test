using Antlr4.Runtime.Tree;
using System.Text;
using test.Content;

public class CodeGenerator : SimpleBaseVisitor<string>
{
    private int labelCounter = 0;
    private StringBuilder code = new StringBuilder();
    private SymbolTable symbolTable;
    private HashSet<Antlr4.Runtime.ParserRuleContext> visitedNodes;

    public CodeGenerator(SymbolTable symbolTable)
    {
        this.symbolTable = symbolTable;
        visitedNodes = new HashSet<Antlr4.Runtime.ParserRuleContext>();
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

        code.AppendLine();
        code.AppendLine($"{functionName} PROC");

        // توليد كود الباراميترات والمتغيرات المحلية
        if (context.arguments() != null)
            SafeVisit(context.arguments());

        // توليد كود الجمل - استخدم SafeVisit بدلاً من Visit
        foreach (SimpleParser.StatementContext? stmt in context.statement())
            SafeVisit(stmt);

        if (returnType == "void")
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
        if (context.INTEGER() != null)
        {
            code.AppendLine($"mov eax, {context.INTEGER().GetText()}");
            return "eax";
        }
        else if (context.IDENTIFIER() != null && context.ASSIGN() == null &&
                 context.INCREMENT() == null && context.DECREMENT() == null)
        {
            string varName = context.IDENTIFIER().GetText();
            code.AppendLine($"mov eax, {varName}");
            return "eax";
        }
        else if (context.ASSIGN() != null && context.expression().Length == 2)
        {
            string varName = context.IDENTIFIER().GetText();
            SafeVisit(context.expression(1));
            code.AppendLine($"mov {varName}, eax");
            return "eax";
        }
        else if (context.binaryOp() != null && context.expression().Length == 2)
        {
            return GenerateBinaryOperation(context);
        }
        else if (context.INCREMENT() != null || context.DECREMENT() != null)
        {
            return GenerateIncrementDecrement(context);
        }
        else if (context.expression().Length == 1)
        {
            return SafeVisit(context.expression(0));
        }

        return "";
    }

    private string GenerateBinaryOperation(SimpleParser.ExpressionContext context)
    {
        SafeVisit(context.expression(0)); // الطرف الأيسر في eax
        code.AppendLine("push eax");
        SafeVisit(context.expression(1)); // الطرف الأيمن في ebx
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
            case "%":
                code.AppendLine("xor edx, edx");
                code.AppendLine("idiv ebx");
                code.AppendLine("mov eax, edx"); // الباقي في edx
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
            case "&&":
                code.AppendLine("and eax, ebx");
                code.AppendLine("cmp eax, 0");
                code.AppendLine("setne al");
                code.AppendLine("movzx eax, al");
                break;
            case "||":
                code.AppendLine("or eax, ebx");
                code.AppendLine("cmp eax, 0");
                code.AppendLine("setne al");
                code.AppendLine("movzx eax, al");
                break;
            default:
                // إذا كانت العملية غير معروفة
                code.AppendLine("; unknown operation: " + op);
                break;
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
        }
        return "";
    }

    private string GenerateIncrementDecrement(SimpleParser.ExpressionContext context)
    {
        bool isIncrement = context.INCREMENT() != null;
        bool isPrefix = context.GetChild(0) is ITerminalNode;

        string varName = null;
        if (context.expression(0) != null && context.expression(0).IDENTIFIER() != null)
        {
            varName = context.expression(0).IDENTIFIER().GetText();
        }

        if (varName != null)
        {
            if (isPrefix)
            {
                // البادئة: ++x أو --x
                if (isIncrement)
                    code.AppendLine($"inc {varName}");
                else
                    code.AppendLine($"dec {varName}");
                
                code.AppendLine($"mov eax, {varName}");
            }
            else
            {
                // اللاحقة: x++ أو x--
                code.AppendLine($"mov eax, {varName}");
                
                if (isIncrement)
                    code.AppendLine($"inc {varName}");
                else
                    code.AppendLine($"dec {varName}");
            }
        }

        return "eax";
    }
}