using Beatmap.Base;
using Beatmap.Enums;
using ChroMapper_PropEdit.Enums;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PaulMapper
{
    public static class PoodleGenerator
    {
        public static BaseArc GenerateArc(BaseNote from, BaseNote to, int? overrideAngle = null)
        {
            BaseNote closestGridSnap = GetClosestGridSnap(from);
            BaseNote closestGridSnap2 = GetClosestGridSnap(to);
            JSONNode jsonnode = new JSONObject();
            jsonnode["coordinates"] = from.GetRealPosition();
            jsonnode["tailCoordinates"] = to.GetRealPosition();
            BaseArc obj = new BaseArc(); //new BaseArc(from.JsonTime, closestGridSnap.PosX, closestGridSnap.PosY, from.Color, overrideAngle.GetValueOrDefault(closestGridSnap.CutDirection), 1f, to.JsonTime, closestGridSnap2.PosX, closestGridSnap2.PosY, overrideAngle.GetValueOrDefault(closestGridSnap2.CutDirection), 1f, 0, jsonnode);

            obj.JsonTime = from.JsonTime;
            obj.PosX = closestGridSnap.PosX;
            obj.PosY = closestGridSnap.PosY;
            obj.Color = from.Color;
            obj.CutDirection = overrideAngle.GetValueOrDefault(closestGridSnap.CutDirection);
            obj.HeadControlPointLengthMultiplier = 1;
            obj.TailJsonTime = to.JsonTime;
            obj.TailPosX = closestGridSnap2.PosX;
            obj.TailPosY = closestGridSnap2.PosY;
            obj.TailCutDirection = overrideAngle.GetValueOrDefault(closestGridSnap2.CutDirection);
            obj.TailControlPointLengthMultiplier = 1f;
            obj.MidAnchorMode = 0;
            obj.CustomData = jsonnode;

            BeatmapObjectContainerCollection collectionForType = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            collectionForType.SpawnObject(obj, true, true);
            return obj;
        }

        public static void SpawnPrecisionArc(bool straight)
        {
            if (SelectionController.SelectedObjects.Count < 2) { Plugin.paulMapper.SetNotice("Select at least two notes", noticeType.Error); return; }

            if (!SelectionController.SelectedObjects.All(n => n.ObjectType == Beatmap.Enums.ObjectType.Note)) { Plugin.paulMapper.SetNotice("Select only notes", noticeType.Error); return; }

            var ordered = SelectionController.SelectedObjects.OrderBy(s => s.JsonTime).ToList();

            List<BeatmapAction> actions = new List<BeatmapAction>();
            for (int i = 1; i < ordered.Count; i++)
            {
                BaseNote from = (BaseNote)ordered[i - 1];
                BaseNote to = (BaseNote)ordered[i];
                BaseArc arc = null;
                if (straight)
                    arc = PoodleGenerator.GenerateArc(from, to, 8);
                else
                    arc = PoodleGenerator.GenerateArc(from, to);

                if (arc != null)
                    actions.Add(new BeatmapObjectPlacementAction(arc, new List<BaseObject>(), "Arcs"));
            }
            BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, true));
        }

        public static List<BaseGrid> SpawnBasePoodle(BaseGrid object1, BaseGrid object2)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(object1.ObjectType);

            float startTime = object1.JsonTime;
            float endTime = object2.JsonTime;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            float npsStart = PaulMapperData.INSTANCE.Precision;
            float npsEnd = PaulMapperData.INSTANCE.UseEndPrecision ? PaulMapperData.INSTANCE.EndPrecision : PaulMapperData.INSTANCE.Precision;

            float precision = npsStart;

            List<BaseGrid> spawnedBeatobjects = new List<BaseGrid>();

            while (distanceInBeats > 0 - 1 / precision)
            {
                BaseGrid copy = null;
                copy = (BaseGrid)object1.Clone();

                copy.JsonTime = (endTime - distanceInBeats);
                if (copy.JsonTime > endTime)
                    break;

                float line = (originalDistance - distanceInBeats);

                copy.CustomData = new JSONObject();
                JSONNode customData = copy.CustomData;

                if (PaulMapperData.INSTANCE.FakeWalls)
                {
                    if (copy is BaseObstacle && PaulMapperData.IsV3())
                    {
                        customData["uninteractable"] = true;
                    }
                    else
                    {
                        customData["_fake"] = true;
                        customData["_interactable"] = false;
                    }
                }

                if (copy.CustomWorldRotation != null)
                {
                    Vector3 rot = copy.CustomWorldRotation.ReadVector3(new Vector3(0, 0, 0));
                    copy.CustomWorldRotation = new Vector3(rot.x, rot.y, 0);
                }

                copy.WriteCustom();
                collection.SpawnObject(copy, false, true);

                BaseGrid beatmapObject = copy;
                spawnedBeatobjects.Add(beatmapObject);

                precision = Mathf.Lerp(npsEnd, npsStart, distanceInBeats / (endTime - startTime));
                distanceInBeats -= 1 / precision;
            }

            return spawnedBeatobjects;
        }

        public static void GenerateQuickPoodle(BaseGrid note1, BaseGrid note2, string easing = null, int precision = 32)
        {
            Dictionary<float, Color> DistColorDict = new Dictionary<float, Color>();

            Color color1 = note1.GetColor();
            Color color2 = note2.GetColor();

            if (color1 != Color.clear && color2 != Color.clear)
            {
                DistColorDict.Add(0, color1);
                DistColorDict.Add(note2.JsonTime - note1.JsonTime, color2);
            }

            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(note1.ObjectType);

            Vector2 n1 = note1.GetPosition();
            Vector2 n2 = note2.GetPosition();

            float ang = Mathf.Atan2(n2.y - n1.y, n2.x - n1.x) * 180 / Mathf.PI;
            ang += 90;
            float noteRotation = ang;

            float startTime = note1.JsonTime;
            float endTime = note2.JsonTime;


            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            Vector2 note1pos = note1.GetRealPosition();
            Vector2 note2pos = note2.GetRealPosition();

            Vector3 note1Scale = note1.GetScale();
            Vector3 note2Scale = note2.GetScale();

            Vector3 note1Rotation = note1.GetRotation();
            Vector3 note2Rotation = note2.GetRotation();

            BaseGrid oldNote = null;
            int noteIndex = 1;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            while (distanceInBeats > 0 - 1 / (float)precision)
            {
                BaseGrid note1Note = note1 as BaseGrid;
                BaseGrid copy = (BaseGrid)note1.Clone();

                if (copy is BaseNote copyNote)
                    copyNote.CutDirection = 0;

                copy.JsonTime = endTime - distanceInBeats;
                if (copy.JsonTime > endTime)
                    break;


                if (note1pos != note2pos)
                {
                    float line = (originalDistance - distanceInBeats) / originalDistance;

                    if (easing != null)
                    {
                        switch (easing)
                        {
                            case "CubicIn":
                                line = Easing.Cubic.In(line);
                                break;
                            case "CubicOut":
                                line = Easing.Cubic.Out(line);
                                break;
                            case "CubicInOut":
                                line = Easing.Cubic.InOut(line);
                                break;

                            case "ExpIn":
                                line = Easing.Exponential.In(line);
                                break;
                            case "ExpOut":
                                line = Easing.Exponential.Out(line);
                                break;
                            case "ExpInOut":
                                line = Easing.Exponential.InOut(line);
                                break;


                            case "easeInBack":
                                line = Easing.Back.In(line);
                                break;
                            case "easeOutBack":
                                line = Easing.Back.Out(line);
                                break;
                            case "easeInOutBack":
                                line = Easing.Back.InOut(line);
                                break;


                            case "easeInBounce":
                                line = Easing.Bounce.In(line);
                                break;
                            case "easeOutBounce":
                                line = Easing.Bounce.Out(line);
                                break;
                            case "easeInOutBounce":
                                line = Easing.Bounce.InOut(line);
                                break;


                            case "easeInSine":
                                line = Easing.Sinusoidal.In(line);
                                break;
                            case "easeOutSine":
                                line = Easing.Sinusoidal.Out(line);
                                break;
                            case "easeInOutSine":
                                line = Easing.Sinusoidal.InOut(line);
                                break;



                            case "easeInQuad":
                                line = Easing.Quadratic.In(line);
                                break;
                            case "easeOutQuad":
                                line = Easing.Quadratic.Out(line);
                                break;
                            case "easeInOutQuad":
                                line = Easing.Quadratic.InOut(line);
                                break;
                        }
                    }

                    JSONNode customData = copy.CustomData;

                    copy.CustomCoordinate = Vector2.Lerp(note1pos, note2pos, line);

                    if (DistColorDict != null && DistColorDict.Count > 0)
                    {
                        copy.CustomColor = ColorHelper.LerpColorFromDict(DistColorDict, copy.JsonTime - startTime);
                    }

                    if (copy is BaseObstacle wall)
                    {
                        wall.SetScale(Vector3.Lerp(note1Scale, note2Scale, line));

                        float rotX = Mathf.Lerp(note1Rotation.x, note2Rotation.x, line);
                        float rotY = Mathf.Lerp(note1Rotation.y, note2Rotation.y, line);
                        float rotZ = Mathf.Lerp(note1Rotation.z, note2Rotation.z, line);

                        wall.CustomLocalRotation = new Vector3(rotX, rotY, rotZ);
                    }

                    if (copy is BaseNote)
                    {
                        if (PaulMapperData.INSTANCE.RotateNotes)
                        {
                            (copy as BaseNote).SetRotation(noteRotation);
                        }
                        else if (PaulMapperData.INSTANCE.Vibro)
                        {
                            (copy as BaseNote).CutDirection = (noteIndex % 2);
                        }
                    }
                }

                copy.WriteCustom();
                collection.SpawnObject(copy, false, false);


                BaseObject beatmapObject = copy;
                spawnedBeatobjects.Add(beatmapObject);


                oldNote = copy;
                distanceInBeats -= 1 / (float)precision;
                noteIndex += 1;
            }

            if (note1 is BaseNote && (spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BaseNote).CustomDirection.HasValue)
                (spawnedBeatobjects[spawnedBeatobjects.Count - 1] as BaseNote).SetRotation((spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BaseNote).CustomDirection.Value);

            foreach (BaseObject beatmapObject in new List<BaseObject>() { note1, note2 })
            {
                collection.DeleteObject(beatmapObject, false);
            }

            BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedBeatobjects, new List<BaseObject>() { note1, note2 }));

            foreach (BaseObject note in spawnedBeatobjects)
            {
                SelectionController.Select(note, true, true, false);
            }
            //beatmapObjectContainerCollection.DeleteObject(note2);
        }

        private static BaseNote GetClosestGridSnap(BaseNote note)
        {
            BaseNote newNote = (BaseNote)note.Clone();
            Vector2 notePos = note.GetRealPosition();

            newNote.PosX = (int)Math.Round(notePos.x + 2);
            newNote.PosY = (int)Math.Round(notePos.y);

            float angle = note.GetNoteDirection();

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

        private static Dictionary<float, int> noteNECutToCutdirection = new Dictionary<float, int>()
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
