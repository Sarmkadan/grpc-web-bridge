#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Running;

namespace GrpcWebBridge.Benchmarks;

public class BenchmarkProgram
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(BenchmarkProgram).Assembly).Run(args);
    }
}
