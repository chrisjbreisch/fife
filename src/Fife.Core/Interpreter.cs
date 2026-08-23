namespace Fife.Core;

/// <summary>Tree-walking evaluator for fife.</summary>
public sealed class Interpreter : Expr.IVisitor<object?>, Stmt.IVisitor<object?>
{
    /// <summary>Deliberately small: fife programs are expected to be scripts, and this keeps a
    /// runaway recursion from overflowing the host stack, which .NET cannot recover from.</summary>
    public const int MaxCallDepth = 100;

    private readonly IErrorReporter _errors;
    private readonly Stack<CallFrame> _frames = new();
    private Dictionary<Expr, int> _locals = [];
    private ClassDefinition _exceptionClass = null!;
    private ClassDefinition _fileExceptionClass = null!;
    private ClassDefinition _webExceptionClass = null!;

    public Interpreter(IErrorReporter errors, TextWriter? output = null, TextReader? input = null)
    {
        _errors = errors;
        Output = output ?? Console.Out;
        Input = input ?? Console.In;
        Globals = new FifeEnvironment();
        _environment = Globals;
        DefineStandardLibrary();
        DefineBuiltInExceptionClass();
    }

    private FifeEnvironment _environment;

    public FifeEnvironment Globals { get; }

    public TextWriter Output { get; }

    public TextReader Input { get; }

    /// <summary>The token of the call currently being evaluated, for natives that need a location
    /// to report a constructor-time error against.</summary>
    public Token? CurrentCallSite => _frames.Count > 0 ? _frames.Peek().CallSite : null;

    /// <summary>The built-in <c>FileException</c> class, for native objects (such as
    /// <see cref="FifeFileInstance"/> and <see cref="FifeDirectoryInstance"/>) that need to raise
    /// a catchable exception via <see cref="CreateException"/>.</summary>
    public ClassDefinition FileExceptionClass => _fileExceptionClass;

