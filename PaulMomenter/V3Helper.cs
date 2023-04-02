using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PaulMapper
{
    public static class V3Helper
    {
        public static void SetColor(this BeatmapObject obj, Color color)
        {
            if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
            {
                obj.CustomData["color"] = color;
            } 

            obj.CustomData["_color"] = color;
        }

        public static void SetPosition(this BeatmapObject obj, Vector2 position)
        {
            if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
            {
                obj.CustomData["coordinates"] = position;
            }

            obj.CustomData["_position"] = position;
        }

        public static void SetRotation(this BeatmapObject obj, float angle)
        {
            if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
            {
                SetRotationV3(ref obj, angle);
            }

            obj.CustomData["_cutDirection"] = angle;
        }

        private static void SetRotationV3(ref BeatmapObject obj, float angle)
        {
            if (obj is BeatmapColorNote)
            {
                (obj as BeatmapColorNote).AngleOffset = (int)angle + 180;
                (obj as BeatmapColorNote).CutDirection = 0;
            }
        }

        public static void SetScale(this BeatmapObject obj, Vector3 scale)
        {
            if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
            {
                obj.CustomData["size"] = scale;
            }

            obj.CustomData["_animation"]["_scale"] = scale;
            obj.CustomData["_scale"] = scale;
        }

        public static void SetScale(this BeatmapObject obj, Vector2 scale)
        {
            if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
            {
                obj.CustomData["size"] = scale;
            }

            obj.CustomData["_scale"] = scale;
        }
    }
}
