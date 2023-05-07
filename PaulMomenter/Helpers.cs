using Beatmap.Base;
using Beatmap.Shared;
using Beatmap.V2;
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
        public static Vector2 GetRealPosition(this BaseGrid p_obj)
        {
            Vector2 result = new Vector2();

            if (p_obj.ObjectType == Beatmap.Enums.ObjectType.Note)
            {
                BaseNote note = p_obj as BaseNote;

                if (p_obj.CustomCoordinate != null)
                {
                    result = p_obj.CustomCoordinate;
                }
                else
                {
                    if (note.PosX >= 1000)
                        result.x = (note.PosX / 1000) - 3;
                    else if (note.PosX <= -1000)
                        result.x = 1997 + note.PosX;
                    else
                        result.x = note.PosX - 2;


                    if (note.PosY >= 1000)
                        result.y = (note.PosY / 1000) - 1;
                    else if (note.PosY <= -1000)
                        result.y = 1999 + note.PosY;
                    else
                        result.y = note.PosY;
                }
            } else if (p_obj.ObjectType == Beatmap.Enums.ObjectType.Obstacle)
            {
                BaseObstacle obstacle = p_obj as BaseObstacle;

                if (p_obj.CustomCoordinate != null)
                {
                    result = p_obj.CustomCoordinate;
                } else
                {
                    result.x = obstacle.PosX - 2;
                    result.y = (float)obstacle.PosY - 0.5f;
                }
            }


             return result;
        }

        public static float GetNoteDirection(this BaseNote note) 
        {
            float result = 0;

            if (note.CustomDirection.HasValue)
            {
                return note.CustomDirection.Value;
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

            return result + note.AngleOffset;
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

        public static bool TryGetColorFromObject(BaseObject beatmapObject, out Color color)
        {
            color = Color.clear;

            if (beatmapObject.CustomColor.HasValue)
            {
                color = beatmapObject.CustomColor.Value;
                return true;
            }

            return false;
        }

        public static void GetObjectScale(BaseGrid beatmapObject, out Vector3 scale)
        {
            scale = new Vector3(1, 1, 1);

            if (beatmapObject.ObjectType == Beatmap.Enums.ObjectType.Obstacle)
            {
                Vector3? size = null;
                if ((beatmapObject as BaseObstacle).CustomSize != null)
                    size = (beatmapObject as BaseObstacle).CustomSize;

                float zScale = 0;
                float xScale = 0;
                float yScale = 0;

                if (size.HasValue)
                {
                    xScale = size.Value.x;
                    yScale = size.Value.y;
                    zScale = size.Value.z != 0 ? size.Value.z : (beatmapObject as BaseObstacle).Duration * EditorScaleController.EditorScale;
                }
                else
                {
                    Beatmap.Shared.ObstacleBounds bounds = (beatmapObject as BaseObstacle).GetShape();
                    xScale = bounds.Width;
                    yScale = bounds.Height;
                    zScale = (beatmapObject as BaseObstacle).Duration * EditorScaleController.EditorScale; //Do some magic
                }


                scale = new Vector3(xScale, yScale, zScale);
            }
            else
            {
                JSONNode customData = beatmapObject.CustomData;
                if (customData != null)
                {
                    if (customData.HasKey("animation") && customData["animation"].HasKey("scale"))
                    { 
                        scale = new Vector3(customData["animation"]["scale"][0], customData["animation"]["scale"][1], customData["animation"]["scale"][2]);
                    }
                } 
            }

        }

        public static BaseNote GetClosestGridSnap(BaseNote note)
        {
            V2Note newNote = new V2Note();
            Vector2 notePos = note.GetRealPosition();

            newNote.SongBpmTime = note.SongBpmTime;

            newNote.PosX = (int)Math.Round(notePos.x + 2);
            newNote.PosY = (int)Math.Round(notePos.y);

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
