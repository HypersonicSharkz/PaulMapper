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
        public bool autoDot = true;
        public float transitionTime = 0.3f;
        public bool transitionRotation = false;

        public static PaulmapperData GetSaveData()
        {

            PaulmapperData data = new PaulmapperData();

            try
            {
                if (File.Exists(Path.Combine(Application.persistentDataPath, "paulMapper.json")))
                    data = JsonConvert.DeserializeObject<PaulmapperData>(File.ReadAllText(Path.Combine(Application.persistentDataPath, "paulMapper.json")));
                else
                    data = new PaulmapperData();

            } catch
            {
                data = new PaulmapperData();
                
            }

            File.WriteAllText(Path.Combine(Application.persistentDataPath, "paulMapper.json"), JsonConvert.SerializeObject(data, Formatting.Indented));
            Instance = data;
            return data;

        }

        public void SaveData()
        {
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "paulMapper.json"), JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

    [Serializable]
    public class SerializableRect
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public SerializableRect(Rect rect)
        {
            x = rect.x;
            y = rect.y;
            width = rect.width;
            height = rect.height;
        }

        public Rect getRect()
        {
            return new Rect(x, y, width, height);
        }

        public void setRect(Rect rect)
        {
            x = rect.x;
            y = rect.y;
            width = rect.width;
            height = rect.height;
        }
    }
}
