using test.Content;

namespace test
{
    public class ASTBuilder : SimpleBaseVisitor<ASTNode>
    {
        private SymbolTable symbolTable;

        public ASTBuilder(SymbolTable symbolTable)
        {
            this.symbolTable = symbolTable;
        }

        public override ASTNode VisitProgram(SimpleParser.ProgramContext context)
        {
            var node = new ProgramNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Name = context.IDENTIFIER().GetText()
            };

            foreach (var member in context.member())
            {
                var memberNode = Visit(member);
                if (memberNode != null)
                    node.Members.Add(memberNode);
            }

            return node;
        }

        public override ASTNode VisitFunction(SimpleParser.FunctionContext context)
        {
            var node = new FunctionNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                ReturnType = context.type()?.GetText() ?? "void",
                Name = context.IDENTIFIER().GetText()
            };

            if (context.arguments() != null)
            {
                foreach (var arg in context.arguments().argument())
                {
                    var paramNode = new ParameterNode
                    {
                        Line = arg.Start.Line,
                        Column = arg.Start.Column,
                        Type = arg.type().GetText(),
                        Name = arg.IDENTIFIER().GetText()
                    };
                    node.Parameters.Add(paramNode);
                }
            }

            foreach (var stmt in context.statement())
            {
                var stmtNode = Visit(stmt) as StatementNode;
                if (stmtNode != null)
                    node.Body.Add(stmtNode);
            }

            return node;
        }

        // يمكنك إضافة المزيد من دوال Visit هنا لباقي العقد

        public override ASTNode VisitExpression(SimpleParser.ExpressionContext context)
        {
            if (context.INTEGER() != null)
            {
                return new IntegerNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Value = int.Parse(context.INTEGER().GetText()),
                    Type = "int"
                };
            }
            // أضف معالجة أنواع التعبيرات الأخرى
            return null;
        }
    }
}
