using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PaulMapper
{
    public class PaulmapperData
    {
        public static PaulmapperData Instance;
        public SerializableRect windowRect = new SerializableRect(new Rect(200, 10, 140, 440));
        public int precision = 32;
        public bool vibro = false;
        public bool rotateNotes = true;
        public bool arcs = true;
        public bool autoDot = true;
        public float transitionTime = 0.3f;
        public bool transitionRotation = false;
        public bool usePointRotations = false;
        internal bool fakeWalls;
        public bool useScale = false;
        public bool disableBadCutDirection = false;
        public bool disableBadCutSpeed = false;
        public bool disableBadCutSaberType = false;


        public static PaulmapperData GetSaveData()
        {

            PaulmapperData data = null;

            try
            {
                data = JsonConvert.DeserializeObject<PaulmapperData>(File.ReadAllText(Path.Combine(Application.persistentDataPath, "paulMapper.json")));
            } catch
            {
                data = new PaulmapperData();
            }

            if (data == null)
                data = new PaulmapperData();

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
            return BeatSaberSongContainer.Instance.Map.Version == "3.2.0";
        }
    }

    [Serializable]
    public class SerializableRect
    {
        public float x;
        public float y;

        public SerializableRect(Rect rect)
        {
            x = rect.x;
            y = rect.y;
        }

        public Rect getRect()
        {
            return new Rect(x, y, 140, 450);
        }

        public void setRect(Rect rect)
        {
            x = rect.x;
            y = rect.y;
        }
    }
}
