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
        List<Token> tokens = Scan("var x = 12.5 >= 3;");

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
    public void SkipsLineAndBlockComments()
    {
        List<Token> tokens = Scan("// gone\n/* also /* nested */ gone */ 42");

        Assert.AreEqual(TokenType.Number, tokens[0].Type);
        Assert.AreEqual(42d, tokens[0].Literal);
    }

    [TestMethod]
    public void TracksLineNumbers()
    {
        List<Token> tokens = Scan("1\n2\n3");

        Assert.AreEqual(1, tokens[0].Line);
        Assert.AreEqual(2, tokens[1].Line);
        Assert.AreEqual(3, tokens[2].Line);
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
