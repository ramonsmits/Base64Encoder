using System;
using System.Collections;
using System.Text;
using NUnit.Framework;

namespace Exyll;

[TestFixture]
public class Base64EncoderFixture
{
	const int Count = 21;
	static readonly byte[][] data = new byte[Count][];
	static readonly string[] strings = new string[Count];

	static Base64EncoderFixture()
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

	public static IEnumerable Strings
	{
		get
		{
			for (int i = 0; i < Count; i++) yield return new TestCaseData(strings[i]);
		}
	}

	public static IEnumerable Data
	{
		get
		{
			for (int i = 0; i < Count; i++) yield return new TestCaseData(data[i]);
		}
	}

	// Base64 tests

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase64_Dogfood_Default(byte[] d)
	{
		var encoder = Base64Encoder.Default;
		Assert.That(encoder.FromBase(encoder.ToBase(d)), Is.EqualTo(d));
	}

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase64_Dogfood_DefaultNoPadding(byte[] d)
	{
		var encoder = Base64Encoder.DefaultNoPadding;
		Assert.That(encoder.FromBase(encoder.ToBase(d)), Is.EqualTo(d));
	}

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase64_Dogfood_UrlEncoding(byte[] d)
	{
		var encoder = Base64Encoder.UrlEncoding;
		Assert.That(encoder.FromBase(encoder.ToBase(d)), Is.EqualTo(d));
	}

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase64(byte[] d)
	{
		Assert.That(Base64Encoder.Default.ToBase(d), Is.EqualTo(Convert.ToBase64String(d)));
	}

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase64_DefaultNoPadding(byte[] d)
	{
		Assert.That(Base64Encoder.DefaultNoPadding.ToBase(d), Is.EqualTo(Convert.ToBase64String(d).TrimEnd('=')));
	}

	[Test, TestCaseSource(nameof(Strings))]
	public void FromBase64(string s)
	{
		Assert.That(Base64Encoder.Default.FromBase(s), Is.EqualTo(Convert.FromBase64String(s)));
	}

	// Base16 tests

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase16_Dogfood(byte[] d)
	{
		var encoder = new Base16Encoder();
		Assert.That(encoder.FromBase(encoder.ToBase(d)), Is.EqualTo(d));
	}

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase16_MatchesBitConverter(byte[] d)
	{
		var encoder = new Base16Encoder();
		var expected = BitConverter.ToString(d).Replace("-", "");
		Assert.That(encoder.ToBase(d), Is.EqualTo(expected));
	}

	[Test]
	public void FromBase16_Issue2()
	{
		var hexString = "49276D206B696C6C696E6720796F757220627261696E206C696B65206120706F69736F6E6F7573206D757368726F6F6D";
		var encoder = new Base16Encoder();
		var decoded = encoder.FromBase(hexString);
		Assert.That(Encoding.ASCII.GetString(decoded), Is.EqualTo("I'm killing your brain like a poisonous mushroom"));
	}

	[Test]
	public void FromBase16_SingleByte()
	{
		var encoder = new Base16Encoder();
		var decoded = encoder.FromBase("49");
		Assert.That(decoded, Is.EqualTo(new byte[] { 0x49 }));
	}

	// Base8 tests

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase8_Dogfood(byte[] d)
	{
		var encoder = new Base7Encoder();
		Assert.That(encoder.FromBase(encoder.ToBase(d)), Is.EqualTo(d));
	}

	[Test]
	public void ToBase8_KnownValue()
	{
		var encoder = new Base7Encoder();
		// 0xFF = 11111111, 3-bit groups MSB-first: 111 111 110 = 7 7 6
		Assert.That(encoder.ToBase(new byte[] { 0xFF }), Is.EqualTo("776"));
	}

	// Base62 tests

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase62_Dogfood(byte[] d)
	{
		var encoder = new Base62Encoder();
		Assert.That(encoder.FromBase(encoder.ToBase(d)), Is.EqualTo(d));
	}

