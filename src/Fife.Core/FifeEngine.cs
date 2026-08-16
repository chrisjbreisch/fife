namespace Fife;

/// <summary>Front end that wires the scanner, parser and interpreter together.</summary>
public sealed class FifeEngine
{
    private readonly IErrorReporter _errors;
    private readonly Interpreter _interpreter;

    public FifeEngine(IErrorReporter? errors = null, TextWriter? output = null)
    {
        _errors = errors ?? new ConsoleErrorReporter();
        _interpreter = new Interpreter(_errors, output);
    }

    public Interpreter Interpreter => _interpreter;

    public bool HadError => _errors.HadError;

    public bool HadRuntimeError => _errors.HadRuntimeError;

    public void Reset() => _errors.Reset();

    public void Run(string source)
    {
        List<Token> tokens = new Scanner(source, _errors).ScanTokens();
        List<Stmt> statements = new Parser(tokens, _errors).Parse();

        if (_errors.HadError) return;

        _interpreter.Interpret(statements);
    }

    /// <summary>
    /// Runs a line of REPL input. If it is a bare expression, its value is returned so the
    /// REPL can echo it; otherwise it is executed as a statement and <c>null</c> is returned.
    /// </summary>
    public object? RunRepl(string source)
    {
        List<Token> tokens = new Scanner(source, _errors).ScanTokens();
        if (_errors.HadError) return null;

        if (!source.TrimEnd().EndsWith(';') && !source.TrimEnd().EndsWith('}'))
        {
            IErrorReporter probe = new SilentErrorReporter();
            Expr? expr = new Parser(tokens, probe).ParseExpression();
            if (expr is not null && !probe.HadError)
            {
                try
                {
                    return _interpreter.Evaluate(expr);
                }
                catch (RuntimeError error)
                {
                    _errors.RuntimeError(error);
                    return null;
                }
            }
        }

        List<Stmt> statements = new Parser(tokens, _errors).Parse();
        if (_errors.HadError) return null;

        _interpreter.Interpret(statements);
        return null;
    }
}

/// <summary>Swallows diagnostics; used when speculatively parsing REPL input.</summary>
internal sealed class SilentErrorReporter : IErrorReporter
{
    public bool HadError { get; private set; }
    public bool HadRuntimeError { get; private set; }

    public void Error(int line, int column, string message) => HadError = true;
    public void Error(Token token, string message) => HadError = true;
    public void RuntimeError(RuntimeError error) => HadRuntimeError = true;
    public void Reset() { HadError = false; HadRuntimeError = false; }
}
