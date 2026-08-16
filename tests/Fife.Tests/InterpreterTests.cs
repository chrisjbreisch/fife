namespace Fife.Tests;

[TestClass]
public sealed class InterpreterTests
{
    private static string Run(string source)
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run(source);
        return output.ToString().ReplaceLineEndings("\n").TrimEnd('\n');
    }

    [TestMethod]
    public void PrintsArithmeticResult()
    {
        Assert.AreEqual("7", Run("print 1 + 2 * 3;"));
    }

    [TestMethod]
    public void RespectsGroupingPrecedence()
    {
        Assert.AreEqual("9", Run("print (1 + 2) * 3;"));
    }

    [TestMethod]
    public void ConcatenatesStrings()
    {
        Assert.AreEqual("fife lang", Run("""print "fife" + " " + "lang";"""));
    }

    [TestMethod]
    public void ScopesVariablesToBlocks()
    {
        Assert.AreEqual("inner\nouter", Run(
            """
            var a = "outer";
            { var a = "inner"; print a; }
            print a;
            """));
    }

    [TestMethod]
    public void ExecutesWhileLoops()
    {
        Assert.AreEqual("0\n1\n2", Run("for (var i = 0; i < 3; i = i + 1) print i;"));
    }

    [TestMethod]
    public void ShortCircuitsLogicalOperators()
    {
        Assert.AreEqual("true", Run("print true or unknownVariable;"));
    }

    [TestMethod]
    public void CallsFunctionsAndReturnsValues()
    {
        Assert.AreEqual("3", Run("fun add(a, b) { return a + b; } print add(1, 2);"));
    }

    [TestMethod]
    public void ClosesOverEnclosingScope()
    {
        Assert.AreEqual("1\n2", Run(
            """
            fun counter() {
              var n = 0;
              fun next() { n = n + 1; return n; }
              return next;
            }
            var c = counter();
            print c();
            print c();
            """));
    }

    [TestMethod]
    public void ReportsRuntimeErrorForBadOperand()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("""print 1 - "two";""");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Operands must be numbers.");
    }

    [TestMethod]
    public void ReportsParseError()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("print 1 +;");

        Assert.IsTrue(engine.HadError);
    }
}
