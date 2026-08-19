namespace Fife.Core;

public abstract class Stmt
{
    public abstract T Accept<T>(IVisitor<T> visitor);

    public interface IVisitor<out T>
    {
        T VisitBlockStmt(Block stmt);
        T VisitClassStmt(Class @class);
        T VisitExpressionStmt(Expression stmt);
        T VisitFunctionStmt(Function stmt);
        T VisitIfStmt(If stmt);
        T VisitReturnStmt(Return stmt);
        T VisitVarStmt(Var stmt);
        T VisitWhileStmt(While stmt);
    }

    public sealed class Block(List<Stmt> statements) : Stmt
    {
        public List<Stmt> Statements { get; } = statements;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitBlockStmt(this);
    }

    public sealed class Expression(Expr expression) : Stmt
    {
        public Expr Expr { get; } = expression;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitExpressionStmt(this);
    }

    public sealed class Parameter(Token name, FifeType type)
    {
        public Token Name { get; } = name;
        public FifeType Type { get; } = type;
    }

    public sealed class Function(Token name, List<Parameter> parameters, List<Stmt> body, FifeType returnType) : Stmt
    {
        public Token Name { get; } = name;
        public List<Parameter> Parameters { get; } = parameters;
        public List<Stmt> Body { get; } = body;
        public FifeType ReturnType { get; } = returnType;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitFunctionStmt(this);
    }

    public sealed class If(Expr condition, Stmt thenBranch, Stmt? elseBranch) : Stmt
    {
        public Expr Condition { get; } = condition;
        public Stmt ThenBranch { get; } = thenBranch;
        public Stmt? ElseBranch { get; } = elseBranch;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitIfStmt(this);
    }

    public sealed class Return(Token keyword, Expr? value) : Stmt
    {
        public Token Keyword { get; } = keyword;
        public Expr? Value { get; } = value;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitReturnStmt(this);
    }

    public sealed class Var(Token name, Expr? initializer, FifeType type = FifeType.Dynamic) : Stmt
    {
        public Token Name { get; } = name;
        public Expr? Initializer { get; } = initializer;
        public FifeType Type { get; } = type;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitVarStmt(this);
    }

    public sealed class While(Expr condition, Stmt body) : Stmt
    {
        public Expr Condition { get; } = condition;
        public Stmt Body { get; } = body;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitWhileStmt(this);
    }

    public sealed class Class(Token name, Expr.Variable? superclass, List<Stmt.Function> methods) : Stmt
    {
        public Token Name { get; } = name;
        public Expr.Variable? Superclass { get; } = superclass;
        public List<Stmt.Function> Methods { get; } = methods;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitClassStmt(this);
    }
}
