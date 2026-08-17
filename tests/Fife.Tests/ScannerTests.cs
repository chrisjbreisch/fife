using Fife.Core;

namespace Fife.Tests;

[TestClass]
public sealed class ScannerTests
{
    private static List<Token> Scan(string source)
    {
        ConsoleErrorReporter errors = new(new StringWriter());
        return new Scanner(source, errors).ScanTokens();
    }

    [TestMethod]
    public void ScansOperatorsAndLiterals()
    {
        var tokens = Scan("var x = 12.5 >= 3;");

        CollectionAssert.AreEqual(
            new[]
            {
                TokenType.Var, TokenType.Identifier, TokenType.Equal, TokenType.Number,
                TokenType.GreaterEqual, TokenType.Number, TokenType.Semicolon, TokenType.Eof
            },
            tokens.Select(t => t.Type).ToArray());

        Assert.AreEqual(12.5, tokens[3].Literal);
    }

    [TestMethod]
    public void ScansIntKeyword()
    {
        var tokens = Scan("int x");

        CollectionAssert.AreEqual(
            new[] { TokenType.Int, TokenType.Identifier, TokenType.Eof },
            tokens.Select(t => t.Type).ToArray());
    }

    [TestMethod]
    public void ScansFloatKeyword()
    {
        var tokens = Scan("float x");

        CollectionAssert.AreEqual(
            new[] { TokenType.Float, TokenType.Identifier, TokenType.Eof },
            tokens.Select(t => t.Type).ToArray());
    }

    [TestMethod]
    public void ScansExponentAndFactorialOperators()
    {
        var tokens = Scan("2^3 6!!");

        CollectionAssert.AreEqual(
            new[] { TokenType.Number, TokenType.Caret, TokenType.Number, TokenType.Number, TokenType.BangBang, TokenType.Eof },
            tokens.Select(t => t.Type).ToArray());
    }

    [TestMethod]
    public void ScansNewlinesAndSkipsEscapedNewlines()
    {
        var tokens = Scan("1\n2 + \\\n3");

        CollectionAssert.AreEqual(
            new[] { TokenType.Number, TokenType.NewLine, TokenType.Number, TokenType.Plus, TokenType.Number, TokenType.Eof },
            tokens.Select(t => t.Type).ToArray());
    }

    [TestMethod]
    public void SkipsLineAndBlockComments()
    {
        var tokens = Scan("// gone\n/* also /* nested */ gone */ 42");

        Assert.AreEqual(TokenType.Number, tokens[1].Type);
        Assert.AreEqual(42d, tokens[1].Literal);
    }

    [TestMethod]
    public void TracksLineNumbers()
    {
        var tokens = Scan("1\n2\n3");

        Assert.AreEqual(1, tokens[0].Line);
        Assert.AreEqual(2, tokens[2].Line);
        Assert.AreEqual(3, tokens[4].Line);
    }

    [TestMethod]
    public void ReportsUnterminatedString()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        new Scanner("\"oops", errors).ScanTokens();

        Assert.IsTrue(errors.HadError);
        StringAssert.Contains(output.ToString(), "Unterminated string.");
    }
}
