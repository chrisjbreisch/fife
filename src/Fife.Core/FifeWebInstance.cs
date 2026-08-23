using System.Net.Http;
using System.Text;

namespace Fife.Core;

/// <summary>Native-backed HTTP client exposed to fife through <see cref="IFifeObject"/>. Requests
/// run synchronously (fife has no async support), and only transport-level failures raise a
/// catchable <c>WebException</c> — a non-2xx response is returned normally so scripts can inspect it.</summary>
public sealed class FifeWebInstance(string? baseUrl = null) : IFifeObject
{
    private static readonly HttpClient Client = new();

    private readonly Dictionary<string, string> _headers = [];
    private string? _authorization;
    private string? _baseUrl = baseUrl;

    public override string ToString() => "Web";

    public object? Get(Token name) => name.Lexeme switch
    {
        "setBaseUrl" => new NativeFunction("setBaseUrl", 1, 1, (_, arguments) =>
        {
            _baseUrl = RequireString(arguments[0], name, "baseUrl");
            return null;
        }),
        "setHeader" => new NativeFunction("setHeader", 2, 2, (_, arguments) =>
        {
            _headers[RequireString(arguments[0], name, "name")] = RequireString(arguments[1], name, "value");
            return null;
        }),
        "setApiKey" => new NativeFunction("setApiKey", 2, 2, (_, arguments) =>
        {
            _headers[RequireString(arguments[0], name, "header")] = RequireString(arguments[1], name, "key");
            return null;
        }),
        "setBearerToken" => new NativeFunction("setBearerToken", 1, 1, (_, arguments) =>
        {
            _authorization = $"Bearer {RequireString(arguments[0], name, "token")}";
            return null;
        }),
        "setBasicAuth" => new NativeFunction("setBasicAuth", 2, 2, (_, arguments) =>
        {
            var username = RequireString(arguments[0], name, "username");
            var password = RequireString(arguments[1], name, "password");
            _authorization = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))}";
            return null;
        }),
        "get" => new NativeFunction("get", 1, 1, (interpreter, arguments) =>
            Send(interpreter, HttpMethod.Get, ResolveUrl(RequireString(arguments[0], name, "url"), name), null, name)),
        "post" => new NativeFunction("post", 1, 2, (interpreter, arguments) =>
            Send(interpreter, HttpMethod.Post, ResolveUrl(RequireString(arguments[0], name, "url"), name), OptionalBody(arguments, name), name)),
        "put" => new NativeFunction("put", 1, 2, (interpreter, arguments) =>
            Send(interpreter, HttpMethod.Put, ResolveUrl(RequireString(arguments[0], name, "url"), name), OptionalBody(arguments, name), name)),
        "patch" => new NativeFunction("patch", 1, 2, (interpreter, arguments) =>
            Send(interpreter, HttpMethod.Patch, ResolveUrl(RequireString(arguments[0], name, "url"), name), OptionalBody(arguments, name), name)),
        "delete" => new NativeFunction("delete", 1, 2, (interpreter, arguments) =>
            Send(interpreter, HttpMethod.Delete, ResolveUrl(RequireString(arguments[0], name, "url"), name), OptionalBody(arguments, name), name)),
        _ => throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.")
    };

    public void Set(Token name, object? value) =>
        throw new RuntimeError(name, "Web has no settable fields.");

    private static string? OptionalBody(List<object?> arguments, Token name) =>
        arguments.Count == 2 ? RequireString(arguments[1], name, "body") : null;

    /// <summary>Combines a relative path with the configured base URL; an absolute URL passes
    /// through unchanged.</summary>
    private string ResolveUrl(string url, Token token)
    {
        if (_baseUrl is null || Uri.TryCreate(url, UriKind.Absolute, out _)) return url;

        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri))
            throw new RuntimeError(token, $"'{_baseUrl}' is not a valid base URL.");

        return new Uri(baseUri, url).ToString();
    }

    private FifeMapInstance Send(Interpreter interpreter, HttpMethod method, string url, string? body, Token token)
    {
        using var request = new HttpRequestMessage(method, url);
        foreach (var (headerName, value) in _headers) request.Headers.TryAddWithoutValidation(headerName, value);
        if (_authorization is not null) request.Headers.TryAddWithoutValidation("Authorization", _authorization);
        if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = Client.Send(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException or InvalidOperationException)
        {
            throw interpreter.CreateException(interpreter.WebExceptionClass, token, $"Request to '{url}' failed: {ex.Message}");
        }

        using (response)
        {
            using var reader = new StreamReader(response.Content.ReadAsStream());
            var result = new FifeMapInstance();
            result.SetIndex(token, "statusCode", (double)(int)response.StatusCode);
            result.SetIndex(token, "body", reader.ReadToEnd());
            result.SetIndex(token, "success", response.IsSuccessStatusCode);
            return result;
        }
    }

    private static string RequireString(object? argument, Token name, string parameterName) =>
        argument as string ?? throw new RuntimeError(name, $"'{parameterName}' must be a string.");
}
