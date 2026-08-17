using Fife.Core;

namespace Fife.Tests;

[TestClass]
public sealed class ParserTests
{
    private static string Print(string source)
    {
        ConsoleErrorReporter errors = new(new StringWriter());
        var tokens = new Scanner(source, errors).ScanTokens();
        var statements = new Parser(tokens, errors).Parse();
        Assert.IsFalse(errors.HadError, "Source failed to parse.");
        return new AstPrinter().Print(statements);
    }

    [TestMethod]
    public void AppliesArithmeticPrecedence()
    {
        Assert.AreEqual("(; (+ 1 (* 2 3)))", Print("1 + 2 * 3\n"));
    }

    [TestMethod]
    public void ParsesPowerAsRightAssociativeAndFactorialAsPostfix()
    {
        Assert.AreEqual("(; (^ 2 (^ 3 2)))", Print("2 ^ 3 ^ 2\n"));
        Assert.AreEqual("(; (!! 6))", Print("6!!\n"));
    }

    [TestMethod]
    public void ParsesNewlineTerminatedStatements()
    {
        Assert.AreEqual("(print (+ 1 2))", Print("print 1 + \\\n" + "2\n"));
    }

    [TestMethod]
    public void ParsesUnaryAndGrouping()
    {
        Assert.AreEqual("(; (- (group (+ 1 2))))", Print("-(1 + 2)\n"));
    }

    [TestMethod]
    public void DesugarsForIntoWhile()
    {
        StringAssert.Contains(Print("for (var i = 0; i < 2; i = i + 1) print i\n"), "(while");
    }

    [TestMethod]
    public void ParsesAssignmentAsRightAssociative()
    {
        Assert.AreEqual("(; (= a (= b 1)))", Print("a = b = 1\n"));
    }

    [TestMethod]
    public void RejectsSemicolonAsStatementTerminator()
    {
        ConsoleErrorReporter errors = new(new StringWriter());
        var tokens = new Scanner("print 1;\n", errors).ScanTokens();
        new Parser(tokens, errors).Parse();

        Assert.IsTrue(errors.HadError);
    }
}
