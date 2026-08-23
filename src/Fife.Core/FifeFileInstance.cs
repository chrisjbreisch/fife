namespace Fife.Core;

/// <summary>Native handle bound to a single file path, exposed to fife through <see cref="IFifeObject"/>.</summary>
public sealed class FifeFileInstance(string path) : IFifeObject
{
    public override string ToString() => $"File(\"{path}\")";

    public object? Get(Token name) => name.Lexeme switch
    {
        "path" => path,
        "exists" => new NativeFunction("exists", 0, 0, (_, _) => File.Exists(path)),
        "size" => new NativeFunction("size", 0, 0, (interpreter, _) =>
        {
            try
            {
                return (double)new FileInfo(path).Length;
            }
            catch (FileNotFoundException)
            {
                throw interpreter.CreateException(interpreter.FileExceptionClass, name, $"File not found: '{path}'.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or DirectoryNotFoundException)
            {
                throw interpreter.CreateException(interpreter.FileExceptionClass, name, $"Can't access '{path}': {ex.Message}");
            }
        }),
        "modifiedTime" => new NativeFunction("modifiedTime", 0, 0, (interpreter, _) =>
        {
            if (!File.Exists(path))
                throw interpreter.CreateException(interpreter.FileExceptionClass, name, $"File not found: '{path}'.");

            return (double)new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds();
        }),
        "read" => new NativeFunction("read", 0, 0, (interpreter, _) =>
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (FileNotFoundException)
            {
                throw interpreter.CreateException(interpreter.FileExceptionClass, name, $"File not found: '{path}'.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw interpreter.CreateException(interpreter.FileExceptionClass, name, $"Can't read '{path}': {ex.Message}");
            }
        }),
        "write" => new NativeFunction("write", 1, 1, (interpreter, arguments) =>
        {
            var content = RequireString(arguments[0], name, "content");
            try
            {
                File.WriteAllText(path, content);
                return null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw interpreter.CreateException(interpreter.FileExceptionClass, name, $"Can't write '{path}': {ex.Message}");
            }
        }),
        "append" => new NativeFunction("append", 1, 1, (interpreter, arguments) =>
        {
            var content = RequireString(arguments[0], name, "content");
            try
            {
                File.AppendAllText(path, content);
                return null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw interpreter.CreateException(interpreter.FileExceptionClass, name, $"Can't write '{path}': {ex.Message}");
            }
        }),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    public void Set(Token name, object? value) =>
        throw new RuntimeError(name, "File has no settable fields.");

    private static string RequireString(object? argument, Token token, string parameterName) =>
        argument as string ?? throw new RuntimeError(token, $"'{parameterName}' must be a string.");
}
