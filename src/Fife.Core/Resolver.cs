
namespace Fife.Core;

public sealed class Resolver(Interpreter interpreter, IErrorReporter errors) : Expr.IVisitor<object?>, Stmt.IVisitor<object?>
{
    private readonly Stack<Dictionary<string, bool>> _scopes = [];

    private enum FunctionType
    {
        None,
        Constructor,
        Function,
        Method
    }

    private enum ClassType
    {
        None,
        Class
    }

    private FunctionType _currentFunction = FunctionType.None;
    private ClassType _currentClass = ClassType.None;

    public object? VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);

        return null;
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);

        return null;
    }

    public object? VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.Callee);
            
        foreach (var argument in expr.Arguments)
            Resolve(argument);

        return null;
    }

    public object? VisitGetExpr(Expr.Get expr)
    {
        Resolve(expr.Object);

        return null;
    }

    public object? VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.Expression);

        return null;
    }

    public object? VisitLiteralExpr(Expr.Literal expr)
    {
        return null;
    }

    public object? VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);

        return null;
    }

    public object? VisitPostfixExpr(Expr.Postfix expr)
    {
        Resolve(expr.Operand);

        return null;
    }

    public object? VisitSetExpr(Expr.Set expr)
    {
        Resolve(expr.Value);
        Resolve(expr.Object);

        return null;
    }

    public object? VisitThisExpr(Expr.This expr)
    {
        if (_currentClass == ClassType.None)
        {
            errors.Error(expr.Keyword, "Can't use 'this' outside of a class.");
            return null;
        }

        ResolveLocal(expr, expr.Keyword);

        return null;
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.Right);

        return null;
    }

    public object? VisitVariableExpr(Expr.Variable expr)
    {
        if (_scopes.Count > 0 && 
            _scopes.Peek().TryGetValue(expr.Name.Lexeme, out var defined) && !defined)
            errors.Error(expr.Name, "Can't read local variable in its own initializer.");

        ResolveLocal(expr, expr.Name);

        return null;
    }


    public object? VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();

        return null;
    }

    public object? VisitClassStmt(Stmt.Class stmt)
    {
        ClassType enclosing = _currentClass;
        _currentClass = ClassType.Class;

        Declare(stmt.Name);
        Define(stmt.Name);

        BeginScope();
        _scopes.Peek()["this"] = true;

        foreach (var method in stmt.Methods)
        {
            var declaration = FunctionType.Method;
            if (method.Name.Lexeme == stmt.Name.Lexeme)
            {
                declaration = FunctionType.Constructor;

                if (method.ReturnType != FifeType.Dynamic)
                    errors.Error(method.Name, "A constructor can't declare a return type.");
            }

            ResolveFunction(method, declaration);
        }

        EndScope();

        _currentClass = enclosing;
        return null;
    }

    public object? VisitExpressionStmt(Stmt.Expression stmt)
    {
        Resolve(stmt.Expr);

        return null;
    }

    public object? VisitFunctionStmt(Stmt.Function stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);

        ResolveFunction(stmt, FunctionType.Function);

        return null;
    }

    public object? VisitIfStmt(Stmt.If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch != null) Resolve(stmt.ElseBranch);

        return null;
    }

    public object? VisitReturnStmt(Stmt.Return stmt)
    {
        if (_currentFunction == FunctionType.None)
            errors.Error(stmt.Keyword, "Can't return from top-level code.");

        if (stmt.Value != null)
        {
            if (_currentFunction == FunctionType.Constructor)
                errors.Error(stmt.Keyword, "Can't return a value from a constructor.");

            Resolve(stmt.Value);
        }

        return null;
    }

    public object? VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.Name);

        if (stmt.Initializer != null)
            Resolve(stmt.Initializer);

        Define(stmt.Name);

        return null;
    }


    public object? VisitWhileStmt(Stmt.While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);

        return null;
    }

    private void BeginScope()
    {
        _scopes.Push(new Dictionary<string, bool>());
    }

    private void EndScope()
    {
        _scopes.Pop();
    }

    public void Resolve(List<Stmt> statements)
    {
        foreach (var statement in statements)
        {
            Resolve(statement);
        }
    }

    public void Resolve(Stmt stmt)
    {
        stmt.Accept(this);
    }

    public void Resolve(Expr expr)
    {
        expr.Accept(this);
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        var distance = 0;
        foreach (var scope in _scopes)          // innermost first
        {
            if (scope.ContainsKey(name.Lexeme))
            {
                interpreter.Resolve(expr, distance);
                return;
            }

            distance++;
        }
    }

    private void ResolveFunction(Stmt.Function function, FunctionType type)
    {
        FunctionType enclosingFunction = _currentFunction;
        _currentFunction = type;
        BeginScope();

        foreach (var parameter in function.Parameters)
        {
            Declare(parameter.Name);
            Define(parameter.Name);
        }
        Resolve(function.Body);

        EndScope();
        _currentFunction = enclosingFunction;
    }

    private void Declare(Token name)
    {
        if (_scopes.Count == 0) return;

        var scope = _scopes.Peek();

        if (scope.ContainsKey(name.Lexeme))
            errors.Error(name, "Already a variable with this name in this scope.");

        scope[name.Lexeme] = false;
    }

    private void Define(Token name)
    {
        if (_scopes.Count == 0) return;

        _scopes.Peek()[name.Lexeme] = true;
    }

}