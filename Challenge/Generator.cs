namespace Challenge
{
    public class Generator
    {
        private readonly Random random = new Random();
        private string[] words;

        public void Generate(long linesCount = int.MaxValue, string fileName = "challenge.txt")
        {
            words = PreGenerateStrings();
            using var writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), fileName));

            for (int i = 0; i < linesCount; i++)
            {
                writer.WriteLine(GenerateNumber() + ". " + GenerateString());
            }
        }

        private string[] PreGenerateStrings()
            => Enumerable.Range(0, 10000)
                .Select(s => new string(Enumerable
                    .Range(0, random.Next(20, 100))
                    .Select(s => (char)random.Next('A', 'Z'))
                    .ToArray()))
                .ToArray();

        string GenerateNumber()
            => random.Next(10000).ToString();

        string GenerateString()
            => words[random.Next(words.Length)];
    }
}
