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

## Language Reference

See [LANGUAGE.md](LANGUAGE.md) for the types, operators, statement syntax, functions, comments,
and standard library reference.

## Extending it

- **New native function**: `interpreter.DefineNative("sqrt", 1, (_, args) => Math.Sqrt((double)args[0]!));`
- **New syntax**: add a node to `Expr` or `Stmt` (plus its visitor method), parse it in `Parser`,
  then evaluate it in `Interpreter`. `AstPrinter` implements both visitors and will need the new
  method too, which keeps the compiler honest about missing cases.
- **Next chapters from the book**: a `Resolver` pass for static scope resolution, then classes
  (`class`, `this`, `super`). The `Class`, `This` and `Super` token types are already scanned.