	// Base10 tests

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase10_Dogfood(byte[] d)
	{
		var encoder = new Base10Encoder();
		Assert.That(encoder.FromBase(encoder.ToBase(d)), Is.EqualTo(d));
	}

	[Test]
	public void ToBase10_KnownValue()
	{
		var encoder = new Base10Encoder();
		Assert.That(encoder.ToBase(new byte[] { 0xFF }), Is.EqualTo("255"));
	}

	// Base52 tests

	[Test, TestCaseSource(nameof(Data))]
	public void ToBase52_Dogfood(byte[] d)
	{
		var encoder = new Base52Encoder();
		Assert.That(encoder.FromBase(encoder.ToBase(d)), Is.EqualTo(d));
	}

	// Span API tests

	[Test]
	public void ToBase_Span_MatchesByteArray()
	{
		var encoder = Base64Encoder.Default;
		var input = new byte[] { 1, 2, 3, 4, 5 };
		Assert.That(encoder.ToBase(input.AsSpan()), Is.EqualTo(encoder.ToBase(input)));
	}

	[Test]
	public void FromBase_Span_MatchesString()
	{
		var encoder = Base64Encoder.Default;
		var encoded = encoder.ToBase(new byte[] { 1, 2, 3, 4, 5 });
		Assert.That(encoder.FromBase(encoded.AsSpan()), Is.EqualTo(encoder.FromBase(encoded)));
	}

	[Test]
	public void TryEncode_Base64()
	{
		var encoder = Base64Encoder.Default;
		var input = new byte[] { 1, 2, 3 };
		Span<char> buffer = stackalloc char[encoder.GetMaxEncodedLength(input.Length)];

		Assert.That(encoder.TryEncode(input, buffer, out int written), Is.True);
		Assert.That(new string(buffer[..written]), Is.EqualTo(Convert.ToBase64String(input)));
	}

	[Test]
	public void TryEncode_BufferTooSmall()
	{
		var encoder = Base64Encoder.Default;
		Span<char> buffer = stackalloc char[1];

		Assert.That(encoder.TryEncode(new byte[] { 1, 2, 3 }, buffer, out _), Is.False);
	}

	[Test]
	public void TryDecode_Base64()
	{
		var encoder = Base64Encoder.Default;
		var encoded = "AQID";
		Span<byte> buffer = stackalloc byte[encoder.GetDecodedLength(encoded)];

		Assert.That(encoder.TryDecode(encoded, buffer, out int written), Is.True);
		Assert.That(buffer[..written].ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
	}

	[Test]
	public void TryDecode_BufferTooSmall()
	{
		var encoder = Base64Encoder.Default;
		Span<byte> buffer = stackalloc byte[1];

		Assert.That(encoder.TryDecode("AQIDBA==", buffer, out _), Is.False);
	}

	[Test]
	public void TryEncode_Base16()
	{
		var encoder = new Base16Encoder();
		var input = new byte[] { 0x49 };
		Span<char> buffer = stackalloc char[encoder.GetMaxEncodedLength(input.Length)];

		Assert.That(encoder.TryEncode(input, buffer, out int written), Is.True);
		Assert.That(new string(buffer[..written]), Is.EqualTo("49"));
	}

	[Test]
	public void GetMaxEncodedLength()
	{
		var b64 = Base64Encoder.Default;
		Assert.That(b64.GetMaxEncodedLength(0), Is.EqualTo(0));
		Assert.That(b64.GetMaxEncodedLength(1), Is.EqualTo(4));
		Assert.That(b64.GetMaxEncodedLength(3), Is.EqualTo(4));

		var b16 = new Base16Encoder();
		Assert.That(b16.GetMaxEncodedLength(1), Is.EqualTo(2));
		Assert.That(b16.GetMaxEncodedLength(3), Is.EqualTo(6));
	}
}
