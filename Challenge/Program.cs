using Challenge;
using System.Collections.Concurrent;

if (!Directory.Exists("test\\"))
    Directory.CreateDirectory("test\\");
var fileName = "test\\Challenge.txt";
var resultFileName = "test\\Result.txt";
new Generator().Generate(100_000_000, fileName);

using var _ = new Metrics(fileName);
SortFile(fileName).GetAwaiter().GetResult();

async Task SortFile(string fileName)
{
    var files = await SplitFile(fileName, 100_000);
    await SortResults(files);
}

async Task<string[]> SplitFile(string fileName, int partsLineCount)
{
    var files = new List<string>();
    int partNumber = 0;

    await foreach (var batch in Batch(File.ReadLinesAsync(fileName), partsLineCount))
    {
        partNumber++;
        var partFileName = "test\\" + partNumber + ".txt";
        files.Add(partFileName);

        Array.Sort(batch, 0, batch.Length);
        await File.WriteAllLinesAsync(partFileName, batch.Select(s => s.FullLine));
    }

    return files.ToArray();
}

async IAsyncEnumerable<Line[]> Batch(IAsyncEnumerable<string> strings, int partLinesCount)
{
    var lines = new Line[partLinesCount];

    var counter = 0;
    await foreach (var @string in strings)
    {
        lines[counter] = new Line(@string);
        counter++;

        if (counter == partLinesCount)
        {
            yield return lines;
            counter = 0;
        }
    }

    if (counter != 0)
    {
        Array.Resize(ref lines, counter);
        yield return lines;
    }
}

async Task SortResults(IEnumerable<string> files)
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

        var lines = concurrentReaders.OrderBy(o => o.Line).ToList();

        await using var writer = new StreamWriter(resultFileName);
        while (lines.Count > 0)
        {
            var current = lines[0];
            await writer.WriteLineAsync(current.Line);

            if (current.Reader.EndOfStream)
            {
                lines.Remove(current);
                continue;
            }

            current.Line = new Line(await current.Reader.ReadLineAsync());
            Reorder(ref lines);
        }
    }
    finally
    {
        foreach (var streamReader in readers)
            streamReader.Dispose();
    }
}

void Reorder(ref List<LineRead> lines)
{
    if (lines.Count == 1)
        return;

    var i = 0;
    while (i < lines.Count - 1 && lines[i].Line.CompareTo(lines[i + 1].Line) > 0)
    {
        (lines[i], lines[i + 1]) = (lines[i + 1], lines[i]);

        if (++i == lines.Count)
            return;
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