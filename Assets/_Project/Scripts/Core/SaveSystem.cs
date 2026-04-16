using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace WorldOfVictoria.Core
{
    public static class SaveSystem
    {
        private const string SaveMagic = "WOV1";
        private const int SaveVersion = 1;
        public const string LastSavePathPlayerPrefsKey = "WorldOfVictoria.LastSavePath";

        public static string DefaultSavePath => Path.Combine(Application.persistentDataPath, "world-of-victoria-save.dat");

        public static bool HasLastSavePath()
        {
            var path = PlayerPrefs.GetString(LastSavePathPlayerPrefsKey, string.Empty);
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        public static string GetLastSavePath()
        {
            return PlayerPrefs.GetString(LastSavePathPlayerPrefsKey, DefaultSavePath);
        }

        public static void SaveLastSavePath(string path)
        {
            PlayerPrefs.SetString(LastSavePathPlayerPrefsKey, path);
            PlayerPrefs.Save();
        }

        public static void Save(string path, GameSaveData saveData)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);

            using var fileStream = File.Create(path);
            using var gzipStream = new GZipStream(fileStream, System.IO.Compression.CompressionLevel.Optimal);
            using var writer = new BinaryWriter(gzipStream);

            writer.Write(SaveMagic);
            writer.Write(SaveVersion);
            writer.Write(saveData.Width);
            writer.Write(saveData.Height);
            writer.Write(saveData.Depth);
            writer.Write(saveData.GenerationSeed);
            writer.Write(saveData.PlayerPosition.x);
            writer.Write(saveData.PlayerPosition.y);
            writer.Write(saveData.PlayerPosition.z);
            writer.Write(saveData.Blocks.Length);
            writer.Write(saveData.Blocks);

            SaveLastSavePath(path);
        }

        public static GameSaveData Load(string path)
        {
            using var fileStream = File.OpenRead(path);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new BinaryReader(gzipStream);

            var magic = reader.ReadString();
            if (!string.Equals(magic, SaveMagic, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Unsupported save magic '{magic}'.");
            }

            var version = reader.ReadInt32();
            if (version != SaveVersion)
            {
                throw new InvalidDataException($"Unsupported save version {version}.");
            }

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var depth = reader.ReadInt32();
            var seed = reader.ReadInt32();
            var playerPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var blockLength = reader.ReadInt32();
            var blocks = reader.ReadBytes(blockLength);

            if (blocks.Length != blockLength)
            {
                throw new EndOfStreamException($"Save file ended unexpectedly. Expected {blockLength} block bytes, got {blocks.Length}.");
            }

            return new GameSaveData(width, height, depth, seed, playerPosition, blocks);
        }
    }

    [Serializable]
    public readonly struct GameSaveData
    {
        public GameSaveData(int width, int height, int depth, int generationSeed, Vector3 playerPosition, byte[] blocks)
        {
            Width = width;
            Height = height;
            Depth = depth;
            GenerationSeed = generationSeed;
            PlayerPosition = playerPosition;
            Blocks = blocks;
        }

        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public int GenerationSeed { get; }
        public Vector3 PlayerPosition { get; }
        public byte[] Blocks { get; }
    }
}
