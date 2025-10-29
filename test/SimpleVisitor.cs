using Antlr4.Runtime.Misc;
using test.Content;
namespace test
{
    public class SimpleVisitor : SimpleBaseVisitor<object>
    {
        private SymbolTable symbolTable;
        private List<string> semanticErrors;
        private string currentFunctionReturnType;

        // المنشئ الأول
        public SimpleVisitor(SymbolTable symbolTable, List<string> semanticErrors)
        {
            this.symbolTable = symbolTable;
            this.semanticErrors = semanticErrors;
        }

        // المنشئ الثاني (إذا كنت بحاجة إليه)
        public SimpleVisitor() : this(new SymbolTable(), new List<string>())
        {
        }

        // VisitMember - تأكد من إرجاع قيمة في جميع المسارات
        public override object VisitMember([NotNull] SimpleParser.MemberContext context)
        {
            if (context.function() != null)
                return Visit(context.function());
            else if (context.@struct() != null)
                return Visit(context.@struct());
            else if (context.global() != null)
                return Visit(context.global());

            return null; // إرجاع قيمة افتراضية
        }

        // باقي الدوال الزائرة - إليك بعض الدوال الأساسية
        public override object VisitProgram([NotNull] SimpleParser.ProgramContext context)
        {
            symbolTable.EnterScope("global");

            string programName = context.IDENTIFIER().GetText();
            AddSemanticInfo($"بدء البرنامج: {programName}");

            foreach (var member in context.member())
            {
                Visit(member);
            }

            symbolTable.ExitScope();
            return null;
        }

        public override object VisitFunction([NotNull] SimpleParser.FunctionContext context)
        {
            string returnType = context.type()?.GetText() ?? "void";
            string functionName = context.IDENTIFIER().GetText();

            currentFunctionReturnType = returnType;
            symbolTable.EnterScope(functionName);

            var functionSymbol = new Symbol(
                functionName,
                "function",
                returnType,
                context.Start.Line,
                context.Start.Column,
                symbolTable.CurrentScope
            );

            if (!symbolTable.AddSymbol(functionSymbol))
            {
                AddSemanticError($"الدالة '{functionName}' معرفة مسبقاً", context);
            }

            if (context.arguments() != null)
            {
                Visit(context.arguments());
            }

            foreach (var stmt in context.statement())
            {
                Visit(stmt);
            }

            symbolTable.ExitScope();
            currentFunctionReturnType = null;
            return null;
        }

        public override object VisitGlobal([NotNull] SimpleParser.GlobalContext context)
        {
            string type = context.type().GetText();

            foreach (var variable in context.variables().variable())
            {
                string varName = variable.IDENTIFIER().GetText();

                var globalSymbol = new Symbol(
                    varName,
                    "global",
                    type,
                    variable.Start.Line,
                    variable.Start.Column,
                    symbolTable.CurrentScope
                );

                if (!symbolTable.AddSymbol(globalSymbol))
                {
                    AddSemanticError($"المتغير العام '{varName}' معرّف مسبقاً", variable);
                }

                if (variable.expression() != null)
                {
                    string exprType = Visit(variable.expression()) as string;
                    if (exprType != null && !AreTypesCompatible(type, exprType))
                    {
                        AddSemanticError($"عدم توافق الأنواع: لا يمكن تعيين {exprType} إلى {type}", variable);
                    }
                }
            }
            return null;
        }

        // الدوال المساعدة
        private bool AreTypesCompatible(string type1, string type2)
        {
            if (type1 == type2) return true;
            if ((type1 == "int" || type1 == "double") && (type2 == "int" || type2 == "double")) return true;
            if (type1 == "null" && type2.StartsWith("struct")) return true;
            return false;
        }

        private void AddSemanticError(string message, Antlr4.Runtime.ParserRuleContext context)
        {
            semanticErrors.Add($"خطأ دلالي في السطر {context.Start.Line}: {message}");
        }

        private void AddSemanticInfo(string message)
        {
            System.Console.WriteLine($"معلومات: {message}");
        }
    }
}