//using Antlr4.Runtime;
//using test.Content;

//namespace test
//{
//    internal class Program
//    {
//        public static void Main(string[] args)
//        {
//            try
//            {
//                //if (args.Length == 0)
//                //{
//                //    Console.WriteLine("Source file");
//                //    return;
//                //}

//                string inputFile = "Content//code.scl";// args[0];
//                string inputCode = File.ReadAllText(inputFile);

//                // التحليل المفرداتي
//                var inputStream = new AntlrInputStream(inputCode);
//                var lexer = new SimpleLexer(inputStream);
//                var tokenStream = new CommonTokenStream(lexer);

//                // التحليل القواعدي
//                var parser = new SimpleParser(tokenStream);
//                var errorListener = new ErrorListener();
//                parser.AddErrorListener(errorListener);

//                var tree = parser.program();

//                // عرض الأخطاء النحوية
//                if (errorListener.HasErrors)
//                {
//                    Console.WriteLine("Lexer Error:");
//                    errorListener.PrintErrors();
//                    return;
//                }

//                // التحليل الدلالي وبناء جدول الرموز
//                var symbolTable = new SymbolTable();
//                var semanticErrors = new List<string>();
//                var semanticVisitor = new SimpleVisitor(symbolTable, semanticErrors);
//                semanticVisitor.Visit(tree);

//                // عرض الأخطاء الدلالية
//                if (semanticErrors.Count > 0)
//                {
//                    Console.WriteLine("Semantic Errors:");
//                    foreach (var error in semanticErrors)
//                    {
//                        Console.WriteLine(error);
//                    }
//                    return;
//                }

//                // بناء شجرة الـ AST
//                var astBuilder = new ASTBuilder(symbolTable);
//                var ast = astBuilder.Visit(tree);

//                // طباعة جدول الرموز (لأغراض التصحيح)
//                symbolTable.PrintSymbolTable();

//                // توليد الكود
//                var codeGenerator = new CodeGenerator(symbolTable);
//                string assemblyCode = codeGenerator.Generate(tree);

//                string outputFile = Path.ChangeExtension(inputFile, ".asm");
//                File.WriteAllText(outputFile, assemblyCode);
//                Console.WriteLine($"Code Generated Succussfully: {outputFile}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error: {ex.Message}");
//                Console.WriteLine($"Error Details: {ex.StackTrace}");
//            }
//        }
//    }
//}



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
            // برنامج اختبار مع متغيرات في نطاقات مختلفة
            //string inputCode = @"program Test {
            //                        int globalVar = 10;

            //                        int main() {
            //                            int x = 5;
            //                            int y = globalVar;
            //                            return x + y;
            //                        }

            //                        void testFunction(int param) {
            //                            int localVar = param;
            //                            if (true) {
            //                                int blockVar = 20;
            //                                localVar = blockVar;
            //                            }
            //                        }
            //                    }";

            Console.WriteLine("بدء التحليل...");

            var inputStream = new AntlrInputStream(inputCode);
            var lexer = new SimpleLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);

            var parser = new SimpleParser(tokenStream);
            var errorListener = new ErrorListener();
            parser.AddErrorListener(errorListener);

            Console.WriteLine("جاري التحليل النحوي...");
            var tree = parser.program();

            if (errorListener.HasErrors)
            {
                Console.WriteLine("تم اكتشاف أخطاء نحوية:");
                errorListener.PrintErrors();
                return;
            }

            Console.WriteLine("✅ التحليل النحوي تم بنجاح!");

            var symbolTable = new SymbolTable();
            var semanticErrors = new List<string>();

            Console.WriteLine("جاري التحليل الدلالي وبناء جدول الرموز...");
            var visitor = new SimpleVisitor(symbolTable, semanticErrors);
            visitor.Visit(tree);

            // طباعة جدول الرموز الكامل
            symbolTable.PrintAllScopes();

            if (semanticErrors.Count > 0)
            {
                Console.WriteLine("❌ تم اكتشاف أخطاء دلالية:");
                foreach (var error in semanticErrors)
                {
                    Console.WriteLine(error);
                }
            }
            else
            {
                Console.WriteLine("✅ التحليل الدلالي تم بنجاح!");

                // اختبار البحث عن رموز
                Console.WriteLine("اختبار البحث في جدول الرموز:");
                TestSymbolLookup(symbolTable, "globalVar");
                TestSymbolLookup(symbolTable, "x");
                TestSymbolLookup(symbolTable, "main");
                TestSymbolLookup(symbolTable, "param");
                TestSymbolLookup(symbolTable, "unknownVar");

                // بناء شجرة الـ AST
                var astBuilder = new ASTBuilder(symbolTable);
                var ast = astBuilder.Visit(tree);

                // طباعة جدول الرموز (لأغراض التصحيح)
                //symbolTable.PrintSymbolTable();

                // توليد الكود
                CodeGenerator codeGenerator = new CodeGenerator(symbolTable);
                string assemblyCode = codeGenerator.Generate(tree);

                string outputFile = Path.ChangeExtension(inputFile, ".asm");
                File.WriteAllText(outputFile, assemblyCode);
                Console.WriteLine($"Code Generated Succussfully: {outputFile}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ خطأ: {ex.Message}");
            Console.WriteLine($"تفاصيل: {ex.StackTrace}");
        }
    }

    private static void TestSymbolLookup(SymbolTable symbolTable, string symbolName)
    {
        var symbol = symbolTable.Lookup(symbolName);
        if (symbol != null)
        {
            Console.WriteLine($"✅ وجد: {symbolName} -> {symbol.DataType} في نطاق {symbol.Scope}");
        }
        else
        {
            Console.WriteLine($"❌ لم يتم العثور على: {symbolName}");
        }
    }
}