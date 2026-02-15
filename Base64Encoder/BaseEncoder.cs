using System;
using System.Buffers;

namespace Exyll;

/// <remarks>Based on http://www.csharp411.com/convert-binary-to-base64-string/</remarks>
public class BaseEncoder
{
	const char PaddingChar = '=';
	const int StackAllocThreshold = 256;

	readonly byte[] _map;

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

		_map = CreateMap(characterSet);
	}

	public virtual string ToBase(byte[]? data)
	{
		if (data is null || data.Length == 0) return string.Empty;
		return ToBase((ReadOnlySpan<byte>)data);
	}

	public string ToBase(ReadOnlySpan<byte> data)
	{
		if (data.IsEmpty) return string.Empty;
		return _isPowerOfTwo ? BitGroupEncode(data) : ArithmeticEncode(data);
	}

	public virtual byte[] FromBase(string? data)
	{
		if (string.IsNullOrEmpty(data)) return [];
		return FromBase(data.AsSpan());
	}

	public byte[] FromBase(ReadOnlySpan<char> data)
	{
		if (data.IsEmpty) return [];
		return _isPowerOfTwo ? BitGroupDecode(data) : ArithmeticDecode(data);
	}

	/// <summary>Encodes bytes into a caller-provided character buffer.</summary>
	public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
	{
		charsWritten = 0;
		if (source.IsEmpty) return true;

		int needed = GetMaxEncodedLength(source.Length);
		if (destination.Length < needed) return false;

		if (_isPowerOfTwo)
			BitGroupEncode(source, destination, out charsWritten);
		else
			ArithmeticEncode(source, destination, out charsWritten);

		return true;
	}

	/// <summary>Decodes characters into a caller-provided byte buffer.</summary>
	public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
	{
		bytesWritten = 0;
		if (source.IsEmpty) return true;

		if (_isPowerOfTwo)
		{
			int needed = GetDecodedLength(source);
			if (destination.Length < needed) return false;
			BitGroupDecode(source, destination, out bytesWritten);
		}
		else
		{
			// Arithmetic: decode to temp, then copy
			byte[] decoded = ArithmeticDecode(source);
			if (destination.Length < decoded.Length) return false;
			decoded.CopyTo(destination);
			bytesWritten = decoded.Length;
		}

		return true;
	}

	/// <summary>Returns the maximum number of characters needed to encode the given number of bytes.</summary>
	public int GetMaxEncodedLength(int byteCount)
	{
		if (byteCount == 0) return 0;

		if (_isPowerOfTwo)
		{
			int charCount = (byteCount * 8 + _bitsPerChar - 1) / _bitsPerChar;
			return PaddingEnabled
				? (charCount + _charsPerBlock - 1) / _charsPerBlock * _charsPerBlock
				: charCount;
		}

		// Arithmetic: upper bound
		return (int)(byteCount * 8.0 / Math.Log2(_base)) + byteCount + 1;
	}

	/// <summary>Returns the exact number of bytes that the given encoded data will decode to.</summary>
	public int GetDecodedLength(ReadOnlySpan<char> data)
	{
		if (data.IsEmpty) return 0;

		if (_isPowerOfTwo)
		{
			int dataChars = data.Length;
			while (dataChars > 0 && data[dataChars - 1] == PaddingChar)
				dataChars--;
			return dataChars * _bitsPerChar / 8;
		}

		// Arithmetic: can't compute without decoding
		return ArithmeticDecode(data).Length;
	}

	// === Bit-group path (power-of-2 bases) ===

	string BitGroupEncode(ReadOnlySpan<byte> data)
	{
		int allocLen = GetMaxEncodedLength(data.Length);

		char[]? rented = null;
		Span<char> buffer = allocLen <= StackAllocThreshold
			? stackalloc char[allocLen]
			: (rented = ArrayPool<char>.Shared.Rent(allocLen));

		try
		{
			BitGroupEncode(data, buffer, out int written);
			return new string(buffer[..written]);
		}
		finally
		{
			if (rented is not null) ArrayPool<char>.Shared.Return(rented);
		}
	}

	void BitGroupEncode(ReadOnlySpan<byte> data, Span<char> output, out int written)
	{
		ReadOnlySpan<char> cs = CharacterSet;
		int bitsPerChar = _bitsPerChar;
		int charMask = _charMask;
		int allocLen = GetMaxEncodedLength(data.Length);

		int bitBuffer = 0, bitsInBuffer = 0, di = 0;

		for (int i = 0; i < data.Length; i++)
		{
			bitBuffer = (bitBuffer << 8) | data[i];
			bitsInBuffer += 8;

			while (bitsInBuffer >= bitsPerChar)
			{
				bitsInBuffer -= bitsPerChar;
				output[di++] = cs[(bitBuffer >> bitsInBuffer) & charMask];
			}
		}

		if (bitsInBuffer > 0)
			output[di++] = cs[(bitBuffer << (bitsPerChar - bitsInBuffer)) & charMask];

		if (PaddingEnabled)
		{
			while (di < allocLen)
				output[di++] = PaddingChar;
		}

		written = di;
	}

	byte[] BitGroupDecode(ReadOnlySpan<char> data)
	{
		int dataChars = data.Length;
		while (dataChars > 0 && data[dataChars - 1] == PaddingChar)
			dataChars--;

		byte[] result = new byte[dataChars * _bitsPerChar / 8];
		BitGroupDecode(data[..dataChars], result, out _);
		return result;
	}

	void BitGroupDecode(ReadOnlySpan<char> data, Span<byte> output, out int written)
	{
		ReadOnlySpan<byte> map = _map;
		int bitsPerChar = _bitsPerChar;

		int bitBuffer = 0, bitsInBuffer = 0, di = 0;

		for (int i = 0; i < data.Length; i++)
		{
			bitBuffer = (bitBuffer << bitsPerChar) | map[data[i]];
			bitsInBuffer += bitsPerChar;

			while (bitsInBuffer >= 8)
			{
				bitsInBuffer -= 8;
				output[di++] = (byte)((bitBuffer >> bitsInBuffer) & 0xFF);
			}
		}

		written = di;
	}

	// === Arithmetic path (non-power-of-2 bases) ===

	string ArithmeticEncode(ReadOnlySpan<byte> data)
	{
		int maxLen = GetMaxEncodedLength(data.Length);

		char[]? rented = null;
		Span<char> buffer = maxLen <= StackAllocThreshold
			? stackalloc char[maxLen]
			: (rented = ArrayPool<char>.Shared.Rent(maxLen));

		try
		{
			ArithmeticEncode(data, buffer, out int written);
			return new string(buffer[..written]);
		}
		finally
		{
			if (rented is not null) ArrayPool<char>.Shared.Return(rented);
		}
	}

	void ArithmeticEncode(ReadOnlySpan<byte> data, Span<char> output, out int written)
	{
		int length = data.Length;

		int leadingZeros = 0;
		while (leadingZeros < length && data[leadingZeros] == 0)
			leadingZeros++;

		if (leadingZeros == length)
		{
			output[..length].Fill(CharacterSet[0]);
			written = length;
			return;
		}

		// Work with a copy for in-place division
		byte[]? rentedInput = null;
		Span<byte> input = length <= StackAllocThreshold
			? stackalloc byte[length]
			: (rentedInput = ArrayPool<byte>.Shared.Rent(length));

		try
		{
			data.CopyTo(input);

			// Build result in reverse into output buffer
			int resultEnd = output.Length;

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

				output[--resultEnd] = CharacterSet[remainder];
				startIndex = newStart;
			}

			for (int i = 0; i < leadingZeros; i++)
				output[--resultEnd] = CharacterSet[0];

			int count = output.Length - resultEnd;

			// Shift to start of output buffer
			output[resultEnd..].CopyTo(output);
			written = count;
		}
		finally
		{
			if (rentedInput is not null) ArrayPool<byte>.Shared.Return(rentedInput);
		}
	}

	byte[] ArithmeticDecode(ReadOnlySpan<char> data)
	{
		int length = data.Length;

		char zeroChar = CharacterSet[0];
		int leadingZeros = 0;
		while (leadingZeros < length && data[leadingZeros] == zeroChar)
			leadingZeros++;

		if (leadingZeros == length)
			return new byte[length];

		// Convert characters to digit values
		ReadOnlySpan<byte> map = _map;
		byte[]? rentedDigits = null;
		Span<byte> digits = length <= StackAllocThreshold
			? stackalloc byte[length]
			: (rentedDigits = ArrayPool<byte>.Shared.Rent(length));

		byte[]? rentedResult = null;
		int resultSize = length * 2;
		Span<byte> result = resultSize <= StackAllocThreshold
			? stackalloc byte[resultSize]
			: (rentedResult = ArrayPool<byte>.Shared.Rent(resultSize));

		try
		{
			for (int i = 0; i < length; i++)
				digits[i] = map[data[i]];

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

			return result[resultIndex..].ToArray();
		}
		finally
		{
			if (rentedDigits is not null) ArrayPool<byte>.Shared.Return(rentedDigits);
			if (rentedResult is not null) ArrayPool<byte>.Shared.Return(rentedResult);
		}
	}

	// === Helpers ===

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

	static int Lcm(int a, int b) => a / Gcd(a, b) * b;
}
