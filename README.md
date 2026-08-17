# fife

A small scripting language implemented in C#, based on the tree-walking interpreter design from
Robert Nystrom's [Crafting Interpreters](https://craftinginterpreters.com/).

## Layout

| Path | Purpose |
| --- | --- |
| `src/Fife.Core` | Scanner, parser, AST and interpreter |
| `src/Fife.Cli` | `fife` executable: REPL and script runner |
| `tests/Fife.Tests` | MSTest suite |
| `examples` | Sample `.fife` scripts |

### Core pipeline

`Scanner` -> `Parser` -> `Interpreter`, orchestrated by `FifeEngine`. All diagnostics flow through
`IErrorReporter`, so a host can capture errors instead of writing to the console.

## Build and run

```pwsh
dotnet build
dotnet test
dotnet run --project src/Fife.Cli -- examples/tour.fife   # run a script
dotnet run --project src/Fife.Cli                          # start the REPL
```

## Language today

- Types: `int`, `float` (represented by C# `double`), `bool`, `string`, `nil`; `var` declarations are dynamic, while typed declarations are checked
- Operators: `+ - * / ^`, `!!` (factorial), `== != <> < <= > >=`, `!`, `and`, `or`; `+` concatenates when either side is a string
- Statements: `var`, blocks, `if`/`else`, `while`, `for`, `return`; statements end at a newline, and `\\` continues onto the next line. Semicolons are only used to separate clauses in `for` headers.
- Functions: `fun` declarations with closures and recursion
- Comments: `//` line comments and nestable `/* ... */` block comments
- Standard library: `clock()`, `read()` / `read(prompt)`, `readln()` / `readln(prompt)`, `write()` / `write(value)`, and `writeln()` / `writeln(value)`

```fife
int count = 0
count = count + 1
writeln(count)

float ratio = 1.5
writeln(ratio)

bool ready = true
string name = "fife"
writeln(ready)
writeln(name)
```

```fife
fun makeCounter() {
  var count = 0
  fun increment() {
    count = count + 1
    return count
  }
  return increment
}

var counter = makeCounter()
writeln(counter()) // 1
writeln(counter()) // 2
```

`read` reads one character and returns its numeric character code. `readln` reads a complete
line. When given one argument, either function writes it as a prompt first. `write` and `writeln`
optionally write one value; `writeln()` writes just a newline.

## Extending it

- **New native function**: `interpreter.DefineNative("sqrt", 1, (_, args) => Math.Sqrt((double)args[0]!));`
- **New syntax**: add a node to `Expr` or `Stmt` (plus its visitor method), parse it in `Parser`,
  then evaluate it in `Interpreter`. `AstPrinter` implements both visitors and will need the new
  method too, which keeps the compiler honest about missing cases.
- **Next chapters from the book**: a `Resolver` pass for static scope resolution, then classes
  (`class`, `this`, `super`). The `Class`, `This` and `Super` token types are already scanned.
