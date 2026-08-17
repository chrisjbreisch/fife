namespace Fife.Core;

/// <summary>Tree-walking evaluator for fife.</summary>
public sealed class Interpreter : Expr.IVisitor<object?>, Stmt.IVisitor<object?>
{
    private readonly IErrorReporter _errors;

    public Interpreter(IErrorReporter errors, TextWriter? output = null, TextReader? input = null)
    {
        _errors = errors;
        Output = output ?? Console.Out;
        Input = input ?? Console.In;
        Globals = new FifeEnvironment();
        _environment = Globals;
        DefineStandardLibrary();
    }

    private FifeEnvironment _environment;

    public FifeEnvironment Globals { get; }

    public TextWriter Output { get; }

    public TextReader Input { get; }

    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeError error)
        {
            _errors.RuntimeError(error);
        }
    }

    /// <summary>Evaluates a bare expression; used by the REPL and tests.</summary>
    public object? Evaluate(Expr expr) => expr.Accept(this);

    public void ExecuteBlock(List<Stmt> statements, FifeEnvironment environment)
    {
        var previous = _environment;
        try
        {
            _environment = environment;
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previous;
        }
    }

    /// <summary>Registers a host function that fife code can call by name.</summary>
    public void DefineNative(string name, int arity, Func<Interpreter, List<object?>, object?> body) =>
        Globals.Define(name, new NativeFunction(name, arity, arity, body));

    public void DefineNative(string name, int minArity, int maxArity, Func<Interpreter, List<object?>, object?> body) =>
        Globals.Define(name, new NativeFunction(name, minArity, maxArity, body));

    private void DefineStandardLibrary()
    {
        DefineNative("clock", 0, (_, _) => (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);
        DefineNative("read", 0, 1, (interpreter, arguments) =>
        {
            WritePrompt(interpreter, arguments);
            return interpreter.Input.Read();
        });
        DefineNative("readln", 0, 1, (interpreter, arguments) =>
        {
            WritePrompt(interpreter, arguments);
            return interpreter.Input.ReadLine();
        });
        DefineNative("write", 0, 1, (interpreter, arguments) =>
        {
            if (arguments.Count == 1) interpreter.Output.Write(Stringify(arguments[0]));
            return null;
        });
        DefineNative("writeln", 0, 1, (interpreter, arguments) =>
        {
            if (arguments.Count == 1) interpreter.Output.WriteLine(Stringify(arguments[0]));
            else interpreter.Output.WriteLine();
            return null;
        });
    }

    private static void WritePrompt(Interpreter interpreter, List<object?> arguments)
    {
        if (arguments.Count == 1) interpreter.Output.Write(Stringify(arguments[0]));
    }

    private void Execute(Stmt statement) => statement.Accept(this);

    // --- Statements ---

    public object? VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new FifeEnvironment(_environment));
        return null;
    }

    public object? VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.Expr);
        return null;
    }

    public object? VisitFunctionStmt(Stmt.Function stmt)
    {
        _environment.Define(stmt.Name.Lexeme, new FifeFunction(stmt, _environment));
        return null;
    }

    public object? VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch is not null)
        {
            Execute(stmt.ElseBranch);
        }

        return null;
    }

    public object? VisitReturnStmt(Stmt.Return stmt) =>
        throw new ReturnException(stmt.Value is null ? null : Evaluate(stmt.Value));

    public object? VisitVarStmt(Stmt.Var stmt)
    {
        var value = stmt.Initializer is null && stmt.Type is FifeType.Int or FifeType.Float
            ? 0d
            : stmt.Initializer is null && stmt.Type == FifeType.Bool
                ? false
                : stmt.Initializer is null && stmt.Type == FifeType.String
                    ? ""
            : stmt.Initializer is null ? null : Evaluate(stmt.Initializer);
        if (stmt.Type == FifeType.Int && (value is not double number || number != Math.Truncate(number)))
        {
            throw new RuntimeError(stmt.Name, "Integer variables require an integer value.");
        }

        if (stmt.Type == FifeType.Float && value is not double)
        {
            throw new RuntimeError(stmt.Name, "Float variables require a number value.");
        }

        if (stmt.Type == FifeType.Bool && value is not bool)
        {
            throw new RuntimeError(stmt.Name, "Bool variables require a boolean value.");
        }

        if (stmt.Type == FifeType.String && value is not string)
        {
            throw new RuntimeError(stmt.Name, "String variables require a string value.");
        }

        _environment.Define(stmt.Name.Lexeme, value, stmt.Type);
        return null;
    }

    public object? VisitWhileStmt(Stmt.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.Body);
        }

        return null;
    }

    // --- Expressions ---

    public object? VisitAssignExpr(Expr.Assign expr)
    {
        var value = Evaluate(expr.Value);
        _environment.Assign(expr.Name, value);
        return value;
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case TokenType.BangEqual: return !IsEqual(left, right);
            case TokenType.EqualEqual: return IsEqual(left, right);

            case TokenType.Greater:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! > (double)right!;
            case TokenType.GreaterEqual:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! >= (double)right!;
            case TokenType.Less:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! < (double)right!;
            case TokenType.LessEqual:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! <= (double)right!;

            case TokenType.Minus:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! - (double)right!;
            case TokenType.Slash:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! / (double)right!;
            case TokenType.Star:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! * (double)right!;
            case TokenType.Caret:
                CheckNumberOperands(expr.Operator, left, right);
                return Math.Pow((double)left!, (double)right!);

            case TokenType.Plus:
                if (left is double dl && right is double dr) return dl + dr;
                if (left is string || right is string) return Stringify(left) + Stringify(right);
                throw new RuntimeError(expr.Operator, "Operands must be two numbers or include a string.");
        }

        return null;
    }

    public object? VisitCallExpr(Expr.Call expr)
    {
        var callee = Evaluate(expr.Callee);

        List<object?> arguments = [];
        foreach (var argument in expr.Arguments)
        {
            arguments.Add(Evaluate(argument));
        }

        if (callee is not ICallable callable)
        {
            throw new RuntimeError(expr.Paren, "Can only call functions and classes.");
        }

        if (arguments.Count < callable.Arity || arguments.Count > callable.MaxArity)
        {
            var expected = callable.Arity == callable.MaxArity
                ? callable.Arity.ToString()
                : $"between {callable.Arity} and {callable.MaxArity}";
            throw new RuntimeError(expr.Paren, $"Expected {expected} arguments but got {arguments.Count}.");
        }

        return callable.Call(this, arguments);
    }

    public object? VisitGroupingExpr(Expr.Grouping expr) => Evaluate(expr.Expression);

    public object? VisitLiteralExpr(Expr.Literal expr) => expr.Value;

    public object? VisitLogicalExpr(Expr.Logical expr)
    {
        var left = Evaluate(expr.Left);

        if (expr.Operator.Type == TokenType.Or)
        {
            if (IsTruthy(left)) return left;
        }
        else if (!IsTruthy(left))
        {
            return left;
        }

        return Evaluate(expr.Right);
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        var right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case TokenType.Bang:
                return !IsTruthy(right);
            case TokenType.Minus:
                CheckNumberOperand(expr.Operator, right);
                return -(double)right!;
            case TokenType.BangBang:
                return Factorial(expr.Operator, right);
        }

        return null;
    }

    public object? VisitVariableExpr(Expr.Variable expr) => _environment.Get(expr.Name);

    // --- Helpers ---

    /// <summary>Everything except <c>nil</c> and <c>false</c> is truthy.</summary>
    public static bool IsTruthy(object? value) => value switch
    {
        null => false,
        bool b => b,
        _ => true
    };

    public static bool IsEqual(object? a, object? b) => a is null ? b is null : a.Equals(b);

    public static string Stringify(object? value)
    {
        switch (value)
        {
            case null:
                return "nil";
            case bool b:
                return b ? "true" : "false";
            case double d:
            {
                var text = d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                return text.EndsWith(".0", StringComparison.Ordinal) ? text[..^2] : text;
            }
            default:
                return value.ToString() ?? "nil";
        }
    }

    private static void CheckNumberOperand(Token op, object? operand)
    {
        if (operand is double) return;
        throw new RuntimeError(op, "Operand must be a number.");
    }

    private static void CheckNumberOperands(Token op, object? left, object? right)
    {
        if (left is double && right is double) return;
        throw new RuntimeError(op, "Operands must be numbers.");
    }

    private static double Factorial(Token op, object? operand)
    {
        if (operand is not double value || value < 0 || value != Math.Truncate(value))
        {
            throw new RuntimeError(op, "Operand must be a non-negative integer.");
        }

        var result = 1d;
        for (var factor = 2d; factor <= value; factor++)
        {
            result *= factor;
        }

        return result;
    }
}
