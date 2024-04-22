using Challenge;
using System.Collections.Concurrent;

if (!Directory.Exists("test\\"))
    Directory.CreateDirectory("test\\");
var fileName = "test\\Challenge.txt";
var resultFileName = "test\\Result.txt";
new Generator().Generate(10_000_000, fileName);

using var _ = new Metrics(fileName);
SortFile(fileName).GetAwaiter().GetResult();

async Task SortFile(string fileName)
{
    var files = SplitFile(fileName, 100_000);
    SortParts(files);
    await MergeParts(files);
}

IEnumerable<string> SplitFile(string fileName, int partsLineCount)
{
    var files = new List<string>();

    using var reader = new StreamReader(fileName);

    int partNumber = 0;
    while (!reader.EndOfStream)
    {
        partNumber++;
        var partFileName = "test\\" + partNumber + ".txt";
        files.Add(partFileName);

        using var writer = new StreamWriter(partFileName);
        for (var i = 0; i < partsLineCount; i++)
        {
            if (reader.EndOfStream)
                break;

            writer.WriteLine(reader.ReadLine());
        }
    }

    return files;
}

void SortParts(IEnumerable<string> files)
{
    Parallel.ForEach(files, (file) =>
    {
        var sortedLines = File.ReadAllLines(file)
            .Select(s => new Line(s))
            .Order();
        File.WriteAllLines(file, sortedLines.Select(s => s.FullLine));
    });
}

async Task MergeParts(IEnumerable<string> files)
{
    var readers = files.Select(s => new StreamReader(s));

    try
    {
        var concurrentReaders = new ConcurrentQueue<LineRead>();
        await Parallel.ForEachAsync(readers, async (reader, token) =>
        {
            concurrentReaders.Enqueue(new LineRead
            {
                Reader = reader,
                Line = new Line(await reader.ReadLineAsync(token))
            });
        });

        var lines = concurrentReaders.ToList();

        await using var writer = new StreamWriter(resultFileName);
        while (lines.Count > 0)
        {
            var current = lines.OrderBy(o => o.Line).First();
            await writer.WriteLineAsync(current.Line);

            if (current.Reader.EndOfStream)
            {
                lines.Remove(current);
                continue;
            }

            current.Line = new Line(await current.Reader.ReadLineAsync());
        }
    }
    finally
    {
        foreach (var streamReader in readers)
            streamReader.Dispose();
    }
}

internal class LineRead
{
    public StreamReader Reader { get; set; }
    public Line Line { get; set; }
}

internal class Line(string line) : IComparable<Line>
{
    private readonly int _pos = line.IndexOf('.');

    public string FullLine { get; } = line;

    public int Number => int.Parse(FullLine.AsSpan(0, _pos));
    public ReadOnlySpan<char> Word => FullLine.AsSpan(_pos + 2);

    public int CompareTo(Line? other)
    {
        var result = Word.CompareTo(other.Word, StringComparison.InvariantCultureIgnoreCase);
        return result != 0 ? result : Number.CompareTo(other.Number);
    }

    public static implicit operator string(Line line)
    {
        return line.FullLine;
    }
}