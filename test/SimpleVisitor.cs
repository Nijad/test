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
            Console.WriteLine("=== تصحيح: جميع الرموز في الجدول ===");
            symbolTable.PrintAllSymbols();
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
                var programContext = GetProgramContext(context);

                if (!IsStructDefined(parentName, programContext))
                {
                    AddSemanticError($"الهيكل الأب '{parentName}' غير معرّف", context);
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"[DEBUG] الهيكل '{structName}' يرث من '{parentName}'");
#endif
                }
            }

            var structSymbol = new Symbol(
                structName,
                "struct",
                structName,
                context.Start.Line,
                context.Start.Column,
                symbolTable.CurrentScope
            );

            if (!symbolTable.AddSymbol(structSymbol))
            {
                AddSemanticError($"الهيكل '{structName}' معرّف مسبقاً", context);
            }
        }

        private List<string> GetAllStructMembers(SimpleParser.StructContext structContext)
        {
            var members = new List<string>();

            if (structContext?.struct_members() != null)
            {
                foreach (var child in structContext.struct_members().children)
                {
                    if (child is SimpleParser.Struct_memberContext memberContext)
                    {
                        var variable = memberContext.variable();
                        if (variable?.IDENTIFIER() != null)
                        {
                            members.Add(variable.IDENTIFIER().GetText());
                        }
                    }
                }
            }

            // إضافة الأعضاء الموروثة
            if (structContext?.IDENTIFIER(1) != null)
            {
                string parentName = structContext.IDENTIFIER(1).GetText();
                var parentStruct = FindStructDefinition(parentName, structContext);
                if (parentStruct != null)
                {
                    members.AddRange(GetAllStructMembers(parentStruct));
                }
            }

            return members;
        }

        private void DebugStructAccess(SimpleParser.ExpressionContext context)
        {
#if DEBUG
            Console.WriteLine($"=== تصحيح الوصول إلى الهياكل ===");
            Console.WriteLine($"التعبير: {context.GetText()}");

            if (context.DOT() != null && context.expression().Length == 1 && context.IDENTIFIER() != null)
            {
                var baseExpr = context.expression(0);
                string baseType = GetExpressionType(baseExpr);
                string memberName = context.IDENTIFIER().GetText();

                Console.WriteLine($"النوع الأساسي: {baseType}");
                Console.WriteLine($"اسم العضو: {memberName}");
                Console.WriteLine($"هل النوع الأساسي هيكل؟: {IsStructType(baseType)}");

                if (IsStructType(baseType))
                {
                    string structName = GetStructNameFromType(baseType);
                    Console.WriteLine($"اسم الهيكل: {structName}");

                    var structDef = FindStructDefinition(structName, context);
                    Console.WriteLine($"تعريف الهيكل: {(structDef != null ? "موجود" : "غير موجود")}");

                    if (structDef != null)
                    {
                        string memberType = GetStructMemberType(structDef, memberName);
                        Console.WriteLine($"نوع العضو: {memberType ?? "غير موجود"}");

                        if (memberType == null)
                        {
                            memberType = FindMemberInParentStructs(structDef, memberName, context);
                            Console.WriteLine($"نوع العضو بعد البحث في الأب: {memberType ?? "غير موجود"}");
                        }
                    }
                }

                if (baseExpr.DOT() != null)
                {
                    Console.WriteLine($"التعبير الأساسي نفسه يحتوي على نقطة: {baseExpr.GetText()}");
                    string baseBaseType = GetExpressionType(baseExpr.expression(0));
                    Console.WriteLine($"النوع الأساسي الأساسي: {baseBaseType}");
                }
            }
            Console.WriteLine($"===================");
#endif
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

                if (!symbolTable.AddSymbol(varSymbol))
                    AddSemanticError($"variable '{varName}' is already declared", context);

                // التحقق من التعبير إذا وجد
                if (context.expression() != null)
                {
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

            // التعيين لأعضاء الهياكل (كتابة) - مثل circle.id = 1
            if (context.DOT() != null && context.expression().Length == 2 && context.IDENTIFIER() != null && context.ASSIGN() != null)
            {
                // زيارة التعبير الأساسي أولاً (مثل circle في circle.id = 1)
                Visit(context.expression(0));

                string baseType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();

                if (!IsStructType(baseType))
                {
                    AddSemanticError($"لا يمكن الوصول إلى الأعضاء من النوع '{baseType}'", context);
                    return "unknown";
                }

                string structName = GetStructNameFromType(baseType);
                var structDef = FindStructDefinition(structName, context);

                if (structDef == null)
                {
                    AddSemanticError($"الهيكل '{structName}' غير معرّف", context);
                    return "unknown";
                }

                // الحصول على نوع العضو
                string memberType = GetStructMemberType(structDef, memberName);
                if (memberType == null)
                {
                    // البحث في الهياكل الأب
                    memberType = FindMemberInParentStructs(structDef, memberName, context);
                }

                if (memberType == null)
                {
                    AddSemanticError($"العضو '{memberName}' غير موجود في الهيكل '{structName}'", context);
                    return "unknown";
                }

                // زيارة التعبير الأيمن والتحقق من التوافق
                string rightType = GetExpressionType(context.expression(1));
                Visit(context.expression(1));

                if (!AreTypesCompatible(memberType, rightType, context))
                {
                    AddSemanticError($"نوع العضو '{memberName}' غير متوافق. المتوقع: {memberType}, المقدم: {rightType}", context);
                }

                return memberType;
            }

            // التعيين العادي - مثل area = calculateArea(circle)
            if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));

                // زيارة الطرفين
                Visit(context.expression(0));
                Visit(context.expression(1));

                if (!AreTypesCompatible(leftType, rightType, context))
                {
                    AddSemanticError($"عدم توافق الأنواع في التعيين. المتوقع: {leftType}, المقدم: {rightType}", context);
                }

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
                    AddSemanticError($"لا يمكن الوصول إلى الأعضاء من النوع '{baseType}'", context);
                    return "unknown";
                }

                string structName = GetStructNameFromType(baseType);
                var structDef = FindStructDefinition(structName, context);

                if (structDef == null)
                {
                    AddSemanticError($"الهيكل '{structName}' غير معرّف", context);
                    return "unknown";
                }

                string memberType = GetStructMemberType(structDef, memberName);
                if (memberType == null)
                {
                    // البحث في الهياكل الأب
                    memberType = FindMemberInParentStructs(structDef, memberName, context);
                }

                if (memberType == null)
                {
                    AddSemanticError($"العضو '{memberName}' غير موجود في الهيكل '{structName}'", context);
                    return "unknown";
                }

                return memberType;
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

            //return null;
            return base.VisitExpression(context);
        }

        private void DebugAssignment(SimpleParser.ExpressionContext context)
        {
#if DEBUG
            if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                Console.WriteLine($"[DEBUG] === تحليل التعيين ===");
                Console.WriteLine($"[DEBUG] التعبير الكامل: {context.GetText()}");

                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));

                Console.WriteLine($"[DEBUG] الطرف الأيسر: {context.expression(0).GetText()} -> {leftType}");
                Console.WriteLine($"[DEBUG] الطرف الأيمن: {context.expression(1).GetText()} -> {rightType}");
                Console.WriteLine($"[DEBUG] متوافق: {AreTypesCompatible(leftType, rightType, context)}");

                // إذا كان الطرف الأيسر وصولاً إلى عضو هيكل
                if (context.expression(0).DOT() != null)
                {
                    var leftExpr = context.expression(0);
                    string baseType = GetExpressionType(leftExpr.expression(0));
                    string memberName = leftExpr.IDENTIFIER().GetText();

                    Console.WriteLine($"[DEBUG]   الوصول إلى العضو: {memberName}");
                    Console.WriteLine($"[DEBUG]   النوع الأساسي: {baseType}");
                    Console.WriteLine($"[DEBUG]   هل النوع الأساسي هيكل؟: {IsStructType(baseType)}");

                    if (IsStructType(baseType))
                    {
                        string structName = GetStructNameFromType(baseType);
                        var structDef = FindStructDefinition(structName, context);
                        Console.WriteLine($"[DEBUG]   تعريف الهيكل: {(structDef != null ? "موجود" : "غير موجود")}");

                        if (structDef != null)
                        {
                            string memberType = GetStructMemberType(structDef, memberName);
                            Console.WriteLine($"[DEBUG]   نوع العضو: {memberType ?? "غير موجود"}");
                        }
                    }
                }
                Console.WriteLine($"[DEBUG] === انتهى تحليل التعيين ===");
            }
