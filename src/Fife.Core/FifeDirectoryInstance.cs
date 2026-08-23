namespace Fife.Core;

/// <summary>Native handle bound to a single directory path, exposed to fife through <see cref="IFifeObject"/>.</summary>
public sealed class FifeDirectoryInstance(string path) : IFifeObject
{
    public override string ToString() => $"Directory(\"{path}\")";

    public object? Get(Token name) => name.Lexeme switch
    {
        "path" => path,
        "exists" => new NativeFunction("exists", 0, 0, (_, _) => Directory.Exists(path)),
        "list" => new NativeFunction("list", 0, 0, (interpreter, _) =>
        {
            try
            {
                var entries = Directory.EnumerateFileSystemEntries(path).Select(entry => (object?)Path.GetFileName(entry));
                return new FifeListInstance(entries);
            }
            catch (DirectoryNotFoundException)
            {
                throw interpreter.CreateException(interpreter.FileExceptionClass, name, $"Directory not found: '{path}'.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw interpreter.CreateException(interpreter.FileExceptionClass, name, $"Can't access '{path}': {ex.Message}");
            }
        }),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    public void Set(Token name, object? value) =>
        throw new RuntimeError(name, "Directory has no settable fields.");
}
