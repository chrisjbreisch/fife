using Fife.Core;

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
        Assert.AreEqual("7", Run("writeln(1 + 2 * 3)\n"));
    }

    [TestMethod]
    public void SupportsIntDeclarationsAndAssignments()
    {
        Assert.AreEqual("0\n3", Run("int x\nwriteln(x)\nx = 3\nwriteln(x)\n"));
    }

    [TestMethod]
    public void RejectsFractionalIntValues()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);

        engine.Run("int x = 1.5\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Integer variables require an integer value.");
    }

    [TestMethod]
    public void SupportsStandardIoFunctionsWithOptionalArguments()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        StringReader input = new("xy\n");
        FifeEngine engine = new(errors, output, input);

        engine.Run("write(\"a\")\nwriteln(\"b\")\nwriteln()\nwriteln(read(\"r:\"))\nwriteln(readln(\"l:\"))\n");

        Assert.AreEqual("ab\n\nr:120\nl:y\n", output.ToString().ReplaceLineEndings("\n"));
        Assert.IsFalse(engine.HadError);
    }

    [TestMethod]
    public void ReplTreatsInputLineAsNewlineTerminated()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);

        engine.RunRepl("writeln(3)");

        Assert.AreEqual("3\n", output.ToString().ReplaceLineEndings("\n"));
        Assert.IsFalse(engine.HadError);
    }

    [TestMethod]
    public void ReplBuffersAnIncompleteBlock()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);

        engine.RunRepl("{");
        Assert.AreEqual("", output.ToString());
        Assert.IsFalse(engine.HadError);

        engine.RunRepl("writeln(3)");
        engine.RunRepl("}");

        Assert.AreEqual("3\n", output.ToString().ReplaceLineEndings("\n"));
        Assert.IsFalse(engine.HadError);
    }

    [TestMethod]
    public void UsesNewlinesAndLineContinuationsForStatements()
    {
        Assert.AreEqual("3\n7", Run("writeln(1 + 2)\nwriteln(3 + \\\n" + "4)\n"));
    }

    [TestMethod]
    public void EvaluatesExponentiationAndFactorial()
    {
        Assert.AreEqual("8\n1\n2\n6\n24\n120\n720", Run(
            "writeln(2 ^ 3)\nwriteln(1!!)\nwriteln(2!!)\nwriteln(3!!)\nwriteln(4!!)\nwriteln(5!!)\nwriteln(6!!)\n"));
    }

    [TestMethod]
    public void RespectsGroupingPrecedence()
    {
        Assert.AreEqual("9", Run("writeln((1 + 2) * 3)\n"));
    }

    [TestMethod]
    public void ConcatenatesStrings()
    {
        Assert.AreEqual("fife lang", Run("writeln(\"fife\" + \" \" + \"lang\")\n"));
    }

    [TestMethod]
    public void ScopesVariablesToBlocks()
    {
        Assert.AreEqual("inner\nouter", Run(
            "var a = \"outer\"\n{\nvar a = \"inner\"\nwriteln(a)\n}\nwriteln(a)\n"));
    }

    [TestMethod]
    public void ExecutesWhileLoops()
    {
        Assert.AreEqual("0\n1\n2", Run("for (var i = 0; i < 3; i = i + 1) writeln(i)\n"));
    }

    [TestMethod]
    public void ShortCircuitsLogicalOperators()
    {
        Assert.AreEqual("true", Run("writeln(true or unknownVariable)\n"));
    }

    [TestMethod]
    public void CallsFunctionsAndReturnsValues()
    {
        Assert.AreEqual("3", Run("fun add(a, b) { return a + b\n}\nwriteln(add(1, 2))\n"));
    }

    [TestMethod]
    public void ClosesOverEnclosingScope()
    {
        Assert.AreEqual("1\n2", Run(
            "fun counter() {\nvar n = 0\nfun next() {\nn = n + 1\nreturn n\n}\nreturn next\n}\nvar c = counter()\nwriteln(c())\nwriteln(c())\n"));
    }

    [TestMethod]
    public void ReportsRuntimeErrorForBadOperand()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("writeln(1 - \"two\")\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Operands must be numbers.");
    }

    [TestMethod]
    public void ReportsParseError()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("writeln(1 +)\n");

        Assert.IsTrue(engine.HadError);
    }
}
