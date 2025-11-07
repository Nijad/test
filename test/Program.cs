using Antlr4.Runtime;
using test;
using test.Content;

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

            Console.WriteLine("building symbol table and semantic analysis...");
            SimpleVisitor visitor = new SimpleVisitor(symbolTable, semanticErrors);
            visitor.Visit(tree);

            // طباعة جدول الرموز الكامل
            symbolTable.PrintAllScopes();

            if (semanticErrors.Count > 0)
            {
                Console.WriteLine("Semantic Error:");
                foreach (var error in semanticErrors)
                    Console.WriteLine(error);
                return;
            }

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
            Console.WriteLine($"❌ error: {ex.Message}");
            Console.WriteLine($"details: {ex.StackTrace}");
        }
    }
}