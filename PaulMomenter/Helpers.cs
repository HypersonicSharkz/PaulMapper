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
                result.x = note._lineIndex - 2;
                result.y = note._lineLayer;
            }

            return result;
        }
    }
}
