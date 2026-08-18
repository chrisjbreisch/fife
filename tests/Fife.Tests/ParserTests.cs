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
        Assert.AreEqual("(; (call writeln (+ 1 2)))", Print("writeln(1 + \\\n" + "2)\n"));
    }

    [TestMethod]
    public void ParsesUnaryAndGrouping()
    {
        Assert.AreEqual("(; (- (group (+ 1 2))))", Print("-(1 + 2)\n"));
    }

    [TestMethod]
    public void DesugarsForIntoWhile()
    {
        StringAssert.Contains(Print("for (var i = 0; i < 2; i = i + 1) writeln(i)\n"), "(while");
    }

    [TestMethod]
    public void ParsesAssignmentAsRightAssociative()
    {
        Assert.AreEqual("(; (= a (= b 1)))", Print("a = b = 1\n"));
    }

    [TestMethod]
    public void ParsesIntDeclarations()
    {
        Assert.AreEqual("(int x 3)", Print("int x = 3\n"));
        Assert.AreEqual("(int x)", Print("int x\n"));
    }

    [TestMethod]
    public void ParsesTypedFunctionDeclarations()
    {
        Assert.AreEqual(
            "(int fun add(int a int b) (return (+ a b)))",
            Print("int fun add(int a, int b) {\nreturn a + b\n}\n"));
        Assert.AreEqual("(var fun noop() )", Print("var fun noop() {\n}\n"));
    }

    [TestMethod]
    public void ParsesFunctionDeclarationsWithoutTypes()
    {
        Assert.AreEqual(
            "(var fun add(var a var b) (return (+ a b)))",
            Print("fun add(a, b) {\nreturn a + b\n}\n"));
        Assert.AreEqual(
            "(int fun add(var a int b) (return (+ a b)))",
            Print("int fun add(a, int b) {\nreturn a + b\n}\n"));
    }

    [TestMethod]
    public void ParsesFloatDeclarations()
    {
        Assert.AreEqual("(float x 1.5)", Print("float x = 1.5\n"));
        Assert.AreEqual("(float x)", Print("float x\n"));
    }

    [TestMethod]
    public void ParsesBoolAndStringDeclarations()
    {
        Assert.AreEqual("(bool ready true)", Print("bool ready = true\n"));
        Assert.AreEqual("(string name hi)", Print("string name = \"hi\"\n"));
    }

    [TestMethod]
    public void RejectsSemicolonAsStatementTerminator()
    {
        ConsoleErrorReporter errors = new(new StringWriter());
        var tokens = new Scanner("writeln(1);\n", errors).ScanTokens();
        new Parser(tokens, errors).Parse();

        Assert.IsTrue(errors.HadError);
    }
}
