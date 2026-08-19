using System.Collections;
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

    public string VisitGetExpr(Expr.Get expr) => Parenthesize(".", expr.Object, expr.Name.Lexeme);

    public string VisitGroupingExpr(Expr.Grouping expr) => Parenthesize("group", expr.Expression);

    public string VisitLiteralExpr(Expr.Literal expr) => Interpreter.Stringify(expr.Value);

    public string VisitLogicalExpr(Expr.Logical expr) => Parenthesize(expr.Operator.Lexeme, expr.Left, expr.Right);

    public string VisitPostfixExpr(Expr.Postfix expr) => Parenthesize($"{expr.Operator.Lexeme} postfix", expr.Operand);

    public string VisitSetExpr(Expr.Set expr) => Parenthesize($"=", expr.Object, expr.Name.Lexeme, expr.Value);    public string VisitSuperExpr(Expr.Super expr) => $"(super {expr.Method.Lexeme})";

    public string VisitThisExpr(Expr.This expr) => "this";

    public string VisitUnaryExpr(Expr.Unary expr) => Parenthesize(expr.Operator.Lexeme, expr.Right);

    public string VisitVariableExpr(Expr.Variable expr) => expr.Name.Lexeme;

    public string VisitBlockStmt(Stmt.Block stmt) => $"(block {Print(stmt.Statements)})";

    public string VisitClassStmt(Stmt.Class stmt) => stmt.Superclass is null
        ? $"(class {stmt.Name.Lexeme} {Print(stmt.Methods)})"
        : $"(class {stmt.Name.Lexeme} : {stmt.Superclass.Name.Lexeme} {Print(stmt.Methods)})";

    public string VisitExpressionStmt(Stmt.Expression stmt) => Parenthesize(";", stmt.Expr);

    public string VisitFunctionStmt(Stmt.Function stmt) =>
        $"({FifeTypes.Name(stmt.ReturnType)} fun {stmt.Name.Lexeme}({string.Join(' ', stmt.Parameters.Select(p => $"{FifeTypes.Name(p.Type)} {p.Name.Lexeme}"))}) {Print(stmt.Body)})";

    public string VisitIfStmt(Stmt.If stmt) => stmt.ElseBranch is null
        ? $"(if {Print(stmt.Condition)} {stmt.ThenBranch.Accept(this)})"
        : $"(if-else {Print(stmt.Condition)} {stmt.ThenBranch.Accept(this)} {stmt.ElseBranch.Accept(this)})";


    public string VisitReturnStmt(Stmt.Return stmt) =>
        stmt.Value is null ? "(return)" : Parenthesize("return", stmt.Value);

    public string VisitVarStmt(Stmt.Var stmt) => stmt.Initializer is null
        ? $"({FifeTypes.Name(stmt.Type)} {stmt.Name.Lexeme})"
        : Parenthesize($"{FifeTypes.Name(stmt.Type)} {stmt.Name.Lexeme}", stmt.Initializer);

    public string VisitWhileStmt(Stmt.While stmt) => $"(while {Print(stmt.Condition)} {stmt.Body.Accept(this)})";

    private string Parenthesize(string name, params object[] parts)
    {
        StringBuilder builder = new();
        builder.Append('(').Append(name);
        Transform(builder, parts);
        builder.Append(')');

        return builder.ToString();
    }

    private void Transform(StringBuilder builder, object[] parts)
    {
        foreach (var part in parts)
        {
            builder.Append(' ');
            switch (part)
            {
                case Expr expr:
                    builder.Append(expr.Accept(this));
                    break;
                case Stmt stmt:
                    builder.Append(stmt.Accept(this));
                    break;
                case Token token:
                    builder.Append(token.Lexeme);
                    break;
                case IList list:
                    Transform(builder, list.Cast<object>().ToArray());
                    break;
                default:
                    builder.Append(part);
                    break;
            }
        }
    }
}
