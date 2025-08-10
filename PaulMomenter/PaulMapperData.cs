using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace PaulMapper
{
    public class PaulMapperData
    {
        public static PaulMapperData Instance;
        public int precision = 32;
        public bool vibro = false;
        public bool rotateNotes = true;
        public bool arcs = true;
        public bool autoDot = true;
        public float transitionTime = 0.3f;
        public bool transitionRotation = true;
        public bool usePointRotations = false;
        internal bool fakeWalls;
        public bool useScale = false;
        public bool disableBadCutDirection = false;
        public bool disableBadCutSpeed = false;
        public bool disableBadCutSaberType = false;
        public int wallRotationAmount = 5;
        public bool enableQuickMenu = true;
        public bool useEndPrecision;
        public int endPrecision;
        public bool adjustToWorldRotation = true;

        public static PaulMapperData GetSaveData()
        {

            PaulMapperData data = null;

            try
            {
                data = JsonConvert.DeserializeObject<PaulMapperData>(File.ReadAllText(Path.Combine(Application.persistentDataPath, "paulMapper.json")));
            } catch
            {
                data = new PaulMapperData();
            }

            if (data == null)
                data = new PaulMapperData();

            File.WriteAllText(Path.Combine(Application.persistentDataPath, "paulMapper.json"), JsonConvert.SerializeObject(data, Formatting.Indented));
            Instance = data;
            return data;

        }

        public void SaveData()
        {
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "paulMapper.json"), JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static bool IsV3()
        {
            return int.Parse(BeatSaberSongContainer.Instance.Map.Version.Split('.')[0]) >= 3;
        }
    }
}
