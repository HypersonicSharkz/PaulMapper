using Beatmap.Base;
using Beatmap.Containers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PaulMapper
{
    public static class NoteHelper
    {
        public static Vector3 GetRotation(this BaseGrid obj)
        {
            Vector3 rot = Vector3.zero;

            if (obj.CustomWorldRotation != null)
            {
                Vector3 worldRot = obj.CustomWorldRotation.ReadVector3();
                if (worldRot.x == 0 && worldRot.y == 0)
                {
                    rot = worldRot;
                }
            }

            rot += obj.CustomLocalRotation != null ? obj.CustomLocalRotation.ReadVector3(Vector3.zero) : Vector3.zero;

            return rot;
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

        public static Vector2 GetRealPosition(this BaseGrid obj)
        {
            Vector2 result = new Vector2();

            if (obj.ObjectType == Beatmap.Enums.ObjectType.Note)
            {
                BaseNote note = obj as BaseNote;

                if (obj.CustomCoordinate != null)
                {
                    result = obj.CustomCoordinate;
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
            }
            else if (obj.ObjectType == Beatmap.Enums.ObjectType.Obstacle)
            {
                BaseObstacle obstacle = obj as BaseObstacle;

                if (obj.CustomCoordinate != null)
                {
                    result = obj.CustomCoordinate;
                }
                else
                {
                    result.x = obstacle.PosX - 2;
                    result.y = (float)obstacle.PosY;
                }
            }


            return result;
        }

        public static void SetScale(this BaseObject obj, Vector3 scale)
        {
            obj.CustomData["animation"]["scale"] = scale;
        }

        public static Color GetColor(this BaseGrid obj)
        {
            Color color = Color.clear;

            if (obj.CustomColor.HasValue)
            {
                color = obj.CustomColor.Value;
            }

            return color;
        }

        public static Vector3 GetObjectScale(this BaseGrid obj)
        {
            Vector3 scale = new Vector3(1, 1, 1);

            if (obj.ObjectType == Beatmap.Enums.ObjectType.Obstacle)
            {
                Vector3? size = null;
                if ((obj as BaseObstacle).CustomSize != null)
                    size = (obj as BaseObstacle).CustomSize;

                float zScale = 0;
                float xScale = 0;
                float yScale = 0;

                if (size.HasValue)
                {
                    xScale = size.Value.x;
                    yScale = size.Value.y;
                    zScale = size.Value.z != 0 ? size.Value.z : (obj as BaseObstacle).Duration * EditorScaleController.EditorScale;
                }
                else
                {
                    Beatmap.Shared.ObstacleBounds bounds = (obj as BaseObstacle).GetShape();
                    xScale = bounds.Width;
                    yScale = bounds.Height;
                    zScale = (obj as BaseObstacle).Duration * EditorScaleController.EditorScale; //Do some magic
                }


                scale = new Vector3(xScale, yScale, zScale);
            }
            else
            {
                JSONNode customData = obj.CustomData;
                if (customData != null)
                {
                    if (customData.HasKey("animation") && customData["animation"].HasKey("scale"))
                    {
                        scale = new Vector3(customData["animation"]["scale"][0], customData["animation"]["scale"][1], customData["animation"]["scale"][2]);
                    }
                }
            }

            return scale;
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

        public static void RotateWalls(bool clockWise, bool leftToRight)
        {
            List<BeatmapAction> actions = new List<BeatmapAction>();
            foreach (BaseObject obj in SelectionController.SelectedObjects)
            {
                if (!(obj is BaseObstacle wall))
                    continue;

                BaseObstacle original = (BaseObstacle)wall.Clone();

                var beatmapObjectContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType);

                Vector3? rotation = null;
                if (wall.CustomLocalRotation != null)
                    rotation = wall.CustomLocalRotation;

                if (leftToRight)
                    wall.CustomLocalRotation = rotation.GetValueOrDefault(Vector3.zero) + new Vector3(0, 0, (clockWise ? -1 : 1) * PaulMapperData.INSTANCE.WallRotationAmount);
                else
                    wall.CustomLocalRotation = rotation.GetValueOrDefault(Vector3.zero) + new Vector3((clockWise ? -1 : 1) * PaulMapperData.INSTANCE.WallRotationAmount, 0, 0);

                ObjectContainer con;
                if (beatmapObjectContainerCollection.LoadedContainers.TryGetValue(wall, out con))
                {
                    con.UpdateGridPosition();
                    if (wall.CustomLocalRotation == null || wall.CustomLocalRotation.ReadVector3() == Vector3.zero)
                        con.Animator.LocalTarget.localEulerAngles = Vector3.zero;
                }

                actions.Add(new BeatmapObjectModifiedAction(wall, wall, original));
            }

            BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, false));
        }
    }
}
