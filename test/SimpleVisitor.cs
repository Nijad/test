using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using test.Content;

namespace test
{
    public class SimpleVisitor : SimpleBaseVisitor<object>
    {
        private SymbolTable symbolTable;
        private List<string> semanticErrors;
        private List<string> semanticWarnings;
        private string currentFunctionReturnType;
        private HashSet<ParserRuleContext> visitedNodes;

        public SimpleVisitor(SymbolTable symbolTable, List<string> semanticErrors, List<string> semanticWarnings)
        {
            this.symbolTable = symbolTable;
            this.semanticErrors = semanticErrors;
            this.semanticWarnings = semanticWarnings;
            visitedNodes = new HashSet<ParserRuleContext>();
        }

        private void AddSemanticWarning(string message, ParserRuleContext context)
        {
            string warning = $"Semantic warning at line {context.Start.Line}, column {context.start.Column}: {message}";
            semanticWarnings.Add(warning);
            Console.WriteLine($"{warning}");
        }

        public override object VisitProgram([NotNull] SimpleParser.ProgramContext context)
        {
            Console.WriteLine("=== All Symbols in symbol table ===");
            symbolTable.PrintAllSymbols();
            symbolTable.EnterScope("global");

            string programName = context.IDENTIFIER().GetText();
            Console.WriteLine($"start program: {programName}");

            // المرحلة 1: جمع جميع التعريفات أولاً (الدوال، المتغيرات العالمية، الهياكل)
            foreach (SimpleParser.MemberContext? member in context.member())
                CollectDeclarations(member);

            // المرحلة 2: التحقق من الأجسام والتعبيرات
            foreach (SimpleParser.MemberContext? member in context.member())
                VisitMemberBody(member);

            symbolTable.ExitScope();
            return null;
        }

        // جمع التعريفات دون زيارة الأجسام
        private void CollectDeclarations(SimpleParser.MemberContext context)
        {
            if (context.function() != null)
                CollectFunctionDeclaration(context.function());
            else if (context.@struct() != null)
                CollectStructDeclaration(context.@struct());
            else if (context.global() != null)
                Visit(context.global()); // 
        }

        // جمع تعريف الدالة دون زيارة الجسم
        private void CollectFunctionDeclaration(SimpleParser.FunctionContext context)
        {
            string returnType = context.type()?.GetText() ?? "void";
            string functionName = context.IDENTIFIER().GetText();

            Symbol functionSymbol = new Symbol(
                functionName,
                "function",
                returnType,
                context.Start.Line,
                context.Start.Column,
                symbolTable.CurrentScope
            );

            if (!symbolTable.AddSymbol(functionSymbol))
                AddSemanticError($"function '{functionName}' is already defined", context);

            // جمع الباراميترات أيضاً
            if (context.arguments() != null)
                foreach (SimpleParser.ArgumentContext? arg in context.arguments().argument())
                {
                    string argName = arg.IDENTIFIER().GetText();
                    string type = arg.type().GetText();

                    Symbol argSymbol = new Symbol(
                        argName,
                        "parameter",
                        type,
                        arg.Start.Line,
                        arg.Start.Column,
                        $"{functionName}_scope"
                    );
                }
        }

        // جمع تعريف الهيكل دون زيارة الأعضاء
        private void CollectStructDeclaration(SimpleParser.StructContext context)
        {
            string structName = context.IDENTIFIER(0).GetText();

            // التحقق من وجود الهيكل الأب إذا كان موجوداً
            if (context.IDENTIFIER(1) != null)
            {
                string parentName = context.IDENTIFIER(1).GetText();
                SimpleParser.ProgramContext programContext = GetProgramContext(context);
                // التحقق من تعريف الهيكل الأب
                if (!IsStructDefined(parentName, programContext))
                    AddSemanticError($"Struct parent '{parentName}' is not declared", context);
                else
                    Console.WriteLine($"Struct '{structName}' inherits from '{parentName}'");
            }
            // إنشاء رمز الهيكل
            Symbol structSymbol = new Symbol(
                structName,
                "struct",
                structName,
                context.Start.Line,
                context.Start.Column,
                symbolTable.CurrentScope
            );
            // إضافة الهيكل إلى جدول الرموز
            if (!symbolTable.AddSymbol(structSymbol))
                AddSemanticError($"الهيكل '{structName}' معرّف مسبقاً", context);
        }

        // الحصول على جميع أعضاء الهيكل، بما في ذلك الأعضاء الموروثة
        private List<string> GetAllStructMembers(SimpleParser.StructContext structContext)
        {
            List<string> members = new List<string>();
            // إضافة الأعضاء المباشرين
            if (structContext?.struct_members() != null)
                foreach (IParseTree? child in structContext.struct_members().children)
                    if (child is SimpleParser.Struct_memberContext memberContext)
                    {
                        SimpleParser.VariableContext variable = memberContext.variable();
                        if (variable?.IDENTIFIER() != null)
                            members.Add(variable.IDENTIFIER().GetText());
                    }

            // إضافة الأعضاء الموروثة
            if (structContext?.IDENTIFIER(1) != null)
            {
                string parentName = structContext.IDENTIFIER(1).GetText();
                SimpleParser.StructContext parentStruct = FindStructDefinition(parentName, structContext);
                if (parentStruct != null)
                    members.AddRange(GetAllStructMembers(parentStruct));
            }

            return members;
        }

