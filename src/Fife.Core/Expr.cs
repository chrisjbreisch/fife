namespace Fife.Core;

public abstract class Expr
{
    public abstract T Accept<T>(IVisitor<T> visitor);

    public interface IVisitor<out T>
    {
        T VisitAssignExpr(Assign expr);
        T VisitBinaryExpr(Binary expr);
        T VisitCallExpr(Call expr);
        T VisitGetExpr(Get expr);
        T VisitGroupingExpr(Grouping expr);
        T VisitLiteralExpr(Literal expr);
        T VisitLogicalExpr(Logical expr);
        T VisitPostfixExpr(Postfix expr);
        T VisitSetExpr(Set expr);
        T VisitSuperExpr(Super expr);
        T VisitThisExpr(This expr);
        T VisitUnaryExpr(Unary expr);
        T VisitVariableExpr(Variable expr);
    }

    public sealed class Assign(Token name, Expr value) : Expr
    {
        public Token Name { get; } = name;
        public Expr Value { get; } = value;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitAssignExpr(this);
    }

    public sealed class Binary(Expr left, Token op, Expr right) : Expr
    {
        public Expr Left { get; } = left;
        public Token Operator { get; } = op;
        public Expr Right { get; } = right;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitBinaryExpr(this);
    }

    public sealed class Call(Expr callee, Token paren, List<Expr> arguments) : Expr
    {
        public Expr Callee { get; } = callee;
        public Token Paren { get; } = paren;
        public List<Expr> Arguments { get; } = arguments;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitCallExpr(this);
    }

    public sealed class Get(Expr obj, Token name) : Expr
    {
        public Expr Object { get; } = obj;
        public Token Name { get; } = name;

        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitGetExpr(this);
    }

    public sealed class Grouping(Expr expression) : Expr
    {
        public Expr Expression { get; } = expression;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitGroupingExpr(this);
    }

    public sealed class Literal(object? value) : Expr
    {
        public object? Value { get; } = value;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitLiteralExpr(this);
    }

    public sealed class Logical(Expr left, Token op, Expr right) : Expr
    {
        public Expr Left { get; } = left;
        public Token Operator { get; } = op;
        public Expr Right { get; } = right;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitLogicalExpr(this);
    }

    public sealed class Postfix(Token op, Expr operand) : Expr
    {
        public Token Operator { get; } = op;
        public Expr Operand { get; } = operand;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitPostfixExpr(this);
    }

    public sealed class Set(Expr obj, Token name, Expr value) : Expr
    {
        public Expr Object { get; } = obj;
        public Token Name { get; } = name;
        public Expr Value { get; } = value;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitSetExpr(this);
    }

    public sealed class Super(Token keyword, Token method) : Expr
    {
        public Token Keyword { get; } = keyword;
        public Token Method { get; } = method;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitSuperExpr(this);
    }

    public sealed class This(Token keyword) : Expr    {
        public Token Keyword { get; } = keyword;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitThisExpr(this);
    }

    public sealed class Unary(Token op, Expr right) : Expr
    {
        public Token Operator { get; } = op;
        public Expr Right { get; } = right;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitUnaryExpr(this);
    }

    public sealed class Variable(Token name) : Expr
    {
        public Token Name { get; } = name;
        public override T Accept<T>(IVisitor<T> visitor) => visitor.VisitVariableExpr(this);
    }
}
