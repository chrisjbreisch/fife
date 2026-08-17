using System.Text;

namespace Fife.Core;

/// <summary>Renders an AST as parenthesized S-expressions. Handy for debugging the parser.</summary>
public sealed class AstPrinter : Expr.IVisitor<string>, Stmt.IVisitor<string>
{
    public string Print(Expr expr) => expr.Accept(this);

    public string Print(IEnumerable<Stmt> statements) =>
        string.Join(Environment.NewLine, statements.Select(s => s.Accept(this)));

    public string VisitAssignExpr(Expr.Assign expr) => Parenthesize($"= {expr.Name.Lexeme}", expr.Value);

    public string VisitBinaryExpr(Expr.Binary expr) => Parenthesize(expr.Operator.Lexeme, expr.Left, expr.Right);

    public string VisitCallExpr(Expr.Call expr) => Parenthesize("call", [expr.Callee, .. expr.Arguments]);

    public string VisitGroupingExpr(Expr.Grouping expr) => Parenthesize("group", expr.Expression);

    public string VisitLiteralExpr(Expr.Literal expr) => Interpreter.Stringify(expr.Value);

    public string VisitLogicalExpr(Expr.Logical expr) => Parenthesize(expr.Operator.Lexeme, expr.Left, expr.Right);

    public string VisitUnaryExpr(Expr.Unary expr) => Parenthesize(expr.Operator.Lexeme, expr.Right);

    public string VisitVariableExpr(Expr.Variable expr) => expr.Name.Lexeme;

    public string VisitBlockStmt(Stmt.Block stmt) => $"(block {Print(stmt.Statements)})";

    public string VisitExpressionStmt(Stmt.Expression stmt) => Parenthesize(";", stmt.Expr);

    public string VisitFunctionStmt(Stmt.Function stmt) =>
        $"(fun {stmt.Name.Lexeme}({string.Join(' ', stmt.Parameters.Select(p => p.Lexeme))}) {Print(stmt.Body)})";

    public string VisitIfStmt(Stmt.If stmt) => stmt.ElseBranch is null
        ? $"(if {Print(stmt.Condition)} {stmt.ThenBranch.Accept(this)})"
        : $"(if-else {Print(stmt.Condition)} {stmt.ThenBranch.Accept(this)} {stmt.ElseBranch.Accept(this)})";


    public string VisitReturnStmt(Stmt.Return stmt) =>
        stmt.Value is null ? "(return)" : Parenthesize("return", stmt.Value);

    public string VisitVarStmt(Stmt.Var stmt) => stmt.Initializer is null
        ? $"({(stmt.IsInt ? "int" : "var")} {stmt.Name.Lexeme})"
        : Parenthesize($"{(stmt.IsInt ? "int" : "var")} {stmt.Name.Lexeme}", stmt.Initializer);

    public string VisitWhileStmt(Stmt.While stmt) => $"(while {Print(stmt.Condition)} {stmt.Body.Accept(this)})";

    private string Parenthesize(string name, params Expr[] exprs)
    {
        StringBuilder builder = new();
        builder.Append('(').Append(name);
        foreach (var expr in exprs)
        {
            builder.Append(' ').Append(expr.Accept(this));
        }

        return builder.Append(')').ToString();
    }
}
