using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PaulMapper
{
    public static class ColorHelper
    {
        public static Color LerpColorFromDict(Dictionary<float, Color> colorDict, float dist)
        {

            if (colorDict.ContainsKey(dist))
            {
                Color lerpedColor;
                if (colorDict.TryGetValue(dist, out lerpedColor))
                    return lerpedColor;
            }

            for (int i = 0; i < colorDict.Count - 1; i++)
            {
                if (dist > colorDict.ToList()[i].Key && dist < colorDict.ToList()[i + 1].Key)
                {
                    Color colorStart = colorDict.ToList()[i].Value;
                    Color colorEnd = colorDict.ToList()[i + 1].Value;

                    float startDist = colorDict.ToList()[i].Key;
                    float endDist = colorDict.ToList()[i + 1].Key - startDist;

                    float t = (dist - startDist) / endDist;
                    Color lerpedColor = Color.Lerp(colorStart, colorEnd, t);

                    return lerpedColor;
                }


            }
            return Color.white;
        }
    }
}
