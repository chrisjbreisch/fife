namespace Fife.Core;

/// <summary>Front end that wires the scanner, parser and interpreter together.</summary>
public sealed class FifeEngine
{
    private readonly IErrorReporter _errors;
    private readonly Interpreter _interpreter;
    private string _replSource = "";

    public FifeEngine(IErrorReporter? errors = null, TextWriter? output = null, TextReader? input = null)
    {
        _errors = errors ?? new ConsoleErrorReporter();
        _interpreter = new Interpreter(_errors, output, input);
    }

    public Interpreter Interpreter => _interpreter;

    public bool HadError => _errors.HadError;

    public bool HadRuntimeError => _errors.HadRuntimeError;

    public void Reset() => _errors.Reset();

    public void Run(string source)
    {
        var tokens = new Scanner(source, _errors).ScanTokens();
        var statements = new Parser(tokens, _errors).Parse();

        if (_errors.HadError) return;

        _interpreter.Interpret(statements);
    }

    /// <summary>
    /// Runs a line of REPL input. If it is a bare expression, its value is returned so the
    /// REPL can echo it; otherwise it is executed as a statement and <c>null</c> is returned.
    /// </summary>
    public object? RunRepl(string source)
    {
        _replSource += source.EndsWith('\n') ? source : source + "\n";
        var replSource = _replSource;
        var tokens = new Scanner(replSource, _errors).ScanTokens();
        if (_errors.HadError)
        {
            _replSource = "";
            return null;
        }

        var blockDepth = 0;
        foreach (var token in tokens)
        {
            if (token.Type == TokenType.LeftBrace) blockDepth++;
            else if (token.Type == TokenType.RightBrace) blockDepth--;
        }

        if (blockDepth > 0) return null;

        if (!replSource.TrimEnd().EndsWith('}'))
        {
            IErrorReporter probe = new SilentErrorReporter();
            var expr = new Parser(tokens, probe).ParseExpression();
            if (expr is not null && !probe.HadError)
            {
                try
                {
                    var value = _interpreter.Evaluate(expr);
                    _replSource = "";
                    return value;
                }
                catch (RuntimeError error)
                {
                    _errors.RuntimeError(error);
                    _replSource = "";
                    return null;
                }
            }
        }

        var statements = new Parser(tokens, _errors).Parse();
        if (_errors.HadError)
        {
            _replSource = "";
            return null;
        }

        _interpreter.Interpret(statements);
        _replSource = "";
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
