// هذا الملف يبني شجرة البنية المجردة (AST) من شجرة ANTLR
// التعليقات توضح أنه وسيط بين المحلل اللغوي وزيارات الشجرة الأخرى

using Antlr4.Runtime.Tree;
using test.Content;
using static test.Content.SimpleParser;

namespace test
{
    public class ASTBuilder : SimpleBaseVisitor<ASTNode>
    {
        private SymbolTable symbolTable;

        public ASTBuilder(SymbolTable symbolTable)
        {
            this.symbolTable = symbolTable;
        }

        public override ASTNode VisitProgram(ProgramContext context)
        {
            ProgramNode node = new ProgramNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Name = context.IDENTIFIER().GetText()
            };

            foreach (MemberContext? member in context.member())
            {
                ASTNode memberNode = Visit(member);
                if (memberNode != null)
                    node.Members.Add(memberNode);
            }

            return node;
        }

        public override ASTNode VisitFunction(FunctionContext context)
        {
            FunctionNode node = new FunctionNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                ReturnType = context.type()?.GetText() ?? "void",
                Name = context.IDENTIFIER().GetText()
            };

            if (context.arguments() != null)
                foreach (ArgumentContext? arg in context.arguments().argument())
                {
                    ParameterNode paramNode = new ParameterNode
                    {
                        Line = arg.Start.Line,
                        Column = arg.Start.Column,
                        Type = arg.type().GetText(),
                        Name = arg.IDENTIFIER().GetText()
                    };
                    node.Parameters.Add(paramNode);
                }

            foreach (StatementContext? stmt in context.statement())
            {
                StatementNode? stmtNode = Visit(stmt) as StatementNode;
                if (stmtNode != null)
                    node.Body.Add(stmtNode);
            }

