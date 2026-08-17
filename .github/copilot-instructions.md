# Fife Project Instructions

## Project Structure

- `src/Fife.Core` contains the scanner, parser, AST, interpreter, runtime environment, and standard library.
- `src/Fife.Cli` contains the REPL and script runner.
- `tests/Fife.Tests` contains the MSTest suite.
- `examples` contains sample `.fife` programs.

## Development Workflow

- Build with `dotnet build Fife.slnx`.
- Run all tests with `dotnet test Fife.slnx --no-restore`.
- Run the sample with `dotnet run --project src/Fife.Cli --no-restore -- examples/tour.fife`.
- Keep changes focused and preserve existing user changes.
- After any language or standard-library update, update `README.md`, run the full test suite, commit the changes, and push to `origin/main`.

## Language Conventions

- The implementation pipeline is `Scanner -> Parser -> Interpreter`, coordinated by `FifeEngine`.
- Ordinary statements are terminated only by newlines. Semicolons are reserved for clauses inside `for` headers.
- A backslash immediately followed by a newline continues the current statement.
- `var` declarations are dynamic. Typed declarations include `int`, `float`, `bool`, and `string`.
- Numeric runtime values use C# `double`; `int` values must be whole numbers, while `float` values may be fractional.
- Typed declarations default to `0`, `0.0`, `false`, or `""` as appropriate and enforce their type on assignment.
- Inequality supports both `!=` and `<>`.
- Output uses `writeln(...)` or `write(...)`; input uses `read(...)` or `readln(...)`. The former `print` statement is removed.

## Implementation Guidance

- Follow existing scanner/parser/interpreter patterns before introducing new abstractions.
- Add scanner, parser, and runtime tests for new syntax or operators.
- Use injected `TextReader` and `TextWriter` streams when testing I/O behavior.
- Keep README language and standard-library documentation synchronized with implementation changes.
- After each completed step: validate it, update the README, LANGUAGE.md, and any relevant documentation, run the full test suite, commit the step separately, and push the active branch.
