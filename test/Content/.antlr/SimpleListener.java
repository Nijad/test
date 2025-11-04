// Generated from c:/Users/nijad/source/repos/test/test/Content/Simple.g4 by ANTLR 4.13.1
import org.antlr.v4.runtime.tree.ParseTreeListener;

/**
 * This interface defines a complete listener for a parse tree produced by
 * {@link SimpleParser}.
 */
public interface SimpleListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by {@link SimpleParser#program}.
	 * @param ctx the parse tree
	 */
	void enterProgram(SimpleParser.ProgramContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#program}.
	 * @param ctx the parse tree
	 */
	void exitProgram(SimpleParser.ProgramContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#member}.
	 * @param ctx the parse tree
	 */
	void enterMember(SimpleParser.MemberContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#member}.
	 * @param ctx the parse tree
	 */
	void exitMember(SimpleParser.MemberContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#function}.
	 * @param ctx the parse tree
	 */
	void enterFunction(SimpleParser.FunctionContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#function}.
	 * @param ctx the parse tree
	 */
	void exitFunction(SimpleParser.FunctionContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#arguments}.
	 * @param ctx the parse tree
	 */
	void enterArguments(SimpleParser.ArgumentsContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#arguments}.
	 * @param ctx the parse tree
	 */
	void exitArguments(SimpleParser.ArgumentsContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#argument}.
	 * @param ctx the parse tree
	 */
	void enterArgument(SimpleParser.ArgumentContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#argument}.
	 * @param ctx the parse tree
	 */
	void exitArgument(SimpleParser.ArgumentContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#struct}.
	 * @param ctx the parse tree
	 */
	void enterStruct(SimpleParser.StructContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#struct}.
	 * @param ctx the parse tree
	 */
	void exitStruct(SimpleParser.StructContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#struct_members}.
	 * @param ctx the parse tree
	 */
	void enterStruct_members(SimpleParser.Struct_membersContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#struct_members}.
	 * @param ctx the parse tree
	 */
	void exitStruct_members(SimpleParser.Struct_membersContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#global}.
	 * @param ctx the parse tree
	 */
	void enterGlobal(SimpleParser.GlobalContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#global}.
	 * @param ctx the parse tree
	 */
	void exitGlobal(SimpleParser.GlobalContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#variables}.
	 * @param ctx the parse tree
	 */
	void enterVariables(SimpleParser.VariablesContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#variables}.
	 * @param ctx the parse tree
	 */
	void exitVariables(SimpleParser.VariablesContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#variable}.
	 * @param ctx the parse tree
	 */
	void enterVariable(SimpleParser.VariableContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#variable}.
	 * @param ctx the parse tree
	 */
	void exitVariable(SimpleParser.VariableContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#type}.
	 * @param ctx the parse tree
	 */
	void enterType(SimpleParser.TypeContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#type}.
	 * @param ctx the parse tree
	 */
	void exitType(SimpleParser.TypeContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#expression}.
	 * @param ctx the parse tree
	 */
	void enterExpression(SimpleParser.ExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#expression}.
	 * @param ctx the parse tree
	 */
	void exitExpression(SimpleParser.ExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#expr_list}.
	 * @param ctx the parse tree
	 */
	void enterExpr_list(SimpleParser.Expr_listContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#expr_list}.
	 * @param ctx the parse tree
	 */
	void exitExpr_list(SimpleParser.Expr_listContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#binaryOp}.
	 * @param ctx the parse tree
	 */
	void enterBinaryOp(SimpleParser.BinaryOpContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#binaryOp}.
	 * @param ctx the parse tree
	 */
	void exitBinaryOp(SimpleParser.BinaryOpContext ctx);
	/**
	 * Enter a parse tree produced by {@link SimpleParser#statement}.
	 * @param ctx the parse tree
	 */
	void enterStatement(SimpleParser.StatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link SimpleParser#statement}.
	 * @param ctx the parse tree
	 */
	void exitStatement(SimpleParser.StatementContext ctx);
}