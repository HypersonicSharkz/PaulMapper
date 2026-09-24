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
