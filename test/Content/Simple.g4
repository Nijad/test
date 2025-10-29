grammar Simple;

// Lexer Rules
WS: [ \t\r\n]+ -> skip;
TAB: '\t' -> skip;
ENTER: '\r'? '\n' -> skip;

COMMENT: '(*' .*? '*)' -> skip;
LINE_COMMENT: '//' ~[\r\n]* -> skip;

// Keywords
BOOLEAN: 'Boolean';
DOUBLE: 'double';
INT: 'int';
IF: 'if';
ELSE: 'else';
WHILE: 'while';
FOR: 'for';
FALSE: 'false';
TRUE: 'true';
NULL: 'null';
VOID: 'void';
ARRAY: 'array';
CLASS: 'class';
PROGRAM: 'program';
STRUCT: 'struct';
STATIC: 'static';
RETURN: 'return';
WITH: 'with';

// Operators
PLUS: '+';
MINUS: '-';
MULT: '*';
DIV: '/';
MOD: '%';
ASSIGN: '=';
EQ: '==';
NEQ: '!=';
LT: '<';
LE: '<=';
GT: '>';
GE: '>=';
AND: '&&';
OR: '||';
INCREMENT: '++';

// Identifiers and literals
IDENTIFIER: [a-zA-Z][a-zA-Z0-9_]*;
INTEGER: [0-9]+;
REAL: [0-9]+ '.' [0-9]* ([eE] [+-]? [0-9]+)?
      | '.' [0-9]+ ([eE] [+-]? [0-9]+)?
      | [0-9]+ [eE] [+-]? [0-9]+
      ;

// Punctuation
LPAREN: '(';
RPAREN: ')';
LBRACE: '{';
RBRACE: '}';
SEMI: ';';
COMMA: ',';
DOT: '.';
COLON: ':';

// Parser Rules
program: PROGRAM IDENTIFIER LBRACE member* RBRACE EOF;

member: function | struct | global;

function: (type | VOID) IDENTIFIER LPAREN arguments? RPAREN LBRACE statement* RBRACE;

arguments: argument (COMMA argument)*;
argument: type IDENTIFIER;

struct: STRUCT IDENTIFIER (COLON IDENTIFIER)? LBRACE struct_members RBRACE;
struct_members: (STATIC? type variable SEMI)*;

global: type variables SEMI;
variables: variable (COMMA variable)*;
variable: IDENTIFIER | IDENTIFIER ASSIGN expression;

type: BOOLEAN | INT | DOUBLE | IDENTIFIER;

expression: 
    expression DOT IDENTIFIER
    | expression DOT IDENTIFIER ASSIGN expression
    | IDENTIFIER LPAREN expr_list? RPAREN
    | expression binaryOp expression
    | INTEGER
    | REAL
    | TRUE
    | FALSE
    | NULL
    | IDENTIFIER
    | IDENTIFIER ASSIGN expression
    | LPAREN expression RPAREN
    | (PLUS | MINUS) expression
    ;

expr_list: expression (COMMA expression)*;

binaryOp: 
    EQ | NEQ | LT | LE | GT | GE | 
    PLUS | MINUS | MULT | DIV | MOD | AND | OR;

statement:
    IF LPAREN expression RPAREN statement
    | IF LPAREN expression RPAREN statement ELSE statement
    | WHILE LPAREN expression RPAREN statement
    | FOR LPAREN type variables SEMI expression? SEMI expression? RPAREN statement
    | WITH LPAREN IDENTIFIER RPAREN statement
    | expression SEMI
    | type variables SEMI
    | SEMI
    | LBRACE statement* RBRACE
    | RETURN expression? SEMI
    ;