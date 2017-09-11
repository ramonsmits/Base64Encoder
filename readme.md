# About

Once in a while I needed to use a different style of Base64 encoding then provided by the .net framework
via `Convert.ToBase64String` and `Convert.FromBase64String`. Currently there are two alternatives where the
first are the other conversion methods `Convert.ToBase64CharArray` and `Convert.FromBase64CharArray` but these
are still very inefficient when you need to combine them with character replacement pre/post-processing.
The second is the use of `FromBase64Transform` and `ToBase64Transform` but both operate on streams and both
don't support other Base64 encodings.

I started browsing for code and found the following two articles by Timm Martin and 'wchvic'

 * http://www.csharp411.com/convert-binary-to-base64-string/
 * http://www.codeproject.com/KB/cs/base64encdec.aspx

Both work and the version by Timm is faster but browsing the code made me think that it could be optimized
even further.

I basically did the following optimalizations:

 * Make use of readonly members resulting inlining references by the .net runtime
 * Unsafe code to make the code more readable and efficient
 * Removed the IF statement from the FOR loop as it was only used to do work in the last 'block'
 * Tests refactored to an nunit fixture

Besides the performance enhancements I also added support for:

 * Base64 encoding with or without padding characters.
 * Base64 decoding accepting strings without padding characters. 
 * Support for a couple of 'standard' Base64 encoding methods like xml and url encoding more information
   can be found at : http://en.wikipedia.org/wiki/Base64

# Base7, Base10, Base16, Base52, Base62

The name of the library is called Base64Encoder but it also provides other base converters. All converters
inherit from a base class BaseCalculater which can convert any byte array to/from a specific base encoding
character set. I needed a Base52 encoder so that is the reason that I refactored the code to support this
and I added Base7, Base10 and Base16 just for demo purpose. The Base62 is included because it is pretty
common as it solves the web incompatibilities of Base64 encoding.

# Benchmark Performance

So lets talk about performance! As why would you use it when you can use the version of the .net framework?

When this code was put on github in 2012 the customer encoder was about twice as fast for encoding a base64 string comparable for decoding. The solution now contains a console project that uses [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet)

```
BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i7-6700K CPU 4.00GHz (Skylake), ProcessorCount=8
Frequency=3914062 Hz, Resolution=255.4891 ns, Timer=TSC
  [Host]       : .NET Framework 4.7 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.2102.0
  LegacyJitX64 : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit LegacyJIT/clrjit-v4.7.2102.0;compatjit-v4.7.2102.0
  LegacyJitX86 : .NET Framework 4.7 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.2102.0
  RyuJitX64    : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2102.0

Runtime=Clr

                Method |          Job |       Jit | Platform |     Mean |    Error |   StdDev |
---------------------- |------------- |---------- |--------- |---------:|---------:|---------:|
   ToBase64WithConvert | LegacyJitX64 | LegacyJit |      X64 | 535.4 ns | 1.959 ns | 1.736 ns |
   ToBase64WithEncoder | LegacyJitX64 | LegacyJit |      X64 | 649.4 ns | 2.668 ns | 2.496 ns |
 FromBase64WithConvert | LegacyJitX64 | LegacyJit |      X64 | 779.3 ns | 4.565 ns | 3.812 ns |
 FromBase64WithEncoder | LegacyJitX64 | LegacyJit |      X64 | 593.1 ns | 2.854 ns | 2.530 ns |
   ToBase64WithConvert | LegacyJitX86 | LegacyJit |      X86 | 764.6 ns | 3.107 ns | 2.594 ns |
   ToBase64WithEncoder | LegacyJitX86 | LegacyJit |      X86 | 740.2 ns | 2.603 ns | 2.435 ns |
 FromBase64WithConvert | LegacyJitX86 | LegacyJit |      X86 | 827.5 ns | 2.692 ns | 2.248 ns |
 FromBase64WithEncoder | LegacyJitX86 | LegacyJit |      X86 | 634.1 ns | 3.157 ns | 2.953 ns |
   ToBase64WithConvert |    RyuJitX64 |    RyuJit |      X64 | 562.9 ns | 3.229 ns | 3.021 ns |
   ToBase64WithEncoder |    RyuJitX64 |    RyuJit |      X64 | 747.1 ns | 3.082 ns | 2.574 ns |
 FromBase64WithConvert |    RyuJitX64 |    RyuJit |      X64 | 780.1 ns | 3.330 ns | 3.115 ns |
 FromBase64WithEncoder |    RyuJitX64 |    RyuJit |      X64 | 582.6 ns | 2.714 ns | 2.539 ns |
```


# Knows issues

 * The `string.ToCharArray` seems to cause a performance problem but because the code is currently as fast
   as Convert (for ToBase64) or significantly faster (FromBase64, two times faster) so it currently does
   not have my priority.
 * The `FromBase64` method does not do *any* validation! So if you need to convert external encoded Base64
   strings then make sure you have validated the Base64 string!
   The readonly field `Base64Encoder.CharacterSet` can help validate your Base64 input.


# Future

 * I probably going to extend it to allow passing Stream as input just to make it more complete.
