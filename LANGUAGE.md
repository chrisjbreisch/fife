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
Con.writeln(1 + \
2)
```

Semicolons are not statement terminators. They are reserved for separating the initializer,
condition, and increment clauses in a `for` header:

```fife
for (var i = 0; i < 3; i = i + 1)
  Con.writeln(i)
```

Supported statement forms are:

- Variable declarations: `var`, `int`, `float`, `bool`, and `string`
- Expression statements
- Blocks delimited by `{` and `}`
- `if` / `else`
- `while`
- `for`
- `return`

The former `print` statement is not part of the language. Use `Con.writeln(...)` or
`Con.write(...)`.

## Scope

A block introduces a new scope, and an inner declaration shadows an outer one of the same name.
Every variable reference is bound to its declaring scope before the program runs, so a reference
always means the same variable no matter when it is evaluated:

```fife
var a = "global"
{
  fun showA() {
    Con.writeln(a)
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

Con.writeln(add(1, 2)) // 3
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
Con.writeln(counter()) // 1
Con.writeln(counter()) // 2
```

## Classes

A class is declared with `class`. Its body holds methods, which are written like functions but
without the `fun` keyword:

```fife
class Greeter {
  greet() {
    Con.writeln("hi")
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
    Con.writeln("hi, " + this.name)
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
Con.writeln(g.name)
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
that is not an instance, string, or number, is a run-time error.

## Inheritance

A class may name a superclass after a colon. It inherits the superclass's methods and may
override any of them:

```fife
class Animal {
  Animal(name) {
    this.name = name
  }

  speak() {
    Con.writeln(this.name + " makes a sound")
  }
}

class Dog : Animal {
  speak() {
    Con.writeln(this.name + " barks")
  }
}
```

Inside a subclass, `super.method(...)` calls the superclass's version, which is how an override
extends rather than replaces behaviour:

```fife
class Dog : Animal {
  speak() {
    super.speak()
    Con.writeln(this.name + " barks")
  }
}
```

Because a constructor is named after its own class, a subclass declares its own under its own
name. A subclass that has none inherits the superclass's constructor, including its parameters:

```fife
class Cat : Animal {
}

Cat("Whiskers").speak() // Whiskers makes a sound
```

A subclass that declares a constructor does not chain to the superclass automatically. Call it
explicitly by name, the same way you would call any other superclass method:

```fife
class Dog : Animal {
  Dog(name) {
    super.Animal(name)
    this.legs = 4
  }
}
```

These mistakes are reported before the program runs:

- Using `super` outside a class, or in a class with no superclass
- Declaring a class that inherits from itself

Naming a superclass that turns out not to be a class is a run-time error.

## Exceptions

Fife has a small built-in `Exception` class with a `message` field:

```fife
class Exception {
    Exception(message) {
        this.message = message
    }
}
```

`throw` raises an exception. Only instances of `Exception` or one of its subclasses may be thrown:

```fife
throw Exception("something went wrong")
```

`try` / `catch` runs a block and recovers from a matching exception:

```fife
try {
    throw Exception("boom")
} catch (Exception e) {
    Con.writeln(e.message)
}
```

The catch type matches the thrown value's class or any of its superclasses, so catching
`Exception` catches every built-in and user-defined exception. User-defined exception classes
inherit from `Exception` the same way any other class inherits from a superclass:

```fife
class InvalidInputException : Exception {
}

try {
    throw InvalidInputException("age must be positive")
} catch (Exception e) {
    Con.writeln(e.message)
}
```

Fife also has one built-in subclass, `FileException`, thrown by `File` and `Directory` members —
see below.

An exception that no enclosing `catch` matches stops the program and is reported the same way
other run-time errors are. `try` does not yet support `finally`.

### Exceptions vs. host errors

`try` / `catch` only ever catches values raised by an explicit `throw`. Failures the interpreter
raises itself — an undefined variable or property, a bad operand type, exceeding the maximum call
depth, and the like — are never caught by fife `catch` clauses, no matter how they are typed. They
always stop the program and print a diagnostic, the same way they did before exceptions existed.
This mirrors Java's split between `Exception` (recoverable, application-level failures) and
`Error` (host/runtime failures that a program should not try to recover from): fife just does not
give the uncatchable side a name yet, since nothing can currently catch it either way.

```fife
try {
    Con.writeln(1 - "two")     // interpreter error, not a thrown Exception
} catch (Exception e) {
    Con.writeln("never runs")
}
```

The program above still stops with `Operands must be numbers.`; the `catch` clause never runs.

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

The standard library is grouped into three namespace objects: `Con` for console input and output,
`Math` for logarithmic and trigonometric functions, and `System` for system functions such as the
clock. These names are intentionally not available as top-level functions.

### `System.clock()`

Returns the current Unix time in seconds.

### `Con.read()` and `Con.read(prompt)`

Reads one character and returns its numeric character code. With one argument, writes the argument
as a prompt before reading.

### `Con.readln()` and `Con.readln(prompt)`

Reads a complete line. With one argument, writes the argument as a prompt before reading.

### `Con.write()` and `Con.write(value)`

Writes no value with zero arguments, or one value without a trailing newline with one argument.

### `Con.writeln()` and `Con.writeln(value)`

Writes a newline with zero arguments, or one value followed by a newline with one argument.

```fife
string name = Con.readln("Name: ")
Con.writeln("Hello, " + name)
```

### `Math.pi()`

Returns π.

### `Math.sin(x)`, `Math.cos(x)`, `Math.tan(x)`

The standard trigonometric functions. `x` is in radians.

### `Math.asin(x)`, `Math.acos(x)`, `Math.atan(x)`

The inverse trigonometric functions. `x` is a ratio; the result is in radians.

### `Math.atan2(y, x)`

The angle, in radians, of the point `(x, y)` from the positive x-axis. Unlike `atan(y / x)`, it
uses the sign of both arguments to pick the correct quadrant.

```fife
Con.writeln(Math.sin(Math.pi() / 2))     // 1
Con.writeln(Math.atan2(1, 1).round(4))   // 0.7854
```

Each of these reports a run-time error if its argument isn't a number.

### `Math.exp(x)`

Returns e raised to the power `x`.

### `Math.log(x)` and `Math.log(x, base)`

Returns the natural logarithm of `x`. With two arguments, returns the logarithm of `x` in the
given `base` instead. There is no separate `sqrt`/`pow` — use `^` and `log`/`exp`.

```fife
Con.writeln(Math.log(Math.exp(1)))     // 1
Con.writeln(Math.log(8, 2))            // 3
```

## Files

`File(path)` creates a native handle bound to a file path. Nothing touches the filesystem until
you call a member:

```fife
var notes = File("notes.txt")
notes.write("hello")
notes.append(" world")
Con.writeln(notes.read())       // hello world
Con.writeln(notes.exists())     // true
Con.writeln(notes.size())       // 11
Con.writeln(notes.modifiedTime() > 0)  // true
Con.writeln(notes.path)         // notes.txt
```

- `path` — the path the handle was created with.
- `exists()` — whether the file exists, without throwing.
- `read()` — the whole file as a string.
- `write(content)` — writes `content`, overwriting the file (creating it if needed).
- `append(content)` — appends `content` (creating the file if it doesn't exist).
- `size()` — the file's size in bytes.
- `modifiedTime()` — the last-modified time as a Unix timestamp in seconds, the same format
  `clock()` uses.

`read()`, `size()`, and `modifiedTime()` throw `FileException` (a built-in subclass of
`Exception`) if the file doesn't exist; other failures — permissions denied, a missing directory,
and so on — throw it too, so filesystem errors can be caught like any other fife exception:

```fife
try {
    File("missing.txt").read()
} catch (Exception e) {
    Con.writeln(e.message)
}
```

`File` has no settable fields.

## Directories

`Directory(path)` creates a native handle bound to a directory path:

```fife
var here = Directory(".")
Con.writeln(here.exists())   // true

var entries = here.list()
for (var i = 0; i < entries.length; i = i + 1) {
    Con.writeln(entries.get(i))
}
```

- `path` — the path the handle was created with.
- `exists()` — whether the directory exists, without throwing.
- `list()` — a `List` of the names of the files and subdirectories directly inside it. Throws
  `FileException` if the directory doesn't exist.

`Directory` has no settable fields.

## Web

`Web()` creates a native HTTP client. Requests run synchronously — there is no async support in
fife, so the call blocks until the response arrives (or the request fails).

```fife
var web = Web()
var response = web.get("https://example.com/api/users")
Con.writeln(response.get("statusCode"))   // 200
Con.writeln(response.get("success"))      // true
Con.writeln(response.get("body"))
```

- `get(url)` — sends a GET request.
- `post(url)` / `post(url, body)` — sends a POST request, with an optional string body.
- `put(url)` / `put(url, body)` — sends a PUT request.
- `patch(url)` / `patch(url, body)` — sends a PATCH request.
- `delete(url)` / `delete(url, body)` — sends a DELETE request.

Each returns a `Map` with three entries: `statusCode` (an `int`), `body` (the response text as a
`string`), and `success` (`true` for a 2xx status). A body, if given, is sent as
`application/json`. A non-2xx response is returned normally, not thrown — only a request that
never gets a response (host unreachable, connection refused, and so on) throws a `WebException`
(a built-in subclass of `Exception`):

```fife
try {
    web.get("https://unreachable.example.invalid")
} catch (Exception e) {
    Con.writeln(e.message)
}
```

### Base URL

`Web(baseUrl)` — or `setBaseUrl(url)` after construction — sets a base URL that relative paths are
resolved against, so requests only need to name the path:

```fife
var api = Web("https://example.com/api/")
Con.writeln(api.get("users/1").get("statusCode"))   // resolves to https://example.com/api/users/1
```

A URL that is already absolute (starts with a scheme like `http://` or `https://`) is used as-is,
ignoring the base URL. An invalid base URL is a run-time error.

### Authentication

Authentication is configured once and applies to every subsequent request on that `Web` instance:

- `setHeader(name, value)` — sets an arbitrary header.
- `setApiKey(header, key)` — sets a header carrying an API key (the header name varies by API,
  commonly `X-Api-Key`).
- `setBearerToken(token)` — sets `Authorization: Bearer <token>`, for JWTs and similar tokens.
- `setBasicAuth(username, password)` — sets `Authorization: Basic <base64>` for username/password
  auth.

```fife
var api = Web()
api.setBearerToken(jwt)
api.setHeader("Accept", "application/json")
Con.writeln(api.get("https://example.com/api/me").get("statusCode"))
```

`Web` has no settable fields.

### String members

Strings expose members through the same `value.name` syntax used for class instances:

```fife
Con.writeln("hello".length)       // 5
Con.writeln("hello".upper())      // HELLO
Con.writeln("HELLO".lower())      // hello
Con.writeln("  hi  ".trim())       // hi
Con.writeln("hello world".substring(0, 5))  // hello
Con.writeln("hello".substring(2))           // llo
Con.writeln("hello".replace("l", "L"))      // heLLo
```

- `length` — the number of characters, as an `int`.
- `upper()` — an uppercased copy.
- `lower()` — a lowercased copy.
- `trim()` — a copy with leading and trailing whitespace removed.
- `substring(start)` — the characters from `start` to the end.
- `substring(start, end)` — the characters from `start` up to but not including `end`.
- `replace(target, replacement)` — a copy with every occurrence of `target` replaced.

`substring` reports a run-time error for an out-of-range or negative index, or an `end` before
`start`.

Strings are immutable: assigning to a string member is a run-time error.

### Number members

Numbers expose members the same way:

```fife
Con.writeln(3.14159.round(2))     // 3.14
Con.writeln(3.6.round())          // 4
Con.writeln(3.7.floor())          // 3
Con.writeln(3.2.ceil())           // 4
Con.writeln((-1.5).abs())         // 1.5
```

- `round()` — rounds to the nearest integer; `round(digits)` rounds to that many decimal places.
- `floor()` — rounds down to the nearest integer.
- `ceil()` — rounds up to the nearest integer.
- `abs()` — the absolute value.

Numbers are immutable: assigning to a number member is a run-time error.

## Lists

`List(...)` creates a native, resizable list. Any arguments become its initial items:

```fife
var empty = List()
var numbers = List(1, 2, 3)
Con.writeln(numbers)   // [1, 2, 3]
```

Lists expose members the same way strings and numbers do:

- `length` — the number of items, as an `int`.
- `get(index)` — the item at `index`.
- `set(index, value)` — replaces the item at `index`, and returns `value`.
- `add(value)` — appends `value`.
- `remove(value)` — removes the first item equal to `value`; returns `true` if one was removed.
- `removeAt(index)` — removes and returns the item at `index`.
- `contains(value)` — whether any item equals `value`.
- `indexOf(value)` — the index of the first item equal to `value`, or `-1`.

`get`, `set`, and `removeAt` report a run-time error for a non-integer or out-of-range index.
Lists have no settable fields — assigning to `list.name` is a run-time error; use `set` instead.

Lists also support `[...]` indexing, equivalent to `get`/`set`:

```fife
var numbers = List(1, 2, 3)
Con.writeln(numbers[0])   // 1
numbers[1] = 9
Con.writeln(numbers)       // [1, 9, 3]
```

`value[index]` and `value[index] = newValue` work on any type that implements the `[]` protocol
(currently only `List`); using them on anything else is a run-time error. Out-of-range or
non-integer indices report the same error as `get`/`set`.

There is no dedicated loop syntax for lists yet, so iterate with an ordinary `for` loop:

```fife
var names = List("Ann", "Bo", "Cy")
for (var i = 0; i < names.length; i = i + 1) {
    Con.writeln(names.get(i))
}
```

## Stacks

`Stack(...)` creates a native, last-in-first-out stack. Any arguments become its initial items,
pushed in order, so the last argument starts on top:

```fife
var stack = Stack(1, 2)
stack.push(3)
Con.writeln(stack.pop())      // 3
Con.writeln(stack.pop())      // 2
```

- `length` — the number of items, as an `int`.
- `isEmpty()` — whether the stack has no items.
- `push(value)` — puts `value` on top.
- `pop()` — removes and returns the top item.
- `peek()` — returns the top item without removing it.

`pop` and `peek` report a run-time error on an empty stack. Stacks have no settable fields.

## Queues

`Queue(...)` creates a native, first-in-first-out queue. Any arguments become its initial items,
enqueued in order:

```fife
var queue = Queue(1, 2)
queue.enqueue(3)
Con.writeln(queue.dequeue())  // 1
Con.writeln(queue.dequeue())  // 2
```

- `length` — the number of items, as an `int`.
- `isEmpty()` — whether the queue has no items.
- `enqueue(value)` — adds `value` at the back.
- `dequeue()` — removes and returns the item at the front.
- `peek()` — returns the item at the front without removing it.

`dequeue` and `peek` report a run-time error on an empty queue. Queues have no settable fields.

## Maps

`Map()` creates a native hash map, keyed by any value except `nil`. Populate it with `set` or
`[...]`:

```fife
var ages = Map()
ages.set("Ann", 30)
ages["Bo"] = 25

Con.writeln(ages.get("Ann"))   // 30
Con.writeln(ages["Bo"])        // 25
Con.writeln(ages.length)       // 2
```

- `length` — the number of entries, as an `int`.
- `get(key)` — the value stored under `key`; a run-time error if `key` isn't present.
- `set(key, value)` — stores `value` under `key`, replacing any existing entry, and returns
  `value`.
- `containsKey(key)` — whether `key` has an entry.
- `remove(key)` — removes the entry for `key`; returns `true` if one was removed.
- `keys()` — a `List` of the map's keys.
- `values()` — a `List` of the map's values.

`[...]` indexing is equivalent to `get`/`set`. Assigning `nil` as a key, or reading a key that
isn't present, is a run-time error. Maps have no settable fields.

## Vectors and Matrices

`Vector(...)` and `Matrix(...)` are native, numeric types backed by
[MathNet.Numerics](https://numerics.mathdotnet.com/). They support named methods rather than
arithmetic operators — `v.add(w)`, not `v + w`.

```fife
var v = Vector(1, 2, 3)
Con.writeln(v)              // Vector[1, 2, 3]
Con.writeln(v.length)        // 3
Con.writeln(v.get(0))        // 1
v.set(0, 9)
Con.writeln(v[0])             // 9 - [...] is equivalent to get/set
```

Vector members:

- `length` — the number of elements, as an `int`.
- `get(index)` / `set(index, value)`, and equivalent `[...]` indexing.
- `add(other)` / `subtract(other)` — element-wise; `other` must be a `Vector` of the same length.
- `multiply(scalar)` — scales every element by a number.
- `dot(other)` — the dot product with another `Vector` of the same length.
- `magnitude()` — the Euclidean (L2) norm.
- `normalize()` — a unit-length copy; a run-time error on a zero vector.

```fife
var m = Matrix(List(1, 2), List(3, 4))
Con.writeln(m)              // Matrix[[1, 2], [3, 4]]
Con.writeln(m.rows)          // 2
Con.writeln(m.columns)       // 2
Con.writeln(m.get(0, 1))     // 2
m.set(0, 1, 9)
```

`Matrix(...)` takes one `List` argument per row; every row must be the same length. There's no
`[...]` indexing for matrices (only a single index is supported today), so use `get`/`set` with
both a row and a column.

Matrix members:

- `rows` / `columns` — dimensions, as `int`s.
- `get(row, column)` / `set(row, column, value)`.
- `add(other)` / `subtract(other)` — `other` must be a `Matrix` of the same dimensions.
- `multiply(other)` — scales by a number, multiplies by another `Matrix` (dimensions must agree),
  or multiplies by a `Vector` (returns a `Vector`).
- `transpose()` — a new, transposed `Matrix`.
- `determinant()` — a run-time error unless the `Matrix` is square.

Vectors and matrices have no settable fields other than through `set`/`[...]`.