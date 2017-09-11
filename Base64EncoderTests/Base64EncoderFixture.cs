using System;
using System.Collections;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;
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
	}
}