    /// <summary>The built-in <c>WebException</c> class, for native objects (such as <see cref="FifeWebInstance"/>)
    /// that need to raise a catchable exception via <see cref="CreateException"/>.</summary>
    public ClassDefinition WebExceptionClass => _webExceptionClass;

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
        catch (FifeThrow thrown)
        {
            var error = new RuntimeError(thrown.Keyword, $"Uncaught exception: {DescribeException(thrown.Instance)}");
            error.Frames = thrown.Frames;
            _errors.RuntimeError(error);
        }
    }

    private static string DescribeException(ClassInstance instance)
    {
        try
        {
            var message = instance.Get(new Token(TokenType.Identifier, "message", null, 0, 0));
            return message is null ? instance.ToString()! : Stringify(message);
        }
        catch (RuntimeError)
        {
            return instance.ToString()!;
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
        Globals.Define(name, CreateNative(name, arity, arity, body));

    public void DefineNative(string name, int minArity, int maxArity, Func<Interpreter, List<object?>, object?> body) 
        => Globals.Define(name, CreateNative(name, minArity, maxArity, body));

    private static NativeFunction CreateNative(
        string name, int minArity, int maxArity, Func<Interpreter, List<object?>, object?> body) =>
        new(name, minArity, maxArity, body);

    private void DefineStandardLibrary()
    {
        var con = new Dictionary<string, ICallable>
        {
            ["read"] = CreateNative("read", 0, 1, (interpreter, arguments) =>
            {
                WritePrompt(interpreter, arguments);
                return interpreter.Input.Read();
            }),
            ["readln"] = CreateNative("readln", 0, 1, (interpreter, arguments) =>
            {
                WritePrompt(interpreter, arguments);
                return interpreter.Input.ReadLine();
            }),
            ["write"] = CreateNative("write", 0, 1, (interpreter, arguments) =>
            {
                if (arguments.Count == 1) interpreter.Output.Write(Stringify(arguments[0]));
                return null;
            }),
            ["writeln"] = CreateNative("writeln", 0, 1, (interpreter, arguments) =>
            {
                if (arguments.Count == 1) interpreter.Output.WriteLine(Stringify(arguments[0]));
                else interpreter.Output.WriteLine();
                return null;
            })
        };
        Globals.Define("Con", new FifeStandardLibrary("Con", con));

        DefineNative("List", 0, 255, (_, arguments) => new FifeListInstance(arguments));
        DefineNative("Stack", 0, 255, (_, arguments) => new FifeStackInstance(arguments));
        DefineNative("Queue", 0, 255, (_, arguments) => new FifeQueueInstance(arguments));
        DefineNative("Map", 0, (_, _) => new FifeMapInstance());
        DefineNative("Vector", 0, 255, (interpreter, arguments) =>
            FifeVectorInstance.FromArguments(arguments, interpreter.CurrentCallSite!));
        DefineNative("Matrix", 1, 255, (interpreter, arguments) =>
            FifeMatrixInstance.FromArguments(arguments, interpreter.CurrentCallSite!));
        DefineNative("Web", 0, 1, (interpreter, arguments) => arguments.Count == 1
            ? new FifeWebInstance(RequireString(arguments[0], interpreter.CurrentCallSite!, "Web"))
            : new FifeWebInstance());
        DefineNative("File", 1, (interpreter, arguments) =>
            new FifeFileInstance(RequireString(arguments[0], interpreter.CurrentCallSite!, "File")));
        DefineNative("Directory", 1, (interpreter, arguments) =>
            new FifeDirectoryInstance(RequireString(arguments[0], interpreter.CurrentCallSite!, "Directory")));

        var math = new Dictionary<string, ICallable>
        {
            ["pi"] = CreateNative("pi", 0, 0, (_, _) => Math.PI),
            ["sin"] = CreateNative("sin", 1, 1, (interpreter, arguments) =>
                Math.Sin(RequireNumber(arguments[0], interpreter.CurrentCallSite!, "sin"))),
            ["cos"] = CreateNative("cos", 1, 1, (interpreter, arguments) =>
                Math.Cos(RequireNumber(arguments[0], interpreter.CurrentCallSite!, "cos"))),
            ["tan"] = CreateNative("tan", 1, 1, (interpreter, arguments) =>
                Math.Tan(RequireNumber(arguments[0], interpreter.CurrentCallSite!, "tan"))),
            ["asin"] = CreateNative("asin", 1, 1, (interpreter, arguments) =>
                Math.Asin(RequireNumber(arguments[0], interpreter.CurrentCallSite!, "asin"))),
            ["acos"] = CreateNative("acos", 1, 1, (interpreter, arguments) =>
                Math.Acos(RequireNumber(arguments[0], interpreter.CurrentCallSite!, "acos"))),
            ["atan"] = CreateNative("atan", 1, 1, (interpreter, arguments) =>
                Math.Atan(RequireNumber(arguments[0], interpreter.CurrentCallSite!, "atan"))),
            ["atan2"] = CreateNative("atan2", 2, 2, (interpreter, arguments) =>
                Math.Atan2(
                    RequireNumber(arguments[0], interpreter.CurrentCallSite!, "atan2"),
                    RequireNumber(arguments[1], interpreter.CurrentCallSite!, "atan2"))),
            ["exp"] = CreateNative("exp", 1, 1, (interpreter, arguments) =>
                Math.Exp(RequireNumber(arguments[0], interpreter.CurrentCallSite!, "exp"))),
            ["log"] = CreateNative("log", 1, 2, (interpreter, arguments) =>
            {
                var value = RequireNumber(arguments[0], interpreter.CurrentCallSite!, "log");
                return arguments.Count == 2
                    ? Math.Log(value, RequireNumber(arguments[1], interpreter.CurrentCallSite!, "log"))
                    : Math.Log(value);
            })
        };
        Globals.Define("Math", new FifeStandardLibrary("Math", math));
        Globals.Define("System", new FifeStandardLibrary("System", new Dictionary<string, ICallable>
        {
            ["clock"] = CreateNative("clock", 0, 0,
                (_, _) => (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0)
        }));
    }

    private static double RequireNumber(object? argument, Token token, string function) =>
        argument is double number ? number : throw new RuntimeError(token, $"{function}() expects a number.");

    private static string RequireString(object? argument, Token token, string function) =>
        argument as string ?? throw new RuntimeError(token, $"{function}() expects a string.");

    private static void WritePrompt(Interpreter interpreter, List<object?> arguments)
    {
        if (arguments.Count == 1) interpreter.Output.Write(Stringify(arguments[0]));
    }

    /// <summary>Defines the built-in <c>Exception</c> class (and its <c>FileException</c>
    /// subclass) by running fife source through the normal scan/parse/resolve pipeline, so user
    /// classes can inherit from them like any other.</summary>
    private void DefineBuiltInExceptionClass()
    {
        const string source =
            "class Exception {\n    Exception(message) {\n        this.message = message\n    }\n}\n"
            + "class FileException : Exception {\n}\n"
            + "class WebException : Exception {\n}\n";

        var reporter = new SilentErrorReporter();
        var tokens = new Scanner(source, reporter).ScanTokens();
        var statements = new Parser(tokens, reporter).Parse();
        if (reporter.HadError)
            throw new InvalidOperationException("Built-in Exception class failed to compile.");

        new Resolver(this, reporter).Resolve(statements);
        if (reporter.HadError)
            throw new InvalidOperationException("Built-in Exception class failed to resolve.");

        foreach (var statement in statements) Execute(statement);

        _exceptionClass = (ClassDefinition)Globals.Get(new Token(TokenType.Identifier, "Exception", null, 0, 0))!;
        _fileExceptionClass = (ClassDefinition)Globals.Get(new Token(TokenType.Identifier, "FileException", null, 0, 0))!;
        _webExceptionClass = (ClassDefinition)Globals.Get(new Token(TokenType.Identifier, "WebException", null, 0, 0))!;
    }

    /// <summary>Constructs an instance of a built-in exception class and wraps it for <c>throw</c>,
    /// so native code can raise a catchable fife exception instead of an uncatchable
    /// <see cref="RuntimeError"/>.</summary>
    public FifeThrow CreateException(ClassDefinition exceptionClass, Token token, string message)
    {
        var instance = (ClassInstance)exceptionClass.Call(this, [message])!;
        return new FifeThrow(token, instance);
    }

    private void Execute(Stmt statement) => statement.Accept(this);

    // --- Statements ---

    public object? VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new FifeEnvironment(_environment));
        return null;
    }

    public object? VisitClassStmt(Stmt.Class stmt)
    {
        object? superclass = null;
        if (stmt.Superclass != null)
        {
            superclass = Evaluate(stmt.Superclass);
            if (superclass is not ClassDefinition)
                throw new RuntimeError(stmt.Superclass.Name, "Superclass must be a class.");
        }

        _environment.Define(stmt.Name.Lexeme, null);

        if (stmt.Superclass != null)
        {
            _environment = new FifeEnvironment(_environment);
            _environment.Define("super", superclass);
        }

        Dictionary<string, FifeFunction> methods = [];
        foreach (var method in stmt.Methods)
        {
            var function = new FifeFunction(method, _environment, method.Name.Lexeme == stmt.Name.Lexeme);
            methods[method.Name.Lexeme] = function;
        }

        var classDefinition = new ClassDefinition(stmt.Name.Lexeme, (ClassDefinition?)superclass, methods);

        if (stmt.Superclass != null) _environment = _environment.Enclosing!;

        _environment.Assign(stmt.Name, classDefinition);

        return null;
    }

    public object? VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.Expr);
        return null;
    }

    public object? VisitFunctionStmt(Stmt.Function stmt)
    {
        _environment.Define(stmt.Name.Lexeme, new FifeFunction(stmt, _environment, false));
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

    public object? VisitThrowStmt(Stmt.Throw stmt)
    {
        var value = Evaluate(stmt.Value);
        if (value is not ClassInstance instance || !IsInstanceOf(instance, _exceptionClass))
            throw new RuntimeError(stmt.Keyword, "Can only throw instances of Exception or a subclass.");

        throw new FifeThrow(stmt.Keyword, instance);
    }

    public object? VisitTryStmt(Stmt.Try stmt)
    {
        try
        {
            ExecuteBlock(stmt.TryBlock, new FifeEnvironment(_environment));
        }
        catch (FifeThrow thrown)
        {
            if (Evaluate(stmt.CatchType) is not ClassDefinition catchClass)
                throw new RuntimeError(stmt.CatchName, "Catch clause type must be a class.");

            if (!IsInstanceOf(thrown.Instance, catchClass)) throw;

            var environment = new FifeEnvironment(_environment);
            environment.Define(stmt.CatchName.Lexeme, thrown.Instance);
            ExecuteBlock(stmt.CatchBlock, environment);
        }

        return null;
    }

    private static bool IsInstanceOf(ClassInstance instance, ClassDefinition type)
    {
        for (var current = instance.ClassDefinition; current != null; current = current.Superclass)
            if (current == type) return true;

        return false;
    }

    public object? VisitVarStmt(Stmt.Var stmt)
    {
        var value = stmt.Initializer is null
            ? FifeTypes.DefaultValue(stmt.Type)
            : Evaluate(stmt.Initializer);

        if (!FifeTypes.Accepts(stmt.Type, value))
        {
            throw new RuntimeError(stmt.Name, FifeTypes.VariableRequirement(stmt.Type));
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
        
        if (_locals.TryGetValue(expr, out int distance))
            _environment.AssignAt(distance, expr.Name, value);
        else
            Globals.Assign(expr.Name, value);
        
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

        if (_frames.Count >= MaxCallDepth)
        {
            throw new RuntimeError(expr.Paren, $"Stack overflow: exceeded the maximum call depth of {MaxCallDepth}.");
        }

        _frames.Push(new CallFrame(callable.Name, expr.Paren));
        try
        {
            return callable.Call(this, arguments);
        }
        catch (RuntimeError error)
        {
            // The innermost frame to see the error captures the deepest stack.
            error.Frames ??= _frames.ToArray();
            throw;
        }
        catch (FifeThrow thrown)
        {
            // Same rationale as above: capture the stack at the deepest frame that sees it.
            thrown.Frames ??= _frames.ToArray();
            throw;
        }
        finally
        {
            _frames.Pop();
        }
    }

    public object? VisitGetExpr(Expr.Get expr)
    {
        var obj = Evaluate(expr.Object);
        return obj switch
        {
            IFifeObject fifeObject => fifeObject.Get(expr.Name),
            string str => FifeString.Get(str, expr.Name),
            double number => FifeNumber.Get(number, expr.Name),
            _ => throw new RuntimeError(expr.Name, "Only class instances, strings, and numbers have properties.")
        };
    }

    public object? VisitGroupingExpr(Expr.Grouping expr) => Evaluate(expr.Expression);

    public object? VisitIndexExpr(Expr.Index expr)
    {
        var obj = Evaluate(expr.Object);
        if (obj is not IFifeIndexable indexable)
            throw new RuntimeError(expr.Bracket, "Only indexable values support '[]'.");

        return indexable.GetIndex(expr.Bracket, Evaluate(expr.IndexValue));
    }

    public object? VisitIndexSetExpr(Expr.IndexSet expr)
    {
        var obj = Evaluate(expr.Object);
        if (obj is not IFifeIndexable indexable)
            throw new RuntimeError(expr.Bracket, "Only indexable values support '[]='.");

        var index = Evaluate(expr.IndexValue);
        var value = Evaluate(expr.Value);
        indexable.SetIndex(expr.Bracket, index, value);
        return value;
    }

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

    public object? VisitPostfixExpr(Expr.Postfix expr) =>
        Factorial(expr.Operator, Evaluate(expr.Operand));

    public object? VisitSetExpr(Expr.Set expr)    {
        var obj = Evaluate(expr.Object);

        if (obj is not IFifeObject fifeObject)
            throw new RuntimeError(expr.Name, "Only class instances have fields.");

        var value = Evaluate(expr.Value);
        fifeObject.Set(expr.Name, value);
        return value;

    }

    public object? VisitSuperExpr(Expr.Super expr)
    {
        var distance = _locals[expr];
        var superclass = (ClassDefinition)_environment.GetAt(distance, "super")!;
        var instance = (ClassInstance)_environment.GetAt(distance - 1, "this")!;

        var method = superclass.FindMethod(expr.Method.Lexeme)
            ?? throw new RuntimeError(expr.Method, $"Undefined property '{expr.Method.Lexeme}'.");

        return method.Bind(instance);
    }

    public object? VisitThisExpr(Expr.This expr)
    {
        return LookupVariable(expr.Keyword, expr);
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
        }

        return null;
    }

    public object? VisitVariableExpr(Expr.Variable expr) => LookupVariable(expr.Name, expr);

    // --- Helpers ---

    private object? LookupVariable(Token name, Expr expr)
    {
        if (_locals.TryGetValue(expr, out int distance))
            return _environment.GetAt(distance, name.Lexeme);
        else
            return Globals.Get(name);
    }


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

    public void Resolve(Expr expr, int depth)
    {
        _locals[expr] = depth;
    }
}
