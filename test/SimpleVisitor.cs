using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using test.Content;

namespace test
{
    public class SimpleVisitor : SimpleBaseVisitor<object>
    {
        private SymbolTable symbolTable;
        private List<string> semanticErrors;
        private string currentFunctionReturnType;
        private HashSet<Antlr4.Runtime.ParserRuleContext> visitedNodes;

        public SimpleVisitor(SymbolTable symbolTable, List<string> semanticErrors)
        {
            this.symbolTable = symbolTable;
            this.semanticErrors = semanticErrors;
            visitedNodes = new HashSet<Antlr4.Runtime.ParserRuleContext>();
        }

        public override object VisitProgram([NotNull] SimpleParser.ProgramContext context)
        {
            symbolTable.EnterScope("global");

            string programName = context.IDENTIFIER().GetText();
            AddSemanticInfo($"start program: {programName}");

            foreach (SimpleParser.MemberContext? member in context.member())
                Visit(member);

            symbolTable.ExitScope();
            return null;
        }

        public override object VisitFunction([NotNull] SimpleParser.FunctionContext context)
        {
            string returnType = context.type()?.GetText() ?? "void";
            string functionName = context.IDENTIFIER().GetText();

            currentFunctionReturnType = returnType;

            // الدخول إلى نطاق الدالة
            symbolTable.EnterScope($"func_{functionName}");

            // إضافة الدالة إلى النطاق العالمي
            Symbol functionSymbol = new Symbol(
                functionName,
                "function",
                returnType,
                context.Start.Line,
                context.Start.Column,
                "global" // الدالة في النطاق العالمي
            );

            if (!symbolTable.AddSymbol(functionSymbol))
                AddSemanticError($"the function '{functionName}' is already declared", context);

            // معالجة الباراميترات (تضاف إلى نطاق الدالة)
            if (context.arguments() != null)
                Visit(context.arguments());

            // معالجة الجمل داخل الدالة
            foreach (SimpleParser.StatementContext? stmt in context.statement())
                Visit(stmt);

            symbolTable.ExitScope();
            currentFunctionReturnType = null;
            return null;
        }

        public override object VisitArgument([NotNull] SimpleParser.ArgumentContext context)
        {
            string type = context.type().GetText();
            string argName = context.IDENTIFIER().GetText();

            Symbol argSymbol = new Symbol(
                argName,
                "parameter",
                type,
                context.Start.Line,
                context.Start.Column,
                symbolTable.CurrentScope
            );

            if (!symbolTable.AddSymbol(argSymbol))
                AddSemanticError($"identifier '{argName}' is already declared", context);

            return null;
        }

        public override object VisitGlobal([NotNull] SimpleParser.GlobalContext context)
        {
            string type = context.type().GetText();

            foreach (SimpleParser.VariableContext? variable in context.variables().variable())
            {
                string varName = variable.IDENTIFIER().GetText();

                Symbol globalSymbol = new Symbol(
                    varName,
                    "global",
                    type,
                    variable.Start.Line,
                    variable.Start.Column,
                    symbolTable.CurrentScope
                );

                if (!symbolTable.AddSymbol(globalSymbol))
                    AddSemanticError($"glogabl variable '{varName}' is already declared", variable);

                // التحقق من التعبير إذا وجد
                if (variable.expression() != null)
                {
                    string exprType = GetExpressionType(variable.expression());
                    if (exprType != null && !AreTypesCompatible(type, exprType))
                        AddSemanticError($"type mismatch: cannot assign {exprType} to {type}", variable);
                }
            }
            return null;
        }

        public override object VisitVariable([NotNull] SimpleParser.VariableContext context)
        {
            string varName = context.IDENTIFIER().GetText();

            // الحصول على نوع المتغير من السياق الأب
            string varType = GetVariableTypeFromContext(context);

            if (varType != null)
            {
                Symbol varSymbol = new Symbol(
                    varName,
                    "local",
                    varType,
                    context.Start.Line,
                    context.Start.Column,
                    symbolTable.CurrentScope
                );

                if (!symbolTable.AddSymbol(varSymbol))
                    AddSemanticError($"variable '{varName}' is already declared", context);

                // التحقق من التعبير إذا وجد
                if (context.expression() != null)
                {
                    string exprType = GetExpressionType(context.expression());
                    if (exprType != null && !AreTypesCompatible(varType, exprType))
                        AddSemanticError($"type mismatch: cannot assign {exprType} to {varType}", context);
                    return exprType;
                }
            }

            return varType;
        }

