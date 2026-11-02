namespace mw2_fps_unlocker.Core;

internal static class MemoryPolicy
{
    internal static bool ValidAddress(ulong address, ulong length, int pointerSize)
    {
        ulong maximum = pointerSize switch { 4 => uint.MaxValue, 8 => 0x00007FFFFFFFFFFF, _ => 0 };
        return address >= 0x10000 && length > 0 && address <= maximum && length - 1 <= maximum - address;
    }

    internal static ulong Add(ulong address, ulong offset, int pointerSize)
    {
        ulong result = checked(address + offset);
        if (!ValidAddress(result, 1, pointerSize)) throw new InvalidDataException("Invalid target address.");
        return result;
    }

    internal static bool RegionAllows(ulong address, ulong length, ulong regionBase, ulong regionSize,
        uint state, uint protection, bool write)
    {
        if (length == 0 || state != 0x1000 || (protection & (0x100 | 0x01)) != 0 ||
            address < regionBase || regionSize == 0 || address - regionBase >= regionSize ||
            length > regionSize - (address - regionBase)) return false;
        // Never make a page writable. Never write executable pages.
        uint basic = protection & 0xFF;
        return write ? basic is 0x04 or 0x08 : basic is 0x02 or 0x04 or 0x08 or 0x20 or 0x40 or 0x80;
    }

    internal static ulong DecodePointer(byte[] bytes, int pointerSize) => pointerSize switch
    {
        4 when bytes.Length == 4 => BitConverter.ToUInt32(bytes),
        8 when bytes.Length == 8 => BitConverter.ToUInt64(bytes),
        _ => throw new InvalidDataException("Pointer width/length mismatch.")
    };
}
