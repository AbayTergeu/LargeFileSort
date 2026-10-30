# LargeFileSort

Test task: generate a large text file and sort its lines.

## Assumptions

### Line format
1. A line is `<number>. <text>`. The separator is the first ". " in the line, so "1. Mr. Smith" has the text "Mr. Smith".
2. The number contains only digits (no sign, no spaces) and must fit in a 64-bit signed integer. Leading zeros are allowed: "01. Apple" has number 1.
3. The text can be empty: "5. " is a valid line. It sorts before any non-empty text.
4. Spaces in the text are kept as is. "1.  Apple" (two spaces) and "1. Apple" have different texts.

### Sort order
1. Text, compared byte by byte in UTF-8 (ordinal, case-sensitive).
   Uppercase letters come before lowercase: "Zebra" < "apple".
2. If the texts are equal, the number (ascending).
3. If both are equal ("1. Apple" and "01. Apple"), the whole line, byte by byte.
   This makes the output the same on every run.

### Invalid lines
By default the sorter stops on the first invalid line and reports its line number.
No output file is left behind. Use `--skip-invalid` to skip such lines;
the number of skipped lines is printed at the end.
An empty line is also an invalid line.

### Line endings
Input may use `\n` or `\r\n` (even mixed). The output always uses `\n`, including after the last line.

### Limits
A single line cannot be longer than the chunk size.

### Output file
- By default the result is written next to the input as `<name>.sorted.txt`.
- `--output` must not point to the input file.
- An existing output file is overwritten.

The sort order, the encoding and the handling of invalid lines were confirmed with the 3dEYE team.

## Test file generator

```
dotnet run --project src/FileGenerator -c Release -- <output-path> <size> [seed]
```

- `size` is in bytes or with a KB / MB / GB suffix, for example `10GB`. Minimum 1KB.
- The file is never larger than `size` and is smaller by less than one line.
- Text parts come from a fixed set of 1000 phrases, so many lines share the same text.
- The same `seed` always produces the same file.

Generating a 1 GB file takes about 6 seconds (including `dotnet run` startup).

## Sorter

```
dotnet run --project src/FileSorter -c Release -- sort <input> [--output <path>] [--temp-dir <path>] [--chunk-mb <n>] [--fan-in <n>] [--skip-invalid]
```

External merge sort in two phases:
1. **Split.** The input is read in chunks of `--chunk-mb` (256 MB by default). Each chunk ends at a line break.
   Its lines are sorted in memory without copying them and written to a temporary run file.
2. **Merge.** Up to `--fan-in` runs (64 by default) are merged at once with a priority queue.
   If there are more runs, they are merged in groups first, and the results are merged again.

- Temporary files are created in a folder next to the output (or in `--temp-dir`) and are deleted at the end, also on errors.
- The result is written to `<output>.partial` and renamed at the end, so a failed sort never leaves a broken output file.
- A UTF-8 BOM at the start of the input is skipped. The output has no BOM.
- Disk space: about 2× the input size besides the input itself (runs + output).