// هذا الملف يبني شجرة البنية المجردة (AST) من شجرة ANTLR
// التعليقات توضح أنه وسيط بين المحلل اللغوي وزيارات الشجرة الأخرى

using Antlr4.Runtime.Tree;
using test.Content;
using static test.Content.SimpleParser;

namespace test
{
    // فئة تبني شجرة البنية المجردة (AST) من شجرة ANTLR
    public class ASTBuilder : SimpleBaseVisitor<ASTNode>
    {
        // جدول الرموز للاستخدام أثناء البناء
        private SymbolTable symbolTable;

        // منشئ الفئة يأخذ جدول الرموز كمعامل
        public ASTBuilder(SymbolTable symbolTable)
        {
            this.symbolTable = symbolTable;
        }

        // زيارة عقدة البرنامج وبناء عقدة البرنامج في AST
        public override ASTNode VisitProgram(ProgramContext context)
        {
            // إنشاء عقدة البرنامج وتعبئة خصائصها
            ProgramNode node = new ProgramNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Name = context.IDENTIFIER().GetText()
            };

            // زيارة جميع الأعضاء في البرنامج وإضافتهم إلى عقدة البرنامج
            foreach (MemberContext? member in context.member())
            {
                // زيارة العضو وبناء عقدة AST المقابلة
                ASTNode memberNode = Visit(member);
                if (memberNode != null)
                    node.Members.Add(memberNode);
            }

            return node;
        }

        // زيارة عقدة الدالة وبناء عقدة الدالة في AST
        public override ASTNode VisitFunction(FunctionContext context)
        {
            // إنشاء عقدة الدالة وتعبئة خصائصها
            FunctionNode node = new FunctionNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                ReturnType = context.type()?.GetText() ?? "void",
                Name = context.IDENTIFIER().GetText()
            };

            // معالجة الباراميترات إذا وجدت
            if (context.arguments() != null)
                foreach (ArgumentContext? arg in context.arguments().argument())
                {
                    // إنشاء عقدة الباراميتر وتعبئة خصائصها
                    ParameterNode paramNode = new ParameterNode
                    {
                        Line = arg.Start.Line,
                        Column = arg.Start.Column,
                        Type = arg.type().GetText(),
                        Name = arg.IDENTIFIER().GetText()
                    };
                    node.Parameters.Add(paramNode);
                }

            // معالجة جسم الدالة
            foreach (StatementContext? stmt in context.statement())
            {
                // زيارة الجملة وبناء عقدة AST المقابلة
                StatementNode? stmtNode = Visit(stmt) as StatementNode;
                if (stmtNode != null)
                    node.Body.Add(stmtNode);
            }

