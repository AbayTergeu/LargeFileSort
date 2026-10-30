using FileGenerator;

const long minSize = 1024;

if (args.Length is not (2 or 3))
{
    return Usage();
}

var outputPath = args[0];
if (!SizeParser.TryParse(args[1], out var targetSize) || targetSize < minSize)
{
    Console.Error.WriteLine($"Invalid size: '{args[1]}'. The minimum is 1KB.");
    return Usage();
}

var seed = Random.Shared.Next();
if (args.Length == 3 && !int.TryParse(args[2], out seed))
{
    Console.Error.WriteLine($"Invalid seed: '{args[2]}'.");
    return Usage();
}

using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1 << 20))
{
    new RandomLinesWriter(seed).Generate(output, targetSize);
}

Console.WriteLine($"Generated '{outputPath}': {new FileInfo(outputPath).Length:N0} bytes, seed {seed}.");
return 0;

static int Usage()
{
    Console.Error.WriteLine("Usage: FileGenerator <output-path> <size> [seed]");
    Console.Error.WriteLine("  size  bytes, or with a KB / MB / GB suffix: 500MB, 10GB. Minimum 1KB.");
    Console.Error.WriteLine("  seed  optional; the same seed always produces the same file.");
    return 1;
}
