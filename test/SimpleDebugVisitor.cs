using test.Content;

namespace test
{
    public class SimpleDebugVisitor : SimpleBaseVisitor<object>
    {
        public override object VisitProgram(SimpleParser.ProgramContext context)
        {
            Console.WriteLine($"visiting program: {context.IDENTIFIER()?.GetText()}");
            return base.VisitProgram(context);
        }

        public override object VisitFunction(SimpleParser.FunctionContext context)
        {
            Console.WriteLine($"visiting function: {context.IDENTIFIER()?.GetText()}");
            return base.VisitFunction(context);
        }

        public override object VisitExpression(SimpleParser.ExpressionContext context)
        {
            Console.WriteLine($"visiting expression: {context.GetText()}");
            return base.VisitExpression(context);
        }
    }
}
