using Challenge;

var fileName = "challenge.txt";
new Generator().Generate(5_000_000, fileName);


using var _ = new Metrics();
SortFile(fileName);

void SortFile(string fileName)
{
}
