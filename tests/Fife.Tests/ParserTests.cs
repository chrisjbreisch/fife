namespace Fife.Tests;

[TestClass]
public sealed class ParserTests
{
    private static string Print(string source)
    {
        ConsoleErrorReporter errors = new(new StringWriter());
        List<Token> tokens = new Scanner(source, errors).ScanTokens();
        List<Stmt> statements = new Parser(tokens, errors).Parse();
        Assert.IsFalse(errors.HadError, "Source failed to parse.");
        return new AstPrinter().Print(statements);
    }

    [TestMethod]
    public void AppliesArithmeticPrecedence()
    {
        Assert.AreEqual("(; (+ 1 (* 2 3)))", Print("1 + 2 * 3;"));
    }

    [TestMethod]
    public void ParsesUnaryAndGrouping()
    {
        Assert.AreEqual("(; (- (group (+ 1 2))))", Print("-(1 + 2);"));
    }

    [TestMethod]
    public void DesugarsForIntoWhile()
    {
        StringAssert.Contains(Print("for (var i = 0; i < 2; i = i + 1) print i;"), "(while");
    }

    [TestMethod]
    public void ParsesAssignmentAsRightAssociative()
    {
        Assert.AreEqual("(; (= a (= b 1)))", Print("a = b = 1;"));
    }
}
