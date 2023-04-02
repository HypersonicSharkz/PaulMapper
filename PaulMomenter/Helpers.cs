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
        public static Vector2 GetRealPosition(this BeatmapObject p_obj)
        {
            Vector2 result = new Vector2();

            JSONNode customData = p_obj.CustomData;

            if (p_obj.BeatmapType == BeatmapObject.ObjectType.Note)
            {
                BeatmapNote note = p_obj as BeatmapNote;

                if (customData != null && customData.HasKey("_position"))
                {
                    result = p_obj.CustomData["_position"].ReadVector2();
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
            } else if (p_obj.BeatmapType == BeatmapObject.ObjectType.Obstacle)
            {
                BeatmapObstacle obstacle = p_obj as BeatmapObstacle;

                if (customData != null && customData.HasKey("_position"))
                {
                    result = p_obj.CustomData["_position"].ReadVector2();
                } else
                {
                    result.x = obstacle.LineIndex - 2;
                    result.y = obstacle.Type * 1.5f;
                }
            }


             return result;
        }

        public static float GetNoteDirection(this BeatmapNote note) 
        {
            float result = 0;

            JSONNode customData = note.CustomData;

            if (customData != null && customData.HasKey("_cutDirection"))
            {
                return note.CustomData["_cutDirection"];
            }

            switch (note.CutDirection)
            {
                case 0:
                    result = 180;
                    break;
                case 1:
                    result = 0;
                    break;
                case 2:
                    result = 270;
                    break;
                case 3:
                    result = 90;
                    break;
                case 4:
                    result = 225;
                    break;
                case 5:
                    result = 135;
                    break;
                case 6:
                    result = 315;
                    break;
                case 7:
                    result = 45;
                    break;
            }
            return result;
        }

        public static double AngleDifference(double angle1, double angle2)
        {
            double diff = (angle2 - angle1 + 180) % 360 - 180;
            return diff < -180 ? diff + 360 : diff;
        }

        public static float LerpDirFromDict(Dictionary<float, float> dirPoints, float time)
        {

            if (dirPoints.ContainsKey(time))
            {
                float dir;
                if (dirPoints.TryGetValue(time, out dir))
                    return (float)dir;
            }

            for (int i = 0; i < dirPoints.Count - 1; i++)
            {
                if (time > dirPoints.ToList()[i].Key && time < dirPoints.ToList()[i + 1].Key)
                {
                    float dirStart = dirPoints.ToList()[i].Value;
                    float dirEnd = dirPoints.ToList()[i + 1].Value;

                    float startDist = dirPoints.ToList()[i].Key;
                    float endDist = dirPoints.ToList()[i + 1].Key - startDist;

                    float t = (time - startDist) / endDist;

                    return Mathf.LerpAngle(dirStart, dirEnd, t); ;
                }


            }
            return 0;
        }

        public static bool TryGetColorFromObject(BeatmapObject beatmapObject, out Color color)
        {
            color = Color.clear;

            JSONNode customData = beatmapObject.CustomData;
            if (customData != null && customData.HasKey("_color"))
            {
                color = customData["_color"];
                return true;
            }

            return false;
        }

        public static void GetObjectScale(BeatmapObject beatmapObject, out Vector3 scale)
        {
            scale = new Vector3(1, 1, 1);

            if (beatmapObject.BeatmapType == BeatmapObject.ObjectType.Obstacle)
            {
                float xScale = (beatmapObject as BeatmapObstacle).Width;
                float yScale = (beatmapObject as BeatmapObstacle).Type == 0 ? 3.5f : 2f;
                float duration = (beatmapObject as BeatmapObstacle).Duration; //Do some magic

                scale = new Vector3(xScale, yScale, duration);

            }

            JSONNode customData = beatmapObject.CustomData;
            if (customData != null)
            {
                if (customData.HasKey("_scale"))
                {
                    scale = new Vector3(customData["_scale"][0], customData["_scale"][1], customData["_scale"][2]);
                }

                if (customData.HasKey("_animation") && customData["_animation"].HasKey("_scale"))
                { 
                    scale = new Vector3(customData["_animation"]["_scale"][0], customData["_animation"]["_scale"][1], customData["_animation"]["_scale"][2]);
                }
            } 
        }

        public static BeatmapNote GetClosestGridSnap(BeatmapNote note)
        {
            BeatmapNote newNote = new BeatmapNote();
            Vector2 notePos = note.GetRealPosition();

            newNote.Time = note.Time;

            newNote.LineIndex = (int)Math.Round(notePos.x + 2);
            newNote.LineLayer = (int)Math.Round(notePos.y);

            float angle = GetNoteDirection(note);

            if (angle > 360) angle -= 360;
            else if (angle < 0) angle += 360;

            //angle += (note is BeatmapColorNote cnote) ? cnote.AngleOffset : 0;

            if (note.CutDirection != 8)
            {
                newNote.CutDirection = noteNECutToCutdirection[noteNECutToCutdirection.Keys.OrderBy(k => Math.Abs(k - angle)).First()];
            }
            else
                newNote.CutDirection = 8;

            return newNote;
        }

        static Dictionary<float, int> noteNECutToCutdirection = new Dictionary<float, int>()
        {
            {0, 1},
            {180, 0},
            {270, 2},
            {90, 3},
            {225, 4},
            {135, 5},
            {315, 6},
            {45, 7}
        };
    }


}
