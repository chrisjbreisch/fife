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

## 1. Host Object Protocol

Generalize property access beyond `ClassInstance`:

- Introduce an `IFifeObject`-style protocol for `Get` and `Set` behavior.
- Have `ClassInstance` implement the protocol.
- Let native objects and primitive adapters expose members through the same expression path.
- Use this to add useful string members such as `length` and string operations.

Today `"hello".length` fails because `Expr.Get` only accepts class instances.

## 2. Indexing

Add indexing before collection types are implemented:

- Parse `value[index]`.
- Add indexed assignment, `value[index] = newValue`.
- Add `Expr.Index` and the corresponding set form.
- Define index error behavior through the exception system.
- Make the protocol usable by lists, strings, vectors, and matrices.

## 3. Standard Library Objects

Implement native-backed objects after the object protocol and indexing are stable:

- `List`
- `Stack`
- `Queue`
- `Matrix`
- `Vector`

Each type should have focused behavior tests and documented construction, member, and error
semantics. Standard-library failures should use the exception system where recovery is useful.

## 4. Operator Dispatch Decision

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

## Deliberately Deferred

- Checked exceptions
- `finally` until try/catch and return unwinding are settled
- A large C#-style exception catalog
- Function overloading
- `void` / `FifeType.None`
