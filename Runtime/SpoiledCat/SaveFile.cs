#if UNITY_SWITCH && !UNITY_EDITOR
#define TT_UNITY_SWITCH
#endif

using System;
using System.IO;
using SpoiledCat.SimpleIO;

public class SaveFile : IDisposable
{
    private MemoryStream memStream;

    public SPath Filename { get; private set; }

    public Stream Stream
    {
        get
        {
            memStream?.Dispose();
            memStream = new MemoryStream();
            return memStream;
        }
    }

    public SaveFile(string filename)
    {
        Filename = MountPoint.GetFilepath(filename);
    }

    public Stream Load()
    {
        var saveBytes = MountPoint.Load(Filename);
        var stream = Stream;
        stream.Write(saveBytes, 0, saveBytes.Length);
        stream.Position = 0;
        return stream;
    }

    public void Save()
    {
        if (memStream == null)
        {
            // nothing to save
            return;
        }

        MountPoint.Save(Filename, memStream.GetBuffer(), memStream.Length);
    }

    public void Delete()
    {
        Filename.DeleteIfExists();
    }

    public void Dispose()
    {
        memStream?.Dispose();
    }
}
