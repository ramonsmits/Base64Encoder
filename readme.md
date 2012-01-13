# About

Once in a while I needed to use a different style of Base64 encoding then provided by the .net framework
via Convert.ToBase64String and Convert.FromBase64String. Currently there are two alternatives where the
first are the other conversion methods Convert.ToBase64CharArray and Convert.FromBase64CharArray but these
are still very inefficient when you need to combine then with character replacement pre/post-processing.
The second is the use of FromBase64Transform and ToBase64Transform but both operate on streams and both
don't support other Base64 encodings.

I started browsing for code and found the following two articles by Timm Martin and 'wchvic '

http://www.csharp411.com/convert-binary-to-base64-string/
http://www.codeproject.com/KB/cs/base64encdec.aspx

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


# Performance

So lets talk about performance! In the 'release' build it basically.


# Knows issues

 * The string.ToCharArray seems to cause a performance problem but because the code is currently as fast
   as Convert (for ToBase64) or significantly faster (FromBase64, two times faster) so it currently does
   not have my priority.
 * The FromBase64 method does not do *any* validation! So if you need to convert external encoded Base64
   strings then make sure you have validated the Base64 string!
   The readonly field Base64Encoder.CharacterSet can help validate your Base64 input.


# Future


 * I probably going to extend it to allow passing Stream as input just to make it more complete.