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
        private string currentFunctionReturnType;
        private HashSet<ParserRuleContext> visitedNodes;

        public SimpleVisitor(SymbolTable symbolTable, List<string> semanticErrors)
        {
            this.symbolTable = symbolTable;
            this.semanticErrors = semanticErrors;
            visitedNodes = new HashSet<ParserRuleContext>();
        }

        public override object VisitProgram([NotNull] SimpleParser.ProgramContext context)
        {
            symbolTable.EnterScope("global");

            string programName = context.IDENTIFIER().GetText();
            AddSemanticInfo($"start program: {programName}");

            //foreach (SimpleParser.MemberContext? member in context.member())
            //    Visit(member);

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
                foreach (var arg in context.arguments().argument())
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

                if (!IsStructDefined(parentName, programContext))
                    AddSemanticError($"struct parent '{parentName}' is not defined", context);
            }

            Symbol structSymbol = new Symbol(
                structName,
                "struct",
                structName,
                context.Start.Line,
                context.Start.Column,
                symbolTable.CurrentScope
            );

            if (!symbolTable.AddSymbol(structSymbol))
                AddSemanticError($"struct '{structName}' is already defined", context);
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
            {
                foreach (var arg in context.arguments().argument())
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
            }

            // زيارة جمل الدالة
            foreach (var stmt in context.statement())
            {
                Visit(stmt);
            }

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

            // الحصول على نوع المتغير من السياق الأب
            string varType = GetVariableTypeFromContext(context);

            if (varType != null)
            {
                Symbol varSymbol = new Symbol(
                    varName,
                    "local",
                    varType,
                    context.Start.Line,
                    context.Start.Column,
                    symbolTable.CurrentScope
                );

                if (!symbolTable.AddSymbol(varSymbol))
                    AddSemanticError($"variable '{varName}' is already declared", context);

                // التحقق من التعبير إذا وجد
                if (context.expression() != null)
                {
                    string exprType = GetExpressionType(context.expression());
                    if (exprType != null && !AreTypesCompatible(varType, exprType, context))
                        AddSemanticError($"type mismatch: cannot assign {exprType} to {varType}", context);

                    return exprType;
                }
            }

            return varType;
        }

        public override object VisitExpression([NotNull] SimpleParser.ExpressionContext context)
        {
            // هذا استدعاء دالة
            if (context.IDENTIFIER() != null && context.LPAREN() != null)
            {
                string functionName = context.IDENTIFIER().GetText();
                Symbol functionSymbol = symbolTable.Lookup(functionName);

                if (functionSymbol == null)
                {
                    AddSemanticError($"function '{functionName}' is not defined", context);
                    return null;
                }

                if (functionSymbol.Type != "function")
                {
                    AddSemanticError($"'{functionName}' is not a function name", context);
                    return null;
                }

                // الحصول على قائمة الباراميترات الفعلية من الاستدعاء
                List<SimpleParser.ExpressionContext> actualArgs = new List<SimpleParser.ExpressionContext>();
                if (context.expr_list() != null)
                    actualArgs = context.expr_list().expression().ToList();

                // الحصول على الباراميترات الرسمية للدالة
                List<Symbol> formalParams = GetFunctionParameters(functionName, context);

                // التحقق من عدد الباراميترات
                if (actualArgs.Count != formalParams.Count)
                {
                    AddSemanticError($"parameters count is not compatible with call function '{functionName}'. expected: {formalParams.Count}, current: {actualArgs.Count}", context);
                    return functionSymbol.DataType; // نعود بنوع الدالة رغم الخطأ
                }

                // التحقق من أنواع الباراميترات
                for (int i = 0; i < actualArgs.Count; i++)
                {
                    string actualArgType = GetExpressionType(actualArgs[i]);
                    string formalParamType = formalParams[i].DataType;

                    if (actualArgType != null && !AreTypesCompatible(formalParamType, actualArgType, context))
                        AddSemanticError($"parameter type {i + 1} in call function '{functionName}' is not compatible expected: {formalParamType}, current: {actualArgType}", actualArgs[i]);

                    // زيارة التعبير الفعلي للتحقق من الأخطاء الداخلية
                    Visit(actualArgs[i]);
                }

                return functionSymbol.DataType; // نوع الإرجاع
            }

            // معالجة الوصول إلى أعضاء الهياكل (قراءة)
            if (context.DOT() != null && context.expression().Length == 1 && context.IDENTIFIER() != null && context.ASSIGN() == null)
            {
                // زيارة التعبير الأساسي (الهيكل)
                Visit(context.expression(0));

                string structType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();

                if (structType.StartsWith("struct_"))
                {
                    string structName = structType.Substring("struct_".Length);
                    SimpleParser.StructContext structDef = FindStructDefinition(structName, context);
                    if (structDef != null)
                    {
                        string memberType = GetStructMemberType(structDef, memberName);
                        if (memberType == null)
                            AddSemanticError($"العضو '{memberName}' غير موجود في الهيكل '{structName}'", context);
                    }
                }

                return GetExpressionType(context);
            }

            // معالجة التعيين لأعضاء الهياكل (كتابة)
            if (context.DOT() != null && context.expression().Length == 2 && context.IDENTIFIER() != null && context.ASSIGN() != null)
            {
                // النموذج: expression . IDENTIFIER = expression
                string structType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();

                // زيارة التعبير الأساسي (الهيكل)
                Visit(context.expression(0));

                // تحديد نوع العضو
                string memberType = null;
                if (structType.StartsWith("struct_"))
                {
                    string structName = structType.Substring("struct_".Length);
                    var structDef = FindStructDefinition(structName, context);
                    if (structDef != null)
                        memberType = GetStructMemberType(structDef, memberName);
                }

                // زيارة التعبير الأيمن والتحقق من التوافق
                string rightType = GetExpressionType(context.expression(1));
                Visit(context.expression(1));

                if (memberType != null && rightType != null && !AreTypesCompatible(memberType, rightType, context))
                    AddSemanticError($"member type '{memberName}' is not compatible. expected: {memberType}, current: {rightType}", context);

                return memberType ?? "unknown";
            }

            if (context.INTEGER() != null)
                return "int";

            if (context.REAL() != null)
                return "double";

            if (context.TRUE() != null || context.FALSE() != null)
                return "bool";

            if (context.NULL() != null)
                return "null";

            if (context.IDENTIFIER() != null && context.expression().Length == 0)
            {
                // معالجة المعرفات البسيطة
                string varName = context.IDENTIFIER().GetText();
                Symbol symbol = symbolTable.Lookup(varName);

                if (symbol != null)
                    return symbol.DataType;

                AddSemanticError($"Identifier '{varName}' is not declared", context);
                return null;
            }

            if (context.expression().Length == 1 && context.ASSIGN() == null)
                // تعبيرات أحادية
                return Visit(context.expression(0));

            if (context.expression().Length == 2 && context.binaryOp() != null)
            {
                // تعبيرات ثنائية
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));

                if (leftType != null && rightType != null)
                    return GetBinaryOperationResultType(leftType, rightType, context.binaryOp().GetText());

                return null;
            }

            if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                // تعيين
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));

                if (leftType != null && rightType != null && !AreTypesCompatible(leftType, rightType, context))
                    AddSemanticError($"type mismatch: {leftType} and {rightType}", context);

                return leftType;
            }

            if (context.INCREMENT() != null || context.DECREMENT() != null)
                return VisitIncrementDecrementExpression(context);

            return null;
        }

        private List<Symbol> GetFunctionParameters(string functionName, SimpleParser.ExpressionContext context)
        {
            var parameters = new List<Symbol>();

            // البحث عن تعريف الدالة في الشجرة
            SimpleParser.FunctionContext functionDefinition = FindFunctionDefinition(functionName, context);

            if (functionDefinition != null && functionDefinition.arguments() != null)
                foreach (var arg in functionDefinition.arguments().argument())
                {
                    string paramName = arg.IDENTIFIER().GetText();
                    string paramType = arg.type().GetText();

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

            return null;
        }

        private SimpleParser.ProgramContext GetProgramContext(Antlr4.Runtime.ParserRuleContext context)
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
            if (context.type() != null && context.variables() != null)
            {
                // تعريف متغير محلي
                Visit(context.type());
                Visit(context.variables());
                return null;
            }
            if (context.LBRACE() != null)
            {
                // كتلة من الجمل - ندخل نطاق جديد للكتلة
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
                // زيارة التعبير أولاً (للاكتشاف المبكر للأخطاء)
                Visit(context.expression(0));

                // تحديد نوع الإرجاع
                string returnType = GetExpressionType(context.expression(0));

                // التحقق من التوافق مع نوع الدالة
                if (currentFunctionReturnType != "void")
                    if (returnType == "unknown")
                        AddSemanticError("The expression type could not be determined in the return statement.", context);
                    else if (!AreTypesCompatible(currentFunctionReturnType, returnType, context))
                        AddSemanticError($"Return type {returnType} is not compatible with function type {currentFunctionReturnType}", context);
            }
            else if (currentFunctionReturnType != "void")
            {
                AddSemanticError("Function must return value", context);
            }

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
                }
                parent = parent.Parent;
            }
            return null;
        }

        private string GetExpressionType(SimpleParser.ExpressionContext context)
        {
            if (context == null) return "unknown";

            if (context.INTEGER() != null) return "int";
            if (context.REAL() != null) return "double";
            if (context.TRUE() != null || context.FALSE() != null) return "bool";
            if (context.NULL() != null) return "null";

            if (context.IDENTIFIER() != null)
            {
                string varName = context.IDENTIFIER().GetText();
                var symbol = symbolTable.Lookup(varName);

                if (symbol != null)
                {
                    return symbol.DataType;
                }
                else
                {
                    // إذا لم يكن متغيراً، قد يكون اسم هيكل
                    var structDef = FindStructDefinition(varName, context);
                    if (structDef != null)
                    {
                        return $"struct_{varName}";
                    }
                }
                return "unknown";
            }

            // معالجة الوصول إلى أعضاء الهياكل: expression '.' IDENTIFIER
            if (context.DOT() != null && context.expression().Length == 1 && context.IDENTIFIER() != null)
            {
                string structType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();

                // إذا كان النوع الأساسي هو هيكل، ابحث عن العضو داخل الهيكل
                if (structType.StartsWith("struct_"))
                {
                    string structName = structType.Substring("struct_".Length);
                    SimpleParser.StructContext structDef = FindStructDefinition(structName, context);
                    if (structDef != null)
                    {
                        string memberType = GetStructMemberType(structDef, memberName);
                        if (memberType != null)
                            return memberType;
                        else
                            AddSemanticError($"العضو '{memberName}' غير موجود في الهيكل '{structName}'", context);
                    }
                }
                else
                    AddSemanticError($"النوع '{structType}' ليس هيكلاً، لا يمكن الوصول إلى الأعضاء", context);
                return "unknown";
            }

            // معالجة التعيين لأعضاء الهياكل: expression '.' IDENTIFIER '=' expression
            if (context.DOT() != null && context.expression().Length == 2 && context.IDENTIFIER() != null && context.ASSIGN() != null)
            {
                // النوع هو نوع العضو المطلوب تعيينه
                string structType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();

                if (structType.StartsWith("struct_"))
                {
                    string structName = structType.Substring("struct_".Length);
                    SimpleParser.StructContext structDef = FindStructDefinition(structName, context);
                    if (structDef != null)
                        return GetStructMemberType(structDef, memberName) ?? "unknown";
                }
                return "unknown";
            }

            // تعبيرات أحادية
            if (context.expression().Length == 1 && context.ASSIGN() == null)
                return GetExpressionType(context.expression(0));

            // عمليات ثنائية
            if (context.binaryOp() != null && context.expression().Length == 2)
            {
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));
                string op = context.binaryOp().GetText();

                return GetBinaryOperationResultType(leftType, rightType, op);
            }

            // تعيين - النوع هو نوع المتغير الأيسر
            if (context.ASSIGN() != null && context.expression().Length == 2)
                return GetExpressionType(context.expression(0));

            return "unknown";
        }

        private string GetStructMemberType(SimpleParser.StructContext structContext, string memberName)
        {
            if (structContext?.struct_members() == null) return null;

            // البحث في أعضاء الهيكل الحالي
            foreach (var child in structContext.struct_members().children)
                if (child is SimpleParser.Struct_memberContext memberContext)
                    if (memberContext.variable()?.IDENTIFIER()?.GetText() == memberName)
                        return memberContext.type().GetText();

            // إذا كان هناك هيكل أب، ابحث في الأعضاء الموروثة
            if (structContext.IDENTIFIER(1) != null)
            {
                string parentName = structContext.IDENTIFIER(1).GetText();
                SimpleParser.StructContext parentStruct = FindStructDefinition(parentName, structContext);
                if (parentStruct != null)
                    return GetStructMemberType(parentStruct, memberName);
            }

            return null;
        }

        private bool AreTypesCompatible(string targetType, string sourceType, ParserRuleContext context)
        {
            if (targetType == sourceType) return true;

            // إذا كان أي من الأنواع غير معروف، نعتبره متوافقاً مؤقتاً لتجنب أخطاء متعددة
            if (targetType == "unknown" || sourceType == "unknown") return true;

            // التحويلات الآمنة
            if (targetType == "double" && sourceType == "int") return true;
            if (targetType == "int" && sourceType == "bool") return true;

            // التحويلات مع فقدان الدقة (قد نريد إعطاء تحذير بدلاً من خطأ)
            if (targetType == "int" && sourceType == "double")
            {
                AddSemanticError($"تحذير: فقدان الدقة عند تحويل {sourceType} إلى {targetType}", context);
                return true;
            }

            // الأنواع المركبة (الهياكل)
            if (targetType == "null" && sourceType.StartsWith("struct_")) return true;
            if (targetType.StartsWith("struct_") && sourceType == "null") return true;

            // الوراثة بين الهياكل
            if (targetType.StartsWith("struct_") && sourceType.StartsWith("struct_"))
                if (IsStructCompatible(targetType, sourceType, context))
                    return true;

            return false;
        }

        private bool IsStructCompatible(string targetStruct, string sourceStruct, Antlr4.Runtime.ParserRuleContext context)
        {
            // تنظيف أسماء الهياكل (إزالة أي بادئات)
            targetStruct = CleanStructName(targetStruct);
            sourceStruct = CleanStructName(sourceStruct);

            // إذا كانا نفس النوع
            if (targetStruct == sourceStruct)
                return true;

            // البحث عن الهيكل المصدر في الشجرة
            SimpleParser.StructContext sourceStructDef = FindStructDefinition(sourceStruct, context);
            if (sourceStructDef != null && sourceStructDef.IDENTIFIER(1) != null)
            {
                string parentName = sourceStructDef.IDENTIFIER(1).GetText();
                parentName = CleanStructName(parentName);

                // التحقق من الوراثة المباشرة أو غير المباشرة
                if (parentName == targetStruct || IsStructCompatible(targetStruct, parentName, context))
                    return true;
            }

            return false;
        }

        private string CleanStructName(string structName)
        {
            // إزالة البادئة "struct_" إذا كانت موجودة
            if (structName.StartsWith("struct_"))
                return structName.Substring("struct_".Length);

            return structName;
        }

        private SimpleParser.StructContext FindStructDefinition(string structName, ParserRuleContext context)
        {
            // البحث في الشجرة عن تعريف الهيكل
            var programContext = GetProgramContext(context);

            if (programContext != null)
                return FindStructInProgram(programContext, structName);

            return null;
        }

        private SimpleParser.StructContext FindStructInProgram(SimpleParser.ProgramContext program, string structName)
        {
            foreach (SimpleParser.MemberContext? member in program.member())
            {
                if (member.@struct() != null)
                {
                    SimpleParser.StructContext structContext = member.@struct();
                    string currentStructName = structContext.IDENTIFIER(0).GetText();

                    if (currentStructName == structName)
                        return structContext;
                }
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
                case "&&":
                case "||":
                    if (leftType == "bool" && rightType == "bool")
                        return "bool";
                    break;
                case "==":
                case "!=":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    if ((leftType == "int" || leftType == "double") && (rightType == "int" || rightType == "double"))
                        return "bool";
                    else if (leftType == "bool" && rightType == "bool")
                        return "bool";
                    break;
            }

            AddSemanticInfo($"Operation is not supported: {leftType} {op} {rightType}");
            return null;
        }

        private void AddSemanticError(string message, Antlr4.Runtime.ParserRuleContext context)
        {
            string error = $"Semantic error at line {context.Start.Line}: {message}";
            semanticErrors.Add(error);
            Console.WriteLine($"❌ {error}");
        }

        private void AddSemanticInfo(string message)
        {
            Console.WriteLine($"{message}");
        }

        private object SafeVisit(Antlr4.Runtime.ParserRuleContext context)
        {
            if (context == null) return null;

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
                string conditionType = SafeVisit(context.expression(0)) as string;
                if (conditionType != "bool")
                    AddSemanticError("if condition must be boolean", context);
            }

            if (context.statement().Length > 0)
                SafeVisit(context.statement(0)); // جملة if

            if (context.ELSE() != null && context.statement().Length > 1)
                SafeVisit(context.statement(1)); // جملة else

            return null;
        }

        private object VisitWhileStatement(SimpleParser.StatementContext context)
        {
            if (context.expression().Length > 0)
            {
                string conditionType = SafeVisit(context.expression(0)) as string;
                if (conditionType != "bool")
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
                if (conditionType != "bool")
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
            {
                varName = context.expression(0).IDENTIFIER().GetText();
            }

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