            return node;
        }

        // زيارة عقدة المتغيرات العالمية وبناء عقدة المتغيرات العالمية في AST
        public override ASTNode VisitGlobal(GlobalContext context)
        {
            // إنشاء عقدة المتغيرات العالمية وتعبئة خصائصها
            GlobalVariableNode node = new GlobalVariableNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Type = context.type().GetText()
            };

            // معالجة جميع المتغيرات المعرفة في العقدة
            foreach (VariableContext? variable in context.variables().variable())
            {
                // إنشاء عقدة تعريف المتغير وتعبئة خصائصها
                VariableDeclNode varNode = new VariableDeclNode
                {
                    Line = variable.Start.Line,
                    Column = variable.Start.Column,
                    Name = variable.IDENTIFIER().GetText(),
                    Type = context.type().GetText()
                };

                // معالجة القيمة الابتدائية إذا وجدت
                if (variable.expression() != null)
                    varNode.InitialValue = Visit(variable.expression()) as ExpressionNode;

                node.Variables.Add(varNode);
            }

            return node;
        }

        // زيارة عقدة الهيكل وبناء عقدة الهيكل في AST
        public override ASTNode VisitStruct(StructContext context)
        {
            // إنشاء عقدة الهيكل وتعبئة خصائصها
            StructNode node = new StructNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Name = context.IDENTIFIER(0).GetText(),
                Parent = context.IDENTIFIER(1)?.GetText()
            };

            // معالجة أعضاء الهيكل إذا وجدت
            if (context.struct_members() != null)
            {
                // زيارة الأعضاء وبناء الهيكل
                StructMembersNode membersNode = VisitStruct_members(context.struct_members()) as StructMembersNode;
                if (membersNode != null)
                    node.Members = membersNode;
            }

            return node;
        }

        // زيارة أعضاء الهيكل وبناء عقدة أعضاء الهيكل في AST
        public override ASTNode VisitStruct_members(Struct_membersContext context)
        {
            // التحقق من صحة السياق
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

        // زيارة عضو الهيكل وبناء عقدة عضو الهيكل في AST
        public override ASTNode VisitStruct_member(Struct_memberContext context)
        {
            // التحقق من صحة السياق
            if (context == null) return null;
            // إنشاء عقدة تمثل عضو الهيكل
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
                // تعيين اسم العضو
                memberNode.Name = context.variable().IDENTIFIER().GetText();
                // معالجة القيمة الابتدائية إذا وجدت
                if (context.variable().expression() != null)
                    memberNode.InitialValue = Visit(context.variable().expression()) as ExpressionNode;
            }

            return memberNode;
        }

        // زيارة عقدة الجملة وبناء عقدة الجملة في AST
        public override ASTNode VisitStatement(StatementContext context)
        {
            // التحقق من نوع الجملة ومعالجتها بناءً على ذلك
            if (context.IF() != null)
                return VisitIfStatement(context);

            // معالجة جملة while
            if (context.WHILE() != null)
                return VisitWhileStatement(context);

            // معالجة جملة for
            if (context.FOR() != null)
                return VisitForStatement(context);

            // معالجة جملة return
            if (context.RETURN() != null)
                return VisitReturnStatement(context);

            // معالجة جملة التعبير
            if (context.expression() != null && context.expression().Length > 0)
                return new ExpressionStatementNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Expression = Visit(context.expression(0)) as ExpressionNode
                };

            // معالجة جملة الكتلة
            if (context.LBRACE() != null)
                return VisitBlockStatement(context);

            // معالجة تعريف المتغيرات المحلية
            if (context.type() != null && context.variables() != null)
                return VisitVariableDeclaration(context);

            return null;
        }

        // زيارة جملة if وبناء عقدة جملة if في AST
        private ASTNode VisitIfStatement(StatementContext context)
        {
            // إنشاء عقدة جملة if وتعبئة خصائصها
            IfStatementNode node = new IfStatementNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Condition = Visit(context.expression(0)) as ExpressionNode,
                ThenStatement = Visit(context.statement(0)) as StatementNode
            };

            // معالجة جملة else إذا وجدت
            if (context.ELSE() != null && context.statement().Length > 1)
                node.ElseStatement = Visit(context.statement(1)) as StatementNode;

            return node;
        }

        // زيارة جملة while وبناء عقدة جملة while في AST
        private ASTNode VisitWhileStatement(StatementContext context)
        {
            // إنشاء عقدة جملة while وتعبئة خصائصها
            return new WhileStatementNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Condition = Visit(context.expression(0)) as ExpressionNode,
                Body = Visit(context.statement(0)) as StatementNode
            };
        }

        // زيارة جملة for وبناء عقدة جملة for في AST
        private ASTNode VisitForStatement(StatementContext context)
        {
            // إنشاء عقدة جملة for وتعبئة خصائصها
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

        // زيارة جملة return وبناء عقدة جملة return في AST
        private ASTNode VisitReturnStatement(StatementContext context)
        {
            // إنشاء عقدة جملة return وتعبئة خصائصها
            ReturnStatementNode node = new ReturnStatementNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            // معالجة قيمة الإرجاع إذا وجدت
            if (context.expression() != null && context.expression().Length > 0)
                node.Value = Visit(context.expression(0)) as ExpressionNode;

            return node;
        }

        // زيارة جملة الكتلة وبناء عقدة جملة الكتلة في AST
        private ASTNode VisitBlockStatement(StatementContext context)
        {
            // إنشاء عقدة جملة الكتلة وتعبئة خصائصها
            BlockStatementNode node = new BlockStatementNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column
            };
            
            // معالجة جميع الجمل داخل الكتلة
            foreach (StatementContext? stmt in context.statement())
            {
                // زيارة الجملة وبناء عقدة AST المقابلة
                StatementNode stmtNode = Visit(stmt) as StatementNode;
                if (stmtNode != null)
                    node.Statements.Add(stmtNode);
            }

            return node;
        }

        // زيارة تعريف المتغيرات وبناء عقدة تعريف المتغيرات في AST
        private ASTNode VisitVariableDeclaration(StatementContext context)
        {
            // إنشاء عقدة تعريف المتغيرات وتعبئة خصائصها
            VariableDeclarationNode node = new VariableDeclarationNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column,
                Type = context.type().GetText()
            };

            // معالجة تعريف المتغيرات المحلية
            foreach (VariableContext? variable in context.variables().variable())
            {
                // إنشاء عقدة تعريف المتغير وتعبئة خصائصها
                VariableDeclNode varNode = new VariableDeclNode
                {
                    Line = variable.Start.Line,
                    Column = variable.Start.Column,
                    Name = variable.IDENTIFIER().GetText(),
                    Type = context.type().GetText()
                };

                // معالجة القيمة الابتدائية إذا وجدت
                if (variable.expression() != null)
                    varNode.InitialValue = Visit(variable.expression()) as ExpressionNode;

                node.Variables.Add(varNode);
            }

            return node;
        }

        // زيارة عقدة التعبير وبناء عقدة التعبير في AST
        public override ASTNode VisitExpression(ExpressionContext context)
        {
            // القيم الصحيحة
            if (context.INTEGER() != null)
                // إنشاء عقدة القيمة الصحيحة وتعبئة خصائصها
                return new IntegerNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Value = int.Parse(context.INTEGER().GetText()),
                    Type = "int"
                };

            // القيم العشرية
            if (context.REAL() != null)
                // إنشاء عقدة القيمة العشرية وتعبئة خصائصها
                return new RealNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Value = double.Parse(context.REAL().GetText()),
                    Type = "double"
                };

            // القيم البوليانية True
            if (context.TRUE() != null)
                // إنشاء عقدة القيمة البوليانية وتعبئة خصائصها
                return new BooleanNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Value = true,
                    Type = "bool"
                };

            // القيم البوليانية False
            if (context.FALSE() != null)
                // إنشاء عقدة القيمة البوليانية وتعبئة خصائصها
                return new BooleanNode
                {
                    Line = context.Start.Line,
                    Column = context.Start.Column,
                    Value = false,
                    Type = "bool"
                };

            // القيمة null
            if (context.NULL() != null)
                // إنشاء عقدة القيمة null وتعبئة خصائصها
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

            // Increment or decrement expression
            if (context.INCREMENT() != null || context.DECREMENT() != null)
                return VisitIncrementDecrementExpression(context);

            return null;
        }

        // تحديد نوع تعبير ثنائي بناءً على العامل
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

        // زيارة عقدة العضو وبناء عقدة العضو في AST
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

        // زيارة تعبير الزيادة والنقصان وبناء عقدة التعبير في AST
        private ASTNode VisitIncrementDecrementExpression(ExpressionContext context)
        {
            bool isIncrement = context.INCREMENT() != null;
            bool isPrefix = context.GetChild(0) is ITerminalNode;

            if (context.expression(0) != null)
            {
                // زيارة المعامل وبناء عقدة التعبير المقابلة
                ExpressionNode? operand = Visit(context.expression(0)) as ExpressionNode;
                // إنشاء عقدة التعبير الأحادي وتعبئة خصائصها
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