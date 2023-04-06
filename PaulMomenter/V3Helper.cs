using Beatmap.Base;
using Beatmap.V3;
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
            if (obj is V3ColorNote)
            {
                obj.AngleOffset = (int)angle - 180;
            }
            else
            {
                obj.CustomDirection = (int)angle;
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
