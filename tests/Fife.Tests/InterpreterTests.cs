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
    public void SupportsBothInequalityOperators()
    {
        Assert.AreEqual("true\ntrue", Run("writeln(1 != 2)\nwriteln(1 <> 2)\n"));
    }

    [TestMethod]
    public void SupportsIntDeclarationsAndAssignments()
    {
        Assert.AreEqual("0\n3", Run("int x\nwriteln(x)\nx = 3\nwriteln(x)\n"));
    }

    [TestMethod]
    public void SupportsFloatDeclarationsAndAssignments()
    {
        Assert.AreEqual("0\n1.5", Run("float x\nwriteln(x)\nx = 1.5\nwriteln(x)\n"));
    }

    [TestMethod]
    public void SupportsBoolAndStringDeclarationsAndAssignments()
    {
        Assert.AreEqual("false\ntrue\n\nhello", Run(
            "bool ready\nwriteln(ready)\nready = true\nwriteln(ready)\nstring name\nwriteln(name)\nname = \"hello\"\nwriteln(name)\n"));
    }

    [TestMethod]
    public void RejectsValuesWithTheWrongDeclaredType()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);

        engine.Run("bool ready = 1\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Bool variables require a boolean value.");
    }

    [TestMethod]
    public void RejectsNonNumericFloatValues()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);

        engine.Run("float x = \"not a number\"\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Float variables require a number value.");
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
            "writeln(2 ^ 3)\nwriteln(1!)\nwriteln(2!)\nwriteln(3!)\nwriteln(4!)\nwriteln(5!)\nwriteln(6!)\n"));
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
        Assert.AreEqual("3", Run("int fun add(int a, int b) { return a + b\n}\nwriteln(add(1, 2))\n"));
    }

    [TestMethod]
    public void ClosesOverEnclosingScope()
    {
        Assert.AreEqual("1\n2", Run(
            "fun counter() {\nvar n = 0\nfun next() {\nn = n + 1\nreturn n\n}\nreturn next\n}\nvar c = counter()\nwriteln(c())\nwriteln(c())\n"));
    }

    [TestMethod]
    public void ResolvesVariablesToTheirDeclaringScope()
    {
        Assert.AreEqual("global\nglobal", Run(
            "var a = \"global\"\n{\nfun showA() {\nwriteln(a)\n}\nshowA()\nvar a = \"block\"\nshowA()\n}\n"));
    }

    [TestMethod]
    public void ResolvesVariablesForReplInput()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);

        foreach (var line in new[]
        {
            "var a = \"global\"", "{", "fun showA() {", "writeln(a)", "}",
            "showA()", "var a = \"block\"", "showA()", "}"
        })
        {
            engine.RunRepl(line);
        }

        Assert.AreEqual("global\nglobal\n", output.ToString().ReplaceLineEndings("\n"));
        Assert.IsFalse(engine.HadError);
    }

    [TestMethod]
    public void ReportsReturnFromTopLevelCode()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("return 1\n");

        Assert.IsTrue(engine.HadError);
        StringAssert.Contains(output.ToString(), "Can't return from top-level code.");
    }

    [TestMethod]
    public void ReportsVariableReadInItsOwnInitializer()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("{\nvar a = a\n}\n");

        Assert.IsTrue(engine.HadError);
        StringAssert.Contains(output.ToString(), "Can't read local variable in its own initializer.");
    }

    [TestMethod]
    public void ReportsDuplicateDeclarationInTheSameScope()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("{\nvar a = 1\nvar a = 2\n}\n");

        Assert.IsTrue(engine.HadError);
        StringAssert.Contains(output.ToString(), "Already a variable with this name in this scope.");
    }

    [TestMethod]
    public void InstantiatesClassesAndCallsMethods()
    {
        Assert.AreEqual("hi", Run(
            "class Greeter {\ngreet() {\nwriteln(\"hi\")\n}\n}\nvar g = Greeter()\ng.greet()\n"));
    }

    [TestMethod]
    public void RunsTheConstructorNamedAfterTheClass()
    {
        Assert.AreEqual("hi, world", Run(
            "class Greeter {\nGreeter(name) {\nthis.name = name\n}\ngreet() {\nwriteln(\"hi, \" + this.name)\n}\n}\nGreeter(\"world\").greet()\n"));
    }

    [TestMethod]
    public void ReadsAndWritesFieldsFromOutsideTheClass()
    {
        Assert.AreEqual("1\n2", Run(
            "class Box {\n}\nvar b = Box()\nb.value = 1\nwriteln(b.value)\nb.value = 2\nwriteln(b.value)\n"));
    }

    [TestMethod]
    public void BindsThisWhenAMethodIsStoredInAVariable()
    {
        Assert.AreEqual("5", Run(
            "class Box {\nBox(v) {\nthis.v = v\n}\nget() {\nreturn this.v\n}\n}\nvar m = Box(5).get\nwriteln(m())\n"));
    }

    [TestMethod]
    public void PrefersFieldsOverMethodsWithTheSameName()
    {
        Assert.AreEqual("field", Run(
            "class C {\nvalue() {\nreturn \"method\"\n}\n}\nvar c = C()\nc.value = \"field\"\nwriteln(c.value)\n"));
    }

    [TestMethod]
    public void StringifiesClassesAndInstances()
    {
        Assert.AreEqual("Box\nBox instance", Run(
            "class Box {\n}\nwriteln(Box)\nwriteln(Box())\n"));
    }

    [TestMethod]
    public void AllowsAnEmptyReturnToExitAConstructorEarly()
    {
        Assert.AreEqual("1\n2", Run(
            "class C {\nC(skip) {\nthis.v = 1\nif (skip) return\nthis.v = 2\n}\n}\nwriteln(C(true).v)\nwriteln(C(false).v)\n"));
    }

    [TestMethod]
    public void ChecksConstructorArity()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("class C {\nC(a) {\nthis.a = a\n}\n}\nC()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Expected 1 arguments but got 0.");
    }

    [TestMethod]
    public void ReportsUndefinedProperty()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("class C {\n}\nC().missing\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Undefined property 'missing'.");
    }

    [TestMethod]
    public void ReportsPropertyAccessOnNonInstances()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var x = 1\nwriteln(x.field)\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Only class instances have properties.");
    }

    [TestMethod]
    public void ReportsFieldAssignmentOnNonInstances()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var x = 1\nx.field = 2\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Only class instances have fields.");
    }

    [TestMethod]
    public void ReportsThisOutsideOfAClass()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("writeln(this)\n");

        Assert.IsTrue(engine.HadError);
        StringAssert.Contains(output.ToString(), "Can't use 'this' outside of a class.");
    }

    [TestMethod]
    public void ReportsReturningAValueFromAConstructor()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("class C {\nC() {\nreturn 1\n}\n}\n");

        Assert.IsTrue(engine.HadError);
        StringAssert.Contains(output.ToString(), "Can't return a value from a constructor.");
    }

    [TestMethod]
    public void EnforcesDeclaredMethodReturnTypes()
    {
        Assert.AreEqual("3", Run(
            "class C {\nint add(int a, int b) {\nreturn a + b\n}\n}\nwriteln(C().add(1, 2))\n"));

        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("class C {\nstring name() {\nreturn 1\n}\n}\nC().name()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "must return a string value.");
    }

    [TestMethod]
    public void ReportsAReturnTypeOnAConstructor()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("class C {\nint C() {\n}\n}\n");

        Assert.IsTrue(engine.HadError);
        StringAssert.Contains(output.ToString(), "A constructor can't declare a return type.");
    }

    [TestMethod]
    public void ReportsACallStackForNestedCalls()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("fun inner() {\nreturn 1 - \"two\"\n}\nfun outer() {\nreturn inner()\n}\nouter()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        Assert.AreEqual(
            "Operands must be numbers.\n[line 2] in inner\n[line 5] in outer\n[line 7] in script\n",
            output.ToString().ReplaceLineEndings("\n"));
    }

    [TestMethod]
    public void ReportsTopLevelErrorsWithoutFrames()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var x = 1 - \"two\"\n");

        Assert.IsTrue(engine.HadRuntimeError);
        Assert.AreEqual(
            "Operands must be numbers.\n[line 1] in script\n",
            output.ToString().ReplaceLineEndings("\n"));
    }

    [TestMethod]
    public void IncludesNativeFunctionsInTheCallStack()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Interpreter.DefineNative("boom", 0, (_, _) =>
            throw new RuntimeError(new Token(TokenType.Identifier, "boom", null, 1, 1), "Native failure."));

        engine.Run("fun go() {\nreturn boom()\n}\ngo()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        Assert.AreEqual(
            "Native failure.\n[line 1] in boom\n[line 2] in go\n[line 4] in script\n",
            output.ToString().ReplaceLineEndings("\n"));
    }

    [TestMethod]
    public void StopsRunawayRecursionAtTheCallDepthLimit()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("fun f(n) {\nreturn f(n + 1)\n}\nf(0)\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(
            output.ToString(),
            $"Stack overflow: exceeded the maximum call depth of {Interpreter.MaxCallDepth}.");
    }

    [TestMethod]
    public void TruncatesVeryDeepCallStacks()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("fun f(n) {\nreturn f(n + 1)\n}\nf(0)\n");

        var lines = output.ToString().ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');

        Assert.IsTrue(lines.Length < 20, $"Trace was not truncated: {lines.Length} lines.");
        StringAssert.EndsWith(lines[^1], " more");
    }

    [TestMethod]
    public void UnwindsTheCallStackAfterAnError()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);

        engine.Run("fun bad() {\nreturn 1 - \"two\"\n}\nbad()\n");
        engine.Reset();
        output.GetStringBuilder().Clear();

        engine.Run("fun good() {\nreturn 1 - \"two\"\n}\ngood()\n");

        Assert.AreEqual(
            "Operands must be numbers.\n[line 2] in good\n[line 4] in script\n",
            output.ToString().ReplaceLineEndings("\n"));
    }

    [TestMethod]
    public void RejectsArgumentsThatDoNotMatchParameterTypes()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("fun double(int n) { return n\n}\ndouble(1.5)\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Parameter 'n' requires an integer value.");
    }

    [TestMethod]
    public void RejectsReturnValuesThatDoNotMatchTheReturnType()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("string fun name() { return 1\n}\nname()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Function 'name' must return a string value.");
    }

    [TestMethod]
    public void AllowsUnannotatedFunctionsToUseAnyValue()
    {
        Assert.AreEqual("1.5\nfife", Run(
            "fun echo(value) { return value\n}\nwriteln(echo(1.5))\nwriteln(echo(\"fife\"))\n"));
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
