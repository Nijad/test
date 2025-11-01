using System.Text;
using test;
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
        this.visitedNodes = new HashSet<Antlr4.Runtime.ParserRuleContext>();
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
        if (context == null) return "";

        if (visitedNodes.Contains(context))
        {
            return ""; // تجنب الزيارة المتكررة
        }

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
        foreach (var member in context.member())
        {
            if (member.global() != null)
            {
                SafeVisit(member.global());
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
                SafeVisit(member.function());
            }
        }

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
        {
            SafeVisit(context.arguments());
        }

        // توليد كود الجمل - استخدم SafeVisit بدلاً من Visit
        foreach (var stmt in context.statement())
        {
            SafeVisit(stmt);
        }

        if (returnType == "void")
        {
            code.AppendLine("ret");
        }

        code.AppendLine($"{functionName} ENDP");
        return "";
    }

    // التصحيح الحاسم: تجاوز VisitStatement بشكل صحيح
    public override string VisitStatement(SimpleParser.StatementContext context)
    {
        if (context == null) return "";

        // تحقق من نوع الجملة وعالجها بشكل مناسب
        if (context.IF() != null)
        {
            return GenerateIfStatement(context);
        }
        else if (context.WHILE() != null)
        {
            return GenerateWhileStatement(context);
        }
        else if (context.FOR() != null)
        {
            return GenerateForStatement(context);
        }
        else if (context.RETURN() != null)
        {
            return GenerateReturnStatement(context);
        }
        else if (context.expression() != null && context.expression().Length > 0)
        {
            // توليد كود للتعبير فقط
            return GenerateExpressionStatement(context);
        }
        else if (context.LBRACE() != null)
        {
            // كتلة من الجمل - استخدم SafeVisit للجمل الداخلية
            foreach (var stmt in context.statement())
            {
                SafeVisit(stmt);
            }
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

    // تجنب استدعاء Visit في الدوال المساعدة - استخدم Generation المباشر
    private string GenerateIfStatement(SimpleParser.StatementContext context)
    {
        string elseLabel = $"L_else_{labelCounter}";
        string endLabel = $"L_end_{labelCounter}";
        labelCounter++;

        // توليد كود الشرط - استخدم SafeVisit بدلاً من Visit
        if (context.expression().Length > 0)
        {
            SafeVisit(context.expression(0));
            code.AppendLine($"cmp eax, 0");
            code.AppendLine($"je {elseLabel}");
        }

        // كود if
        if (context.statement().Length > 0)
        {
            SafeVisit(context.statement(0));
        }
        code.AppendLine($"jmp {endLabel}");

        // كود else
        code.AppendLine($"{elseLabel}:");
        if (context.ELSE() != null && context.statement().Length > 1)
        {
            SafeVisit(context.statement(1));
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
        if (context.expression().Length > 0)
        {
            SafeVisit(context.expression(0));
            code.AppendLine($"cmp eax, 0");
            code.AppendLine($"je {endLabel}");
        }

        // كود جسم while
        if (context.statement().Length > 0)
        {
            SafeVisit(context.statement(0));
        }
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
        {
            SafeVisit(context.variables());
        }

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
        {
            SafeVisit(context.statement(0));
        }

        // التحديث
        if (context.expression().Length > 1)
        {
            SafeVisit(context.expression(1));
        }

        code.AppendLine($"jmp {startLabel}");
        code.AppendLine($"{endLabel}:");
        return "";
    }

    private string GenerateReturnStatement(SimpleParser.StatementContext context)
    {
        if (context.expression() != null && context.expression().Length > 0)
        {
            SafeVisit(context.expression(0));
        }
        code.AppendLine("ret");
        return "";
    }

    private string GenerateExpressionStatement(SimpleParser.StatementContext context)
    {
        if (context.expression().Length > 0)
        {
            SafeVisit(context.expression(0));
        }
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
        else if (context.ASSIGN() != null && context.expression().Length == 2)
        {
            string varName = context.IDENTIFIER().GetText();
            SafeVisit(context.expression(1)); // قيمة التعيين في eax
            code.AppendLine($"mov {varName}, eax");
            return "eax";
        }
        else if (context.binaryOp() != null && context.expression().Length == 2)
        {
            return GenerateBinaryOperation(context);
        }
        else if (context.expression().Length == 1)
        {
            // تعبيرات أحادية
            return SafeVisit(context.expression(0));
        }

        return "";
    }

    private string GenerateBinaryOperation(SimpleParser.ExpressionContext context)
    {
        SafeVisit(context.expression(0)); // الطرف الأيسر في eax
        code.AppendLine("push eax");
        SafeVisit(context.expression(1)); // الطرف الأيمن في eax
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
}