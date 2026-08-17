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
| `!!` | Factorial; for example, `6!!` is `720` |
| `==` | Equality |
| `!=`, `<>` | Inequality |
| `<`, `<=`, `>`, `>=` | Numeric comparison |
| `!` | Boolean negation |
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

## Functions

Functions are declared with `fun`. They support parameters, return values, closures, and
recursion:

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