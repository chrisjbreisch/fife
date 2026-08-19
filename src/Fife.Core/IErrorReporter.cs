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
    private const int MaxReportedFrames = 10;

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
        _writer.WriteLine(error.Message);
        _writer.WriteLine(FormatFrame(error.Token.Line, error.Frames is { Count: > 0 } f ? f[0].Name : "script"));

        if (error.Frames is { Count: > 0 } frames)
        {
            var shown = Math.Min(frames.Count, MaxReportedFrames);
            for (var i = 0; i < shown; i++)
            {
                var caller = i + 1 < frames.Count ? frames[i + 1].Name : "script";
                _writer.WriteLine(FormatFrame(frames[i].CallSite.Line, caller));
            }

            if (frames.Count > shown)
            {
                _writer.WriteLine($"  ... {frames.Count - shown} more");
            }
        }

        HadRuntimeError = true;
    }

    private static string FormatFrame(int line, string name) => $"[line {line}] in {name}";

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
