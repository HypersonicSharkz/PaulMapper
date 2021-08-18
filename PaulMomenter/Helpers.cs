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

            JSONNode customData = note._customData;
            if (customData != null && customData.HasKey("_position"))
            {
                result = note._customData["_position"].ReadVector2();
            }
            else
            {
                if (note._lineIndex >= 1000)
                    result.x = (note._lineIndex / 1000) - 3;
                else if (note._lineIndex <= -1000)
                    result.x = 1997 + note._lineIndex;
                else
                    result.x = note._lineIndex - 2;


                if (note._lineLayer >= 1000)
                    result.y = (note._lineLayer / 1000) - 1;
                else if (note._lineLayer <= -1000)
                    result.y = 1999 + note._lineLayer;
                else
                    result.y = note._lineLayer;
            }

            return result;
        }

        public static bool TryGetColorFromObject(BeatmapObject beatmapObject, out Color color)
        {
            color = Color.clear;
            if (beatmapObject.beatmapType != BeatmapObject.Type.NOTE)
                return false;

            JSONNode customData = beatmapObject._customData;
            if (customData != null && customData.HasKey("_color"))
            {
                color = customData["_color"];
                return true;
            }

            return false;
        }
    }


}
