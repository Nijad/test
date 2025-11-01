using test.Content;

namespace test
{
    public class SimpleDebugVisitor : SimpleBaseVisitor<object>
    {
        public override object VisitProgram(SimpleParser.ProgramContext context)
        {
            Console.WriteLine($"زيارة البرنامج: {context.IDENTIFIER()?.GetText()}");
            return base.VisitProgram(context);
        }

        public override object VisitFunction(SimpleParser.FunctionContext context)
        {
            Console.WriteLine($"زيارة الدالة: {context.IDENTIFIER()?.GetText()}");
            return base.VisitFunction(context);
        }

        public override object VisitExpression(SimpleParser.ExpressionContext context)
        {
            Console.WriteLine($"زيارة تعبير: {context.GetText()}");
            return base.VisitExpression(context);
        }
    }
}
