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
        // (;(+1(*23)))
        Assert.AreEqual("(; (+ 1 (* 2 3)))", Print("1 + 2 * 3\n"));
    }

    [TestMethod]
    public void ParsesPowerAsRightAssociativeAndFactorialAsPostfix()
    {
        Assert.AreEqual("(; (^ 2 (^ 3 2)))", Print("2 ^ 3 ^ 2\n"));
        Assert.AreEqual("(; (! postfix 6))", Print("6!\n"));
        Assert.AreEqual("(; (! (! postfix a)))", Print("!a!\n"));
        Assert.AreEqual("(; (! (! a)))", Print("!!a\n"));
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
    public void ParsesClassDeclarations()
    {
        Assert.AreEqual(
            "(class Greeter (var fun greet() (; (call writeln hi))))",
            Print("class Greeter {\ngreet() {\nwriteln(\"hi\")\n}\n}\n"));
        Assert.AreEqual("(class Empty )", Print("class Empty {\n}\n"));
    }

    [TestMethod]
    public void ParsesClassInheritance()
    {
        Assert.AreEqual(
            "(class Dog : Animal (var fun speak() (; (call (super speak)))))",
            Print("class Dog : Animal {\nspeak() {\nsuper.speak()\n}\n}\n"));
    }

    [TestMethod]
    public void ParsesPropertyAccessAndAssignment()
    {
        Assert.AreEqual("(; (. a b))", Print("a.b\n"));
        Assert.AreEqual("(; (= a b 1))", Print("a.b = 1\n"));
        Assert.AreEqual("(; (call (. (. a b) c)))", Print("a.b.c()\n"));
    }

    [TestMethod]
    public void ParsesThisInMethods()
    {
        Assert.AreEqual(
            "(class C (var fun get() (return (. this x))))",
            Print("class C {\nget() {\nreturn this.x\n}\n}\n"));
    }

    [TestMethod]
    public void ParsesTypedMethodDeclarations()
    {
        Assert.AreEqual(
            "(class C (int fun get(int n) (return n)))",
            Print("class C {\nint get(int n) {\nreturn n\n}\n}\n"));
    }

    [TestMethod]
    public void ParsesFunctionDeclarationsWithoutTypes()
    {        Assert.AreEqual(
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
