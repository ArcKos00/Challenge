using BenchmarkDotNet.Columns;
using System.Diagnostics;
using System.Globalization;

namespace Challenge
{
    internal class Metrics : IDisposable
    {
        private readonly Stopwatch _stopwatch = new();
        private long _startAllocation;
        private long _endAllocation;
        private readonly string _fileName;

        public Metrics(string fileName)
        {
            _fileName = fileName;
            StartRecord();
        }

        private void StartRecord()
        {
            _startAllocation = GC.GetTotalAllocatedBytes(false);
            _stopwatch.Start();
        }

        private void StopRecord()
        {
            _endAllocation = GC.GetTotalAllocatedBytes(false);
            _stopwatch.Stop();
        }

        public void Dispose()
        {
            StopRecord();
            File.Delete(_fileName);
            Console.WriteLine($"Allocation: {SizeValue.FromBytes(_endAllocation - _startAllocation).ToString(SizeUnit.MB, CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Time: {_stopwatch.Elapsed}");
            Console.ReadKey();
        }
    }
}