        private void DebugStructAccess(SimpleParser.ExpressionContext context)
        {
            Console.WriteLine($"=== Access to Structs ===");
            Console.WriteLine($"Expression: {context.GetText()}");

            if (context.DOT() != null && context.expression().Length == 1 && context.IDENTIFIER() != null)
            {
                SimpleParser.ExpressionContext baseExpr = context.expression(0);
                string baseType = GetExpressionType(baseExpr);
                string memberName = context.IDENTIFIER().GetText();

                Console.WriteLine($"Base Type: {baseType}");
                Console.WriteLine($"Member Name: {memberName}");
                Console.WriteLine($"Is Base Type Struct?: {IsStructType(baseType)}");

                if (IsStructType(baseType))
                {
                    string structName = GetStructNameFromType(baseType);
                    Console.WriteLine($"Struct Name: {structName}");

                    SimpleParser.StructContext? structDef = FindStructDefinition(structName, context);
                    Console.WriteLine($"Struct Definition: {(structDef != null ? "Exists" : "Does not exist")}");

                    if (structDef != null)
                    {
                        string memberType = GetStructMemberType(structDef, memberName);
                        Console.WriteLine($"Member Type: {memberType ?? "Does not exist"}");

                        if (memberType == null)
                        {
                            memberType = FindMemberInParentStructs(structDef, memberName, context);
                            Console.WriteLine($"Member Type after searching in parents: {memberType ?? "Does not exist"}");
                        }
                    }
                }

                if (baseExpr.DOT() != null)
                {
                    Console.WriteLine($"Base expression itself has a dot: {baseExpr.GetText()}");
                    string baseBaseType = GetExpressionType(baseExpr.expression(0));
                    Console.WriteLine($"Base Base Type: {baseBaseType}");
                }
            }
            Console.WriteLine($"===================");
        }

        private bool IsStructDefined(string structName, SimpleParser.ProgramContext program)
        {
            return FindStructInProgram(program, structName) != null;
        }

        // زيارة أجسام الأعضاء بعد جمع جميع التعريفات
        private void VisitMemberBody(SimpleParser.MemberContext context)
        {
            if (context.function() != null)
                VisitFunctionBody(context.function());
            else if (context.@struct() != null)
                Visit(context.@struct());
        }

        // زيارة جسم الدالة بعد جمع جميع التعريفات
        private void VisitFunctionBody(SimpleParser.FunctionContext context)
        {
            string functionName = context.IDENTIFIER().GetText();
            string returnType = context.type()?.GetText() ?? "void";

            currentFunctionReturnType = returnType;
            symbolTable.EnterScope(functionName);

            // إضافة الباراميترات إلى نطاق الدالة
            if (context.arguments() != null)
                foreach (SimpleParser.ArgumentContext? arg in context.arguments().argument())
                {
                    string argName = arg.IDENTIFIER().GetText();
                    string type = arg.type().GetText();

                    Symbol argSymbol = new Symbol(
                        argName,
                        "parameter",
                        type,
                        arg.Start.Line,
                        arg.Start.Column,
                        symbolTable.CurrentScope
                    );

                    if (!symbolTable.AddSymbol(argSymbol))
                        AddSemanticError($"Identifier '{argName}' is already defined", arg);
                }

            // زيارة جمل الدالة
            foreach (SimpleParser.StatementContext? stmt in context.statement())
                Visit(stmt);

            // التحقق من إرجاع القيمة
            if (returnType != "void" && !CheckAllPathsReturn(context))
                AddSemanticError($"Function '{functionName}' does not returne value in all paths", context);

            symbolTable.ExitScope();
            currentFunctionReturnType = null;
        }


        public override object VisitFunction([NotNull] SimpleParser.FunctionContext context)
        {
            string returnType = context.type()?.GetText() ?? "void";
            string functionName = context.IDENTIFIER().GetText();

            currentFunctionReturnType = returnType;

            // الدخول إلى نطاق الدالة
            symbolTable.EnterScope($"func_{functionName}");

            // إضافة الدالة إلى النطاق العالمي
            Symbol functionSymbol = new Symbol(
                functionName,
                "function",
                returnType,
                context.Start.Line,
                context.Start.Column,
                "global" // الدالة في النطاق العالمي
            );

            if (!symbolTable.AddSymbol(functionSymbol))
                AddSemanticError($"the function '{functionName}' is already declared", context);

            // معالجة الباراميترات (تضاف إلى نطاق الدالة)
            if (context.arguments() != null)
                Visit(context.arguments());

            // معالجة الجمل داخل الدالة
            foreach (SimpleParser.StatementContext? stmt in context.statement())
                Visit(stmt);

            symbolTable.ExitScope();
            currentFunctionReturnType = null;
            return null;
        }

        private bool CheckAllPathsReturn(SimpleParser.FunctionContext context)
        {
            bool hasReturn = false;

            foreach (SimpleParser.StatementContext? stmt in context.statement())
                if (HasReturnStatement(stmt))
                    hasReturn = true;
                else if (stmt.IF() != null && stmt.statement().Length >= 1)
                    // إذا كان هناك if مع else، تحقق من أن كلا الفرعين يرجعان قيمة
                    if (stmt.statement().Length == 2)
                    {
                        bool thenReturns = HasReturnStatement(stmt.statement(0));
                        bool elseReturns = HasReturnStatement(stmt.statement(1));
                        if (thenReturns && elseReturns)
                            hasReturn = true;
                    }

            return hasReturn;
        }

        private bool HasReturnStatement(SimpleParser.StatementContext context)
        {
            if (context.RETURN() != null)
                return true;

            // البحث في الكتلة
            if (context.LBRACE() != null)
                foreach (SimpleParser.StatementContext? stmt in context.statement())
                    if (HasReturnStatement(stmt))
                        return true;

            // if مع else - يجب أن يرجع كلا الفرعين
            if (context.IF() != null && context.statement().Length == 2)
                return HasReturnStatement(context.statement(0)) &&
                       HasReturnStatement(context.statement(1));

            return false;
        }

