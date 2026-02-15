namespace Exyll;

/// <remarks>Based on http://www.csharp411.com/convert-binary-to-base64-string/</remarks>
public class Base64Encoder : BaseEncoder
{
	const string CharacterSetBase = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

	public readonly char PlusChar;
	public readonly char SlashChar;

	public static readonly Base64Encoder Default = new('+', '/', true);
	public static readonly Base64Encoder DefaultNoPadding = new('+', '/', false);
	public static readonly Base64Encoder UrlEncoding = new('-', '_', false);
	public static readonly Base64Encoder XmlEncoding = new('_', ':', false);
	public static readonly Base64Encoder RegExEncoding = new('!', '-', false);
	public static readonly Base64Encoder FileEncoding = new('+', '-', false);

	public Base64Encoder(char plusChar, char slashChar, bool paddingEnabled)
		: base((CharacterSetBase + plusChar + slashChar).ToCharArray(), paddingEnabled)
	{
		PlusChar = plusChar;
		SlashChar = slashChar;
	}

	public static byte[] FromBase64String(string data) => Default.FromBase(data);

	public static string ToBase64String(byte[] data) => Default.ToBase(data);
}
