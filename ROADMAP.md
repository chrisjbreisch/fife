# Fife Roadmap

This roadmap captures the next language and standard-library work after classes, inheritance,
call-stack tracing, and exception handling.

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
- `Dictionary`/`Map` — planned next, using the `IFifeIndexable` protocol for `map[key]` access.
- `Matrix`
- `Vector`

Each type should have focused behavior tests and documented construction, member, and error
semantics. Standard-library failures should use the exception system where recovery is useful.

## 2. Operator Dispatch Decision

Decide how objects participate in operators before adding substantial matrix/vector APIs:

- Keep operations as named methods such as `vector.add(other)`; or
- Add operator dispatch for native/class objects.

The current `+` implementation handles numbers and strings directly. Operator dispatch is more
expressive for vectors and matrices, but it increases complexity around type annotations and
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

## Deliberately Deferred

- Checked exceptions
- `finally` until try/catch and return unwinding are settled
- A large C#-style exception catalog
- Function overloading
- `void` / `FifeType.None`