        public override object VisitArgument([NotNull] SimpleParser.ArgumentContext context)
        {
            string type = context.type().GetText();
            string argName = context.IDENTIFIER().GetText();

            Symbol argSymbol = new Symbol(
                argName,
                "parameter",
                type,
                context.Start.Line,
                context.Start.Column,
                symbolTable.CurrentScope
            );

            if (!symbolTable.AddSymbol(argSymbol))
                AddSemanticError($"identifier '{argName}' is already declared", context);

            return null;
        }

        public override object VisitGlobal([NotNull] SimpleParser.GlobalContext context)
        {
            string type = context.type().GetText();

            foreach (SimpleParser.VariableContext? variable in context.variables().variable())
            {
                string varName = variable.IDENTIFIER().GetText();

                Symbol globalSymbol = new Symbol(
                    varName,
                    "global",
                    type,
                    variable.Start.Line,
                    variable.Start.Column,
                    symbolTable.CurrentScope
                );

                if (!symbolTable.AddSymbol(globalSymbol))
                    AddSemanticError($"glogabl variable '{varName}' is already declared", variable);

                // التحقق من التعبير إذا وجد
                if (variable.expression() != null)
                {
                    string exprType = GetExpressionType(variable.expression());
                    if (exprType != null && !AreTypesCompatible(type, exprType, context))
                        AddSemanticError($"type mismatch: cannot assign {exprType} to {type}", variable);
                }
            }
            return null;
        }

        public override object VisitVariable([NotNull] SimpleParser.VariableContext context)
        {
            string varName = context.IDENTIFIER().GetText();
            string varType = GetVariableTypeFromContext(context);

            if (varType != null)
            {
                // تطبيع نوع الهيكل
                varType = NormalizeStructType(varType, context);
                Symbol varSymbol = new Symbol(
                    varName,
                    "local",
                    varType,
                    context.Start.Line,
                    context.Start.Column,
                    symbolTable.CurrentScope
                );

                // إضافة المتغير إلى جدول الرموز
                //التحقق من عدم إعادة التعريف في نفس النطاق ضمناً
                if (!symbolTable.AddSymbol(varSymbol))
                    AddSemanticError($"variable '{varName}' is already declared", context);

                // التحقق من التعبير إذا وجد
                if (context.expression() != null)
                {
                    // **زيارة التعبير أولاً لضمان التحقق من جميع الأخطاء**
                    Visit(context.expression());

                    string exprType = GetExpressionType(context.expression());
                    exprType = NormalizeStructType(exprType, context);

                    if (exprType != null && !AreTypesCompatible(varType, exprType, context))
                        AddSemanticError($"type mismatch: cannot assign {exprType} to {varType}", context);

                    return exprType;
                }
            }

            return varType;
        }

        public override object VisitExpression([NotNull] SimpleParser.ExpressionContext context)
        {
            // تفعيل التصحيح للتعيينات
            DebugAssignment(context);
            if (context.expr_list() != null)
                foreach (SimpleParser.ExpressionContext? expr in context.expr_list().expression())
                    Visit(expr);

            // استدعاء الدوال
            if (context.IDENTIFIER() != null && context.LPAREN() != null)
            {
                string functionName = context.IDENTIFIER().GetText();
                Symbol functionSymbol = symbolTable.Lookup(functionName);// البحث عن تعريف الدالة

                if (functionSymbol == null) // الدالة غير معرفة
                {
                    AddSemanticError($"Function '{functionName}' is not declared", context);
                    return "unknown";
                }

                if (functionSymbol.Type != "function")// ليس دالة
                {
                    AddSemanticError($"'{functionName}' is not function", context);
                    return "unknown";
                }

                // الحصول على المعاملات الفعلية الممررة
                SimpleParser.ExpressionContext[] actualParams = context.expr_list()?.expression() ?? new SimpleParser.ExpressionContext[0];

                // الحصول على المعاملات المتوقعة من تعريف الدالة
                List<Symbol> expectedParams = GetFunctionParameters(functionName, context);

                // التحقق من عدد المعاملات
                if (actualParams.Length != expectedParams.Count)
                {
                    AddSemanticError($"Incorrect number of arguments in function call '{functionName}'. Expected: {expectedParams.Count}, Provided: {actualParams.Length}", context);
                    return functionSymbol.DataType;
                }

                // التحقق من توافق أنواع المعاملات
                for (int i = 0; i < actualParams.Length; i++)
                {
                    Visit(actualParams[i]); // زيارة المعامل للتأكد من صحته
                    string actualType = GetExpressionType(actualParams[i]);
                    string expectedType = expectedParams[i].DataType;

                    if (!AreTypesCompatible(expectedType, actualType, context))
                    {
                        AddSemanticError($"Argument type mismatch at position {i + 1} in function call '{functionName}'. Expected: {expectedType}, Provided: {actualType}", context);
                    }
                }

                return functionSymbol.DataType;
            }

            // التعيين لأعضاء الهياكل (كتابة) - مثل circle.id = 1
            if (context.DOT() != null && context.expression().Length == 2 && context.IDENTIFIER() != null && context.ASSIGN() != null)
            {
                // زيارة التعبير الأساسي أولاً (مثل circle في circle.id = 1)
                Visit(context.expression(0));

                string baseType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();

                if (!IsStructType(baseType))
                {
                    AddSemanticError($"Cannot access members of type '{baseType}'", context);
                    return "unknown";
                }

                string structName = GetStructNameFromType(baseType);
                SimpleParser.StructContext structDef = FindStructDefinition(structName, context);

                if (structDef == null)
                {
                    AddSemanticError($"Struct '{structName}' is not defined", context);
                    return "unknown";
                }

                // الحصول على نوع العضو
                string memberType = GetStructMemberType(structDef, memberName);
                // البحث في الهياكل الأب
                if (memberType == null)
                    memberType = FindMemberInParentStructs(structDef, memberName, context);

                if (memberType == null)
                {
                    AddSemanticError($"Member '{memberName}' is not found in struct '{structName}'", context);
                    return "unknown";
                }

                // زيارة التعبير الأيمن والتحقق من التوافق
                string rightType = GetExpressionType(context.expression(1));
                Visit(context.expression(1));

                if (!AreTypesCompatible(memberType, rightType, context))
                    AddSemanticError($"Member type '{memberName}' is not compatible. Expected: {memberType}, Provided: {rightType}", context);

                return memberType;
            }

            // معالجة العامل المنطقي NOT (!)
            if (context.expression().Length == 1 && context.GetChild(0) is ITerminalNode terminalNode &&
                terminalNode.Symbol.Type == SimpleParser.NOT)
            {
                string operandType = GetExpressionType(context.expression(0));
                Visit(context.expression(0));

                if (operandType != "boolean" && operandType != "unknown")
                    AddSemanticError($"Operator ! can only be applied to boolean expressions, not {operandType}", context);

                return "boolean";
            }

            if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                // التحقق من الطرف الأيسر في التعيين
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));

