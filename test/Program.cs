// البرنامج الرئيسي: يدير خط أنابيب الترجمة البسيط
// الخطوات: التحليل اللغوي (Lexer) -> التحليل التركيبي (Parser) -> زيارة الشجرة للبناء الدلالي -> توليد الشيفرة

using Antlr4.Runtime;
using test.Content;

namespace test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Compiler start");
            try
            {
                string inputFile = "Content//code.scl";// args[0];
                string inputCode = File.ReadAllText(inputFile);

                Console.WriteLine("start analysing...");
                Console.WriteLine();
                AntlrInputStream inputStream = new AntlrInputStream(inputCode);
                CustomLexer lexer = new CustomLexer(inputStream);
                CommonTokenStream tokenStream = new CommonTokenStream(lexer);

                SimpleParser parser = new SimpleParser(tokenStream);
                ErrorListener errorListener = new ErrorListener();
                parser.AddErrorListener(errorListener);

                Console.WriteLine("syntax analysing is processing...");
                SimpleParser.ProgramContext tree = parser.program();

                if (lexer.HasLexicalErrors)
                {
                    Console.WriteLine();
                    Console.WriteLine("Lexical Errors:");
                    foreach (string error in lexer.LexicalErrorsList)
                        Console.WriteLine(error);
                    return; // إيقاف البرنامج عند وجود أخطاء لغوية
                }

                if (errorListener.HasSyntaxErrors)
                {
                    Console.WriteLine();
                    Console.WriteLine("Syntax Errors:");
                    errorListener.PrintErrors();
                    return; // إيقاف البرنامج عند وجود أخطاء نحوية
                }

                Console.WriteLine("Syntax parsing was done successfuly!");
                Console.WriteLine();
                SymbolTable symbolTable = new SymbolTable();
                List<string> semanticErrors = new List<string>();
                List<string> semanticWarnings = new List<string>();

                Console.WriteLine("building symbol table and semantic analysis...");
                SimpleVisitor visitor = new SimpleVisitor(symbolTable, semanticErrors, semanticWarnings);
                visitor.Visit(tree);

                if (semanticErrors.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Semantic Errors:");
                    foreach (string error in semanticErrors)
                        Console.WriteLine(error);
                    return; // إيقاف البرنامج عند وجود أخطاء دلالية
                }
                
                // طباعة التحذيرات إذا وجدت (بدون إيقاف البرنامج)
                if (semanticWarnings.Count == 0)
                    Console.WriteLine("Semantic analysis was done successfully!");
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Semantic Warnings:");
                    foreach (string warning in semanticWarnings)
                        Console.WriteLine(warning);
                    Console.WriteLine();
                    Console.WriteLine("Semantic analysis was done with warnings!");
                }
                Console.WriteLine();
                // طباعة جدول الرموز الكامل
                //symbolTable.PrintAllScopes();

                // بناء شجرة الـ AST
                ASTBuilder astBuilder = new ASTBuilder(symbolTable);
                ASTNode ast = astBuilder.Visit(tree);

                // توليد الكود
                CodeGenerator codeGenerator = new CodeGenerator(symbolTable);
                string assemblyCode = codeGenerator.Generate(tree);

                string outputFile = Path.ChangeExtension(inputFile, ".asm");
                File.WriteAllText(outputFile, assemblyCode);
                Console.WriteLine($"Code Generated Succussfully");
                Console.WriteLine($"Output file : {outputFile}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {ex.Message}");
                Console.WriteLine($"details: {ex.StackTrace}");
            }
        }
    }
}