using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Versioned local slot; write a complete temp file before replacing the last good save.</summary>
    public sealed class CheckpointStore
    {
        private readonly string path;
        public CheckpointStore(string filePath)
        {
            path = filePath;
        }

        public bool Exists => File.Exists(path);

        public void Write(CheckpointData data)
        {
            Validate(data);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(JsonUtility.ToJson(data));
                    writer.Flush();
                    stream.Flush(true);
                }
            }

            if (File.Exists(path))
            {
                File.Replace(temporary, path, path + ".bak");
            }
            else
            {
                File.Move(temporary, path);
            }
        }

        public CheckpointData Read()
        {
            var data = JsonUtility.FromJson<CheckpointData>(File.ReadAllText(path));
            Validate(data);
            return data;
        }

        public void Delete()
        {
            foreach (var file in new[]
            {
                path,
                path + ".bak",
                path + ".tmp"
            }

            )
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        public static void Validate(CheckpointData data)
        {
            if (data == null || data.version != CheckpointData.CurrentVersion)
            {
                throw new InvalidDataException("Unsupported checkpoint version");
            }

            if (string.IsNullOrWhiteSpace(data.levelId) || string.IsNullOrWhiteSpace(data.checkpointId) || data.entities == null)
            {
                throw new InvalidDataException("Incomplete checkpoint");
            }

            if (!float.IsFinite(data.elapsedSeconds) || data.elapsedSeconds < 0)
            {
                throw new InvalidDataException("Invalid checkpoint elapsed time");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entity in data.entities)
            {
                if (entity == null || string.IsNullOrWhiteSpace(entity.id) || !ids.Add(entity.id) || entity.parts == null)
                {
                    throw new InvalidDataException("Invalid checkpoint entity ID");
                }

                CheckpointValueValidation.ValidatePose(entity.position, entity.rotation);
            }
        }
    }
}
