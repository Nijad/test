// Generated from c:/Users/nijad/source/repos/test/test/Content/Simple.g4 by ANTLR 4.13.1
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast", "CheckReturnValue"})
public class SimpleParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.13.1", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		WS=1, TAB=2, ENTER=3, COMMENT=4, LINE_COMMENT=5, BOOLEAN=6, DOUBLE=7, 
		INT=8, IF=9, ELSE=10, WHILE=11, FOR=12, FALSE=13, TRUE=14, NULL=15, VOID=16, 
		ARRAY=17, CLASS=18, PROGRAM=19, STRUCT=20, STATIC=21, RETURN=22, WITH=23, 
		PLUS=24, MINUS=25, MULT=26, DIV=27, MOD=28, ASSIGN=29, EQ=30, NEQ=31, 
		LT=32, LE=33, GT=34, GE=35, AND=36, OR=37, INCREMENT=38, DECREMENT=39, 
		IDENTIFIER=40, INTEGER=41, REAL=42, LPAREN=43, RPAREN=44, LBRACE=45, RBRACE=46, 
		SEMI=47, COMMA=48, DOT=49, COLON=50;
	public static final int
		RULE_program = 0, RULE_member = 1, RULE_function = 2, RULE_arguments = 3, 
		RULE_argument = 4, RULE_struct = 5, RULE_struct_members = 6, RULE_struct_member = 7, 
		RULE_global = 8, RULE_variables = 9, RULE_variable = 10, RULE_type = 11, 
		RULE_expression = 12, RULE_expr_list = 13, RULE_binaryOp = 14, RULE_statement = 15;
	private static String[] makeRuleNames() {
		return new String[] {
			"program", "member", "function", "arguments", "argument", "struct", "struct_members", 
			"struct_member", "global", "variables", "variable", "type", "expression", 
			"expr_list", "binaryOp", "statement"
		};
	}
	public static final String[] ruleNames = makeRuleNames();

	private static String[] makeLiteralNames() {
		return new String[] {
			null, null, "'\\t'", null, null, null, "'Boolean'", "'double'", "'int'", 
			"'if'", "'else'", "'while'", "'for'", "'false'", "'true'", "'null'", 
			"'void'", "'array'", "'class'", "'program'", "'struct'", "'static'", 
			"'return'", "'with'", "'+'", "'-'", "'*'", "'/'", "'%'", "'='", "'=='", 
			"'!='", "'<'", "'<='", "'>'", "'>='", "'&&'", "'||'", "'++'", "'--'", 
			null, null, null, "'('", "')'", "'{'", "'}'", "';'", "','", "'.'", "':'"
		};
	}
	private static final String[] _LITERAL_NAMES = makeLiteralNames();
	private static String[] makeSymbolicNames() {
		return new String[] {
			null, "WS", "TAB", "ENTER", "COMMENT", "LINE_COMMENT", "BOOLEAN", "DOUBLE", 
			"INT", "IF", "ELSE", "WHILE", "FOR", "FALSE", "TRUE", "NULL", "VOID", 
			"ARRAY", "CLASS", "PROGRAM", "STRUCT", "STATIC", "RETURN", "WITH", "PLUS", 
			"MINUS", "MULT", "DIV", "MOD", "ASSIGN", "EQ", "NEQ", "LT", "LE", "GT", 
			"GE", "AND", "OR", "INCREMENT", "DECREMENT", "IDENTIFIER", "INTEGER", 
			"REAL", "LPAREN", "RPAREN", "LBRACE", "RBRACE", "SEMI", "COMMA", "DOT", 
			"COLON"
		};
	}
	private static final String[] _SYMBOLIC_NAMES = makeSymbolicNames();
	public static final Vocabulary VOCABULARY = new VocabularyImpl(_LITERAL_NAMES, _SYMBOLIC_NAMES);

	/**
	 * @deprecated Use {@link #VOCABULARY} instead.
	 */
	@Deprecated
	public static final String[] tokenNames;
	static {
		tokenNames = new String[_SYMBOLIC_NAMES.length];
		for (int i = 0; i < tokenNames.length; i++) {
			tokenNames[i] = VOCABULARY.getLiteralName(i);
			if (tokenNames[i] == null) {
				tokenNames[i] = VOCABULARY.getSymbolicName(i);
			}

			if (tokenNames[i] == null) {
				tokenNames[i] = "<INVALID>";
			}
		}
	}

	@Override
	@Deprecated
	public String[] getTokenNames() {
		return tokenNames;
	}

	@Override

	public Vocabulary getVocabulary() {
		return VOCABULARY;
	}

	@Override
	public String getGrammarFileName() { return "Simple.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public SimpleParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ProgramContext extends ParserRuleContext {
		public TerminalNode PROGRAM() { return getToken(SimpleParser.PROGRAM, 0); }
		public TerminalNode IDENTIFIER() { return getToken(SimpleParser.IDENTIFIER, 0); }
		public TerminalNode LBRACE() { return getToken(SimpleParser.LBRACE, 0); }
		public TerminalNode RBRACE() { return getToken(SimpleParser.RBRACE, 0); }
		public TerminalNode EOF() { return getToken(SimpleParser.EOF, 0); }
		public List<MemberContext> member() {
			return getRuleContexts(MemberContext.class);
		}
		public MemberContext member(int i) {
			return getRuleContext(MemberContext.class,i);
		}
		public ProgramContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_program; }
	}

	public final ProgramContext program() throws RecognitionException {
		ProgramContext _localctx = new ProgramContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_program);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(32);
			match(PROGRAM);
			setState(33);
			match(IDENTIFIER);
			setState(34);
			match(LBRACE);
			setState(38);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & 1099512742336L) != 0)) {
				{
				{
				setState(35);
				member();
				}
				}
				setState(40);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(41);
			match(RBRACE);
			setState(42);
			match(EOF);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class MemberContext extends ParserRuleContext {
		public FunctionContext function() {
			return getRuleContext(FunctionContext.class,0);
		}
		public StructContext struct() {
			return getRuleContext(StructContext.class,0);
		}
		public GlobalContext global() {
			return getRuleContext(GlobalContext.class,0);
		}
		public MemberContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_member; }
	}

	public final MemberContext member() throws RecognitionException {
		MemberContext _localctx = new MemberContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_member);
		try {
			setState(47);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,1,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(44);
				function();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(45);
				struct();
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(46);
				global();
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class FunctionContext extends ParserRuleContext {
		public TerminalNode IDENTIFIER() { return getToken(SimpleParser.IDENTIFIER, 0); }
		public TerminalNode LPAREN() { return getToken(SimpleParser.LPAREN, 0); }
		public TerminalNode RPAREN() { return getToken(SimpleParser.RPAREN, 0); }
		public TerminalNode LBRACE() { return getToken(SimpleParser.LBRACE, 0); }
		public TerminalNode RBRACE() { return getToken(SimpleParser.RBRACE, 0); }
		public TypeContext type() {
			return getRuleContext(TypeContext.class,0);
		}
		public TerminalNode VOID() { return getToken(SimpleParser.VOID, 0); }
		public ArgumentsContext arguments() {
			return getRuleContext(ArgumentsContext.class,0);
		}
		public List<StatementContext> statement() {
			return getRuleContexts(StatementContext.class);
		}
		public StatementContext statement(int i) {
			return getRuleContext(StatementContext.class,i);
		}
		public FunctionContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_function; }
	}

	public final FunctionContext function() throws RecognitionException {
		FunctionContext _localctx = new FunctionContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_function);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(51);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case BOOLEAN:
			case DOUBLE:
			case INT:
			case IDENTIFIER:
				{
				setState(49);
				type();
				}
				break;
			case VOID:
				{
				setState(50);
				match(VOID);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
			setState(53);
			match(IDENTIFIER);
			setState(54);
			match(LPAREN);
			setState(56);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if ((((_la) & ~0x3f) == 0 && ((1L << _la) & 1099511628224L) != 0)) {
				{
				setState(55);
				arguments();
				}
			}

			setState(58);
			match(RPAREN);
			setState(59);
			match(LBRACE);
			setState(63);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & 193239231560640L) != 0)) {
				{
				{
				setState(60);
				statement();
				}
				}
				setState(65);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(66);
			match(RBRACE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ArgumentsContext extends ParserRuleContext {
		public List<ArgumentContext> argument() {
			return getRuleContexts(ArgumentContext.class);
		}
		public ArgumentContext argument(int i) {
			return getRuleContext(ArgumentContext.class,i);
		}
		public List<TerminalNode> COMMA() { return getTokens(SimpleParser.COMMA); }
		public TerminalNode COMMA(int i) {
			return getToken(SimpleParser.COMMA, i);
		}
		public ArgumentsContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_arguments; }
	}

	public final ArgumentsContext arguments() throws RecognitionException {
		ArgumentsContext _localctx = new ArgumentsContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_arguments);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(68);
			argument();
			setState(73);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==COMMA) {
				{
				{
				setState(69);
				match(COMMA);
				setState(70);
				argument();
				}
				}
				setState(75);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ArgumentContext extends ParserRuleContext {
		public TypeContext type() {
			return getRuleContext(TypeContext.class,0);
		}
		public TerminalNode IDENTIFIER() { return getToken(SimpleParser.IDENTIFIER, 0); }
		public ArgumentContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_argument; }
	}

	public final ArgumentContext argument() throws RecognitionException {
		ArgumentContext _localctx = new ArgumentContext(_ctx, getState());
		enterRule(_localctx, 8, RULE_argument);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(76);
			type();
			setState(77);
			match(IDENTIFIER);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class StructContext extends ParserRuleContext {
		public TerminalNode STRUCT() { return getToken(SimpleParser.STRUCT, 0); }
		public List<TerminalNode> IDENTIFIER() { return getTokens(SimpleParser.IDENTIFIER); }
		public TerminalNode IDENTIFIER(int i) {
			return getToken(SimpleParser.IDENTIFIER, i);
		}
		public TerminalNode LBRACE() { return getToken(SimpleParser.LBRACE, 0); }
		public Struct_membersContext struct_members() {
			return getRuleContext(Struct_membersContext.class,0);
		}
		public TerminalNode RBRACE() { return getToken(SimpleParser.RBRACE, 0); }
		public TerminalNode COLON() { return getToken(SimpleParser.COLON, 0); }
		public StructContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_struct; }
	}

	public final StructContext struct() throws RecognitionException {
		StructContext _localctx = new StructContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_struct);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(79);
			match(STRUCT);
			setState(80);
			match(IDENTIFIER);
			setState(83);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==COLON) {
				{
				setState(81);
				match(COLON);
				setState(82);
				match(IDENTIFIER);
				}
			}

			setState(85);
			match(LBRACE);
			setState(86);
			struct_members();
			setState(87);
			match(RBRACE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Struct_membersContext extends ParserRuleContext {
		public List<Struct_memberContext> struct_member() {
			return getRuleContexts(Struct_memberContext.class);
		}
		public Struct_memberContext struct_member(int i) {
			return getRuleContext(Struct_memberContext.class,i);
		}
		public Struct_membersContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_struct_members; }
	}

	public final Struct_membersContext struct_members() throws RecognitionException {
		Struct_membersContext _localctx = new Struct_membersContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_struct_members);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(92);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & 1099513725376L) != 0)) {
				{
				{
				setState(89);
				struct_member();
				}
				}
				setState(94);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Struct_memberContext extends ParserRuleContext {
		public TypeContext type() {
			return getRuleContext(TypeContext.class,0);
		}
		public VariableContext variable() {
			return getRuleContext(VariableContext.class,0);
		}
		public TerminalNode SEMI() { return getToken(SimpleParser.SEMI, 0); }
		public TerminalNode STATIC() { return getToken(SimpleParser.STATIC, 0); }
		public Struct_memberContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_struct_member; }
	}

	public final Struct_memberContext struct_member() throws RecognitionException {
		Struct_memberContext _localctx = new Struct_memberContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_struct_member);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(96);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==STATIC) {
				{
				setState(95);
				match(STATIC);
				}
			}

			setState(98);
			type();
			setState(99);
			variable();
			setState(100);
			match(SEMI);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class GlobalContext extends ParserRuleContext {
		public TypeContext type() {
			return getRuleContext(TypeContext.class,0);
		}
		public VariablesContext variables() {
			return getRuleContext(VariablesContext.class,0);
		}
		public TerminalNode SEMI() { return getToken(SimpleParser.SEMI, 0); }
		public GlobalContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_global; }
	}

	public final GlobalContext global() throws RecognitionException {
		GlobalContext _localctx = new GlobalContext(_ctx, getState());
		enterRule(_localctx, 16, RULE_global);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(102);
			type();
			setState(103);
			variables();
			setState(104);
			match(SEMI);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class VariablesContext extends ParserRuleContext {
		public List<VariableContext> variable() {
			return getRuleContexts(VariableContext.class);
		}
		public VariableContext variable(int i) {
			return getRuleContext(VariableContext.class,i);
		}
		public List<TerminalNode> COMMA() { return getTokens(SimpleParser.COMMA); }
		public TerminalNode COMMA(int i) {
			return getToken(SimpleParser.COMMA, i);
		}
		public VariablesContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_variables; }
	}

	public final VariablesContext variables() throws RecognitionException {
		VariablesContext _localctx = new VariablesContext(_ctx, getState());
		enterRule(_localctx, 18, RULE_variables);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(106);
			variable();
			setState(111);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==COMMA) {
				{
				{
				setState(107);
				match(COMMA);
				setState(108);
				variable();
				}
				}
				setState(113);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class VariableContext extends ParserRuleContext {
		public TerminalNode IDENTIFIER() { return getToken(SimpleParser.IDENTIFIER, 0); }
		public TerminalNode ASSIGN() { return getToken(SimpleParser.ASSIGN, 0); }
		public ExpressionContext expression() {
			return getRuleContext(ExpressionContext.class,0);
		}
		public VariableContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_variable; }
	}

	public final VariableContext variable() throws RecognitionException {
		VariableContext _localctx = new VariableContext(_ctx, getState());
		enterRule(_localctx, 20, RULE_variable);
		try {
			setState(118);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,10,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(114);
				match(IDENTIFIER);
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(115);
				match(IDENTIFIER);
				setState(116);
				match(ASSIGN);
				setState(117);
				expression(0);
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class TypeContext extends ParserRuleContext {
		public TerminalNode BOOLEAN() { return getToken(SimpleParser.BOOLEAN, 0); }
		public TerminalNode INT() { return getToken(SimpleParser.INT, 0); }
		public TerminalNode DOUBLE() { return getToken(SimpleParser.DOUBLE, 0); }
		public TerminalNode IDENTIFIER() { return getToken(SimpleParser.IDENTIFIER, 0); }
		public TypeContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_type; }
	}

	public final TypeContext type() throws RecognitionException {
		TypeContext _localctx = new TypeContext(_ctx, getState());
		enterRule(_localctx, 22, RULE_type);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(120);
			_la = _input.LA(1);
			if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & 1099511628224L) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class ExpressionContext extends ParserRuleContext {
		public TerminalNode IDENTIFIER() { return getToken(SimpleParser.IDENTIFIER, 0); }
		public TerminalNode LPAREN() { return getToken(SimpleParser.LPAREN, 0); }
		public TerminalNode RPAREN() { return getToken(SimpleParser.RPAREN, 0); }
		public Expr_listContext expr_list() {
			return getRuleContext(Expr_listContext.class,0);
		}
		public TerminalNode INTEGER() { return getToken(SimpleParser.INTEGER, 0); }
		public TerminalNode REAL() { return getToken(SimpleParser.REAL, 0); }
		public TerminalNode TRUE() { return getToken(SimpleParser.TRUE, 0); }
		public TerminalNode FALSE() { return getToken(SimpleParser.FALSE, 0); }
		public TerminalNode NULL() { return getToken(SimpleParser.NULL, 0); }
		public TerminalNode ASSIGN() { return getToken(SimpleParser.ASSIGN, 0); }
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode PLUS() { return getToken(SimpleParser.PLUS, 0); }
		public TerminalNode MINUS() { return getToken(SimpleParser.MINUS, 0); }
		public TerminalNode INCREMENT() { return getToken(SimpleParser.INCREMENT, 0); }
		public TerminalNode DECREMENT() { return getToken(SimpleParser.DECREMENT, 0); }
		public TerminalNode DOT() { return getToken(SimpleParser.DOT, 0); }
		public BinaryOpContext binaryOp() {
			return getRuleContext(BinaryOpContext.class,0);
		}
		public ExpressionContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expression; }
	}

	public final ExpressionContext expression() throws RecognitionException {
		return expression(0);
	}

	private ExpressionContext expression(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		ExpressionContext _localctx = new ExpressionContext(_ctx, _parentState);
		ExpressionContext _prevctx = _localctx;
		int _startState = 24;
		enterRecursionRule(_localctx, 24, RULE_expression, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(148);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,12,_ctx) ) {
			case 1:
				{
				setState(123);
				match(IDENTIFIER);
				setState(124);
				match(LPAREN);
				setState(126);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & 17317358526464L) != 0)) {
					{
					setState(125);
					expr_list();
					}
				}

				setState(128);
				match(RPAREN);
				}
				break;
			case 2:
				{
				setState(129);
				match(INTEGER);
				}
				break;
			case 3:
				{
				setState(130);
				match(REAL);
				}
				break;
			case 4:
				{
				setState(131);
				match(TRUE);
				}
				break;
			case 5:
				{
				setState(132);
				match(FALSE);
				}
				break;
			case 6:
				{
				setState(133);
				match(NULL);
				}
				break;
			case 7:
				{
				setState(134);
				match(IDENTIFIER);
				}
				break;
			case 8:
				{
				setState(135);
				match(IDENTIFIER);
				setState(136);
				match(ASSIGN);
				setState(137);
				expression(7);
				}
				break;
			case 9:
				{
				setState(138);
				match(LPAREN);
				setState(139);
				expression(0);
				setState(140);
				match(RPAREN);
				}
				break;
			case 10:
				{
				setState(142);
				_la = _input.LA(1);
				if ( !(_la==PLUS || _la==MINUS) ) {
				_errHandler.recoverInline(this);
				}
				else {
					if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
					_errHandler.reportMatch(this);
					consume();
				}
				setState(143);
				expression(5);
				}
				break;
			case 11:
				{
				setState(144);
				match(INCREMENT);
				setState(145);
				expression(2);
				}
				break;
			case 12:
				{
				setState(146);
				match(DECREMENT);
				setState(147);
				expression(1);
				}
				break;
			}
			_ctx.stop = _input.LT(-1);
			setState(168);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,14,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					setState(166);
					_errHandler.sync(this);
					switch ( getInterpreter().adaptivePredict(_input,13,_ctx) ) {
					case 1:
						{
						_localctx = new ExpressionContext(_parentctx, _parentState);
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(150);
						if (!(precpred(_ctx, 16))) throw new FailedPredicateException(this, "precpred(_ctx, 16)");
						setState(151);
						match(DOT);
						setState(152);
						match(IDENTIFIER);
						setState(153);
						match(ASSIGN);
						setState(154);
						expression(17);
						}
						break;
					case 2:
						{
						_localctx = new ExpressionContext(_parentctx, _parentState);
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(155);
						if (!(precpred(_ctx, 14))) throw new FailedPredicateException(this, "precpred(_ctx, 14)");
						setState(156);
						binaryOp();
						setState(157);
						expression(15);
						}
						break;
					case 3:
						{
						_localctx = new ExpressionContext(_parentctx, _parentState);
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(159);
						if (!(precpred(_ctx, 17))) throw new FailedPredicateException(this, "precpred(_ctx, 17)");
						setState(160);
						match(DOT);
						setState(161);
						match(IDENTIFIER);
						}
						break;
					case 4:
						{
						_localctx = new ExpressionContext(_parentctx, _parentState);
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(162);
						if (!(precpred(_ctx, 4))) throw new FailedPredicateException(this, "precpred(_ctx, 4)");
						setState(163);
						match(INCREMENT);
						}
						break;
					case 5:
						{
						_localctx = new ExpressionContext(_parentctx, _parentState);
						pushNewRecursionContext(_localctx, _startState, RULE_expression);
						setState(164);
						if (!(precpred(_ctx, 3))) throw new FailedPredicateException(this, "precpred(_ctx, 3)");
						setState(165);
						match(DECREMENT);
						}
						break;
					}
					} 
				}
				setState(170);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,14,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			unrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class Expr_listContext extends ParserRuleContext {
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public List<TerminalNode> COMMA() { return getTokens(SimpleParser.COMMA); }
		public TerminalNode COMMA(int i) {
			return getToken(SimpleParser.COMMA, i);
		}
		public Expr_listContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expr_list; }
	}

	public final Expr_listContext expr_list() throws RecognitionException {
		Expr_listContext _localctx = new Expr_listContext(_ctx, getState());
		enterRule(_localctx, 26, RULE_expr_list);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(171);
			expression(0);
			setState(176);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==COMMA) {
				{
				{
				setState(172);
				match(COMMA);
				setState(173);
				expression(0);
				}
				}
				setState(178);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class BinaryOpContext extends ParserRuleContext {
		public TerminalNode EQ() { return getToken(SimpleParser.EQ, 0); }
		public TerminalNode NEQ() { return getToken(SimpleParser.NEQ, 0); }
		public TerminalNode LT() { return getToken(SimpleParser.LT, 0); }
		public TerminalNode LE() { return getToken(SimpleParser.LE, 0); }
		public TerminalNode GT() { return getToken(SimpleParser.GT, 0); }
		public TerminalNode GE() { return getToken(SimpleParser.GE, 0); }
		public TerminalNode PLUS() { return getToken(SimpleParser.PLUS, 0); }
		public TerminalNode MINUS() { return getToken(SimpleParser.MINUS, 0); }
		public TerminalNode MULT() { return getToken(SimpleParser.MULT, 0); }
		public TerminalNode DIV() { return getToken(SimpleParser.DIV, 0); }
		public TerminalNode MOD() { return getToken(SimpleParser.MOD, 0); }
		public TerminalNode AND() { return getToken(SimpleParser.AND, 0); }
		public TerminalNode OR() { return getToken(SimpleParser.OR, 0); }
		public BinaryOpContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_binaryOp; }
	}

	public final BinaryOpContext binaryOp() throws RecognitionException {
		BinaryOpContext _localctx = new BinaryOpContext(_ctx, getState());
		enterRule(_localctx, 28, RULE_binaryOp);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(179);
			_la = _input.LA(1);
			if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & 274324258816L) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	@SuppressWarnings("CheckReturnValue")
	public static class StatementContext extends ParserRuleContext {
		public TerminalNode IF() { return getToken(SimpleParser.IF, 0); }
		public TerminalNode LPAREN() { return getToken(SimpleParser.LPAREN, 0); }
		public List<ExpressionContext> expression() {
			return getRuleContexts(ExpressionContext.class);
		}
		public ExpressionContext expression(int i) {
			return getRuleContext(ExpressionContext.class,i);
		}
		public TerminalNode RPAREN() { return getToken(SimpleParser.RPAREN, 0); }
		public List<StatementContext> statement() {
			return getRuleContexts(StatementContext.class);
		}
		public StatementContext statement(int i) {
			return getRuleContext(StatementContext.class,i);
		}
		public TerminalNode ELSE() { return getToken(SimpleParser.ELSE, 0); }
		public TerminalNode WHILE() { return getToken(SimpleParser.WHILE, 0); }
		public TerminalNode FOR() { return getToken(SimpleParser.FOR, 0); }
		public TypeContext type() {
			return getRuleContext(TypeContext.class,0);
		}
		public VariablesContext variables() {
			return getRuleContext(VariablesContext.class,0);
		}
		public List<TerminalNode> SEMI() { return getTokens(SimpleParser.SEMI); }
		public TerminalNode SEMI(int i) {
			return getToken(SimpleParser.SEMI, i);
		}
		public TerminalNode WITH() { return getToken(SimpleParser.WITH, 0); }
		public TerminalNode IDENTIFIER() { return getToken(SimpleParser.IDENTIFIER, 0); }
		public TerminalNode LBRACE() { return getToken(SimpleParser.LBRACE, 0); }
		public TerminalNode RBRACE() { return getToken(SimpleParser.RBRACE, 0); }
		public TerminalNode RETURN() { return getToken(SimpleParser.RETURN, 0); }
		public StatementContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_statement; }
	}

	public final StatementContext statement() throws RecognitionException {
		StatementContext _localctx = new StatementContext(_ctx, getState());
		enterRule(_localctx, 30, RULE_statement);
		int _la;
		try {
			setState(242);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,20,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(181);
				match(IF);
				setState(182);
				match(LPAREN);
				setState(183);
				expression(0);
				setState(184);
				match(RPAREN);
				setState(185);
				statement();
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(187);
				match(IF);
				setState(188);
				match(LPAREN);
				setState(189);
				expression(0);
				setState(190);
				match(RPAREN);
				setState(191);
				statement();
				setState(192);
				match(ELSE);
				setState(193);
				statement();
				}
				break;
			case 3:
				enterOuterAlt(_localctx, 3);
				{
				setState(195);
				match(WHILE);
				setState(196);
				match(LPAREN);
				setState(197);
				expression(0);
				setState(198);
				match(RPAREN);
				setState(199);
				statement();
				}
				break;
			case 4:
				enterOuterAlt(_localctx, 4);
				{
				setState(201);
				match(FOR);
				setState(202);
				match(LPAREN);
				setState(203);
				type();
				setState(204);
				variables();
				setState(205);
				match(SEMI);
				setState(207);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & 17317358526464L) != 0)) {
					{
					setState(206);
					expression(0);
					}
				}

				setState(209);
				match(SEMI);
				setState(211);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & 17317358526464L) != 0)) {
					{
					setState(210);
					expression(0);
					}
				}

				setState(213);
				match(RPAREN);
				setState(214);
				statement();
				}
				break;
			case 5:
				enterOuterAlt(_localctx, 5);
				{
				setState(216);
				match(WITH);
				setState(217);
				match(LPAREN);
				setState(218);
				match(IDENTIFIER);
				setState(219);
				match(RPAREN);
				setState(220);
				statement();
				}
				break;
			case 6:
				enterOuterAlt(_localctx, 6);
				{
				setState(221);
				expression(0);
				setState(222);
				match(SEMI);
				}
				break;
			case 7:
				enterOuterAlt(_localctx, 7);
				{
				setState(224);
				type();
				setState(225);
				variables();
				setState(226);
				match(SEMI);
				}
				break;
			case 8:
				enterOuterAlt(_localctx, 8);
				{
				setState(228);
				match(SEMI);
				}
				break;
			case 9:
				enterOuterAlt(_localctx, 9);
				{
				setState(229);
				match(LBRACE);
				setState(233);
				_errHandler.sync(this);
				_la = _input.LA(1);
				while ((((_la) & ~0x3f) == 0 && ((1L << _la) & 193239231560640L) != 0)) {
					{
					{
					setState(230);
					statement();
					}
					}
					setState(235);
					_errHandler.sync(this);
					_la = _input.LA(1);
				}
				setState(236);
				match(RBRACE);
				}
				break;
			case 10:
				enterOuterAlt(_localctx, 10);
				{
				setState(237);
				match(RETURN);
				setState(239);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & 17317358526464L) != 0)) {
					{
					setState(238);
					expression(0);
					}
				}

				setState(241);
				match(SEMI);
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public boolean sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 12:
			return expression_sempred((ExpressionContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean expression_sempred(ExpressionContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 16);
		case 1:
			return precpred(_ctx, 14);
		case 2:
			return precpred(_ctx, 17);
		case 3:
			return precpred(_ctx, 4);
		case 4:
			return precpred(_ctx, 3);
		}
		return true;
	}

	public static final String _serializedATN =
		"\u0004\u00012\u00f5\u0002\u0000\u0007\u0000\u0002\u0001\u0007\u0001\u0002"+
		"\u0002\u0007\u0002\u0002\u0003\u0007\u0003\u0002\u0004\u0007\u0004\u0002"+
		"\u0005\u0007\u0005\u0002\u0006\u0007\u0006\u0002\u0007\u0007\u0007\u0002"+
		"\b\u0007\b\u0002\t\u0007\t\u0002\n\u0007\n\u0002\u000b\u0007\u000b\u0002"+
		"\f\u0007\f\u0002\r\u0007\r\u0002\u000e\u0007\u000e\u0002\u000f\u0007\u000f"+
		"\u0001\u0000\u0001\u0000\u0001\u0000\u0001\u0000\u0005\u0000%\b\u0000"+
		"\n\u0000\f\u0000(\t\u0000\u0001\u0000\u0001\u0000\u0001\u0000\u0001\u0001"+
		"\u0001\u0001\u0001\u0001\u0003\u00010\b\u0001\u0001\u0002\u0001\u0002"+
		"\u0003\u00024\b\u0002\u0001\u0002\u0001\u0002\u0001\u0002\u0003\u0002"+
		"9\b\u0002\u0001\u0002\u0001\u0002\u0001\u0002\u0005\u0002>\b\u0002\n\u0002"+
		"\f\u0002A\t\u0002\u0001\u0002\u0001\u0002\u0001\u0003\u0001\u0003\u0001"+
		"\u0003\u0005\u0003H\b\u0003\n\u0003\f\u0003K\t\u0003\u0001\u0004\u0001"+
		"\u0004\u0001\u0004\u0001\u0005\u0001\u0005\u0001\u0005\u0001\u0005\u0003"+
		"\u0005T\b\u0005\u0001\u0005\u0001\u0005\u0001\u0005\u0001\u0005\u0001"+
		"\u0006\u0005\u0006[\b\u0006\n\u0006\f\u0006^\t\u0006\u0001\u0007\u0003"+
		"\u0007a\b\u0007\u0001\u0007\u0001\u0007\u0001\u0007\u0001\u0007\u0001"+
		"\b\u0001\b\u0001\b\u0001\b\u0001\t\u0001\t\u0001\t\u0005\tn\b\t\n\t\f"+
		"\tq\t\t\u0001\n\u0001\n\u0001\n\u0001\n\u0003\nw\b\n\u0001\u000b\u0001"+
		"\u000b\u0001\f\u0001\f\u0001\f\u0001\f\u0003\f\u007f\b\f\u0001\f\u0001"+
		"\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001"+
		"\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001"+
		"\f\u0003\f\u0095\b\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001"+
		"\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001\f\u0001"+
		"\f\u0005\f\u00a7\b\f\n\f\f\f\u00aa\t\f\u0001\r\u0001\r\u0001\r\u0005\r"+
		"\u00af\b\r\n\r\f\r\u00b2\t\r\u0001\u000e\u0001\u000e\u0001\u000f\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001"+
		"\u000f\u0003\u000f\u00d0\b\u000f\u0001\u000f\u0001\u000f\u0003\u000f\u00d4"+
		"\b\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001"+
		"\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001\u000f\u0001"+
		"\u000f\u0005\u000f\u00e8\b\u000f\n\u000f\f\u000f\u00eb\t\u000f\u0001\u000f"+
		"\u0001\u000f\u0001\u000f\u0003\u000f\u00f0\b\u000f\u0001\u000f\u0003\u000f"+
		"\u00f3\b\u000f\u0001\u000f\u0000\u0001\u0018\u0010\u0000\u0002\u0004\u0006"+
		"\b\n\f\u000e\u0010\u0012\u0014\u0016\u0018\u001a\u001c\u001e\u0000\u0003"+
		"\u0002\u0000\u0006\b((\u0001\u0000\u0018\u0019\u0002\u0000\u0018\u001c"+
		"\u001e%\u010f\u0000 \u0001\u0000\u0000\u0000\u0002/\u0001\u0000\u0000"+
		"\u0000\u00043\u0001\u0000\u0000\u0000\u0006D\u0001\u0000\u0000\u0000\b"+
		"L\u0001\u0000\u0000\u0000\nO\u0001\u0000\u0000\u0000\f\\\u0001\u0000\u0000"+
		"\u0000\u000e`\u0001\u0000\u0000\u0000\u0010f\u0001\u0000\u0000\u0000\u0012"+
		"j\u0001\u0000\u0000\u0000\u0014v\u0001\u0000\u0000\u0000\u0016x\u0001"+
		"\u0000\u0000\u0000\u0018\u0094\u0001\u0000\u0000\u0000\u001a\u00ab\u0001"+
		"\u0000\u0000\u0000\u001c\u00b3\u0001\u0000\u0000\u0000\u001e\u00f2\u0001"+
		"\u0000\u0000\u0000 !\u0005\u0013\u0000\u0000!\"\u0005(\u0000\u0000\"&"+
		"\u0005-\u0000\u0000#%\u0003\u0002\u0001\u0000$#\u0001\u0000\u0000\u0000"+
		"%(\u0001\u0000\u0000\u0000&$\u0001\u0000\u0000\u0000&\'\u0001\u0000\u0000"+
		"\u0000\')\u0001\u0000\u0000\u0000(&\u0001\u0000\u0000\u0000)*\u0005.\u0000"+
		"\u0000*+\u0005\u0000\u0000\u0001+\u0001\u0001\u0000\u0000\u0000,0\u0003"+
		"\u0004\u0002\u0000-0\u0003\n\u0005\u0000.0\u0003\u0010\b\u0000/,\u0001"+
		"\u0000\u0000\u0000/-\u0001\u0000\u0000\u0000/.\u0001\u0000\u0000\u0000"+
		"0\u0003\u0001\u0000\u0000\u000014\u0003\u0016\u000b\u000024\u0005\u0010"+
		"\u0000\u000031\u0001\u0000\u0000\u000032\u0001\u0000\u0000\u000045\u0001"+
		"\u0000\u0000\u000056\u0005(\u0000\u000068\u0005+\u0000\u000079\u0003\u0006"+
		"\u0003\u000087\u0001\u0000\u0000\u000089\u0001\u0000\u0000\u00009:\u0001"+
		"\u0000\u0000\u0000:;\u0005,\u0000\u0000;?\u0005-\u0000\u0000<>\u0003\u001e"+
		"\u000f\u0000=<\u0001\u0000\u0000\u0000>A\u0001\u0000\u0000\u0000?=\u0001"+
		"\u0000\u0000\u0000?@\u0001\u0000\u0000\u0000@B\u0001\u0000\u0000\u0000"+
		"A?\u0001\u0000\u0000\u0000BC\u0005.\u0000\u0000C\u0005\u0001\u0000\u0000"+
		"\u0000DI\u0003\b\u0004\u0000EF\u00050\u0000\u0000FH\u0003\b\u0004\u0000"+
		"GE\u0001\u0000\u0000\u0000HK\u0001\u0000\u0000\u0000IG\u0001\u0000\u0000"+
		"\u0000IJ\u0001\u0000\u0000\u0000J\u0007\u0001\u0000\u0000\u0000KI\u0001"+
		"\u0000\u0000\u0000LM\u0003\u0016\u000b\u0000MN\u0005(\u0000\u0000N\t\u0001"+
		"\u0000\u0000\u0000OP\u0005\u0014\u0000\u0000PS\u0005(\u0000\u0000QR\u0005"+
		"2\u0000\u0000RT\u0005(\u0000\u0000SQ\u0001\u0000\u0000\u0000ST\u0001\u0000"+
		"\u0000\u0000TU\u0001\u0000\u0000\u0000UV\u0005-\u0000\u0000VW\u0003\f"+
		"\u0006\u0000WX\u0005.\u0000\u0000X\u000b\u0001\u0000\u0000\u0000Y[\u0003"+
		"\u000e\u0007\u0000ZY\u0001\u0000\u0000\u0000[^\u0001\u0000\u0000\u0000"+
		"\\Z\u0001\u0000\u0000\u0000\\]\u0001\u0000\u0000\u0000]\r\u0001\u0000"+
		"\u0000\u0000^\\\u0001\u0000\u0000\u0000_a\u0005\u0015\u0000\u0000`_\u0001"+
		"\u0000\u0000\u0000`a\u0001\u0000\u0000\u0000ab\u0001\u0000\u0000\u0000"+
		"bc\u0003\u0016\u000b\u0000cd\u0003\u0014\n\u0000de\u0005/\u0000\u0000"+
		"e\u000f\u0001\u0000\u0000\u0000fg\u0003\u0016\u000b\u0000gh\u0003\u0012"+
		"\t\u0000hi\u0005/\u0000\u0000i\u0011\u0001\u0000\u0000\u0000jo\u0003\u0014"+
		"\n\u0000kl\u00050\u0000\u0000ln\u0003\u0014\n\u0000mk\u0001\u0000\u0000"+
		"\u0000nq\u0001\u0000\u0000\u0000om\u0001\u0000\u0000\u0000op\u0001\u0000"+
		"\u0000\u0000p\u0013\u0001\u0000\u0000\u0000qo\u0001\u0000\u0000\u0000"+
		"rw\u0005(\u0000\u0000st\u0005(\u0000\u0000tu\u0005\u001d\u0000\u0000u"+
		"w\u0003\u0018\f\u0000vr\u0001\u0000\u0000\u0000vs\u0001\u0000\u0000\u0000"+
		"w\u0015\u0001\u0000\u0000\u0000xy\u0007\u0000\u0000\u0000y\u0017\u0001"+
		"\u0000\u0000\u0000z{\u0006\f\uffff\uffff\u0000{|\u0005(\u0000\u0000|~"+
		"\u0005+\u0000\u0000}\u007f\u0003\u001a\r\u0000~}\u0001\u0000\u0000\u0000"+
		"~\u007f\u0001\u0000\u0000\u0000\u007f\u0080\u0001\u0000\u0000\u0000\u0080"+
		"\u0095\u0005,\u0000\u0000\u0081\u0095\u0005)\u0000\u0000\u0082\u0095\u0005"+
		"*\u0000\u0000\u0083\u0095\u0005\u000e\u0000\u0000\u0084\u0095\u0005\r"+
		"\u0000\u0000\u0085\u0095\u0005\u000f\u0000\u0000\u0086\u0095\u0005(\u0000"+
		"\u0000\u0087\u0088\u0005(\u0000\u0000\u0088\u0089\u0005\u001d\u0000\u0000"+
		"\u0089\u0095\u0003\u0018\f\u0007\u008a\u008b\u0005+\u0000\u0000\u008b"+
		"\u008c\u0003\u0018\f\u0000\u008c\u008d\u0005,\u0000\u0000\u008d\u0095"+
		"\u0001\u0000\u0000\u0000\u008e\u008f\u0007\u0001\u0000\u0000\u008f\u0095"+
		"\u0003\u0018\f\u0005\u0090\u0091\u0005&\u0000\u0000\u0091\u0095\u0003"+
		"\u0018\f\u0002\u0092\u0093\u0005\'\u0000\u0000\u0093\u0095\u0003\u0018"+
		"\f\u0001\u0094z\u0001\u0000\u0000\u0000\u0094\u0081\u0001\u0000\u0000"+
		"\u0000\u0094\u0082\u0001\u0000\u0000\u0000\u0094\u0083\u0001\u0000\u0000"+
		"\u0000\u0094\u0084\u0001\u0000\u0000\u0000\u0094\u0085\u0001\u0000\u0000"+
		"\u0000\u0094\u0086\u0001\u0000\u0000\u0000\u0094\u0087\u0001\u0000\u0000"+
		"\u0000\u0094\u008a\u0001\u0000\u0000\u0000\u0094\u008e\u0001\u0000\u0000"+
		"\u0000\u0094\u0090\u0001\u0000\u0000\u0000\u0094\u0092\u0001\u0000\u0000"+
		"\u0000\u0095\u00a8\u0001\u0000\u0000\u0000\u0096\u0097\n\u0010\u0000\u0000"+
		"\u0097\u0098\u00051\u0000\u0000\u0098\u0099\u0005(\u0000\u0000\u0099\u009a"+
		"\u0005\u001d\u0000\u0000\u009a\u00a7\u0003\u0018\f\u0011\u009b\u009c\n"+
		"\u000e\u0000\u0000\u009c\u009d\u0003\u001c\u000e\u0000\u009d\u009e\u0003"+
		"\u0018\f\u000f\u009e\u00a7\u0001\u0000\u0000\u0000\u009f\u00a0\n\u0011"+
		"\u0000\u0000\u00a0\u00a1\u00051\u0000\u0000\u00a1\u00a7\u0005(\u0000\u0000"+
		"\u00a2\u00a3\n\u0004\u0000\u0000\u00a3\u00a7\u0005&\u0000\u0000\u00a4"+
		"\u00a5\n\u0003\u0000\u0000\u00a5\u00a7\u0005\'\u0000\u0000\u00a6\u0096"+
		"\u0001\u0000\u0000\u0000\u00a6\u009b\u0001\u0000\u0000\u0000\u00a6\u009f"+
		"\u0001\u0000\u0000\u0000\u00a6\u00a2\u0001\u0000\u0000\u0000\u00a6\u00a4"+
		"\u0001\u0000\u0000\u0000\u00a7\u00aa\u0001\u0000\u0000\u0000\u00a8\u00a6"+
		"\u0001\u0000\u0000\u0000\u00a8\u00a9\u0001\u0000\u0000\u0000\u00a9\u0019"+
		"\u0001\u0000\u0000\u0000\u00aa\u00a8\u0001\u0000\u0000\u0000\u00ab\u00b0"+
		"\u0003\u0018\f\u0000\u00ac\u00ad\u00050\u0000\u0000\u00ad\u00af\u0003"+
		"\u0018\f\u0000\u00ae\u00ac\u0001\u0000\u0000\u0000\u00af\u00b2\u0001\u0000"+
		"\u0000\u0000\u00b0\u00ae\u0001\u0000\u0000\u0000\u00b0\u00b1\u0001\u0000"+
		"\u0000\u0000\u00b1\u001b\u0001\u0000\u0000\u0000\u00b2\u00b0\u0001\u0000"+
		"\u0000\u0000\u00b3\u00b4\u0007\u0002\u0000\u0000\u00b4\u001d\u0001\u0000"+
		"\u0000\u0000\u00b5\u00b6\u0005\t\u0000\u0000\u00b6\u00b7\u0005+\u0000"+
		"\u0000\u00b7\u00b8\u0003\u0018\f\u0000\u00b8\u00b9\u0005,\u0000\u0000"+
		"\u00b9\u00ba\u0003\u001e\u000f\u0000\u00ba\u00f3\u0001\u0000\u0000\u0000"+
		"\u00bb\u00bc\u0005\t\u0000\u0000\u00bc\u00bd\u0005+\u0000\u0000\u00bd"+
		"\u00be\u0003\u0018\f\u0000\u00be\u00bf\u0005,\u0000\u0000\u00bf\u00c0"+
		"\u0003\u001e\u000f\u0000\u00c0\u00c1\u0005\n\u0000\u0000\u00c1\u00c2\u0003"+
		"\u001e\u000f\u0000\u00c2\u00f3\u0001\u0000\u0000\u0000\u00c3\u00c4\u0005"+
		"\u000b\u0000\u0000\u00c4\u00c5\u0005+\u0000\u0000\u00c5\u00c6\u0003\u0018"+
		"\f\u0000\u00c6\u00c7\u0005,\u0000\u0000\u00c7\u00c8\u0003\u001e\u000f"+
		"\u0000\u00c8\u00f3\u0001\u0000\u0000\u0000\u00c9\u00ca\u0005\f\u0000\u0000"+
		"\u00ca\u00cb\u0005+\u0000\u0000\u00cb\u00cc\u0003\u0016\u000b\u0000\u00cc"+
		"\u00cd\u0003\u0012\t\u0000\u00cd\u00cf\u0005/\u0000\u0000\u00ce\u00d0"+
		"\u0003\u0018\f\u0000\u00cf\u00ce\u0001\u0000\u0000\u0000\u00cf\u00d0\u0001"+
		"\u0000\u0000\u0000\u00d0\u00d1\u0001\u0000\u0000\u0000\u00d1\u00d3\u0005"+
		"/\u0000\u0000\u00d2\u00d4\u0003\u0018\f\u0000\u00d3\u00d2\u0001\u0000"+
		"\u0000\u0000\u00d3\u00d4\u0001\u0000\u0000\u0000\u00d4\u00d5\u0001\u0000"+
		"\u0000\u0000\u00d5\u00d6\u0005,\u0000\u0000\u00d6\u00d7\u0003\u001e\u000f"+
		"\u0000\u00d7\u00f3\u0001\u0000\u0000\u0000\u00d8\u00d9\u0005\u0017\u0000"+
		"\u0000\u00d9\u00da\u0005+\u0000\u0000\u00da\u00db\u0005(\u0000\u0000\u00db"+
		"\u00dc\u0005,\u0000\u0000\u00dc\u00f3\u0003\u001e\u000f\u0000\u00dd\u00de"+
		"\u0003\u0018\f\u0000\u00de\u00df\u0005/\u0000\u0000\u00df\u00f3\u0001"+
		"\u0000\u0000\u0000\u00e0\u00e1\u0003\u0016\u000b\u0000\u00e1\u00e2\u0003"+
		"\u0012\t\u0000\u00e2\u00e3\u0005/\u0000\u0000\u00e3\u00f3\u0001\u0000"+
		"\u0000\u0000\u00e4\u00f3\u0005/\u0000\u0000\u00e5\u00e9\u0005-\u0000\u0000"+
		"\u00e6\u00e8\u0003\u001e\u000f\u0000\u00e7\u00e6\u0001\u0000\u0000\u0000"+
		"\u00e8\u00eb\u0001\u0000\u0000\u0000\u00e9\u00e7\u0001\u0000\u0000\u0000"+
		"\u00e9\u00ea\u0001\u0000\u0000\u0000\u00ea\u00ec\u0001\u0000\u0000\u0000"+
		"\u00eb\u00e9\u0001\u0000\u0000\u0000\u00ec\u00f3\u0005.\u0000\u0000\u00ed"+
		"\u00ef\u0005\u0016\u0000\u0000\u00ee\u00f0\u0003\u0018\f\u0000\u00ef\u00ee"+
		"\u0001\u0000\u0000\u0000\u00ef\u00f0\u0001\u0000\u0000\u0000\u00f0\u00f1"+
		"\u0001\u0000\u0000\u0000\u00f1\u00f3\u0005/\u0000\u0000\u00f2\u00b5\u0001"+
		"\u0000\u0000\u0000\u00f2\u00bb\u0001\u0000\u0000\u0000\u00f2\u00c3\u0001"+
		"\u0000\u0000\u0000\u00f2\u00c9\u0001\u0000\u0000\u0000\u00f2\u00d8\u0001"+
		"\u0000\u0000\u0000\u00f2\u00dd\u0001\u0000\u0000\u0000\u00f2\u00e0\u0001"+
		"\u0000\u0000\u0000\u00f2\u00e4\u0001\u0000\u0000\u0000\u00f2\u00e5\u0001"+
		"\u0000\u0000\u0000\u00f2\u00ed\u0001\u0000\u0000\u0000\u00f3\u001f\u0001"+
		"\u0000\u0000\u0000\u0015&/38?IS\\`ov~\u0094\u00a6\u00a8\u00b0\u00cf\u00d3"+
		"\u00e9\u00ef\u00f2";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}