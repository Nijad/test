using Antlr4.Runtime;
using test.Content;

namespace test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                string inputFile = "Content//code.scl";// args[0];
                string inputCode = File.ReadAllText(inputFile);

                Console.WriteLine("start analysing...");

                AntlrInputStream inputStream = new AntlrInputStream(inputCode);
                SimpleLexer lexer = new SimpleLexer(inputStream);
                CommonTokenStream tokenStream = new CommonTokenStream(lexer);

                SimpleParser parser = new SimpleParser(tokenStream);
                ErrorListener errorListener = new ErrorListener();
                parser.AddErrorListener(errorListener);

                Console.WriteLine("syntax analysing is processing...");
                SimpleParser.ProgramContext tree = parser.program();

                if (errorListener.HasErrors)
                {
                    Console.WriteLine("Syntax Error:");
                    errorListener.PrintErrors();
                    return;
                }

                Console.WriteLine("Syntax parsing was done successfuly!");

                SymbolTable symbolTable = new SymbolTable();
                List<string> semanticErrors = new List<string>();
                List<string> semanticWarnings = new List<string>();

                Console.WriteLine("building symbol table and semantic analysis...");
                SimpleVisitor visitor = new SimpleVisitor(symbolTable, semanticErrors, semanticWarnings);
                visitor.Visit(tree);

                // طباعة جدول الرموز الكامل
                symbolTable.PrintAllScopes();

                if (semanticErrors.Count > 0)
                {
                    Console.WriteLine("Semantic Error:");
                    foreach (string error in semanticErrors)
                        Console.WriteLine(error);
                    return;
                }

                // طباعة التحذيرات إذا وجدت (بدون إيقاف البرنامج)
                if (semanticWarnings.Count > 0)
                {
                    Console.WriteLine("Semantic Warnings:");
                    foreach (string warning in semanticWarnings)
                        Console.WriteLine(warning);
                    Console.WriteLine("Semantic analysis was done with wornings!");
                }
                else
                    Console.WriteLine("Semantic analysis was done successfuly!");

                // بناء شجرة الـ AST
                ASTBuilder astBuilder = new ASTBuilder(symbolTable);
                ASTNode ast = astBuilder.Visit(tree);

                // توليد الكود
                CodeGenerator codeGenerator = new CodeGenerator(symbolTable);
                string assemblyCode = codeGenerator.Generate(tree);

                string outputFile = Path.ChangeExtension(inputFile, ".asm");
                File.WriteAllText(outputFile, assemblyCode);
                Console.WriteLine($"Code Generated Succussfully: {outputFile}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {ex.Message}");
                Console.WriteLine($"details: {ex.StackTrace}");
            }
        }
    }
}