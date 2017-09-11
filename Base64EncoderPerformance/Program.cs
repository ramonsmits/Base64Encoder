using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using Exyll;

class Program
{
    static void Main()
    {
        BenchmarkRunner.Run<PerformanceTests>();
    }
}

[LegacyJitX64Job, LegacyJitX86Job, RyuJitX64Job]
public class PerformanceTests
{
    const int Count = 21;
    static readonly byte[][] data = new byte[Count][];
    static readonly string[] strings = new string[Count];

    static PerformanceTests()
    {
        Random r = new Random(1337);
        for (int i = 0; i < Count; i++)
        {
            data[i] = new byte[i];

            for (int j = 0; j < i; j++)
            {
                data[i][j] = (byte)r.Next(255);
            }

            strings[i] = Convert.ToBase64String(data[i]);
        }
    }

    [Benchmark]
    public void ToBase64WithConvert()
    {
        for (int j = 0; j < Count; j++)
        {
            Convert.ToBase64String(data[j]);
        }
    }

    [Benchmark]
    public void ToBase64WithEncoder()
    {
        var b64Encoder = Base64Encoder.Default;
        for (int j = 0; j < Count; j++)
        {
            b64Encoder.ToBase(data[j]);
        }
    }

    [Benchmark]
    public void FromBase64WithConvert()
    {
        for (int j = 0; j < Count; j++)
        {
            Convert.FromBase64String(strings[j]);
        }
    }

    [Benchmark]
    public void FromBase64WithEncoder()
    {
        var b64Encoder = Base64Encoder.Default;

        for (int j = 0; j < Count; j++)
        {
            b64Encoder.FromBase(strings[j]);
        }
    }
}
