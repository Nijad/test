using Antlr4.Runtime;
using test.Content;

namespace test
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                //if (args.Length == 0)
                //{
                //    Console.WriteLine("Source file");
                //    return;
                //}

                string inputFile = "Content//code.scl";// args[0];
                string inputCode = File.ReadAllText(inputFile);

                // التحليل المفرداتي
                var inputStream = new AntlrInputStream(inputCode);
                var lexer = new SimpleLexer(inputStream);
                var tokenStream = new CommonTokenStream(lexer);

                // التحليل القواعدي
                var parser = new SimpleParser(tokenStream);
                var errorListener = new ErrorListener();
                parser.AddErrorListener(errorListener);

                var tree = parser.program();

                // عرض الأخطاء النحوية
                if (errorListener.HasErrors)
                {
                    Console.WriteLine("Lexer Error:");
                    errorListener.PrintErrors();
                    return;
                }

                // التحليل الدلالي وبناء جدول الرموز
                var symbolTable = new SymbolTable();
                var semanticErrors = new List<string>();
                var semanticVisitor = new SimpleVisitor(symbolTable, semanticErrors);
                semanticVisitor.Visit(tree);

                // عرض الأخطاء الدلالية
                if (semanticErrors.Count > 0)
                {
                    Console.WriteLine("Semantic Errors:");
                    foreach (var error in semanticErrors)
                    {
                        Console.WriteLine(error);
                    }
                    return;
                }

                // بناء شجرة الـ AST
                var astBuilder = new ASTBuilder(symbolTable);
                var ast = astBuilder.Visit(tree);

                // طباعة جدول الرموز (لأغراض التصحيح)
                symbolTable.PrintSymbolTable();

                // توليد الكود
                var codeGenerator = new CodeGenerator(symbolTable);
                string assemblyCode = codeGenerator.Generate(tree);

                string outputFile = Path.ChangeExtension(inputFile, ".asm");
                File.WriteAllText(outputFile, assemblyCode);
                Console.WriteLine($"Code Generated Succussfully: {outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Error Details: {ex.StackTrace}");
            }
        }
    }
}
