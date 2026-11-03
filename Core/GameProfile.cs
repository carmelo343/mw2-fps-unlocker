using System.Text.Json;

namespace mw2_fps_unlocker.Core;

// No profiles ship with the diagnostic release. A hash match identifies a file;
// it does not prove that offsets have been researched correctly.
internal sealed record DvarSpec(ulong Rva, int NamePointerOffset, int TypeOffset, byte Type, int ValueOffset);
internal sealed record GameProfile(string Sha256, int PointerSize, DvarSpec Fps, DvarSpec Fov)
{
    internal const string EmbeddedProfile = "@embedded";
    private const string ResourceName = "mw2_fps_unlocker.mw2-profile.json";

    internal static GameProfile Load(string path)
    {
        string json;
        if (path == EmbeddedProfile)
        {
            using var stream = typeof(GameProfile).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidDataException("This build has no embedded game profile.");
            using var reader = new StreamReader(stream);
            json = reader.ReadToEnd();
        }
        else json = File.ReadAllText(path);
        var profile = JsonSerializer.Deserialize<GameProfile>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Empty profile.");
        profile.Validate();
        return profile;
    }

    internal void Validate()
    {
        if (Sha256 == null || Sha256.Length != 64 || !Sha256.All(Uri.IsHexDigit) ||
            PointerSize is not (4 or 8) || Fps == null || Fov == null)
            throw new InvalidDataException("Invalid executable identity or missing dvar profile.");
        foreach (var spec in new[] { Fps, Fov })
        {
            if (spec.Rva == 0 || spec.NamePointerOffset < 0 || spec.NamePointerOffset > 128 ||
                spec.TypeOffset < spec.NamePointerOffset + PointerSize || spec.TypeOffset > 128 ||
                spec.ValueOffset <= spec.TypeOffset || spec.ValueOffset > 256 || spec.ValueOffset % 4 != 0)
                throw new InvalidDataException("Invalid or overlapping dvar layout.");
        }
        if (Fps.Rva == Fov.Rva) throw new InvalidDataException("FPS and FOV globals overlap.");
    }

    internal void Match(GameImage image)
    {
        Validate();
        if (!string.Equals(Sha256, image.Sha256, StringComparison.OrdinalIgnoreCase) || PointerSize != image.PointerSize)
            throw new InvalidDataException("Profile does not match the exact executable hash/architecture.");
        foreach (var spec in new[] { Fps, Fov })
            if (spec.Rva > (ulong)image.ImageSize || (ulong)PointerSize > (ulong)image.ImageSize - spec.Rva)
                throw new InvalidDataException("Profile global lies outside the main module.");
    }
}
