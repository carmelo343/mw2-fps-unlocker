using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace mw2_fps_unlocker.Core;

internal sealed record GameImage(string Sha256, int PointerSize, ushort Machine, int ImageSize, uint Timestamp)
{
    internal static GameImage Load(string path)
    {
        using var file = File.OpenRead(path);
        string hash = Convert.ToHexString(SHA256.HashData(file));
        file.Position = 0;
        using var pe = new PEReader(file);
        var headers = pe.PEHeaders;
        var optional = headers.PEHeader ?? throw new InvalidDataException("Missing PE optional header.");
        int width = (headers.CoffHeader.Machine, optional.Magic) switch
        {
            (System.Reflection.PortableExecutable.Machine.I386, PEMagic.PE32) => 4,
            (System.Reflection.PortableExecutable.Machine.Amd64, PEMagic.PE32Plus) => 8,
            _ => throw new InvalidDataException("Only x86 PE32 and x64 PE32+ game images are supported.")
        };
        if (optional.SizeOfImage <= 0) throw new InvalidDataException("Invalid image size.");
        return new(hash, width, (ushort)headers.CoffHeader.Machine, optional.SizeOfImage,
            unchecked((uint)headers.CoffHeader.TimeDateStamp));
    }
}
