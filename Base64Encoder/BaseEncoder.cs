using System;

namespace Exyll
{
	/// <remarks>Based on http://www.csharp411.com/convert-binary-to-base64-string/</remarks>
	public class BaseEncoder
	{
		const char PaddingChar = '=';
		readonly byte[] Map;

		public readonly char[] CharacterSet;
		public readonly bool PaddingEnabled;

		readonly int _base;
		readonly int _bitsPerChar;
		readonly int _charMask;
		readonly int _charsPerBlock;
		readonly bool _isPowerOfTwo;

		public BaseEncoder(char[] characterSet, bool paddingEnabled)
		{
			PaddingEnabled = paddingEnabled;
			CharacterSet = characterSet;
			_base = characterSet.Length;
			_isPowerOfTwo = (_base & (_base - 1)) == 0 && _base > 1;

			if (_isPowerOfTwo)
			{
				int temp = _base;
				while (temp > 1) { temp >>= 1; _bitsPerChar++; }
				_charMask = (1 << _bitsPerChar) - 1;
				_charsPerBlock = Lcm(8, _bitsPerChar) / _bitsPerChar;
			}

			Map = CreateMap(CharacterSet);
		}

		public virtual string ToBase(byte[] data)
		{
			int length;
			if (data == null || 0 == (length = data.Length))
				return string.Empty;

			return _isPowerOfTwo
				? BitGroupEncode(data, length)
				: ArithmeticEncode(data, length);
		}

		public virtual byte[] FromBase(string data)
		{
			int length = data == null ? 0 : data.Length;

			if (length == 0)
				return new byte[0];

			return _isPowerOfTwo
				? BitGroupDecode(data, length)
				: ArithmeticDecode(data, length);
		}

		unsafe string BitGroupEncode(byte[] data, int length)
		{
			int bitsPerChar = _bitsPerChar;
			int charMask = _charMask;

			int charCount = (length * 8 + bitsPerChar - 1) / bitsPerChar;
			int paddedCount = (charCount + _charsPerBlock - 1) / _charsPerBlock * _charsPerBlock;
			int allocLen = PaddingEnabled ? paddedCount : charCount;

			char[] output = new char[allocLen];

			fixed (byte* _d = data)
			fixed (char* _cs = CharacterSet)
			fixed (char* _out = output)
			{
				byte* d = _d;
				char* sp = _out;

				int bitBuffer = 0;
				int bitsInBuffer = 0;

				for (int i = 0; i < length; i++)
				{
					bitBuffer = (bitBuffer << 8) | *d++;
					bitsInBuffer += 8;

					while (bitsInBuffer >= bitsPerChar)
					{
						bitsInBuffer -= bitsPerChar;
						*sp++ = _cs[(bitBuffer >> bitsInBuffer) & charMask];
					}
				}

				if (bitsInBuffer > 0)
					*sp++ = _cs[(bitBuffer << (bitsPerChar - bitsInBuffer)) & charMask];

				if (PaddingEnabled)
				{
					char* end = _out + paddedCount;
					while (sp < end)
						*sp++ = PaddingChar;
				}
			}

			return new string(output, 0, allocLen);
		}

		unsafe byte[] BitGroupDecode(string data, int length)
		{
			int bitsPerChar = _bitsPerChar;

			int dataChars = length;
			while (dataChars > 0 && data[dataChars - 1] == PaddingChar)
				dataChars--;

			byte[] result = new byte[dataChars * bitsPerChar / 8];

			fixed (char* _p = data)
			fixed (byte* _d = result)
			{
				char* p = _p;
				byte* dp = _d;

				int bitBuffer = 0;
				int bitsInBuffer = 0;

				for (int i = 0; i < dataChars; i++)
				{
					bitBuffer = (bitBuffer << bitsPerChar) | Map[*p++];
					bitsInBuffer += bitsPerChar;

					while (bitsInBuffer >= 8)
					{
						bitsInBuffer -= 8;
						*dp++ = (byte)((bitBuffer >> bitsInBuffer) & 0xFF);
					}
				}
			}

			return result;
		}

		string ArithmeticEncode(byte[] data, int length)
		{
			int leadingZeros = 0;
			while (leadingZeros < length && data[leadingZeros] == 0)
				leadingZeros++;

			if (leadingZeros == length)
				return new string(CharacterSet[0], length);

			byte[] input = new byte[length];
			Array.Copy(data, input, length);

			char[] result = new char[length * 8];
			int resultIndex = result.Length;

			int startIndex = leadingZeros;
			while (startIndex < length)
			{
				int remainder = 0;
				int newStart = length;

				for (int i = startIndex; i < length; i++)
				{
					int acc = remainder * 256 + input[i];
					input[i] = (byte)(acc / _base);
					remainder = acc % _base;
					if (input[i] != 0 && newStart == length)
						newStart = i;
				}

				result[--resultIndex] = CharacterSet[remainder];
				startIndex = newStart;
			}

			for (int i = 0; i < leadingZeros; i++)
				result[--resultIndex] = CharacterSet[0];

			return new string(result, resultIndex, result.Length - resultIndex);
		}

		byte[] ArithmeticDecode(string data, int length)
		{
			char zeroChar = CharacterSet[0];
			int leadingZeros = 0;
			while (leadingZeros < length && data[leadingZeros] == zeroChar)
				leadingZeros++;

			if (leadingZeros == length)
				return new byte[length];

			byte[] digits = new byte[length];
			for (int i = 0; i < length; i++)
				digits[i] = Map[data[i]];

			byte[] result = new byte[length * 2];
			int resultIndex = result.Length;

			int startIndex = leadingZeros;
			while (startIndex < length)
			{
				int remainder = 0;
				int newStart = length;

				for (int i = startIndex; i < length; i++)
				{
					int acc = remainder * _base + digits[i];
					digits[i] = (byte)(acc / 256);
					remainder = acc % 256;
					if (digits[i] != 0 && newStart == length)
						newStart = i;
				}

				result[--resultIndex] = (byte)remainder;
				startIndex = newStart;
			}

			for (int i = 0; i < leadingZeros; i++)
				result[--resultIndex] = 0;

			int outputLen = result.Length - resultIndex;
			byte[] output = new byte[outputLen];
			Array.Copy(result, resultIndex, output, 0, outputLen);
			return output;
		}

		static byte[] CreateMap(char[] characterSet)
		{
			int maxChar = 0;
			for (int i = 0; i < characterSet.Length; i++)
			{
				if (characterSet[i] > maxChar)
					maxChar = characterSet[i];
			}

			byte[] map = new byte[maxChar + 1];

			for (byte i = 0; i < characterSet.Length; i++)
				map[characterSet[i]] = i;

			return map;
		}

		static int Gcd(int a, int b)
		{
			while (b != 0) { int t = b; b = a % b; a = t; }
			return a;
		}

		static int Lcm(int a, int b)
		{
			return a / Gcd(a, b) * b;
		}
	}
}
