using System.Net;
using System.Net.Sockets;
using System.Text;
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
        engine.Run("var x = true\nwriteln(x.field)\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Only class instances, strings, and numbers have properties.");
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
    public void ReadsTheLengthOfAString()
    {
        Assert.AreEqual("5", Run("writeln(\"hello\".length)\n"));
    }

    [TestMethod]
    public void CallsStringMemberMethods()
    {
        Assert.AreEqual("HELLO\nhello\nhi", Run(
            "writeln(\"hello\".upper())\nwriteln(\"HELLO\".lower())\nwriteln(\"  hi  \".trim())\n"));
    }

    [TestMethod]
    public void ExtractsASubstring()
    {
        Assert.AreEqual("hello\nllo", Run(
            "writeln(\"hello world\".substring(0, 5))\nwriteln(\"hello\".substring(2))\n"));
    }

    [TestMethod]
    public void ReportsOutOfRangeSubstringIndices()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("writeln(\"hi\".substring(0, 5))\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "'end' is out of range.");
    }

    [TestMethod]
    public void ReplacesAllOccurrencesInAString()
    {
        Assert.AreEqual("hxllb", Run("writeln(\"hello\".replace(\"e\", \"x\").replace(\"o\", \"b\"))\n"));
    }

    [TestMethod]
    public void CreatesAndPrintsAList()
    {
        Assert.AreEqual("[1, 2, 3]\n[]", Run("writeln(List(1, 2, 3))\nwriteln(List())\n"));
    }

    [TestMethod]
    public void AddsAndGetsListItems()
    {
        Assert.AreEqual("2\na\nb", Run(
            "var list = List()\n"
            + "list.add(\"a\")\nlist.add(\"b\")\n"
            + "writeln(list.length)\nwriteln(list.get(0))\nwriteln(list.get(1))\n"));
    }

    [TestMethod]
    public void SetsAListItemByIndex()
    {
        Assert.AreEqual("[1, 9, 3]", Run("var list = List(1, 2, 3)\nlist.set(1, 9)\nwriteln(list)\n"));
    }

    [TestMethod]
    public void RemovesAListItemByValueAndByIndex()
    {
        Assert.AreEqual("true\nfalse\n[2, 3]\nb\n[a, c]", Run(
            "var list = List(1, 2, 3)\n"
            + "writeln(list.remove(1))\nwriteln(list.remove(99))\nwriteln(list)\n"
            + "var letters = List(\"a\", \"b\", \"c\")\n"
            + "writeln(letters.removeAt(1))\nwriteln(letters)\n"));
    }

    [TestMethod]
    public void FindsItemsInAList()
    {
        Assert.AreEqual("true\nfalse\n1\n-1", Run(
            "var list = List(\"a\", \"b\", \"c\")\n"
            + "writeln(list.contains(\"b\"))\nwriteln(list.contains(\"z\"))\n"
            + "writeln(list.indexOf(\"b\"))\nwriteln(list.indexOf(\"z\"))\n"));
    }

    [TestMethod]
    public void IteratesOverAListWithAForLoop()
    {
        Assert.AreEqual("a\nb\nc", Run(
            "var list = List(\"a\", \"b\", \"c\")\n"
            + "for (var i = 0; i < list.length; i = i + 1) {\nwriteln(list.get(i))\n}\n"));
    }

    [TestMethod]
    public void ReportsOutOfRangeListIndices()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var list = List(1, 2)\nwriteln(list.get(5))\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "'index' is out of range.");
    }

    [TestMethod]
    public void ReadsAndWritesAListItemWithBracketSyntax()
    {
        Assert.AreEqual("a\n[a, z, c]", Run(
            "var list = List(\"a\", \"b\", \"c\")\n"
            + "writeln(list[0])\nlist[1] = \"z\"\nwriteln(list)\n"));
    }

    [TestMethod]
    public void IteratesOverAListWithBracketSyntax()
    {
        Assert.AreEqual("a\nb\nc", Run(
            "var list = List(\"a\", \"b\", \"c\")\n"
            + "for (var i = 0; i < list.length; i = i + 1) {\nwriteln(list[i])\n}\n"));
    }

    [TestMethod]
    public void ReportsOutOfRangeBracketIndices()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var list = List(1, 2)\nwriteln(list[5])\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "'index' is out of range.");
    }

    [TestMethod]
    public void ReportsIndexingANonIndexableValue()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var x = 1\nwriteln(x[0])\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Only indexable values support '[]'.");
    }

    [TestMethod]
    public void ReportsIndexAssignmentOnANonIndexableValue()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var x = 1\nx[0] = 2\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Only indexable values support '[]='.");
    }

    [TestMethod]
    public void ReportsAssigningToAListField()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var list = List()\nlist.length = 5\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Lists have no settable fields; use set(index, value).");
    }

    [TestMethod]
    public void PushesAndPopsAStackInLifoOrder()
    {
        Assert.AreEqual("3\n2\n1\ntrue", Run(
            "var stack = Stack(1, 2)\nstack.push(3)\n"
            + "writeln(stack.pop())\nwriteln(stack.pop())\nwriteln(stack.pop())\nwriteln(stack.isEmpty())\n"));
    }

    [TestMethod]
    public void PeeksAStackWithoutRemoving()
    {
        Assert.AreEqual("2\n2\n1", Run(
            "var stack = Stack(1, 2)\nwriteln(stack.peek())\nwriteln(stack.pop())\nwriteln(stack.pop())\n"));
    }

    [TestMethod]
    public void ReportsPoppingAnEmptyStack()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var stack = Stack()\nstack.pop()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Can't pop an empty stack.");
    }

    [TestMethod]
    public void EnqueuesAndDequeuesAQueueInFifoOrder()
    {
        Assert.AreEqual("1\n2\n3\ntrue", Run(
            "var queue = Queue(1, 2)\nqueue.enqueue(3)\n"
            + "writeln(queue.dequeue())\nwriteln(queue.dequeue())\nwriteln(queue.dequeue())\nwriteln(queue.isEmpty())\n"));
    }

    [TestMethod]
    public void PeeksAQueueWithoutRemoving()
    {
        Assert.AreEqual("1\n1\n2", Run(
            "var queue = Queue(1, 2)\nwriteln(queue.peek())\nwriteln(queue.dequeue())\nwriteln(queue.dequeue())\n"));
    }

    [TestMethod]
    public void ReportsDequeuingAnEmptyQueue()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var queue = Queue()\nqueue.dequeue()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Can't dequeue an empty queue.");
    }

    [TestMethod]
    public void PrintsStacksAndQueues()
    {
        Assert.AreEqual("Stack[1, 2]\nQueue[1, 2]", Run("writeln(Stack(1, 2))\nwriteln(Queue(1, 2))\n"));
    }

    [TestMethod]
    public void SetsAndGetsMapEntries()
    {
        Assert.AreEqual("1\ntrue\nfalse", Run(
            "var map = Map()\nmap.set(\"a\", 1)\n"
            + "writeln(map.get(\"a\"))\nwriteln(map.containsKey(\"a\"))\nwriteln(map.containsKey(\"z\"))\n"));
    }

    [TestMethod]
    public void UsesBracketSyntaxForMapEntries()
    {
        Assert.AreEqual("1\n2\n1", Run(
            "var map = Map()\nmap[\"a\"] = 1\nwriteln(map[\"a\"])\n"
            + "map[\"a\"] = 2\nwriteln(map[\"a\"])\nwriteln(map.length)\n"));
    }

    [TestMethod]
    public void RemovesAMapEntry()
    {
        Assert.AreEqual("true\nfalse\nfalse", Run(
            "var map = Map()\nmap.set(\"a\", 1)\n"
            + "writeln(map.remove(\"a\"))\nwriteln(map.remove(\"a\"))\nwriteln(map.containsKey(\"a\"))\n"));
    }

    [TestMethod]
    public void ListsMapKeysAndValues()
    {
        Assert.AreEqual("2\ntrue\ntrue\n2\ntrue\ntrue", Run(
            "var map = Map()\nmap.set(\"a\", 1)\nmap.set(\"b\", 2)\n"
            + "var keys = map.keys()\nwriteln(keys.length)\n"
            + "writeln(keys.contains(\"a\"))\nwriteln(keys.contains(\"b\"))\n"
            + "var values = map.values()\nwriteln(values.length)\n"
            + "writeln(values.contains(1))\nwriteln(values.contains(2))\n"));
    }

    [TestMethod]
    public void ReportsGettingAMissingMapKey()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var map = Map()\nwriteln(map[\"missing\"])\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Key not found.");
    }

    [TestMethod]
    public void ReportsANilMapKey()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var map = Map()\nmap[nil] = 1\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Map keys can't be nil.");
    }

    [TestMethod]
    public void ReportsAssigningToAMapField()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var map = Map()\nmap.length = 5\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Maps have no settable fields; use set(key, value).");
    }

    [TestMethod]
    public void PrintsAMap()
    {
        Assert.AreEqual("{a: 1}", Run("var map = Map()\nmap.set(\"a\", 1)\nwriteln(map)\n"));
    }

    [TestMethod]
    public void CreatesAndPrintsAVector()
    {
        Assert.AreEqual("Vector[1, 2, 3]", Run("writeln(Vector(1, 2, 3))\n"));
    }

    [TestMethod]
    public void ReadsAndWritesVectorElements()
    {
        Assert.AreEqual("1\n9\nVector[1, 9, 3]", Run(
            "var v = Vector(1, 2, 3)\nwriteln(v.get(0))\nv.set(1, 9)\nwriteln(v[1])\nwriteln(v)\n"));
    }

    [TestMethod]
    public void AddsAndSubtractsVectors()
    {
        Assert.AreEqual("Vector[4, 6]\nVector[-2, -2]", Run(
            "writeln(Vector(1, 2).add(Vector(3, 4)))\nwriteln(Vector(1, 2).subtract(Vector(3, 4)))\n"));
    }

    [TestMethod]
    public void MultipliesAVectorByAScalar()
    {
        Assert.AreEqual("Vector[2, 4]", Run("writeln(Vector(1, 2).multiply(2))\n"));
    }

    [TestMethod]
    public void ComputesTheDotProductOfTwoVectors()
    {
        Assert.AreEqual("32", Run("writeln(Vector(1, 2, 3).dot(Vector(4, 5, 6)))\n"));
    }

    [TestMethod]
    public void ComputesTheMagnitudeOfAVector()
    {
        Assert.AreEqual("5", Run("writeln(Vector(3, 4).magnitude())\n"));
    }

    [TestMethod]
    public void NormalizesAVector()
    {
        Assert.AreEqual("Vector[0.6, 0.8]", Run("writeln(Vector(3, 4).normalize())\n"));
    }

    [TestMethod]
    public void ReportsNormalizingAZeroVector()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("Vector(0, 0).normalize()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Can't normalize a zero vector.");
    }

    [TestMethod]
    public void ReportsMismatchedVectorSizes()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("Vector(1, 2).add(Vector(1, 2, 3))\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Expected a Vector of length 2.");
    }

    [TestMethod]
    public void CreatesAndPrintsAMatrix()
    {
        Assert.AreEqual("Matrix[[1, 2], [3, 4]]\n2\n2", Run(
            "var m = Matrix(List(1, 2), List(3, 4))\n"
            + "writeln(m)\nwriteln(m.rows)\nwriteln(m.columns)\n"));
    }

    [TestMethod]
    public void ReadsAndWritesMatrixElements()
    {
        Assert.AreEqual("2\nMatrix[[1, 9], [3, 4]]", Run(
            "var m = Matrix(List(1, 2), List(3, 4))\n"
            + "writeln(m.get(0, 1))\nm.set(0, 1, 9)\nwriteln(m)\n"));
    }

    [TestMethod]
    public void AddsAndSubtractsMatrices()
    {
        Assert.AreEqual("Matrix[[6, 8], [10, 12]]\nMatrix[[-4, -4], [-4, -4]]", Run(
            "var a = Matrix(List(1, 2), List(3, 4))\nvar b = Matrix(List(5, 6), List(7, 8))\n"
            + "writeln(a.add(b))\nwriteln(a.subtract(b))\n"));
    }

    [TestMethod]
    public void MultipliesAMatrixByAScalarMatrixAndVector()
    {
        Assert.AreEqual("Matrix[[2, 4], [6, 8]]\nMatrix[[19, 22], [43, 50]]\nVector[3, 7]", Run(
            "var a = Matrix(List(1, 2), List(3, 4))\nvar b = Matrix(List(5, 6), List(7, 8))\n"
            + "writeln(a.multiply(2))\nwriteln(a.multiply(b))\nwriteln(a.multiply(Vector(1, 1)))\n"));
    }

    [TestMethod]
    public void TransposesAMatrix()
    {
        Assert.AreEqual("Matrix[[1, 3], [2, 4]]", Run("writeln(Matrix(List(1, 2), List(3, 4)).transpose())\n"));
    }

    [TestMethod]
    public void ComputesTheDeterminantOfASquareMatrix()
    {
        Assert.AreEqual("-2", Run("writeln(Matrix(List(1, 2), List(3, 4)).determinant())\n"));
    }

    [TestMethod]
    public void ReportsTheDeterminantOfANonSquareMatrix()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("Matrix(List(1, 2, 3), List(4, 5, 6)).determinant()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Determinant requires a square Matrix.");
    }

    [TestMethod]
    public void ReportsMismatchedMatrixRowLengths()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("Matrix(List(1, 2), List(3))\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "All Matrix rows must have the same length.");
    }

    [TestMethod]
    public void ReportsANonListMatrixRow()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("Matrix(1, 2)\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Each Matrix row must be a List of numbers.");
    }

    [TestMethod]
    public void ComputesBasicTrigValues()
    {
        Assert.AreEqual("0\n1\n0", Run("writeln(sin(0))\nwriteln(cos(0))\nwriteln(tan(0))\n"));
    }

    [TestMethod]
    public void ComputesTrigValuesUsingPi()
    {
        Assert.AreEqual("1\n0\n3.1416", Run(
            "writeln(sin(pi() / 2).round(4))\nwriteln(cos(pi() / 2).round(4))\nwriteln(pi().round(4))\n"));
    }

    [TestMethod]
    public void ComputesInverseTrigValues()
    {
        Assert.AreEqual("1.5708\n0\n0.7854", Run(
            "writeln(asin(1).round(4))\nwriteln(acos(1))\nwriteln(atan(1).round(4))\n"));
    }

    [TestMethod]
    public void ComputesAtan2()
    {
        Assert.AreEqual("0.7854", Run("writeln(atan2(1, 1).round(4))\n"));
    }

    [TestMethod]
    public void ReportsANonNumberArgumentToATrigFunction()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("sin(\"x\")\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "sin() expects a number.");
    }

    [TestMethod]
    public void ComputesExp()
    {
        Assert.AreEqual("1\n2.7183", Run("writeln(exp(0))\nwriteln(exp(1).round(4))\n"));
    }

    [TestMethod]
    public void ComputesNaturalLog()
    {
        Assert.AreEqual("0\n1", Run("writeln(log(1))\nwriteln(log(exp(1)).round(4))\n"));
    }

    [TestMethod]
    public void ComputesLogWithAnExplicitBase()
    {
        Assert.AreEqual("3\n2", Run("writeln(log(8, 2))\nwriteln(log(100, 10).round(4))\n"));
    }

    [TestMethod]
    public void ReportsANonNumberArgumentToLog()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("log(\"x\")\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "log() expects a number.");
    }

    [TestMethod]
    public void WritesAndReadsAFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fife-test-{Guid.NewGuid():N}.txt");
        try
        {
            Assert.AreEqual("hello", Run(
                $"var file = File(\"{path}\")\nfile.write(\"hello\")\nwriteln(file.read())\n"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void AppendsToAFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fife-test-{Guid.NewGuid():N}.txt");
        try
        {
            Assert.AreEqual("hello world", Run(
                $"var file = File(\"{path}\")\nfile.write(\"hello\")\nfile.append(\" world\")\nwriteln(file.read())\n"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ChecksWhetherAFileExists()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fife-test-{Guid.NewGuid():N}.txt");
        try
        {
            Assert.AreEqual("false\ntrue", Run(
                $"var file = File(\"{path}\")\nwriteln(file.exists())\nfile.write(\"hi\")\nwriteln(file.exists())\n"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void CatchesAMissingFileAsAFileException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fife-test-missing-{Guid.NewGuid():N}.txt");
        Assert.AreEqual("caught: File not found: " + $"'{path}'.", Run(
            $"try {{\nFile(\"{path}\").read()\n}} catch (Exception e) {{\nwriteln(\"caught: \" + e.message)\n}}\n"));
    }

    [TestMethod]
    public void ReportsANonStringArgumentToTheFileConstructor()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("File(1)\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "File() expects a string.");
    }

    [TestMethod]
    public void ReportsTheSizeOfAFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fife-test-{Guid.NewGuid():N}.txt");
        try
        {
            Assert.AreEqual("5", Run($"var file = File(\"{path}\")\nfile.write(\"hello\")\nwriteln(file.size())\n"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ReportsFileSizeForAMissingFileAsAFileException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fife-test-missing-{Guid.NewGuid():N}.txt");
        Assert.AreEqual("caught: File not found: " + $"'{path}'.", Run(
            $"try {{\nFile(\"{path}\").size()\n}} catch (Exception e) {{\nwriteln(\"caught: \" + e.message)\n}}\n"));
    }

    [TestMethod]
    public void ReportsTheModifiedTimeOfAFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fife-test-{Guid.NewGuid():N}.txt");
        try
        {
            Assert.AreEqual("true", Run(
                $"var file = File(\"{path}\")\nfile.write(\"hi\")\nwriteln(file.modifiedTime() > 0)\n"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ReportsAFilesPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fife-test-{Guid.NewGuid():N}.txt");
        Assert.AreEqual(path, Run($"writeln(File(\"{path}\").path)\n"));
    }

    [TestMethod]
    public void ChecksWhetherADirectoryExists()
    {
        Assert.AreEqual("true\nfalse", Run(
            $"writeln(Directory(\"{Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)}\").exists())\n"
            + "writeln(Directory(\"no-such-directory-should-exist\").exists())\n"));
    }

    [TestMethod]
    public void ListsTheContentsOfADirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"fife-test-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "a.txt");
        try
        {
            Assert.AreEqual("1\ntrue", Run(
                $"File(\"{filePath}\").write(\"hi\")\n"
                + $"var entries = Directory(\"{directory}\").list()\n"
                + "writeln(entries.length)\nwriteln(entries.contains(\"a.txt\"))\n"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void ReportsListingAMissingDirectoryAsAFileException()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"fife-test-missing-dir-{Guid.NewGuid():N}");
        Assert.AreEqual("caught: Directory not found: " + $"'{directory}'.", Run(
            $"try {{\nDirectory(\"{directory}\").list()\n}} catch (Exception e) {{\nwriteln(\"caught: \" + e.message)\n}}\n"));
    }

    [TestMethod]
    public void ReportsAnUndefinedStringProperty()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("writeln(\"hello\".missing)\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Undefined property 'missing'.");
    }

    [TestMethod]
    public void ReportsAssigningToAStringProperty()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("\"hello\".length = 1\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Only class instances have fields.");
    }

    [TestMethod]
    public void CallsNumberMemberMethods()
    {
        Assert.AreEqual("3.14\n3\n4\n1.5", Run(
            "writeln(3.14159.round(2))\nwriteln(3.7.floor())\nwriteln(3.2.ceil())\nwriteln((-1.5).abs())\n"));
    }

    [TestMethod]
    public void RoundsToTheNearestIntegerWithoutDigits()
    {
        Assert.AreEqual("4", Run("writeln(3.6.round())\n"));
    }

    [TestMethod]
    public void ReportsAnUndefinedNumberProperty()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("writeln(1.5.missing)\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Undefined property 'missing'.");
    }

    [TestMethod]
    public void ReportsAssigningToANumberProperty()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("1.5.round = 1\n");

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
    public void InheritsMethodsFromASuperclass()
    {
        Assert.AreEqual("Rex makes a sound", Run(
            "class Animal {\nAnimal(name) {\nthis.name = name\n}\nspeak() {\nwriteln(this.name + \" makes a sound\")\n}\n}\n"
            + "class Dog : Animal {\nDog(name) {\nsuper.Animal(name)\n}\n}\nDog(\"Rex\").speak()\n"));
    }

    [TestMethod]
    public void OverridesSuperclassMethodsAndCallsThemWithSuper()
    {
        Assert.AreEqual("base\nderived", Run(
            "class A {\nspeak() {\nwriteln(\"base\")\n}\n}\n"
            + "class B : A {\nspeak() {\nsuper.speak()\nwriteln(\"derived\")\n}\n}\nB().speak()\n"));
    }

    [TestMethod]
    public void CallsTheSuperclassConstructorByName()
    {
        Assert.AreEqual("Rex\n4", Run(
            "class Animal {\nAnimal(name) {\nthis.name = name\n}\n}\n"
            + "class Dog : Animal {\nDog(name) {\nsuper.Animal(name)\nthis.legs = 4\n}\n}\n"
            + "var d = Dog(\"Rex\")\nwriteln(d.name)\nwriteln(d.legs)\n"));
    }

    [TestMethod]
    public void InheritsTheSuperclassConstructor()
    {
        Assert.AreEqual("Whiskers", Run(
            "class Animal {\nAnimal(name) {\nthis.name = name\n}\n}\nclass Cat : Animal {\n}\nwriteln(Cat(\"Whiskers\").name)\n"));
    }

    [TestMethod]
    public void InheritsConstructorsThroughSeveralLevels()
    {
        Assert.AreEqual("7", Run(
            "class A {\nA(x) {\nthis.x = x\n}\n}\nclass B : A {\n}\nclass C : B {\n}\nwriteln(C(7).x)\n"));
    }

    [TestMethod]
    public void ChecksArityAgainstAnInheritedConstructor()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("class Animal {\nAnimal(name) {\nthis.name = name\n}\n}\nclass Cat : Animal {\n}\nCat()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Expected 1 arguments but got 0.");
    }

    [TestMethod]
    public void ReportsInheritingFromANonClass()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("var notAClass = 1\nclass A : notAClass {\n}\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Superclass must be a class.");
    }

    [TestMethod]
    public void ReportsSelfInheritance()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("class A : A {\n}\n");

        Assert.IsTrue(engine.HadError);
        StringAssert.Contains(output.ToString(), "A class can't inherit from itself.");
    }

    [TestMethod]
    public void ReportsSuperOutsideOfAClass()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("writeln(super.foo)\n");

        Assert.IsTrue(engine.HadError);
        StringAssert.Contains(output.ToString(), "Can't use 'super' outside of a class.");
    }

    [TestMethod]
    public void ReportsSuperInAClassWithoutASuperclass()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("class A {\nm() {\nreturn super.foo()\n}\n}\n");

        Assert.IsTrue(engine.HadError);
        StringAssert.Contains(output.ToString(), "Can't use 'super' in a class with no superclass.");
    }

    [TestMethod]
    public void ReportsAnUndefinedSuperclassMethod()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("class A {\n}\nclass B : A {\nm() {\nreturn super.nope()\n}\n}\nB().m()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Undefined property 'nope'.");
    }

    [TestMethod]
    public void ReportsACallStackForNestedCalls()    {
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
    public void DoesNotLetATryCatchRecoverFromStackOverflow()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run(
            "fun f(n) {\nreturn f(n + 1)\n}\n"
            + "try {\nf(0)\n} catch (Exception e) {\nwriteln(\"never\")\n}\n");

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

    [TestMethod]
    public void CatchesAThrownBuiltInException()
    {
        Assert.AreEqual("boom", Run(
            "try {\nthrow Exception(\"boom\")\n} catch (Exception e) {\nwriteln(e.message)\n}\n"));
    }

    [TestMethod]
    public void CatchesAThrownUserDefinedExceptionSubclass()
    {
        Assert.AreEqual("file not found", Run(
            "class FileException : Exception {\n}\n"
            + "try {\nthrow FileException(\"file not found\")\n} catch (Exception e) {\nwriteln(e.message)\n}\n"));
    }

    [TestMethod]
    public void RunsCodeAfterATryCatchWhenNoExceptionIsThrown()
    {
        Assert.AreEqual("ok\nafter", Run(
            "try {\nwriteln(\"ok\")\n} catch (Exception e) {\nwriteln(\"never\")\n}\nwriteln(\"after\")\n"));
    }

    [TestMethod]
    public void LetsAnUnmatchedExceptionTypePropagate()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run(
            "class FileException : Exception {\n}\n"
            + "class OtherException : Exception {\n}\n"
            + "try {\nthrow FileException(\"nope\")\n} catch (OtherException e) {\nwriteln(\"never\")\n}\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Uncaught exception: nope");
    }

    [TestMethod]
    public void DoesNotCatchInterpreterErrorsAsFifeExceptions()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run(
            "try {\nwriteln(1 - \"two\")\n} catch (Exception e) {\nwriteln(\"never\")\n}\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Operands must be numbers.");
        StringAssert.DoesNotMatch(output.ToString(), new System.Text.RegularExpressions.Regex("never"));
    }

    [TestMethod]
    public void ReportsThrowingANonExceptionValue()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("throw 1\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Can only throw instances of Exception or a subclass.");
    }

    [TestMethod]
    public void ReportsAnUncaughtExceptionAtTopLevel()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("throw Exception(\"boom\")\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "Uncaught exception: boom");
    }

    [TestMethod]
    public void ReportsACallStackForAnUncaughtExceptionFromNestedCalls()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("fun inner() {\nthrow Exception(\"boom\")\n}\nfun outer() {\ninner()\n}\nouter()\n");

        Assert.IsTrue(engine.HadRuntimeError);
        Assert.AreEqual(
            "Uncaught exception: boom\n[line 2] in inner\n[line 5] in outer\n[line 7] in script\n",
            output.ToString().ReplaceLineEndings("\n"));
    }

    /// <summary>Starts a one-shot local HTTP server, runs a fife script against it, and returns
    /// the script's output. <paramref name="handler"/> handles exactly one request.</summary>
    private static string RunAgainstLocalServer(Action<HttpListenerContext> handler, Func<string, string> buildSource)
    {
        var port = GetFreeTcpPort();
        var baseUrl = $"http://localhost:{port}";
        HttpListener listener = new();
        listener.Prefixes.Add(baseUrl + "/");
        listener.Start();

        var serverTask = Task.Run(() => handler(listener.GetContext()));
        try
        {
            var output = Run(buildSource(baseUrl));
            serverTask.Wait(TimeSpan.FromSeconds(5));
            return output;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static int GetFreeTcpPort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [TestMethod]
    public void PerformsAGetRequest()
    {
        var output = RunAgainstLocalServer(
            context =>
            {
                context.Response.StatusCode = 200;
                var bytes = Encoding.UTF8.GetBytes("pong");
                context.Response.OutputStream.Write(bytes);
                context.Response.Close();
            },
            baseUrl =>
                $"var web = Web()\nvar response = web.get(\"{baseUrl}/ping\")\n"
                + "writeln(response.get(\"statusCode\"))\nwriteln(response.get(\"body\"))\nwriteln(response.get(\"success\"))\n");

        Assert.AreEqual("200\npong\ntrue", output);
    }

    [TestMethod]
    public void PerformsAPostRequestWithABody()
    {
        string? receivedMethod = null;
        string? receivedBody = null;

        var output = RunAgainstLocalServer(
            context =>
            {
                receivedMethod = context.Request.HttpMethod;
                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    receivedBody = reader.ReadToEnd();
                }

                context.Response.StatusCode = 201;
                var bytes = Encoding.UTF8.GetBytes("created");
                context.Response.OutputStream.Write(bytes);
                context.Response.Close();
            },
            baseUrl =>
                $"var web = Web()\nvar response = web.post(\"{baseUrl}/items\", \"name=widget\")\n"
                + "writeln(response.get(\"statusCode\"))\nwriteln(response.get(\"body\"))\n");

        Assert.AreEqual("201\ncreated", output);
        Assert.AreEqual("POST", receivedMethod);
        Assert.AreEqual("name=widget", receivedBody);
    }

    [TestMethod]
    public void SendsCustomHeadersAndApiKeys()
    {
        string? apiKey = null;
        string? customHeader = null;

        var output = RunAgainstLocalServer(
            context =>
            {
                apiKey = context.Request.Headers["X-Api-Key"];
                customHeader = context.Request.Headers["X-Custom"];
                context.Response.StatusCode = 200;
                context.Response.Close();
            },
            baseUrl =>
                $"var web = Web()\nweb.setApiKey(\"X-Api-Key\", \"secret-key\")\n"
                + "web.setHeader(\"X-Custom\", \"hi\")\n"
                + $"web.get(\"{baseUrl}/\")\n");

        Assert.AreEqual("", output);
        Assert.AreEqual("secret-key", apiKey);
        Assert.AreEqual("hi", customHeader);
    }

    [TestMethod]
    public void SendsABearerToken()
    {
        string? authorization = null;

        RunAgainstLocalServer(
            context =>
            {
                authorization = context.Request.Headers["Authorization"];
                context.Response.StatusCode = 200;
                context.Response.Close();
            },
            baseUrl => $"var web = Web()\nweb.setBearerToken(\"my-jwt\")\nweb.get(\"{baseUrl}/\")\n");

        Assert.AreEqual("Bearer my-jwt", authorization);
    }

    [TestMethod]
    public void SendsBasicAuthCredentials()
    {
        string? authorization = null;

        RunAgainstLocalServer(
            context =>
            {
                authorization = context.Request.Headers["Authorization"];
                context.Response.StatusCode = 200;
                context.Response.Close();
            },
            baseUrl => $"var web = Web()\nweb.setBasicAuth(\"user\", \"pass\")\nweb.get(\"{baseUrl}/\")\n");

        var expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"));
        Assert.AreEqual(expected, authorization);
    }

    [TestMethod]
    public void ReturnsNonSuccessStatusesWithoutThrowing()
    {
        var output = RunAgainstLocalServer(
            context =>
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
            },
            baseUrl =>
                $"var response = Web().get(\"{baseUrl}/missing\")\n"
                + "writeln(response.get(\"statusCode\"))\nwriteln(response.get(\"success\"))\n");

        Assert.AreEqual("404\nfalse", output);
    }

    [TestMethod]
    public void ReportsAFailedRequestAsACatchableWebException()
    {
        var port = GetFreeTcpPort();
        Assert.AreEqual("caught", Run(
            $"try {{\nWeb().get(\"http://localhost:{port}/\")\n"
            + "} catch (Exception e) {\nwriteln(\"caught\")\n}\n"));
    }

    [TestMethod]
    public void ReportsANonStringUrlArgumentToWebGet()
    {
        StringWriter output = new();
        ConsoleErrorReporter errors = new(output);
        FifeEngine engine = new(errors, output);
        engine.Run("Web().get(1)\n");

        Assert.IsTrue(engine.HadRuntimeError);
        StringAssert.Contains(output.ToString(), "'url' must be a string.");
    }

    [TestMethod]
    public void PerformsADeleteRequest()
    {
        string? receivedMethod = null;

        var output = RunAgainstLocalServer(
            context =>
            {
                receivedMethod = context.Request.HttpMethod;
                context.Response.StatusCode = 204;
                context.Response.Close();
            },
            baseUrl =>
                $"var response = Web().delete(\"{baseUrl}/items/1\")\nwriteln(response.get(\"statusCode\"))\n");

        Assert.AreEqual("204", output);
        Assert.AreEqual("DELETE", receivedMethod);
    }

    [TestMethod]
    public void ResolvesRelativeUrlsAgainstABaseUrlSetInTheConstructor()
    {
        string? requestedPath = null;

        var output = RunAgainstLocalServer(
            context =>
            {
                requestedPath = context.Request.Url!.AbsolutePath;
                context.Response.StatusCode = 200;
                context.Response.Close();
            },
            baseUrl =>
                $"var web = Web(\"{baseUrl}/\")\nvar response = web.get(\"users/1\")\nwriteln(response.get(\"statusCode\"))\n");

        Assert.AreEqual("200", output);
        Assert.AreEqual("/users/1", requestedPath);
    }

    [TestMethod]
    public void ResolvesRelativeUrlsAgainstABaseUrlSetWithSetBaseUrl()
    {
        string? requestedPath = null;

        var output = RunAgainstLocalServer(
            context =>
            {
                requestedPath = context.Request.Url!.AbsolutePath;
                context.Response.StatusCode = 200;
                context.Response.Close();
            },
            baseUrl =>
                $"var web = Web()\nweb.setBaseUrl(\"{baseUrl}/\")\nvar response = web.get(\"users/1\")\n"
                + "writeln(response.get(\"statusCode\"))\n");

        Assert.AreEqual("200", output);
        Assert.AreEqual("/users/1", requestedPath);
    }

    [TestMethod]
    public void UsesAnAbsoluteUrlEvenWhenABaseUrlIsSet()
    {
        string? requestedPath = null;

        var output = RunAgainstLocalServer(
            context =>
            {
                requestedPath = context.Request.Url!.AbsolutePath;
                context.Response.StatusCode = 200;
                context.Response.Close();
            },
            baseUrl =>
                $"var web = Web(\"https://ignored.example.invalid/\")\n"
                + $"var response = web.get(\"{baseUrl}/direct\")\nwriteln(response.get(\"statusCode\"))\n");

        Assert.AreEqual("200", output);
        Assert.AreEqual("/direct", requestedPath);
    }
}

