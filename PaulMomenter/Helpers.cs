using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PaulMapper
{
    public static class Helper
    {
        public static Vector2 GetRealPosition(this BeatmapNote note)
        {
            Vector2 result = new Vector2();

            JSONNode customData = note.CustomData;
            if (customData != null && customData.HasKey("_position"))
            {
                result = note.CustomData["_position"].ReadVector2();
            }
            else
            {
                if (note.LineIndex >= 1000)
                    result.x = (note.LineIndex / 1000) - 3;
                else if (note.LineIndex <= -1000)
                    result.x = 1997 + note.LineIndex;
                else
                    result.x = note.LineIndex - 2;


                if (note.LineLayer >= 1000)
                    result.y = (note.LineLayer / 1000) - 1;
                else if (note.LineLayer <= -1000)
                    result.y = 1999 + note.LineLayer;
                else
                    result.y = note.LineLayer;
            }

            return result;
        }

        public static bool TryGetColorFromObject(BeatmapObject beatmapObject, out Color color)
        {
            color = Color.clear;
            if (beatmapObject.BeatmapType != BeatmapObject.ObjectType.Note)
                return false;

            JSONNode customData = beatmapObject.CustomData;
            if (customData != null && customData.HasKey("_color"))
            {
                color = customData["_color"];
                return true;
            }

            return false;
        }
    }


}
