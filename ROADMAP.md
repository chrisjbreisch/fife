# Fife Roadmap

This roadmap captures the language and standard-library work done after classes, inheritance,
call-stack tracing, and exception handling. As of 2026-08-23, every planned item below is
complete; the file is kept as a record of what was decided and why.

## Design Principles

- Fife uses unchecked exceptions. Functions do not need `throws` declarations.
- Fife has a small exception hierarchy, not a mirror of C#'s thousands of exception types.
- User-defined classes can inherit from the built-in `Exception` class.
- Recoverable, user-visible failures are catchable; interpreter and host failures remain outside
  normal fife exception handling unless explicitly promoted later.
- `void` / `FifeType.None` is intentionally out of scope. Omitting a return type already permits a
  function to return no value, so a separate type is not currently worth the extra semantics.

## 1. Standard Library Objects

Implement native-backed objects using the host object protocol:

- `List` — done; see Completed Foundations.
- `Stack` — done; see Completed Foundations.
- `Queue` — done; see Completed Foundations.
- `Map` — done; see Completed Foundations.
- `Vector` — done; see Completed Foundations.
- `Matrix` — done; see Completed Foundations.

Each type should have focused behavior tests and documented construction, member, and error
semantics. Standard-library failures should use the exception system where recovery is useful.

## Operator Dispatch Decision (resolved)

Decided 2026-08-23, when `Vector`/`Matrix` were implemented: named methods only (`vector.add(w)`,
`matrix.multiply(other)`), not `+`/`-`/`*` operator overloading. `VisitBinaryExpr` still only
handles numbers and strings directly; no operator-dispatch machinery was added. Revisit only if a
concrete need for `v + w` syntax outweighs the added complexity around type annotations and
runtime errors.

## Completed Foundations

- Dynamic and static declarations: `var`, `int`, `float`, `bool`, and `string`
- Optional typed function and method annotations with runtime parameter/return checks
- Newline-only statements and backslash line continuation
- `!=` and `<>` inequality
- Postfix factorial `!` and prefix/double negation
- Resolver-based lexical scope binding
- Classes, fields, methods, `this`, and constructors named after their classes
- Inheritance using `:` and `super`
- Inherited constructors
- Runtime call-stack traces and a maximum call depth
- A built-in `Exception` class, `throw`, and `try` / `catch (Type name)` matched by walking
  `ClassDefinition.Superclass`
- An `IFifeObject` protocol for `Get`/`Set` behavior, implemented by `ClassInstance` and extended
  to strings (`length`, `upper()`, `lower()`, `trim()`, `substring()`, `replace()`) and numbers
  (`round()`, `floor()`, `ceil()`, `abs()`)
- A native `List` type (`FifeListInstance`) built on the `IFifeObject` protocol, with
  `length`/`get`/`set`/`add`/`remove`/`removeAt`/`contains`/`indexOf`
- Native `Stack` (`push`/`pop`/`peek`/`isEmpty`) and `Queue` (`enqueue`/`dequeue`/`peek`/`isEmpty`)
  types, same `IFifeObject` pattern as `List`
- `[...]` indexing: `value[index]` and `value[index] = newValue`, via a new `IFifeIndexable`
  protocol (`GetIndex`/`SetIndex`). `List` implements it alongside its existing `get`/`set`
  methods; both do the same range/type checking.
- A native `Map` type (`FifeMapInstance`), backed by a real `Dictionary<object, object?>` and
  implementing both `IFifeObject` and `IFifeIndexable`, with
  `length`/`get`/`set`/`containsKey`/`remove`/`keys()`/`values()` (the latter two return `List`s)
- Native `Vector` and `Matrix` types, backed by
  [MathNet.Numerics](https://numerics.mathdotnet.com/) (`MathNet.Numerics.LinearAlgebra.Vector<double>`
  / `Matrix<double>`). `Vector` implements `IFifeIndexable`; `Matrix` does not, since it needs two
  indices and `[...]` only supports one — use `get(row, column)`/`set(row, column, value)`
  instead. Arithmetic is named methods only (`add`, `subtract`, `multiply`), not operators — see
  "Operator Dispatch Decision" above.
- A built-in `FileException : Exception` (first real subclass, bootstrapped alongside `Exception`
  itself) and file I/O standard-library functions: `readFile`/`writeFile`/`appendFile`/
  `fileExists`. File failures throw a catchable `FileException` via a new general-purpose
  `Interpreter.CreateException(ClassDefinition, Token, string)` helper, instead of an uncatchable
  `RuntimeError` — the first stdlib functions to use catchable exceptions rather than `RuntimeError`.

## Deliberately Deferred

- Checked exceptions
- `finally` until try/catch and return unwinding are settled
- A large C#-style exception catalog
- Function overloading
- `void` / `FifeType.None`
- `+`/`-`/`*` operator overloading on `Vector`/`Matrix` (or any native/class object) — see
  "Operator Dispatch Decision" above
