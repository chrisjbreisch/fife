# Fife Language Reference

This guide describes the current Fife language syntax and standard library.

## Types And Variables

Fife supports dynamic declarations with `var` and checked declarations with an explicit type:

| Declaration | Runtime type | Default value | Assignment rule |
| --- | --- | --- | --- |
| `var name` | Dynamic | `nil` | Any value |
| `int name` | Number (`double`) | `0` | Must be a whole number |
| `float name` | Number (`double`) | `0.0` | Must be numeric; may be fractional |
| `bool name` | Boolean | `false` | Must be `true` or `false` |
| `string name` | String | `""` | Must be a string |

Declarations may have an initializer:

```fife
var answer = 42
int count = 0
float ratio = 1.5
bool ready = true
string name = "fife"
```

Typed declarations enforce their type when initialized and whenever they are assigned a new
value. `var` declarations remain dynamically typed.

## Operators

| Operators | Meaning |
| --- | --- |
| `+` | Addition, or string concatenation when either operand is a string |
| `-` | Subtraction |
| `*` | Multiplication |
| `/` | Division |
| `^` | Exponentiation |
| `!` (postfix) | Factorial; for example, `6!` is `720` |
| `==` | Equality |
| `!=`, `<>` | Inequality |
| `<`, `<=`, `>`, `>=` | Numeric comparison |
| `!` (prefix) | Boolean negation |
| `and`, `or` | Logical operators |

Exponentiation is right-associative, so `2 ^ 3 ^ 2` means `2 ^ (3 ^ 2)`.

## Statements

Statements end at a newline. A backslash immediately followed by a newline continues the current
statement onto the next line:

```fife
writeln(1 + \
2)
```

Semicolons are not statement terminators. They are reserved for separating the initializer,
condition, and increment clauses in a `for` header:

```fife
for (var i = 0; i < 3; i = i + 1)
  writeln(i)
```

Supported statement forms are:

- Variable declarations: `var`, `int`, `float`, `bool`, and `string`
- Expression statements
- Blocks delimited by `{` and `}`
- `if` / `else`
- `while`
- `for`
- `return`

The former `print` statement is not part of the language. Use `writeln(...)` or `write(...)`.

## Scope

A block introduces a new scope, and an inner declaration shadows an outer one of the same name.
Every variable reference is bound to its declaring scope before the program runs, so a reference
always means the same variable no matter when it is evaluated:

```fife
var a = "global"
{
  fun showA() {
    writeln(a)
  }

  showA()      // global
  var a = "block"
  showA()      // global
}
```

The second `showA()` still writes `global`, because the `a` inside `showA` was bound to the outer
declaration when the function was declared.

Three mistakes are reported before execution begins:

- Reading a local variable inside its own initializer, as in `var a = a`
- Declaring the same name twice in one local scope
- Using `return` outside a function

Declaring the same name twice is allowed at the top level, which keeps redefinition convenient in
the REPL.

## Functions

Functions are declared with `fun`. A function may state a return type before `fun`, and each
parameter may state its own type. Every annotation is optional:

```fife
int fun add(int a, int b) {
  return a + b
}

writeln(add(1, 2)) // 3
```

An annotation may be any of `var`, `int`, `float`, `bool`, or `string`. Omitting an annotation is
the same as writing `var`, so these two declarations are equivalent:

```fife
fun describe(value) {
  return "value: " + value
}

var fun describe(var value) {
  return "value: " + value
}
```

Annotations can be mixed, so you can type only the parts that matter:

```fife
int fun sum(int count, values) {
  return count
}
```

Annotations are checked while the program runs. An argument must match its parameter's type, and
a returned value must match the declared return type:

```fife
int fun double(int n) {
  return n * 2
}

double(1.5) // Error: Parameter 'n' requires an integer value.
```

A typed parameter behaves like a typed variable inside the body, so assigning it a value of
another type is also an error. A function with a return type other than `var` must return a
matching value; reaching the end of the body without doing so is an error.

Functions support closures and recursion:

```fife
fun makeCounter() {
  int count = 0
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

## Classes

A class is declared with `class`. Its body holds methods, which are written like functions but
without the `fun` keyword:

```fife
class Greeter {
  greet() {
    writeln("hi")
  }
}

var g = Greeter()
g.greet()
```

Calling the class creates an instance. A method whose name matches the class name is its
constructor, and it runs on every instantiation:

```fife
class Greeter {
  Greeter(name) {
    this.name = name
  }

  greet() {
    writeln("hi, " + this.name)
  }
}

Greeter("world").greet() // hi, world
```

Inside a method, `this` refers to the instance the method was called on. A method keeps its
instance even when it is stored in a variable and called later.

Fields are created by assigning to them and may be read or written from outside the class:

```fife
var g = Greeter("world")
g.name = "fife"
writeln(g.name)
```

A field shadows a method of the same name. Methods take type annotations exactly as functions do:

```fife
class Adder {
  int add(int a, int b) {
    return a + b
  }
}
```

A constructor may use a bare `return` to exit early. These mistakes are reported before the
program runs:

- Using `this` outside a class
- Returning a value from a constructor
- Declaring a return type on a constructor, which always returns its instance

Reading a property that is neither a field nor a method, or using property syntax on something
that is not an instance, is a run-time error.

## Errors

A run-time error stops the program and prints the message followed by a call stack, innermost
first. Each line gives the line number and the function that was executing there, with `script`
standing for top-level code:

```text
Undefined property 'missing'.
[line 3] in get
[line 12] in inner
[line 8] in outer
[line 15] in script
```

Long stacks are truncated after ten frames.

Calls may nest 100 deep. Exceeding that reports a stack overflow rather than crashing the host,
so a runaway recursion produces an ordinary error:

```text
Stack overflow: exceeded the maximum call depth of 100.
```

## Comments

Line comments begin with `//`. Block comments use `/*` and `*/` and may be nested:

```fife
// This is a line comment.
/* This is a block comment.
   /* Nested comments are supported. */
*/
```

## Standard Library

### `clock()`

Returns the current Unix time in seconds.

### `read()` and `read(prompt)`

Reads one character and returns its numeric character code. With one argument, writes the argument
as a prompt before reading.

### `readln()` and `readln(prompt)`

Reads a complete line. With one argument, writes the argument as a prompt before reading.

### `write()` and `write(value)`

Writes no value with zero arguments, or one value without a trailing newline with one argument.

### `writeln()` and `writeln(value)`

Writes a newline with zero arguments, or one value followed by a newline with one argument.

```fife
string name = readln("Name: ")
writeln("Hello, " + name)
```