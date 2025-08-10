using Beatmap.Base;
using Beatmap.V3;
using UnityEngine;

namespace PaulMapper
{
    public static class V3Helper
    {
        public static void SetColor(this BaseObject obj, Color color)
        {
            obj.CustomColor = color;
        }

        public static void SetPosition(this BaseGrid obj, Vector2 position)
        {
            obj.CustomCoordinate = position;
        }

        public static void SetRotation(this BaseNote obj, float angle)
        {
            if (PaulMapperData.IsV3())
            {
                obj.AngleOffset = (int)angle - 180;
            } 
            else
            {
                obj.CustomDirection = angle;
            }
        }

        public static void SetScale(this BaseObject obj, Vector3 scale)
        {
            obj.CustomData["animation"]["scale"] = scale;
        }

        public static void SetScale(this BaseObstacle obj, Vector3 scale)
        {
             obj.CustomSize = scale;
        }
    }
}
