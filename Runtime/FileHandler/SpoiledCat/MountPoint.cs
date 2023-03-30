using SpoiledCat.SimpleIO;

// switch stuff is in MountPoint_Switch
// all other implementations are in MountPoint_NonSwitch

public static partial class MountPoint
{
    public static byte[] Load(SPath file) => InternalLoad(file);
    public static void Save(SPath file, byte[] bytes, long length) => InternalSave(file, bytes, length);
    public static SPath GetFilepath(string filename) => InternalGetFilePath(filename);
}