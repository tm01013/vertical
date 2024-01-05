# Syntax guide for vertical

#### Table of contents:

- [Columns](#columns)
- [Execution points](#execution-points)
- [Variables](#variables)
- [Builtin commands](#builtin-commands)

<br>

## Columns

- In vertical the code is written vertically so instead of rows there is columns.
- The execution starts on the first character of the first column.
- If the execution is at the end of any column the program will stop.
- Every column is a "method".
- You can jump between columns using [execution points](#execution-points).

## Execution points

- Execution points is the same as goto labels
- You can define a execution point like this:<br>
  `§`<br>
  `0`

- To jump to thisexecution point use the `@` keyword:<br>
  `@`<br>
  `0`
- An execution point label can be any positive intager

## Variables

- You can define variables like this:<br>
  **Int:**

  ```
  a   <-variable name
  #   <- value of a int surrounded with hashtags
  1   <- value (can be a int expression)
  #
  ```

  **String:** <br>

  ```
  b   <-variable name
  "   <- value of a string starts with a double quote.
  H   <- value
  i
  '   <- value of a string ends with a single quote.
  ```

- **A variable name only can be a sigle lovercase letter!!** (hungarian letters supported)
- To give a variable a different value use the format from above.
- You cannot assign a string value to a previously int variable and vica versa.
- You can create global variables with the `G` keyword before the variable name.
- Global variables can accessed outside the column where it was created.
- If you want to overrwite a global variable you also need the `G` keyword before the variable name

- **Int expressions:**
  - Int expressions is math expression that can be used when defining ints
  - Only addition, substraction and multiplication is supported
  - Negative nubmers are supported
  - You can use int variables in int expressions
  - You cannot use floating point numbersin int expressions

## If statements

- Example if statement:

  ```
  ?   <- if statement starts with a question mark
  a
  |   <- the actual statement
  b
  /   <- if true sign
      <- some code that happenes when the statement is true
  \   <- if false sign
      <- some code that happenes when the statement is false
  _   <- end if sign
  ```

- The if true, if false and end if signes must be in this order.
- The expression operators:
  - `|` this operator means equal
  - `!` this operator means not equal
  - `>` this operator means the first side is bigger (only can used on ints or int expressions)
  - `<` this operator means the second side is bigger (only can used on ints or int expressions)
- Strings only can compared in a variable form (you cannot compare raw strings just string variables)
- Ints and int expressions can compared freely.
- **Dont leave spaces in a if statement!**

## Builtin commands

<hr>

### Print - `P`

- It's like Console.WriteLine()
- It **will start a new line** after writeing the variable to the console
- You only can print a single variable

### Write - `W`

- It's like Console.Write()
- It **won't** start a new line after writeing the variable to the console
- You only can print a single variable

### Quit - `Q`

- It will stop the program.
