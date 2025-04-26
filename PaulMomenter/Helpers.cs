using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.V2;
using PaulMapper.PaulHelper;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PaulMapper
{
    public static class Helper
    {
        public static Vector3 GetRotation(this BaseGrid p_obj)
        {
            Vector3 rot = Vector3.zero;

            if (p_obj.CustomWorldRotation != null)
            {
                Vector3 worldRot = p_obj.CustomWorldRotation.ReadVector3();
                if (worldRot.x == 0 && worldRot.y == 0)
                {
                    rot = worldRot;
                }
            }

            rot += p_obj.CustomLocalRotation != null ? p_obj.CustomLocalRotation.ReadVector3(Vector3.zero) : Vector3.zero;

            return rot;
        }

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
                    result.y = (float)obstacle.PosY;
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

        public static float GetRotationValueAtTime(float time, List<BaseObject> beatmapObjects)
        {
            //Get all relevant rotations
            EventGridContainer eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Event) as EventGridContainer;
            IEnumerable<BaseEvent> rotations = eventsContainer.AllRotationEvents.Where(x => PaulMaker.CompareRound(x.SongBpmTime, beatmapObjects.First().SongBpmTime, 0.0001f) != -1 && PaulMaker.CompareRound(x.SongBpmTime, beatmapObjects.Last().SongBpmTime, 0.0001f) != 1).OrderBy(x => x.SongBpmTime);

            BaseEvent rotEvent = rotations.LastOrDefault(x => x.SongBpmTime <= time);
            if (rotEvent == null)
            {
                if (rotations.Count() == 1)
                {
                    rotEvent = rotations.First();
                }
                else
                    return -1;
            }

            float t1 = rotEvent.SongBpmTime;

            //Rotation at first note
            float rot1 = eventsContainer.AllRotationEvents.Where(x => x.SongBpmTime < t1).Sum(x => x.Rotation);
            float rot2 = rot1 + rotEvent.Rotation;


            //Get time of last rotation, or last note if it is further away
            float t2 = 0;
            BaseEvent rotEventEnd = rotations.FirstOrDefault(x => x.SongBpmTime >= time);

            if (rotEventEnd == null || rotEventEnd.SongBpmTime > beatmapObjects.Last().SongBpmTime)
                t2 = beatmapObjects.Last().SongBpmTime;
            else
                t2 = rotEventEnd.SongBpmTime;

            if (t1 == t2)
                return rot1;

            return Mathf.Lerp(rot1, rot2, (time - t1) / (t2 - t1));
        }

        public static BaseNote GetClosestGridSnap(BaseNote note)
        {
            BaseNote newNote = new BaseNote();
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

        private static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            Vector3 direction = point - pivot;
            Vector3 rotatedDirection = rotation * direction;
            return pivot + rotatedDirection;
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
                    wall.CustomLocalRotation = rotation.GetValueOrDefault(Vector3.zero) + new Vector3(0, 0, (clockWise ? -1 : 1) * PaulmapperData.Instance.wallRotationAmount);
                else
                    wall.CustomLocalRotation = rotation.GetValueOrDefault(Vector3.zero) + new Vector3((clockWise ? -1 : 1) * PaulmapperData.Instance.wallRotationAmount, 0, 0);

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

        public static void SpawnPrecisionArc()
        {
            if (SelectionController.SelectedObjects.Count < 2) { Plugin.momenter.SetNotice("Select at least two notes", noticeType.Error); return; }

            if (!SelectionController.SelectedObjects.All(n => n.ObjectType == Beatmap.Enums.ObjectType.Note)) { Plugin.momenter.SetNotice("Select only notes", noticeType.Error); return; }

            var ordered = SelectionController.SelectedObjects.OrderBy(s => s.JsonTime).ToList();

            bool straight = Event.current.modifiers == EventModifiers.Shift;

            List<BeatmapAction> actions = new List<BeatmapAction>();
            for (int i = 1; i < ordered.Count; i++)
            {
                BaseNote from = (BaseNote)ordered[i - 1];
                BaseNote to = (BaseNote)ordered[i];
                BaseArc arc = null;
                if (straight)
                    arc = PaulMaker.GenerateArc(from, to, 8);
                else
                    arc = PaulMaker.GenerateArc(from, to);

                if (arc != null)
                    actions.Add(new BeatmapObjectPlacementAction(arc, new List<BaseObject>(), "Arcs"));
            }
            BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, true));
        }

        public static void DeleteObjectFix(this BeatmapObjectContainerCollection col, BaseObject obj, bool triggersAction = true, bool refreshesPool = true, string comment = "No comment.", bool inCollectionOfDeletes = false)
        {
            //col.DeleteObject(obj, triggersAction, refreshesPool, comment, inCollectionOfDeletes);

            Type type = typeof(BeatmapObjectContainerCollection);
            MethodInfo method = type.GetMethod("DeleteObject", new Type[] { typeof(BaseObject), typeof(bool), typeof(bool), typeof(string), typeof(bool) });

            if (method == null)
            {
                //Dev build
                method = type.GetMethod("DeleteObject", new Type[] { typeof(BaseObject), typeof(bool), typeof(bool), typeof(string), typeof(bool), typeof(bool) });
                method.Invoke(col, new object[] { obj, triggersAction, refreshesPool, comment, inCollectionOfDeletes, true });
            }
            else
            {
                //Stable build
                method.Invoke(col, new object[] { obj, triggersAction, refreshesPool, comment, inCollectionOfDeletes });
            }
        }

        public static void SpawnObjectFix(this BeatmapObjectContainerCollection col, BaseObject obj, bool removeConflicting = true, bool refreshesPool = true, bool inCollectionOfSpawns = false)
        {
            col.SpawnObject(obj, removeConflicting, refreshesPool, inCollectionOfSpawns);

            /*
            Type type = typeof(BeatmapObjectContainerCollection);
            MethodInfo method = type.GetMethod("SpawnObject", new Type[] { typeof(BaseObject), typeof(bool), typeof(bool)});

            if (method == null)
            {
                //Normal build
                method = type.GetMethod("SpawnObject", new Type[] { typeof(BaseObject), typeof(bool), typeof(bool), typeof(bool) });
                method.Invoke(col, new object[] { obj, removeConflicting, refreshesPool, inCollectionOfSpawns });
            }
            else
                //Anim build
                method.Invoke(col, new object[] { obj, removeConflicting, refreshesPool});*/
        }
    }
}
