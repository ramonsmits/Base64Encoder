using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Exyll;

BenchmarkRunner.Run<PerformanceTests>();

[MemoryDiagnoser]
public class PerformanceTests
{
	const int Count = 21;
	static readonly byte[][] data = new byte[Count][];
	static readonly string[] strings = new string[Count];

	static PerformanceTests()
	{
		Random r = new(1337);
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

	[Benchmark(Baseline = true)]
	public void ToBase64_Convert()
	{
		for (int j = 0; j < Count; j++)
			Convert.ToBase64String(data[j]);
	}

	[Benchmark]
	public void ToBase64_Encoder()
	{
		var encoder = Base64Encoder.Default;
		for (int j = 0; j < Count; j++)
			encoder.ToBase(data[j]);
	}

	[Benchmark]
	public void ToBase64_Encoder_Span()
	{
		var encoder = Base64Encoder.Default;
		for (int j = 0; j < Count; j++)
			encoder.ToBase(data[j].AsSpan());
	}

	[Benchmark]
	public void ToBase64_TryEncode()
	{
		var encoder = Base64Encoder.Default;
		Span<char> buffer = stackalloc char[32];
		for (int j = 0; j < Count; j++)
			encoder.TryEncode(data[j], buffer, out _);
	}

	[Benchmark]
	public void FromBase64_Convert()
	{
		for (int j = 0; j < Count; j++)
			Convert.FromBase64String(strings[j]);
	}

	[Benchmark]
	public void FromBase64_Encoder()
	{
		var encoder = Base64Encoder.Default;
		for (int j = 0; j < Count; j++)
			encoder.FromBase(strings[j]);
	}

	[Benchmark]
	public void FromBase64_Encoder_Span()
	{
		var encoder = Base64Encoder.Default;
		for (int j = 0; j < Count; j++)
			encoder.FromBase(strings[j].AsSpan());
	}

	[Benchmark]
	public void ToBase16_Encoder()
	{
		var encoder = new Base16Encoder();
		for (int j = 0; j < Count; j++)
			encoder.ToBase(data[j]);
	}

	[Benchmark]
	public void ToBase62_Encoder()
	{
		var encoder = new Base62Encoder();
		for (int j = 0; j < Count; j++)
			encoder.ToBase(data[j]);
	}
}
