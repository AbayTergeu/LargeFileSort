# App LargeFileSort - test task for generate and sort lines in file.

# Assumptions

# Line format
1. A line is `<number>. <text>`. The separator is the first ". " in the line, so "1. Mr. Smith" has the text "Mr. Smith".
2. The number contains only digits (no sign, no spaces) and must fit in a 64-bit signed integer. Leading zeros are allowed: "01. Apple" has number 1.
3. The text can be empty: "5. " is a valid line. It sorts before any non-empty text.
4. Spaces in the text are kept as is. "1.  Apple" (two spaces) and "1. Apple" have different texts.

# Sort order
1. Text, compared byte by byte in UTF-8 (ordinal, case-sensitive).
   Uppercase letters come before lowercase: "Zebra" < "apple".
2. If the texts are equal, the number (ascending).
3. If both are equal ("1. Apple" and "01. Apple"), the whole line, byte by byte.
   This makes the output the same on every run.

# Invalid lines
	By default the sorter stops on the first invalid line and reports its line number.
	No output file is left behind. Use `--skip-invalid` to skip such lines;
	the number of skipped lines is printed at the end.

# Line endings
	Input may use `\n` or `\r\n` (even mixed). The output always uses `\n`, including after the last line.

# Limits
	A single line cannot be longer than the chunk size.

# Output file
	By default the result is written next to the input as `<name>.sorted.txt`.
	`--output` must not point to the input file.
	An existing output file is overwritten.