                // زيارة الطرفين
                Visit(context.expression(0));
                Visit(context.expression(1));

                // تحسين: التحقق من أن الطرف الأيسر يمكن تعيينه
                if (leftType == "unknown")
                    AddSemanticError($"Variable is not defined on the left-hand side", context.expression(0));

                if (!AreTypesCompatible(leftType, rightType, context))
                    AddSemanticError($"Type mismatch in assignment. Expected: {leftType}, Provided: {rightType}", context);

                return leftType;
            }

            // معالجة الوصول إلى أعضاء الهياكل (قراءة)
            if (context.DOT() != null && context.expression().Length == 1 && context.IDENTIFIER() != null && context.ASSIGN() == null)
            {
                // زيارة التعبير الأساسي أولاً
                Visit(context.expression(0));

                string baseType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();

                if (!IsStructType(baseType))
                {
                    AddSemanticError($"Cannot access members of type '{baseType}'", context);
                    return "unknown";
                }

                string structName = GetStructNameFromType(baseType);
                SimpleParser.StructContext structDef = FindStructDefinition(structName, context);

                if (structDef == null)
                {
                    AddSemanticError($"Struct '{structName}' is not defined", context);
                    return "unknown";
                }

                string memberType = GetStructMemberType(structDef, memberName);
                // البحث في الهياكل الأب
                if (memberType == null)
                    memberType = FindMemberInParentStructs(structDef, memberName, context);

                if (memberType == null)
                {
                    AddSemanticError($"Member '{memberName}' is not found in struct '{structName}'", context);
                    return "unknown";
                }

                return memberType;
            }

            if (context.INTEGER() != null)
                return "int";

            if (context.REAL() != null)
                return "double";

            if (context.TRUE() != null || context.FALSE() != null)
                return "boolean";

            if (context.NULL() != null)
                return "null";

            // معالجة المعرفات البسيطة
            if (context.IDENTIFIER() != null && context.expression().Length == 0)
            {
                string varName = context.IDENTIFIER().GetText();
                Symbol symbol = symbolTable.Lookup(varName);

                if (symbol != null)
                    return symbol.DataType;

                AddSemanticError($"Identifier '{varName}' is not declared", context);
                return null;
            }

            // تعبيرات أحادية
            if (context.expression().Length == 1 && context.ASSIGN() == null)
                return Visit(context.expression(0));

            if (context.binaryOp() != null && context.expression().Length == 2)
            {
                // تعبيرات ثنائية
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));
                string op = context.binaryOp().GetText();

                // زيارة كلا الطرفين
                Visit(context.expression(0));
                Visit(context.expression(1));

                string resultType = GetBinaryOperationResultType(leftType, rightType, op);
                if (resultType != null)
                    return resultType;

                return "unknown";
            }

            if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));

                if (leftType != null && rightType != null && !AreTypesCompatible(leftType, rightType, context))
                    AddSemanticError($"type mismatch: {leftType} and {rightType}", context);

                return leftType;
            }

            if (context.INCREMENT() != null || context.DECREMENT() != null)
                return VisitIncrementDecrementExpression(context);

            //return null;
            return base.VisitExpression(context);
        }

        private void DebugAssignment(SimpleParser.ExpressionContext context)
        {
            if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                Console.WriteLine($"=== Assignment Analysis ===");
                Console.WriteLine($"Full expression: {context.GetText()}");

                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));

                Console.WriteLine($"Left side: {context.expression(0).GetText()} -> {leftType}");
                Console.WriteLine($"Right side: {context.expression(1).GetText()} -> {rightType}");
                Console.WriteLine($"Compatible: {AreTypesCompatible(leftType, rightType, context)}");

                // إذا كان الطرف الأيسر وصولاً إلى عضو هيكل
                if (context.expression(0).DOT() != null)
                {
                    SimpleParser.ExpressionContext leftExpr = context.expression(0);
                    string baseType = GetExpressionType(leftExpr.expression(0));
                    string memberName = leftExpr.IDENTIFIER().GetText();

                    Console.WriteLine($"Access to member: {memberName}");
                    Console.WriteLine($"Base type: {baseType}");
                    Console.WriteLine($"Is base type a struct?: {IsStructType(baseType)}");

                    if (IsStructType(baseType))
                    {
                        string structName = GetStructNameFromType(baseType);
                        SimpleParser.StructContext? structDef = FindStructDefinition(structName, context);
                        Console.WriteLine($"Struct definition: {(structDef != null ? "Exists" : "Does not exist")}");

                        if (structDef != null)
                        {
                            string memberType = GetStructMemberType(structDef, memberName);
                            Console.WriteLine($"Member type: {memberType ?? "Does not exist"}");
                        }
                    }
                }
                Console.WriteLine($"=== End of Assignment Analysis ===");
            }
        }

        private void DebugStructInheritance(SimpleParser.StructContext structContext, string memberName)
        {
            Console.WriteLine($"Searching for member '{memberName}' in struct '{structContext.IDENTIFIER(0)?.GetText()}'");

            // البحث في الأعضاء المباشرين
            if (structContext.struct_members() != null)
                foreach (IParseTree? child in structContext.struct_members().children)
                    if (child is SimpleParser.Struct_memberContext memberContext)
                    {
                        SimpleParser.VariableContext variable = memberContext.variable();
                        if (variable?.IDENTIFIER()?.GetText() == memberName)
                        {
                            Console.WriteLine($"Found member '{memberName}' in current struct");
                            return;
                        }
                    }

            // البحث في الهيكل الأب
            if (structContext.IDENTIFIER(1) != null)
            {
                string parentName = structContext.IDENTIFIER(1).GetText();
                Console.WriteLine($"Searching in parent struct: '{parentName}'");
            }
            else
                Console.WriteLine($"No parent struct");
        }

        private List<Symbol> GetFunctionParameters(string functionName, SimpleParser.ExpressionContext context)
        {
            List<Symbol> parameters = new List<Symbol>();

            SimpleParser.FunctionContext functionDefinition = FindFunctionDefinition(functionName, context);

            if (functionDefinition != null && functionDefinition.arguments() != null)
                foreach (SimpleParser.ArgumentContext? arg in functionDefinition.arguments().argument())
                {
                    string paramName = arg.IDENTIFIER().GetText();
                    string paramType = arg.type().GetText();

                    // استخدام النوع الطبيعي بدون بادئة
                    paramType = NormalizeStructType(paramType, context);

                    parameters.Add(new Symbol(
                        paramName,
                        "parameter",
                        paramType,
                        arg.Start.Line,
                        arg.Start.Column,
                        $"{functionName}_params"
                    ));
                }

            return parameters;
        }

        private SimpleParser.FunctionContext FindFunctionDefinition(string functionName, SimpleParser.ExpressionContext currentContext)
        {
            // البحث في الشجرة عن تعريف الدالة
            SimpleParser.ProgramContext program = GetProgramContext(currentContext);

            if (program != null)
                foreach (SimpleParser.MemberContext? member in program.member())
                    if (member.function() != null)
                    {
                        SimpleParser.FunctionContext func = member.function();
                        if (func.IDENTIFIER().GetText() == functionName)
                            return func;
                    }

            // البحث في النطاق العالمي لجدول الرموز
            Symbol symbol = symbolTable.LookupGlobal(functionName);

            // إذا وجدنا الرمز لكن لا يمكننا العثور على التعريف، نعيد null
            // وسيتم التعامل مع الخطأ في مكان آخر
            if (symbol != null && symbol.Type == "function")
                return null;

            return null;
        }

        private SimpleParser.ProgramContext GetProgramContext(ParserRuleContext context)
        {
            if (context == null) return null;

            // إذا كان السياق الحالي هو البرنامج نفسه
            if (context is SimpleParser.ProgramContext programContext)
                return programContext;

            // البحث التصاعدي في الشجرة
            RuleContext parent = context.Parent;
            while (parent != null)
            {
                if (parent is SimpleParser.ProgramContext parentProgram)
                    return parentProgram;
                parent = parent.Parent;
            }

            return null;
        }

        public override object VisitStatement([NotNull] SimpleParser.StatementContext context)
        {
            if (context.IF() != null)
                return VisitIfStatement(context);

            if (context.WHILE() != null)
                return VisitWhileStatement(context);

            if (context.FOR() != null)
                return VisitForStatement(context);

            if (context.RETURN() != null)
                return VisitReturnStatement(context);

            if (context.expression() != null && context.expression().Length > 0)
                return Visit(context.expression(0));

            // تعريف متغير محلي
            if (context.type() != null && context.variables() != null)
            {
                Visit(context.type());
                Visit(context.variables());
                return null;
            }

            // كتلة من الجمل - ندخل نطاق جديد للكتلة
            if (context.LBRACE() != null)
            {
                symbolTable.EnterScope($"block_{context.Start.Line}");
                foreach (SimpleParser.StatementContext? stmt in context.statement())
                    Visit(stmt);
                symbolTable.ExitScope();
                return null;
            }

            return null;
        }

        private object VisitReturnStatement(SimpleParser.StatementContext context)
        {
            if (context.expression() != null && context.expression().Length > 0)
            {
                SimpleParser.ExpressionContext expression = context.expression(0);

                // تفعيل نظام التصحيح للهياكل
                DebugStructAccess(expression);

                // زيارة التعبير أولاً
                Visit(expression);

                string returnType = GetExpressionType(expression);

                if (returnType == "unknown")
                    AddSemanticError("The expression type could not be determined in the return statement.", context);
                else if (currentFunctionReturnType != "void" && !AreTypesCompatible(currentFunctionReturnType, returnType, context))
                    if (currentFunctionReturnType != "int" || returnType != "double")
                        AddSemanticError($"The return type {returnType} does not match the function {currentFunctionReturnType}", context);
            }
            else if (currentFunctionReturnType != "void")
                AddSemanticError("The function must return value", context);

            return null;
        }

        private string GetVariableTypeFromContext(SimpleParser.VariableContext context)
        {
            RuleContext parent = context.Parent;
            while (parent != null)
            {
                if (parent is SimpleParser.GlobalContext globalContext)
                    return globalContext.type().GetText();
                else if (parent is SimpleParser.VariablesContext variablesContext)
                {
                    RuleContext grandParent = variablesContext.Parent;
                    if (grandParent is SimpleParser.GlobalContext global)
                        return global.type().GetText();
                    else if (grandParent is SimpleParser.StatementContext statement)
                        // متغير محلي داخل دالة
                        return statement.type().GetText();
                    else if (grandParent is SimpleParser.Struct_memberContext structMember)
                        // عضو في هيكل
                        return structMember.type().GetText();
                }
                parent = parent.Parent;
            }
            return null;
        }

        private string GetExpressionType(SimpleParser.ExpressionContext context)
        {
            if (context == null) return "unknown";

            // الأنواع الأساسية
            if (context.INTEGER() != null) return "int";
            if (context.REAL() != null) return "double";
            if (context.TRUE() != null || context.FALSE() != null) return "boolean";
            if (context.NULL() != null) return "null";

            // المعرفات (متغيرات)
            if (context.IDENTIFIER() != null && context.expression().Length == 0 && context.LPAREN() == null)
            {
                string varName = context.IDENTIFIER().GetText();
                Symbol symbol = symbolTable.Lookup(varName);

                if (symbol != null)
                    return symbol.DataType;
                return "unknown";
            }

            // الوصول إلى أعضاء الهياكل: expression '.' IDENTIFIER
            if (context.DOT() != null && context.expression().Length == 1 && context.IDENTIFIER() != null && context.ASSIGN() == null)
            {
                string baseType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();

                // استخدام IsStructType للتحقق بدلاً من StartsWith
                if (IsStructType(baseType))
                {
                    string structName = GetStructNameFromType(baseType);
                    SimpleParser.StructContext structDef = FindStructDefinition(structName, context);

                    if (structDef != null)
                    {
                        string memberType = GetStructMemberType(structDef, memberName);
                        if (memberType != null)
                            return memberType;

                        // البحث في الهياكل الأب
                        memberType = FindMemberInParentStructs(structDef, memberName, context);
                        if (memberType != null)
                            return memberType;
                    }
                }
                else if (baseType != "unknown")
                    AddSemanticError($"Members of this type '{baseType}' are not accessible", context);

                return "unknown";
            }

            // التعيين لأعضاء الهياكل: expression '.' IDENTIFIER '=' expression
            if (context.DOT() != null && context.expression().Length == 2 && context.IDENTIFIER() != null && context.ASSIGN() != null)
            {
                string baseType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();

                if (IsStructType(baseType))
                {
                    string structName = GetStructNameFromType(baseType);
                    SimpleParser.StructContext structDef = FindStructDefinition(structName, context);

                    if (structDef != null)
                    {
                        string memberType = GetStructMemberType(structDef, memberName);
                        if (memberType != null)
                            return memberType;
                    }
                }
                return "unknown";
            }

            if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                // في التعيين، النوع هو نوع الطرف الأيسر
                string leftType = GetExpressionType(context.expression(0));

                // إذا كان الطرف الأيسر غير معروف، حاول التعرف عليه كمُعرف
                if (leftType == "unknown" && context.expression(0).IDENTIFIER() != null)
                {
                    string varName = context.expression(0).IDENTIFIER().GetText();
                    AddSemanticError($"Variable '{varName}' is not declared", context);
                }

                return leftType;
            }

            // استدعاء الدوال
            if (context.IDENTIFIER() != null && context.LPAREN() != null)
            {
                string functionName = context.IDENTIFIER().GetText();
                Symbol functionSymbol = symbolTable.Lookup(functionName);

                if (functionSymbol != null && functionSymbol.Type == "function")
                    return functionSymbol.DataType;

                return "unknown";
            }

            // التعبيرات الثنائية
            if (context.binaryOp() != null && context.expression().Length == 2)
            {
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));
                string op = context.binaryOp().GetText();

                string resultType = GetBinaryOperationResultType(leftType, rightType, op);
                if (resultType != null)
                    return resultType;
            }

            // في التعيين، النوع هو نوع الطرف الأيسر
            if (context.ASSIGN() != null && context.expression().Length == 2)
                return GetExpressionType(context.expression(0));

            // التعبيرات الأحادية
            if (context.expression().Length == 1 && context.ASSIGN() == null)
                return GetExpressionType(context.expression(0));

            // معالجة العامل المنطقي NOT (!)
            if (context.expression().Length == 1 && context.GetChild(0) is ITerminalNode terminalNode &&
                terminalNode.Symbol.Type == SimpleParser.NOT)
            {
                string operandType = GetExpressionType(context.expression(0));
                if (operandType == "boolean")
                    return "boolean";
                return "unknown";
            }

            return "unknown";
        }

        private bool IsStructType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            // إذا كان النوع معروفاً كهيكل في جدول الرموز
            Symbol symbol = symbolTable.Lookup(typeName);
            if (symbol != null && symbol.Type == "struct")
                return true;

            // أو إذا كان النوع يحتوي على بادئة struct_
            if (typeName.StartsWith("struct_"))
                return true;

            // أو إذا كان اسم هيكل معرّف
            SimpleParser.ProgramContext programContext = GetProgramContext(null); // نحتاج سياقاً للبحث
            if (programContext != null)
            {
                SimpleParser.StructContext structDef = FindStructInProgram(programContext, typeName);
                if (structDef != null)
                    return true;
            }

            return false;
        }

        private string GetStructNameFromType(string typeName)
        {
            if (typeName.StartsWith("struct_"))
                return typeName.Substring("struct_".Length);
            return typeName;
        }

        private string NormalizeStructType(string typeName, ParserRuleContext context)
        {
            if (typeName == null) return "unknown";

            // إذا كان النوع يحتوي على بادئة struct_، أزل البادئة للبحث
            if (typeName.StartsWith("struct_"))
            {
                string cleanName = typeName.Substring("struct_".Length);
                // تحقق إذا كان الاسم النظيف يتوافق مع هيكل معرّف
                SimpleParser.StructContext structDef = FindStructDefinition(cleanName, context);
                // إرجاع الاسم بدون بادئة
                if (structDef != null)
                    return cleanName;
            }

            // إذا كان النوع بدون بادئة، تحقق إذا كان هيكلاً
            if (typeName != "int" && typeName != "double" && typeName != "boolean" &&
                typeName != "void" && typeName != "unknown" && typeName != "null")
            {
                SimpleParser.StructContext structDef = FindStructDefinition(typeName, context);
                // إرجاع الاسم بدون بادئة
                if (structDef != null)
                    return typeName;
            }

            return typeName;
        }

        private string FindMemberInParentStructs(SimpleParser.StructContext structContext, string memberName, ParserRuleContext context)
        {
            if (structContext == null)
                return null;

            // إذا كان هناك هيكل أب، ابحث في أعضائه
            if (structContext.IDENTIFIER(1) != null)
            {
                string parentName = structContext.IDENTIFIER(1).GetText();
                SimpleParser.StructContext parentStruct = FindStructDefinition(parentName, context);
                if (parentStruct != null)
                {
                    string memberType = GetStructMemberType(parentStruct, memberName);
                    if (memberType != null)
                        return memberType;
                    // إذا لم يتم العثور، ابحث في أسلاف الهيكل الأب
                    return FindMemberInParentStructs(parentStruct, memberName, context);
                }
            }

            return null;
        }

        // الحصول على نوع عضو الهيكل، مع دعم الوراثة
        private string GetStructMemberType(SimpleParser.StructContext structContext, string memberName)
        {
            DebugStructInheritance(structContext, memberName);
            // التحقق من الأعضاء المباشرين أولاً
            if (structContext?.struct_members() == null)
                return null;

            // البحث في الأعضاء المباشرين للهيكل
            foreach (IParseTree? child in structContext.struct_members().children)
                if (child is SimpleParser.Struct_memberContext memberContext)
                {
                    SimpleParser.VariableContext variable = memberContext.variable();
                    if (variable?.IDENTIFIER()?.GetText() == memberName)
                        return memberContext.type().GetText();
                }

            // إذا لم يتم العثور على العضو في الهيكل الحالي، ابحث في الهيكل الأب
            if (structContext.IDENTIFIER(1) != null)
            {
                string parentName = structContext.IDENTIFIER(1).GetText();
                SimpleParser.StructContext parentStruct = FindStructDefinition(parentName, structContext);
                if (parentStruct != null)
                {
                    Console.WriteLine($"البحث في الهيكل الأب '{parentName}' للعضو '{memberName}'");
                    return GetStructMemberType(parentStruct, memberName);
                }
            }

            return null;
        }

        private bool AreTypesCompatible(string targetType, string sourceType, ParserRuleContext context)
        {
            if (targetType == "unknown" || sourceType == "unknown")
                return true;

            // إذا كانا نفس النوع
            if (targetType == sourceType)
                return true;

            // التحويلات الآمنة
            if (targetType == "double" && sourceType == "int")
                return true;

            // منع التحويل من bool إلى int أو double والعكس
            if ((targetType == "boolean" && (sourceType == "int" || sourceType == "double")) ||
                ((targetType == "int" || targetType == "double") && sourceType == "boolean"))
            {
                return false;
            }

            // التحويلات مع فقدان الدقة (تحذير)
            if (targetType == "int" && sourceType == "double")
            {
                AddSemanticWarning($"فقدان الدقة عند تحويل {sourceType} إلى {targetType}", context);
                return true;
            }

            // null مقبول للهياكل
            if (IsStructType(targetType) && sourceType == "null")
                return true;

            if (IsStructType(sourceType) && targetType == "null")
                return true;

            // الوراثة بين الهياكل
            if (IsStructType(targetType) && IsStructType(sourceType))
            {
                string targetStruct = GetStructNameFromType(targetType);
                string sourceStruct = GetStructNameFromType(sourceType);

                if (IsSourceInheritsFromTarget(sourceStruct, targetStruct, context))
                    return true;
            }

            return false;
        }

        private bool IsSourceInheritsFromTarget(string sourceStruct, string targetStruct, ParserRuleContext context)
        {
            SimpleParser.StructContext sourceDef = FindStructDefinition(sourceStruct, context);
            if (sourceDef == null)
                return false;

            // التحقق من الوراثة المباشرة
            if (sourceDef.IDENTIFIER(1) != null)
            {
                string parentName = sourceDef.IDENTIFIER(1).GetText();
                if (parentName == targetStruct)
                    return true;

                // التحقق من الوراثة غير المباشرة (متعددة المستويات)
                return IsSourceInheritsFromTarget(parentName, targetStruct, context);
            }

            return false;
        }

        private SimpleParser.StructContext FindStructDefinition(string structName, ParserRuleContext context)
        {
            // البحث في الشجرة عن تعريف الهيكل
            SimpleParser.ProgramContext programContext = GetProgramContext(context);

            if (programContext != null)
                return FindStructInProgram(programContext, structName);

            return null;
        }

        private SimpleParser.StructContext FindStructInProgram(SimpleParser.ProgramContext program, string structName)
        {
            foreach (SimpleParser.MemberContext? member in program.member())
                if (member.@struct() != null)
                {
                    SimpleParser.StructContext structContext = member.@struct();
                    string currentStructName = structContext.IDENTIFIER(0).GetText();

                    if (currentStructName == structName)
                        return structContext;
                }
            return null;
        }

        private string GetBinaryOperationResultType(string leftType, string rightType, string op)
        {
            switch (op)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                    if ((leftType == "int" || leftType == "double") && (rightType == "int" || rightType == "double"))
                        return (leftType == "double" || rightType == "double") ? "double" : "int";
                    break;
                case "%": // إضافة دعم لعامل الباقي
                    if ((leftType == "int" || leftType == "double") && (rightType == "int" || rightType == "double"))
                        return "int"; // الباقي دائماً int
                    break;
                case "&&":
                case "||":
                    if (leftType == "boolean" && rightType == "boolean")
                        return "boolean";
                    break;
                case "==":
                case "!=":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    // السماح بالمقارنة بين أي نوعين متشابهين
                    if (leftType == rightType)
                        return "boolean";
                    // السماح بالمقارنة بين الأنواع الرقمية
                    else if ((leftType == "int" || leftType == "double") && (rightType == "int" || rightType == "double"))
                        return "boolean";
                    // السماح بالمقارنة بين القيم المنطقية
                    else if (leftType == "boolean" && rightType == "boolean")
                        return "boolean";
                    break;
            }

            Console.WriteLine($"Operation is not supported: {leftType} {op} {rightType}");
            return null;
        }

        private void AddSemanticError(string message, ParserRuleContext context)
        {
            string error = $"Semantic error at line {context.Start.Line}, column {context.start.Column}: {message}";
            semanticErrors.Add(error);
            Console.WriteLine($"{error}");
        }

        private object SafeVisit(ParserRuleContext context)
        {
            if (context == null)
                return null;

            if (visitedNodes.Contains(context))
                return null;

            visitedNodes.Add(context);
            try
            {
                return Visit(context);
            }
            finally
            {
                visitedNodes.Remove(context);
            }
        }

        private object VisitIfStatement(SimpleParser.StatementContext context)
        {
            if (context.expression().Length > 0)
            {
                // زيارة التعبير الشرطي أولاً
                Visit(context.expression(0));

                string conditionType = GetExpressionType(context.expression(0));

                // تحسين: التعامل مع العامل ! في الشروط
                if (conditionType == "unknown")
                {
                    // محاولة إصلاح المشكلة عن طريق التحقق من التعبير مباشرة
                    SimpleParser.ExpressionContext expr = context.expression(0);

                    // إذا كان التعبير يبدأ بـ !، فالنوع يجب أن يكون boolean
                    if (expr.GetChild(0) is ITerminalNode terminal && terminal.Symbol.Type == SimpleParser.NOT)
                        conditionType = "boolean";
                }

                if (conditionType != "boolean")
                    AddSemanticError("if condition must be boolean", context);
            }

            // جملة if
            if (context.statement().Length > 0)
                SafeVisit(context.statement(0));

            // جملة else
            if (context.ELSE() != null && context.statement().Length > 1)
                SafeVisit(context.statement(1));

            return null;
        }

        private object VisitWhileStatement(SimpleParser.StatementContext context)
        {
            if (context.expression().Length > 0)
            {
                string conditionType = SafeVisit(context.expression(0)) as string;
                if (conditionType != "boolean")
                    AddSemanticError("while condition must be boolean", context);
            }

            if (context.statement().Length > 0)
                SafeVisit(context.statement(0));

            return null;
        }

        private object VisitForStatement(SimpleParser.StatementContext context)
        {
            symbolTable.EnterScope("for");

            // تعريف المتغيرات
            SafeVisit(context.type());
            SafeVisit(context.variables());

            // الشرط
            if (context.expression().Length > 0)
            {
                string conditionType = SafeVisit(context.expression(0)) as string;
                if (conditionType != "boolean")
                    AddSemanticError("for condition must be boolean", context);
            }

            // التحديث
            if (context.expression().Length > 1)
                SafeVisit(context.expression(1));

            // جملة for
            if (context.statement().Length > 0)
                SafeVisit(context.statement(0));

            symbolTable.ExitScope();
            return null;
        }

        private object VisitIncrementDecrementExpression(SimpleParser.ExpressionContext context)
        {
            bool isIncrement = context.INCREMENT() != null;
            bool isPrefix = context.expression().Length == 1 &&
                           (context.INCREMENT() != null || context.DECREMENT() != null) &&
                           context.GetChild(0) is ITerminalNode;

            string varName = null;
            if (context.expression(0) != null && context.expression(0).IDENTIFIER() != null)
                varName = context.expression(0).IDENTIFIER().GetText();

            // التحقق من وجود المتغير
            if (varName != null)
            {
                Symbol symbol = symbolTable.Lookup(varName);
                if (symbol == null)
                {
                    AddSemanticError($"Identifier '{varName}' is not declared", context);
                    return null;
                }

                // التحقق من أن النوع رقمي
                if (symbol.DataType != "int" && symbol.DataType != "double")
                {
                    AddSemanticError($"Can not apply increament '++' or decreament '--' on the type {symbol.DataType}", context);
                    return null;
                }
            }

            return isIncrement ? "int" : "int"; // الناتج دائماً int
        }
    }
}