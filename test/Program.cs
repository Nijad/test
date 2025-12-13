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
                // قراءة ملف الإدخال
                string inputFile = "Content//code.scl";// args[0];
                // قراءة محتوى الملف
                string inputCode = File.ReadAllText(inputFile);

                Console.WriteLine("start analysing...");
                Console.WriteLine();

                // التحليل اللغوي والتحليل التركيبي
                AntlrInputStream inputStream = new AntlrInputStream(inputCode);
                // إنشاء الـ Lexer والـ Parser
                CustomLexer lexer = new CustomLexer(inputStream);
                // إنشاء تيار الرموز من الـ Lexer
                CommonTokenStream tokenStream = new CommonTokenStream(lexer);
                // إنشاء الـ Parser
                SimpleParser parser = new SimpleParser(tokenStream);
                // إضافة مستمع للأخطاء النحوية
                ErrorListener errorListener = new ErrorListener();
                parser.AddErrorListener(errorListener);

                Console.WriteLine("syntax analysing is processing...");
                // بدء التحليل التركيبي
                SimpleParser.ProgramContext tree = parser.program();
                
                // التحقق من وجود أخطاء لغوية
                if (lexer.HasLexicalErrors)
                {
                    Console.WriteLine();
                    Console.WriteLine("Lexical Errors:");
                    foreach (string error in lexer.LexicalErrorsList)
                        Console.WriteLine(error);
                    return; // إيقاف البرنامج عند وجود أخطاء لغوية
                }

                // التحقق من وجود أخطاء نحوية
                if (errorListener.HasSyntaxErrors)
                {
                    Console.WriteLine();
                    Console.WriteLine("Syntax Errors:");
                    errorListener.PrintErrors();
                    return; // إيقاف البرنامج عند وجود أخطاء نحوية
                }

                Console.WriteLine("Syntax parsing was done successfuly!");
                Console.WriteLine();
                // البناء الدلالي
                SymbolTable symbolTable = new SymbolTable();
                List<string> semanticErrors = new List<string>();
                List<string> semanticWarnings = new List<string>();

                Console.WriteLine("building symbol table and semantic analysis...");
                // زيارة شجرة التحليل التركيبي لبناء جدول الرموز والتحليل الدلالي
                SimpleVisitor visitor = new SimpleVisitor(symbolTable, semanticErrors, semanticWarnings);
                visitor.Visit(tree);

                // طباعة الأخطاء الدلالية إذا وجدت (مع إيقاف البرنامج)
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

                // بناء شجرة التحليل التركيبي المجرد (AST)
                ASTBuilder astBuilder = new ASTBuilder(symbolTable);
                // زيارة شجرة التحليل التركيبي لبناء جدول الرموز والتحليل الدلالي
                ASTNode ast = astBuilder.Visit(tree);

                // توليد الشيفرة
                CodeGenerator codeGenerator = new CodeGenerator(symbolTable);
                string assemblyCode = codeGenerator.Generate(tree);
                // كتابة الشيفرة المولدة إلى ملف الإخراج
                string outputFile = Path.ChangeExtension(inputFile, ".asm");
                // حفظ الشيفرة المولدة في ملف
                File.WriteAllText(outputFile, assemblyCode);
                Console.WriteLine($"Code Generated Successfully");
                Console.WriteLine($"Output file : {outputFile}");

                Console.WriteLine("Program execution finished.");
            }
            catch (Exception ex)
            {
                // التقاط وطباعة أي استثناء غير متوقع
                Console.WriteLine($"error: {ex.Message}");
                Console.WriteLine($"details: {ex.StackTrace}");
            }
        }
    }
}