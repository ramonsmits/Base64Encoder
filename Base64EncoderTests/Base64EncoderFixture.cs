using System;
using System.Collections;
using NUnit.Framework;

namespace Exyll
{
	[TestFixture]
	public class Base64EncoderFixture
	{
		const int Count = 21;
		static readonly byte[][] data = new byte[Count][];
		static readonly string[] strings = new string[Count];

		static Base64EncoderFixture()
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

		public IEnumerable Strings
		{
			get
			{
				for (int i = 0; i < Count; i++) yield return new TestCaseData(strings[i]);
			}
		}

		public IEnumerable Data
		{
			get
			{
				for (int i = 0; i < Count; i++) yield return new TestCaseData(data[i]);
			}
		}

		// Base64 tests

		[Test, TestCaseSource("Data")]
		public void ToBase64_Dogfood_Default(byte[] d)
		{
			var b64Encoder = Base64Encoder.Default;
			CollectionAssert.AreEqual(d, b64Encoder.FromBase(b64Encoder.ToBase(d)));
		}

		[Test, TestCaseSource("Data")]
		public void ToBase64_Dogfood_DefaultNoPadding(byte[] d)
		{
			var b64Encoder = Base64Encoder.DefaultNoPadding;
			CollectionAssert.AreEqual(d, b64Encoder.FromBase(b64Encoder.ToBase(d)));
		}

		[Test, TestCaseSource("Data")]
		public void ToBase64_Dogfood_UrlEncoding(byte[] d)
		{
			var b64Encoder = Base64Encoder.UrlEncoding;
			CollectionAssert.AreEqual(d, b64Encoder.FromBase(b64Encoder.ToBase(d)));
		}

		[Test, TestCaseSource("Data")]
		public void ToBase64(byte[] d)
		{
			Assert.AreEqual(Convert.ToBase64String(d), Base64Encoder.Default.ToBase(d));
		}

		[Test, TestCaseSource("Data")]
		public void ToBase64_DefaultNoPadding(byte[] d)
		{
			Assert.AreEqual(Convert.ToBase64String(d), Base64Encoder.DefaultNoPadding.ToBase(d));
		}

		[Test, TestCaseSource("Strings")]
		public void FromBase64(string s)
		{
			CollectionAssert.AreEqual(Convert.FromBase64String(s), Base64Encoder.Default.FromBase(s));
		}

		// Base16 tests

		[Test, TestCaseSource("Data")]
		public void ToBase16_Dogfood(byte[] d)
		{
			var encoder = new Base16Encoder();
			CollectionAssert.AreEqual(d, encoder.FromBase(encoder.ToBase(d)));
		}

		[Test, TestCaseSource("Data")]
		public void ToBase16_MatchesBitConverter(byte[] d)
		{
			var encoder = new Base16Encoder();
			var expected = BitConverter.ToString(d).Replace("-", "");
			Assert.AreEqual(expected, encoder.ToBase(d));
		}

		[Test]
		public void FromBase16_Issue2()
		{
			var hexString = "49276D206B696C6C696E6720796F757220627261696E206C696B65206120706F69736F6E6F7573206D757368726F6F6D";
			var encoder = new Base16Encoder();
			var data = encoder.FromBase(hexString);
			var text = System.Text.Encoding.ASCII.GetString(data);
			Assert.AreEqual("I'm killing your brain like a poisonous mushroom", text);
		}

		[Test]
		public void FromBase16_SingleByte()
		{
			var encoder = new Base16Encoder();
			var data = encoder.FromBase("49");
			Assert.AreEqual(1, data.Length);
			Assert.AreEqual(0x49, data[0]);
		}

		// Base8 tests (Base7Encoder has 8 chars: "01234567")

		[Test, TestCaseSource("Data")]
		public void ToBase8_Dogfood(byte[] d)
		{
			var encoder = new Base7Encoder();
			CollectionAssert.AreEqual(d, encoder.FromBase(encoder.ToBase(d)));
		}

		[Test]
		public void ToBase8_KnownValue()
		{
			var encoder = new Base7Encoder();
			// 0xFF = 11111111 in binary, in 3-bit groups: 011 111 111 = 3 7 7
			Assert.AreEqual("377", encoder.ToBase(new byte[] { 0xFF }));
		}

		// Base62 tests

		[Test, TestCaseSource("Data")]
		public void ToBase62_Dogfood(byte[] d)
		{
			var encoder = new Base62Encoder();
			CollectionAssert.AreEqual(d, encoder.FromBase(encoder.ToBase(d)));
		}

		// Base10 tests

		[Test, TestCaseSource("Data")]
		public void ToBase10_Dogfood(byte[] d)
		{
			var encoder = new Base10Encoder();
			CollectionAssert.AreEqual(d, encoder.FromBase(encoder.ToBase(d)));
		}

		[Test]
		public void ToBase10_KnownValue()
		{
			var encoder = new Base10Encoder();
			Assert.AreEqual("255", encoder.ToBase(new byte[] { 0xFF }));
		}

		// Base52 tests

		[Test, TestCaseSource("Data")]
		public void ToBase52_Dogfood(byte[] d)
		{
			var encoder = new Base52Encoder();
			CollectionAssert.AreEqual(d, encoder.FromBase(encoder.ToBase(d)));
		}
	}
}
