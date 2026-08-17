namespace Fife.Core;

/// <summary>Sink for compile-time and run-time diagnostics produced by the interpreter pipeline.</summary>
public interface IErrorReporter
{
    bool HadError { get; }
    bool HadRuntimeError { get; }

    void Error(int line, int column, string message);
    void Error(Token token, string message);
    void RuntimeError(RuntimeError error);
    void Reset();
}

public class ConsoleErrorReporter(TextWriter? writer = null) : IErrorReporter
{
    private readonly TextWriter _writer = writer ?? Console.Error;

    public bool HadError { get; private set; }
    public bool HadRuntimeError { get; private set; }

    public void Error(int line, int column, string message) => Report(line, column, "", message);

    public void Error(Token token, string message)
    {
        if (token.Type == TokenType.Eof)
        {
            Report(token.Line, token.Column, " at end", message);
        }
        else
        {
            Report(token.Line, token.Column, $" at '{token.Lexeme}'", message);
        }
    }

    public void RuntimeError(RuntimeError error)
    {
        _writer.WriteLine($"{error.Message}\n[line {error.Token.Line}]");
        HadRuntimeError = true;
    }

    public void Reset()
    {
        HadError = false;
        HadRuntimeError = false;
    }

    private void Report(int line, int column, string where, string message)
    {
        _writer.WriteLine($"[line {line}, col {column}] Error{where}: {message}");
        HadError = true;
    }
}
