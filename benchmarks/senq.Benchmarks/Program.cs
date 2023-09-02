using BenchmarkDotNet.Running;

namespace Benchmarks {

    class Benchmark {
        public static void Main(string[] args) {
            //throw new System.Exception();
            var summary = BenchmarkRunner.Run<StaticBenchmarks>();
        }
    }
}