            return node;
        }

        public override ASTNode VisitGlobal(GlobalContext context)
        {
            GlobalVariableNode node = new GlobalVariableNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Type = context.type().GetText()
            };

            foreach (VariableContext? variable in context.variables().variable())
            {
                VariableDeclNode varNode = new VariableDeclNode
                {
                    Line = variable.Start.Line,
                    Column = variable.Start.Column,
                    Name = variable.IDENTIFIER().GetText(),
                    Type = context.type().GetText()
                };

                if (variable.expression() != null)
                    varNode.InitialValue = Visit(variable.expression()) as ExpressionNode;

                node.Variables.Add(varNode);
            }

            return node;
        }

        public override ASTNode VisitStruct(StructContext context)
        {
            StructNode node = new StructNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Name = context.IDENTIFIER(0).GetText(),
                Parent = context.IDENTIFIER(1)?.GetText()
            };

            if (context.struct_members() != null)
            {
                StructMembersNode membersNode = VisitStruct_members(context.struct_members()) as StructMembersNode;
                if (membersNode != null)
                    node.Members = membersNode;
            }

            return node;
        }

        public override ASTNode VisitStruct_members(Struct_membersContext context)
        {
            if (context == null) return null;

            // إنشاء عقدة تمثل مجموعة أعضاء الهيكل
            StructMembersNode node = new StructMembersNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            // معالجة جميع الأعضاء في الهيكل
            foreach (IParseTree child in context.children)
                if (child is Struct_memberContext memberContext)
                {
                    StructMemberNode memberNode = VisitStruct_member(memberContext) as StructMemberNode;
                    if (memberNode != null)
                        node.Members.Add(memberNode);
                }

            return node;
        }

        public override ASTNode VisitStruct_member(Struct_memberContext context)
        {
            if (context == null) return null;

            StructMemberNode memberNode = new StructMemberNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                IsStatic = context.STATIC() != null,
                Type = context.type().GetText()
            };

            // معالجة المتغير داخل العضو
            if (context.variable() != null)
            {
                memberNode.Name = context.variable().IDENTIFIER().GetText();

                if (context.variable().expression() != null)
                    memberNode.InitialValue = Visit(context.variable().expression()) as ExpressionNode;
            }

            return memberNode;
        }

        public override ASTNode VisitStatement(StatementContext context)
        {
            if (context.IF() != null)
                return VisitIfStatement(context);

            if (context.WHILE() != null)
                return VisitWhileStatement(context);

            if (context.FOR() != null)
                return VisitForStatement(context);

            if (context.RETURN() != null)
                return VisitReturnStatement(context);

            if (context.expression() != null && context.expression().Length > 0)
                return new ExpressionStatementNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Expression = Visit(context.expression(0)) as ExpressionNode
                };

            if (context.LBRACE() != null)
                return VisitBlockStatement(context);

            if (context.type() != null && context.variables() != null)
                return VisitVariableDeclaration(context);

            return null;
        }

        private ASTNode VisitIfStatement(StatementContext context)
        {
            IfStatementNode node = new IfStatementNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Condition = Visit(context.expression(0)) as ExpressionNode,
                ThenStatement = Visit(context.statement(0)) as StatementNode
            };

            if (context.ELSE() != null && context.statement().Length > 1)
                node.ElseStatement = Visit(context.statement(1)) as StatementNode;

            return node;
        }

        private ASTNode VisitWhileStatement(StatementContext context)
        {
            return new WhileStatementNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Condition = Visit(context.expression(0)) as ExpressionNode,
                Body = Visit(context.statement(0)) as StatementNode
            };
        }

        private ASTNode VisitForStatement(StatementContext context)
        {
            ForStatementNode node = new ForStatementNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            // جملة for تحتوي على: for ( <Type> <Variables> ; <Expression>? ; <Expression>? ) <Statement>

            // 1. معالجة التهيئة (تعريف المتغيرات)
            if (context.type() != null && context.variables() != null)
                node.Initialization = VisitVariableDeclaration(context);

            // 2. معالجة الشرط (إذا وجد)
            if (context.expression().Length > 0 && context.expression(0) != null)
                node.Condition = Visit(context.expression(0)) as ExpressionNode;

            // 3. معالجة التحديث (إذا وجد)
            if (context.expression().Length > 1 && context.expression(1) != null)
                node.Update = Visit(context.expression(1)) as ExpressionNode;

            // 4. معالجة جسم for
            if (context.statement().Length > 0 && context.statement(0) != null)
                node.Body = Visit(context.statement(0)) as StatementNode;

            return node;
        }

        private ASTNode VisitReturnStatement(StatementContext context)
        {
            ReturnStatementNode node = new ReturnStatementNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            if (context.expression() != null && context.expression().Length > 0)
                node.Value = Visit(context.expression(0)) as ExpressionNode;

            return node;
        }

        private ASTNode VisitBlockStatement(StatementContext context)
        {
            BlockStatementNode node = new BlockStatementNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            foreach (StatementContext? stmt in context.statement())
            {
                StatementNode stmtNode = Visit(stmt) as StatementNode;
                if (stmtNode != null)
                    node.Statements.Add(stmtNode);
            }

            return node;
        }

        private ASTNode VisitVariableDeclaration(StatementContext context)
        {
            VariableDeclarationNode node = new VariableDeclarationNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Type = context.type().GetText()
            };

            // معالجة تعريف المتغيرات المحلية
            foreach (VariableContext? variable in context.variables().variable())
            {
                VariableDeclNode varNode = new VariableDeclNode
                {
                    Line = variable.Start.Line,
                    Column = variable.Start.Column,
                    Name = variable.IDENTIFIER().GetText(),
                    Type = context.type().GetText()
                };

                if (variable.expression() != null)
                    varNode.InitialValue = Visit(variable.expression()) as ExpressionNode;

                node.Variables.Add(varNode);
            }

            return node;
        }

        public override ASTNode VisitExpression(ExpressionContext context)
        {
            if (context.INTEGER() != null)
                return new IntegerNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Value = int.Parse(context.INTEGER().GetText()),
                    Type = "int"
                };

            if (context.REAL() != null)
                return new RealNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Value = double.Parse(context.REAL().GetText()),
                    Type = "double"
                };

            if (context.TRUE() != null)
                return new BooleanNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Value = true,
                    Type = "bool"
                };

            if (context.FALSE() != null)
                return new BooleanNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Value = false,
                    Type = "bool"
                };

            if (context.NULL() != null)
                return new NullNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Type = "null"
                };

            // Assignment expression
            if (context.IDENTIFIER() != null && context.ASSIGN() != null && context.expression().Length == 2)
                return new BinaryExpressionNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Left = new IdentifierNode
                    {
                        Line = context.Start.Line,
                        Column = context.Start.Column,
                        Name = context.IDENTIFIER().GetText(),
                        Type = "identifier"
                    },
                    Operator = "=",
                    Right = Visit(context.expression(1)) as ExpressionNode,
                    Type = "assignment"
                };

            // Simple identifier
            if (context.IDENTIFIER() != null && context.expression().Length == 0)
                return new IdentifierNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Name = context.IDENTIFIER().GetText(),
                    Type = "identifier"
                };

            // Binary operation
            if (context.binaryOp() != null && context.expression().Length == 2)
                return new BinaryExpressionNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Left = Visit(context.expression(0)) as ExpressionNode,
                    Operator = context.binaryOp().GetText(),
                    Right = Visit(context.expression(1)) as ExpressionNode,
                    Type = GetBinaryExpressionType(context.binaryOp().GetText())
                };

            // Unary expression or parenthesized expression
            if (context.expression().Length == 1 && context.ASSIGN() == null)
                return Visit(context.expression(0));

            if (context.INCREMENT() != null || context.DECREMENT() != null)
                return VisitIncrementDecrementExpression(context);

            return null;
        }

        private string GetBinaryExpressionType(string operator_)
        {
            switch (operator_)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                case "%":
                    return "arithmetic";
                case "==":
                case "!=":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    return "comparison";
                case "&&":
                case "||":
                    return "logical";
                default:
                    return "binary";
            }
        }

        public override ASTNode VisitMember(MemberContext context)
        {
            if (context.function() != null)
                return Visit(context.function());
            
            if (context.@struct() != null)
                return Visit(context.@struct());
            
            if (context.global() != null)
                return Visit(context.global());

            return null;
        }

        private ASTNode VisitIncrementDecrementExpression(ExpressionContext context)
        {
            bool isIncrement = context.INCREMENT() != null;
            bool isPrefix = context.GetChild(0) is ITerminalNode;

            string operatorType = isIncrement ? "increment" : "decrement";

            if (context.expression(0) != null)
            {
                ExpressionNode? operand = Visit(context.expression(0)) as ExpressionNode;

                return new UnaryExpressionNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Operator = isIncrement ? "++" : "--",
                    Operand = operand,
                    IsPrefix = isPrefix,
                    Type = operand?.Type ?? "int"
                };
            }

            return null;
        }
    }
}