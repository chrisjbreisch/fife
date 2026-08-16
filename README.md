# fife

A small scripting language implemented in C#, based on the tree-walking `jlox` interpreter from
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

- Types: numbers (double), strings, booleans, `nil`
- Operators: `+ - * /`, `== != < <= > >=`, `!`, `and`, `or`; `+` concatenates when either side is a string
- Statements: `var`, `print`, blocks, `if`/`else`, `while`, `for`, `return`
- Functions: `fun` declarations with closures and recursion
- Comments: `//` line comments and nestable `/* ... */` block comments
- Natives: `clock()`

```fife
fun makeCounter() {
  var count = 0;
  fun increment() {
    count = count + 1;
    return count;
  }
  return increment;
}

var counter = makeCounter();
print counter(); // 1
print counter(); // 2
```

## Extending it

- **New native function**: `interpreter.DefineNative("sqrt", 1, (_, args) => Math.Sqrt((double)args[0]!));`
- **New syntax**: add a node to `Expr` or `Stmt` (plus its visitor method), parse it in `Parser`,
  then evaluate it in `Interpreter`. `AstPrinter` implements both visitors and will need the new
  method too, which keeps the compiler honest about missing cases.
- **Next chapters from the book**: a `Resolver` pass for static scope resolution, then classes
  (`class`, `this`, `super`). The `Class`, `This` and `Super` token types are already scanned.
