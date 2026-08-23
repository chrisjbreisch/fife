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

### Examples

| File | Demonstrates |
| --- | --- |
| `tour.fife` | Core language: variables, control flow, functions, closures, classes |
| `exceptions.fife` | `throw`, `try`/`catch`, and the built-in `Exception` hierarchy |
| `collections.fife` | `List`, `Stack`, `Queue`, `Map`, and `[...]` indexing |
| `math.fife` | Number members, trig/log functions, `Vector`, `Matrix` |
| `strings.fife` | String members: `upper`, `lower`, `trim`, `substring`, `replace` |
| `files.fife` | `File` and `Directory` objects |
| `web.fife` | The `Web` HTTP client, including authentication |

Run any of them with `dotnet run --project src/Fife.Cli -- examples/<file>.fife`.


### Core pipeline

`Scanner` -> `Parser` -> `Resolver` -> `Interpreter`, orchestrated by `FifeEngine`. The resolver
binds each variable reference to its declaring scope before execution. All diagnostics flow through
`IErrorReporter`, so a host can capture errors instead of writing to the console.

`Vector` and `Matrix` are backed by [MathNet.Numerics](https://numerics.mathdotnet.com/), a
dependency of `src/Fife.Core`.

## Build and run

```pwsh
dotnet build
dotnet test
dotnet run --project src/Fife.Cli -- examples/tour.fife   # run a script
dotnet run --project src/Fife.Cli                          # start the REPL
```

## Language Reference

See [LANGUAGE.md](LANGUAGE.md) for the types, operators, statement syntax, functions, classes,
exceptions, comments, and standard library reference.

## Roadmap

See [ROADMAP.md](ROADMAP.md) for the remaining open decision on operator dispatch.

## Extending it

- **New native function**: `interpreter.DefineNative("sqrt", 1, (_, args) => Math.Sqrt((double)args[0]!));`
- **New syntax**: add a node to `Expr` or `Stmt` (plus its visitor method), parse it in `Parser`,
  then evaluate it in `Interpreter`. `AstPrinter` implements both visitors and will need the new
  method too, which keeps the compiler honest about missing cases.
- **Next chapters from the book**: complete. Classes and inheritance are implemented, with `:`
  in place of the book's `<` for naming a superclass.
