namespace test
{
    // العقدة الأساسية في شجرة الـ AST
    public abstract class ASTNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    // عقدة البرنامج
    public class ProgramNode : ASTNode
    {
        public string Name { get; set; }
        public List<ASTNode> Members { get; set; } = new List<ASTNode>();
    }

    // عقدة الدالة
    public class FunctionNode : ASTNode
    {
        public string ReturnType { get; set; }
        public string Name { get; set; }
        public List<ParameterNode> Parameters { get; set; } = new List<ParameterNode>();
        public List<ASTNode> Body { get; set; } = new List<ASTNode>();
    }

    // عقدة الباراميتر
    public class ParameterNode : ASTNode
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }

    // عقدة الهيكل
    public class StructNode : ASTNode
    {
        public string Name { get; set; }
        public string Parent { get; set; }
        public StructMembersNode Members { get; set; } = new StructMembersNode();
    }

    public class StructMembersNode : ASTNode
    {
        public List<StructMemberNode> Members { get; set; } = new List<StructMemberNode>();
    }

    // عقدة عضو الهيكل
    public class StructMemberNode : ASTNode
    {
        public bool IsStatic { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public ExpressionNode InitialValue { get; set; }
    }

    // عقدة المتغير العالمي
    public class GlobalVariableNode : ASTNode
    {
        public string Type { get; set; }
        public List<VariableDeclNode> Variables { get; set; } = new List<VariableDeclNode>();
    }

    // عقدة تعريف المتغير
    public class VariableDeclNode : ASTNode
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public ExpressionNode InitialValue { get; set; }
    }

    // العقدة الأساسية للتعبير
    public abstract class ExpressionNode : ASTNode
    {
        public string Type { get; set; }
    }

    // تعبير ثنائي
    public class BinaryExpressionNode : ExpressionNode
    {
        public ExpressionNode Left { get; set; }
        public string Operator { get; set; }
        public ExpressionNode Right { get; set; }
    }

    // تعبير معرف
    public class IdentifierNode : ExpressionNode
    {
        public string Name { get; set; }
    }

    // تعبير عدد صحيح
    public class IntegerNode : ExpressionNode
    {
        public int Value { get; set; }
    }

    // تعبير عدد حقيقي
    public class RealNode : ExpressionNode
    {
        public double Value { get; set; }
    }

    // تعبير منطقي
    public class BooleanNode : ExpressionNode
    {
        public bool Value { get; set; }
    }

    // تعبير null
    public class NullNode : ExpressionNode
    {
    }

    // العقدة الأساسية للجمل
    public abstract class StatementNode : ASTNode
    {
    }

    // جملة if
    public class IfStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public StatementNode ThenStatement { get; set; }
        public StatementNode ElseStatement { get; set; }
    }

    // جملة while
    public class WhileStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public StatementNode Body { get; set; }
    }

    // جملة return
    public class ReturnStatementNode : StatementNode
    {
        public ExpressionNode Value { get; set; }
    }

    // جملة تعبير
    public class ExpressionStatementNode : StatementNode
    {
        public ExpressionNode Expression { get; set; }
    }

    // كتلة جمل
    public class BlockStatementNode : StatementNode
    {
        public List<StatementNode> Statements { get; set; } = new List<StatementNode>();
    }

    // عقدة تعريف المتغير 
    public class VariableDeclarationNode : StatementNode
    {
        public string Type { get; set; }
        public List<VariableDeclNode> Variables { get; set; } = new List<VariableDeclNode>();
    }

    // عقدة العملية الأحادية
    public class UnaryExpressionNode : ExpressionNode
    {
        public string Operator { get; set; } // "++", "--", "+", "-", "!"
        public ExpressionNode Operand { get; set; }
        public bool IsPrefix { get; set; } // true للبادئة، false للاحقة
    }

    // عقدة جملة for
    public class ForStatementNode : StatementNode
    {
        public ASTNode Initialization { get; set; }  // تعريف المتغيرات
        public ExpressionNode Condition { get; set; } // الشرط
        public ExpressionNode Update { get; set; }    // التحديث
        public StatementNode Body { get; set; }       // جسم for
    }
}
