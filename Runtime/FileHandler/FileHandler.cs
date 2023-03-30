using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEditor;
using UnityEngine;


/*
 REALLY BASIC SERIALIZED SAVE GAME SYSTEM

 CREATION:
 public class GameProgressSave : SaveGameHandler.SaveData
 {
     public List<DealData> deals;
 }

 LOADING:
 gameProgress = SaveGameHandler.Load(savename, gameProgress);

 SAVING:
 SaveGameHandler.Save(gameProgress);
*/


namespace PackageFileHandler.Runtime
{
    public static class FileHandler
    {
        public const int CurrentSaveVersion = 3;

        [Serializable]
        public class SaveData : ISerializationCallbackReceiver
        {
            [SerializeField] private int fileVersion = CurrentSaveVersion;
            public int FileVersion => fileVersion;

            public void Serialize(Stream stream, bool compress = true)
            {
                var json = JsonUtility.ToJson(this);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                if (compress)
                {
                    stream.WriteByte(0xde);
                    Compress(stream, bytes);
                }
                else
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            public static T Deserialize<T>(Stream stream)
                where T : SaveData, new()
            {
                if (stream.Length < 2)
                    return null;

                string json;

                var header1 = stream.ReadByte();
                if (header1 == 0xde)
                {
                    byte[] bytes = Decompress(stream);
                    json = Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    stream.Position = 0;
                    json = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
                }

                return JsonUtility.FromJson<T>(json);
            }

            public static T DeserializeOldFormat<T>(Stream stream)
                where T : SaveData
            {
                // try the old format
                BinaryFormatter bf = new BinaryFormatter();
                return (T)bf.Deserialize(stream);
            }

            static Stream Compress(Stream stream, byte[] data)
            {
                using var zipStream = new GZipStream(stream, CompressionMode.Compress, true);
                zipStream.Write(data, 0, data.Length);
                return stream;
            }

            static byte[] Decompress(Stream stream)
            {
                using var zipStream = new GZipStream(stream, CompressionMode.Decompress);
                using var resultStream = new MemoryStream();
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }

            public virtual void OnBeforeSerialize()
            {}

            public virtual void OnAfterDeserialize()
            {
                ResetFileVersion();
            }

            protected void ResetFileVersion()
            {
                fileVersion = CurrentSaveVersion;
            }
            
        }

        private static readonly Dictionary<string, SaveFile> _saveFiles = new();

        public static void Save(string savename, SaveData data)
        {
            var fileName = GetFileName(savename);
            Debug.Log("#SaveSystem# saving to file " + fileName);
            if (!_saveFiles.TryGetValue(fileName, out var saveFile))
            {
                saveFile = new SaveFile(fileName);
                _saveFiles.Add(fileName, saveFile);
            }

            var compress = true;
#if !RYTMOS_FORCE_COMPRESS_SAVE && (UNITY_EDITOR || DEBUG)
            compress = false;
#endif
            data.Serialize(saveFile.Stream, compress);
            saveFile.Save();
        }

        public static T Load<T>(string savename, T saveData = null)
            where T : SaveData, new()
        {
            Debug.Log("#SaveSystem# loading " + savename);
            var fileName = GetFileName(savename);
            if (!_saveFiles.TryGetValue(fileName, out var saveFile))
            {
                saveFile = new SaveFile(fileName);
                _saveFiles.Add(fileName, saveFile);
            }

            if (saveFile.Filename.Exists())
            {
                Debug.Log("#SaveSystem# " + savename + " exist");
                var stream = saveFile.Load();
                if (stream.Length == 0)
                {
                    Debug.Log("#SaveSystem# nothing in file");
                    return null;
                }

                T thisSave = null;
                bool loaded = false;
                Exception error;

                try
                {
                    thisSave = SaveData.Deserialize<T>(stream);
                    loaded = true;
                    Debug.Log("#SaveSystem# loaded " + savename);
                }
                catch (Exception ex)
                {
                    error = ex;
                    Debug.Log("#SaveSystem# " + error);

                }
                

                if (!loaded)
                {
                    Debug.Log("#SaveSystem# couldn't load save file, empty or corrupt");
                    //Debug.LogException(error);
                    saveFile.Delete();
                    thisSave = null;
                }


                if (thisSave != null)
                {
                    if (saveData != null && thisSave.FileVersion < saveData.FileVersion)
                    {
                        Debug.Log("#SaveSystem# save file deprecated " + fileName);
                    }
                    else
                    {
                        Debug.Log("#SaveSystem# loaded save " + fileName);
                        saveData = thisSave;
                    }
                }
            }

            if (savename == null)
            {
                Debug.Log("#SaveSystem# savedata is null");
            }
            return saveData ?? new T();
        }

        public static void Delete(string savename)
        {
            var fileName = GetFileName(savename);
            if (!_saveFiles.TryGetValue(fileName, out var saveFile))
            {
                saveFile = new SaveFile(fileName);
            }
            else
            {
                _saveFiles.Remove(fileName);
            }

            saveFile.Delete();
        }

        private const string extension = ".far";

        private static string GetFileName(string savename)
        {
            return savename + extension;
        }

#if UNITY_EDITOR
        [MenuItem("Rytmos/Open Save Location")]
        public static void OpenSaveLocation()
        {
            EditorUtility.OpenWithDefaultApp(Application.persistentDataPath);
        }
#endif
    }
}