        public override object VisitExpression([NotNull] SimpleParser.ExpressionContext context)
        {
            if (context.INTEGER() != null)
                return "int";
            else if (context.REAL() != null)
                return "double";
            else if (context.TRUE() != null || context.FALSE() != null)
                return "bool";
            else if (context.NULL() != null)
                return "null";
            else if (context.IDENTIFIER() != null && context.expression().Length == 0)
            {
                // معالجة المعرفات البسيطة
                string varName = context.IDENTIFIER().GetText();
                Symbol symbol = symbolTable.Lookup(varName);

                if (symbol != null)
                    return symbol.DataType;

                AddSemanticError($"Identifier '{varName}' is not declared", context);
                return null;

            }
            else if (context.expression().Length == 1 && context.ASSIGN() == null)
                // تعبيرات أحادية
                return Visit(context.expression(0));
            else if (context.expression().Length == 2 && context.binaryOp() != null)
            {
                // تعبيرات ثنائية
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));

                if (leftType != null && rightType != null)
                    return GetBinaryOperationResultType(leftType, rightType, context.binaryOp().GetText());
            }
            else if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                // تعيين
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));

                if (leftType != null && rightType != null && !AreTypesCompatible(leftType, rightType))
                    AddSemanticError($"type mismatch: {leftType} and {rightType}", context);

                return leftType;
            }
            else if (context.INCREMENT() != null || context.DECREMENT() != null)
                return VisitIncrementDecrementExpression(context);
            return null;
        }

        public override object VisitStatement([NotNull] SimpleParser.StatementContext context)
        {
            if (context.IF() != null)
                return VisitIfStatement(context);
            else if (context.WHILE() != null)
                return VisitWhileStatement(context);
            else if (context.FOR() != null)
                return VisitForStatement(context);
            else if (context.RETURN() != null)
                return VisitReturnStatement(context);
            else if (context.expression() != null && context.expression().Length > 0)
                return Visit(context.expression(0));
            else if (context.type() != null && context.variables() != null)
            {
                // تعريف متغير محلي
                Visit(context.type());
                Visit(context.variables());
                return null;
            }
            else if (context.LBRACE() != null)
            {
                // كتلة من الجمل - ندخل نطاق جديد للكتلة
                symbolTable.EnterScope($"block_{context.Start.Line}");
                foreach (SimpleParser.StatementContext? stmt in context.statement())
                    Visit(stmt);
                symbolTable.ExitScope();
                return null;
            }

            return null;
        }

        private object VisitReturnStatement(SimpleParser.StatementContext context)
        {
            if (context.expression() != null && context.expression().Length > 0)
            {
                string returnType = GetExpressionType(context.expression(0));

                if (returnType == null)
                    AddSemanticError("Can not detect expression type in return statement", context);
                else if (currentFunctionReturnType != "void" && !AreTypesCompatible(currentFunctionReturnType, returnType))
                    AddSemanticError($"return type {returnType} is not match with function type{currentFunctionReturnType}", context);
            }
            else if (currentFunctionReturnType != "void")
                AddSemanticError("Funciton must return value", context);

            return null;
        }

        private string GetVariableTypeFromContext(SimpleParser.VariableContext context)
        {
            RuleContext parent = context.Parent;
            while (parent != null)
            {
                if (parent is SimpleParser.GlobalContext globalContext)
                    return globalContext.type().GetText();
                else if (parent is SimpleParser.VariablesContext variablesContext)
                {
                    RuleContext grandParent = variablesContext.Parent;
                    if (grandParent is SimpleParser.GlobalContext global)
                        return global.type().GetText();
                    else if (grandParent is SimpleParser.StatementContext statement)
                        // متغير محلي داخل دالة
                        return statement.type().GetText();
                }
                parent = parent.Parent;
            }
            return null;
        }

        private string GetExpressionType(SimpleParser.ExpressionContext context)
        {
            object result = Visit(context);
            return result as string;
        }

        private bool AreTypesCompatible(string type1, string type2)
        {
            if (type1 == type2) return true;
            if (type1 == "null" && (type2.StartsWith("struct") || type2 == "class")) return true;
            if ((type1 == "int" && type2 == "double") || (type1 == "double" && type2 == "int")) return true;
            return false;
        }

        private string GetBinaryOperationResultType(string leftType, string rightType, string op)
        {
            switch (op)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                    if ((leftType == "int" || leftType == "double") && (rightType == "int" || rightType == "double"))
                        return (leftType == "double" || rightType == "double") ? "double" : "int";
                    break;
                case "&&":
                case "||":
                    if (leftType == "bool" && rightType == "bool")
                        return "bool";
                    break;
                case "==":
                case "!=":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    if ((leftType == "int" || leftType == "double") && (rightType == "int" || rightType == "double"))
                        return "bool";
                    else if (leftType == "bool" && rightType == "bool")
                        return "bool";
                    break;
            }

            AddSemanticInfo($"Operation is not supported: {leftType} {op} {rightType}");
            return null;
        }

        private void AddSemanticError(string message, Antlr4.Runtime.ParserRuleContext context)
        {
            string error = $"Semantic error at line {context.Start.Line}: {message}";
            semanticErrors.Add(error);
            Console.WriteLine($"❌ {error}");
        }

        private void AddSemanticInfo(string message)
        {
            Console.WriteLine($"{message}");
        }

        private object SafeVisit(Antlr4.Runtime.ParserRuleContext context)
        {
            if (context == null) return null;

            if (visitedNodes.Contains(context))
                return null;

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

        private object VisitIfStatement(SimpleParser.StatementContext context)
        {
            if (context.expression().Length > 0)
            {
                string conditionType = SafeVisit(context.expression(0)) as string;
                if (conditionType != "bool")
                    AddSemanticError("if condition must be boolean", context);
            }

            if (context.statement().Length > 0)
                SafeVisit(context.statement(0)); // جملة if

            if (context.ELSE() != null && context.statement().Length > 1)
                SafeVisit(context.statement(1)); // جملة else

            return null;
        }

        private object VisitWhileStatement(SimpleParser.StatementContext context)
        {
            if (context.expression().Length > 0)
            {
                string conditionType = SafeVisit(context.expression(0)) as string;
                if (conditionType != "bool")
                    AddSemanticError("while condition must be boolean", context);
            }

            if (context.statement().Length > 0)
                SafeVisit(context.statement(0));

            return null;
        }

        private object VisitForStatement(SimpleParser.StatementContext context)
        {
            symbolTable.EnterScope("for");

            // تعريف المتغيرات
            SafeVisit(context.type());
            SafeVisit(context.variables());

            // الشرط
            if (context.expression().Length > 0)
            {
                string conditionType = SafeVisit(context.expression(0)) as string;
                if (conditionType != "bool")
                    AddSemanticError("for condition must be boolean", context);
            }

            // التحديث
            if (context.expression().Length > 1)
                SafeVisit(context.expression(1));

            // جملة for
            if (context.statement().Length > 0)
                SafeVisit(context.statement(0));

            symbolTable.ExitScope();
            return null;
        }

        private object VisitIncrementDecrementExpression(SimpleParser.ExpressionContext context)
        {
            bool isIncrement = context.INCREMENT() != null;
            bool isPrefix = context.expression().Length == 1 &&
                           (context.INCREMENT() != null || context.DECREMENT() != null) &&
                           context.GetChild(0) is ITerminalNode;

            string varName = null;
            if (context.expression(0) != null && context.expression(0).IDENTIFIER() != null)
            {
                varName = context.expression(0).IDENTIFIER().GetText();
            }

            // التحقق من وجود المتغير
            if (varName != null)
            {
                Symbol symbol = symbolTable.Lookup(varName);
                if (symbol == null)
                {
                    AddSemanticError($"Identifier '{varName}' is not declared", context);
                    return null;
                }

                // التحقق من أن النوع رقمي
                if (symbol.DataType != "int" && symbol.DataType != "double")
                {
                    AddSemanticError($"Can not apply increament '++' or decreament '--' on the type {symbol.DataType}", context);
                    return null;
                }
            }

            return isIncrement ? "int" : "int"; // الناتج دائماً int
        }
    }
}