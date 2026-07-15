using System.Reflection;
using PdfSharp.Fonts;

namespace SoundLens.Api.Features.Reports.Common;

public sealed class SoundLensPdfFontResolver : IFontResolver
{
    public const string FamilyName = "Noto Sans";
    private const string RegularFace = "NotoSans-Regular";
    private const string BoldFace = "NotoSans-Bold";
    private static readonly Lazy<byte[]> RegularFont = new(() => LoadFont("NotoSans-Regular.ttf"));
    private static readonly Lazy<byte[]> BoldFont = new(() => LoadFont("NotoSans-Bold.ttf"));

    public static SoundLensPdfFontResolver Instance { get; } = new();

    public string DefaultFontName => FamilyName;

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(isBold ? BoldFace : RegularFace);

    public byte[]? GetFont(string faceName) => faceName switch
    {
        RegularFace => RegularFont.Value,
        BoldFace => BoldFont.Value,
        _ => null
    };

    private static byte[] LoadFont(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"SoundLens.Api.Assets.Fonts.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName) ??
            throw new InvalidOperationException($"Embedded PDF font '{resourceName}' was not found.");
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