#endif
        }

        private void DebugStructInheritance(SimpleParser.StructContext structContext, string memberName)
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] البحث عن العضو '{memberName}' في الهيكل '{structContext.IDENTIFIER(0)?.GetText()}'");

            // البحث في الأعضاء المباشرين
            if (structContext.struct_members() != null)
            {
                foreach (var child in structContext.struct_members().children)
                {
                    if (child is SimpleParser.Struct_memberContext memberContext)
                    {
                        var variable = memberContext.variable();
                        if (variable?.IDENTIFIER()?.GetText() == memberName)
                        {
                            Console.WriteLine($"[DEBUG]   وجد العضو '{memberName}' في الهيكل الحالي");
                            return;
                        }
                    }
                }
            }

            // البحث في الهيكل الأب
            if (structContext.IDENTIFIER(1) != null)
            {
                string parentName = structContext.IDENTIFIER(1).GetText();
                Console.WriteLine($"[DEBUG]   البحث في الهيكل الأب: '{parentName}'");
            }
            else
            {
                Console.WriteLine($"[DEBUG]   لا يوجد هيكل أب");
            }
#endif
        }

        private void DebugTypeCompatibility(string targetType, string sourceType, bool isCompatible, ParserRuleContext context)
        {
#if DEBUG
            string normalizedTarget = NormalizeStructType(targetType, context);
            string normalizedSource = NormalizeStructType(sourceType, context);

            Console.WriteLine($"[DEBUG] توافق الأنواع: '{targetType}' -> '{sourceType}'");
            Console.WriteLine($"[DEBUG]   مطبّع: '{normalizedTarget}' -> '{normalizedSource}'");
            Console.WriteLine($"[DEBUG]   متوافق: {isCompatible}");

            if (normalizedTarget != "int" && normalizedTarget != "double" && normalizedTarget != "bool" &&
                normalizedSource != "int" && normalizedSource != "double" && normalizedSource != "bool")
            {
                Console.WriteLine($"[DEBUG]   وراثة: {IsSourceInheritsFromTarget(normalizedSource, normalizedTarget, context)}");
            }
#endif
        }

        private List<Symbol> GetFunctionParameters(string functionName, SimpleParser.ExpressionContext context)
        {
            var parameters = new List<Symbol>();

            var functionDefinition = FindFunctionDefinition(functionName, context);

            if (functionDefinition != null && functionDefinition.arguments() != null)
            {
                foreach (var arg in functionDefinition.arguments().argument())
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
                var expression = context.expression(0);

                // تفعيل نظام التصحيح للهياكل
                DebugStructAccess(expression);

                // زيارة التعبير أولاً
                Visit(expression);

                string returnType = GetExpressionType(expression);

                if (returnType == "unknown")
                {
                    AddSemanticError("تعذر تحديد نوع التعبير في جملة الإرجاع", context);
                }
                else if (currentFunctionReturnType != "void" && !AreTypesCompatible(currentFunctionReturnType, returnType, context))
                {
                    AddSemanticError($"نوع الإرجاع {returnType} لا يتوافق مع نوع الدالة {currentFunctionReturnType}", context);
                }
            }
            else if (currentFunctionReturnType != "void")
            {
                AddSemanticError("الدالة يجب أن ترجع قيمة", context);
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
            if (context.TRUE() != null || context.FALSE() != null) return "bool";
            if (context.NULL() != null) return "null";

            // المعرفات (متغيرات)
            if (context.IDENTIFIER() != null && context.expression().Length == 0 && context.LPAREN() == null)
            {
                string varName = context.IDENTIFIER().GetText();
                var symbol = symbolTable.Lookup(varName);

                if (symbol != null)
                {
                    return symbol.DataType;
                }
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
                    var structDef = FindStructDefinition(structName, context);

                    if (structDef != null)
                    {
                        string memberType = GetStructMemberType(structDef, memberName);
                        if (memberType != null)
                        {
                            return memberType;
                        }

                        // البحث في الهياكل الأب
                        memberType = FindMemberInParentStructs(structDef, memberName, context);
                        if (memberType != null)
                        {
                            return memberType;
                        }
                    }
                }
                else if (baseType != "unknown")
                {
                    AddSemanticError($"لا يمكن الوصول إلى الأعضاء من النوع '{baseType}'", context);
                }

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
                    var structDef = FindStructDefinition(structName, context);

                    if (structDef != null)
                    {
                        string memberType = GetStructMemberType(structDef, memberName);
                        if (memberType != null)
                        {
                            return memberType;
                        }
                    }
                }
                return "unknown";
            }

            // استدعاء الدوال
            if (context.IDENTIFIER() != null && context.LPAREN() != null)
            {
                string functionName = context.IDENTIFIER().GetText();
                var functionSymbol = symbolTable.Lookup(functionName);
                return functionSymbol?.DataType ?? "unknown";
            }

            // التعبيرات الثنائية
            if (context.binaryOp() != null && context.expression().Length == 2)
            {
                string leftType = GetExpressionType(context.expression(0));
                string rightType = GetExpressionType(context.expression(1));
                string op = context.binaryOp().GetText();

                return GetBinaryOperationResultType(leftType, rightType, op);
            }

            // التعيين
            if (context.ASSIGN() != null && context.expression().Length == 2)
            {
                // في التعيين، النوع هو نوع الطرف الأيسر
                return GetExpressionType(context.expression(0));
            }

            // التعبيرات الأحادية
            if (context.expression().Length == 1 && context.ASSIGN() == null)
            {
                return GetExpressionType(context.expression(0));
            }

            return "unknown";
        }

        private bool IsStructType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;

            // إذا كان النوع معروفاً كهيكل في جدول الرموز
            var symbol = symbolTable.Lookup(typeName);
            if (symbol != null && symbol.Type == "struct")
            {
                return true;
            }

            // أو إذا كان النوع يحتوي على بادئة struct_
            if (typeName.StartsWith("struct_"))
            {
                return true;
            }

            // أو إذا كان اسم هيكل معرّف
            var programContext = GetProgramContext(null); // نحتاج سياقاً للبحث
            if (programContext != null)
            {
                var structDef = FindStructInProgram(programContext, typeName);
                if (structDef != null)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetStructNameFromType(string typeName)
        {
            if (typeName.StartsWith("struct_"))
            {
                return typeName.Substring("struct_".Length);
            }
            return typeName;
        }

        private string NormalizeStructType(string typeName, Antlr4.Runtime.ParserRuleContext context)
        {
            if (typeName == null) return "unknown";

            // إذا كان النوع يحتوي على بادئة struct_، أزل البادئة للبحث
            if (typeName.StartsWith("struct_"))
            {
                string cleanName = typeName.Substring("struct_".Length);
                // تحقق إذا كان الاسم النظيف يتوافق مع هيكل معرّف
                var structDef = FindStructDefinition(cleanName, context);
                if (structDef != null)
                {
                    return cleanName; // إرجاع الاسم بدون بادئة للتوحيد
                }
            }

            // إذا كان النوع بدون بادئة، تحقق إذا كان هيكلاً
            if (typeName != "int" && typeName != "double" && typeName != "bool" &&
                typeName != "void" && typeName != "unknown" && typeName != "null")
            {
                var structDef = FindStructDefinition(typeName, context);
                if (structDef != null)
                {
                    return typeName; // إرجاع الاسم بدون بادئة
                }
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
                var parentStruct = FindStructDefinition(parentName, context);
                if (parentStruct != null)
                {
                    string memberType = GetStructMemberType(parentStruct, memberName);
                    if (memberType != null)
                    {
                        return memberType;
                    }
                    // إذا لم يتم العثور، ابحث في أسلاف الهيكل الأب
                    return FindMemberInParentStructs(parentStruct, memberName, context);
                }
            }

            return null;
        }

        private string GetStructMemberType(SimpleParser.StructContext structContext, string memberName)
        {
            DebugStructInheritance(structContext, memberName);

            if (structContext?.struct_members() == null)
                return null;

            // البحث في الأعضاء المباشرين للهيكل
            foreach (var child in structContext.struct_members().children)
            {
                if (child is SimpleParser.Struct_memberContext memberContext)
                {
                    var variable = memberContext.variable();
                    if (variable?.IDENTIFIER()?.GetText() == memberName)
                    {
                        return memberContext.type().GetText();
                    }
                }
            }

            // إذا لم يتم العثور على العضو في الهيكل الحالي، ابحث في الهيكل الأب
            if (structContext.IDENTIFIER(1) != null)
            {
                string parentName = structContext.IDENTIFIER(1).GetText();
                var parentStruct = FindStructDefinition(parentName, structContext);
                if (parentStruct != null)
                {
#if DEBUG
                    Console.WriteLine($"[DEBUG]   البحث في الهيكل الأب '{parentName}' للعضو '{memberName}'");
#endif
                    return GetStructMemberType(parentStruct, memberName);
                }
            }

            return null;
        }

        private void DebugExpressionType(SimpleParser.ExpressionContext context, string message = "")
        {
#if DEBUG
            string type = GetExpressionType(context);
            Console.WriteLine($"[DEBUG] التعبير: {context.GetText()}, النوع: {type}, {message}");

            if (context.DOT() != null && context.expression().Length == 1 && context.IDENTIFIER() != null)
            {
                string baseType = GetExpressionType(context.expression(0));
                string memberName = context.IDENTIFIER().GetText();
                Console.WriteLine($"[DEBUG]   النوع الأساسي: {baseType}, العضو: {memberName}");

                if (baseType.StartsWith("struct_"))
                {
                    string structName = baseType.Substring("struct_".Length);
                    var structDef = FindStructDefinition(structName, context);
                    Console.WriteLine($"[DEBUG]   تعريف الهيكل: {(structDef != null ? "موجود" : "غير موجود")}");

                    if (structDef != null)
                    {
                        string memberType = GetStructMemberType(structDef, memberName);
                        Console.WriteLine($"[DEBUG]   نوع العضو: {memberType ?? "غير موجود"}");
                    }
                }
            }
#endif
        }

        private bool AreTypesCompatible(string targetType, string sourceType, Antlr4.Runtime.ParserRuleContext context)
        {
            if (targetType == "unknown" || sourceType == "unknown")
                return true;

            // إذا كانا نفس النوع
            if (targetType == sourceType)
                return true;

            // التحويلات الآمنة
            if (targetType == "double" && sourceType == "int")
                return true;
            if (targetType == "int" && sourceType == "bool")
                return true;

            // التحويلات مع فقدان الدقة (تحذير)
            if (targetType == "int" && sourceType == "double")
            {
                AddSemanticError($"تحذير: فقدان الدقة عند تحويل {sourceType} إلى {targetType}", context);
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

        private void CollectVariableDeclaration(string varName, string varType, SimpleParser.VariableContext context)
        {
            // تطبيع نوع الهيكل إذا لزم الأمر
            if (IsStructType(varType))
            {
                varType = GetStructNameFromType(varType);
            }

            var varSymbol = new Symbol(
                varName,
                "local",
                varType,
                context.Start.Line,
                context.Start.Column,
                symbolTable.CurrentScope
            );

            if (!symbolTable.AddSymbol(varSymbol))
            {
                AddSemanticError($"المتغير '{varName}' معرّف مسبقاً", context);
            }
        }

        private bool IsSourceInheritsFromTarget(string sourceStruct, string targetStruct, ParserRuleContext context)
        {
            var sourceDef = FindStructDefinition(sourceStruct, context);
            if (sourceDef == null) return false;

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