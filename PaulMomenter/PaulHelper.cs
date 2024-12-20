using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using Extreme.Mathematics.Curves;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PaulMapper.PaulHelper
{
    public static class PaulFinder
    {
        public static List<Paul> pauls = new List<Paul>();
        public static int currentPaul = 0;
        static Paul lastPaul;

        public static List<Paul> FindAllPauls(List<BaseNote> allNotes)
        {
            List<BaseNote> notesLeft = allNotes.Where(n => n.Type == 0).ToList();
            List<BaseNote> notesRight = allNotes.Where(n => n.Type == 1).ToList();

            List<Paul> pauls = new List<Paul>();

            List<Paul> leftPauls = FindPauls(notesLeft);
            leftPauls.ForEach(p => p.notes.ForEach(n => n.CustomData["_paul"] = $"{p.notes.First().SongBpmTime}" ));
            pauls.AddRange(leftPauls);

            List<Paul> rightPauls = FindPauls(notesRight);
            rightPauls.ForEach(p => p.notes.ForEach(n => n.CustomData["_paul"] = $"{p.notes.First().SongBpmTime}"));
            pauls.AddRange(rightPauls);

            return pauls;
        }

        public static List<Paul> FindPauls(List<BaseNote> notesOneSide)
        {
            if (notesOneSide.Count <= 0)
                return new List<Paul>();

            //Find closest notes

            BaseNote oldNote = notesOneSide[0];

            List<Paul> foundPauls = new List<Paul>();

            bool paul = false;

            List<BaseNote> groupedNotes = new List<BaseNote>();
            float lastPrecision = 0;

            foreach (BaseNote note in notesOneSide)
            {
                if (note.SongBpmTime != oldNote.SongBpmTime)
                {
                    float dist = note.SongBpmTime - oldNote.SongBpmTime;

                    if (lastPrecision != 0)
                    {

                        if (dist > lastPrecision - 0.01 && dist < lastPrecision + 0.01 && notesOneSide.IndexOf(note) != notesOneSide.Count - 1)
                        {
                            //Is still part of a paul
                            if (!paul)
                            {
                                paul = true;
                                groupedNotes = new List<BaseNote>
                                {
                                    notesOneSide[notesOneSide.IndexOf(note) - 2],
                                    oldNote,
                                    note
                                };
                            }
                            else
                            {
                                groupedNotes.Add(note);
                            }

                        }
                        else
                        {
                            if (paul)
                            {
                                if (notesOneSide.IndexOf(note) == notesOneSide.Count - 1)
                                    groupedNotes.Add(note);

                                paul = false;

                                //For a paul to be a paul it must be longer than 4 notes and not too long

                                if (groupedNotes.Count > 4 && (int)(1 / lastPrecision) > 8)
                                {
                                    foundPauls.Add(new Paul() { notes = groupedNotes, PaulPrecision = (int)(1 / lastPrecision) }); //Create paul
                                }

                            }
                        }

                        lastPrecision = dist;
                    }
                    else
                    {
                        //First note
                        lastPrecision = dist;
                    }
                }

                oldNote = note;

            }

            return foundPauls.OrderBy(p => p.Beat).ToList();
        }

        public static void GoToPaul(Paul paul)
        {
            SelectionController.DeselectAll();

            currentPaul = pauls.IndexOf(paul);

            PaulMapper.ats.MoveToSongBpmTime(paul.notes[0].SongBpmTime);


        }

        public static void SelectCurrentPaul()
        {
            Paul paul = null;

            Paul closest = pauls.OrderBy(p => Math.Abs(p.Beat - PaulMapper.ats.CurrentSongBpmTime) ).First();
            if (lastPaul != null && closest == lastPaul && pauls.Any(p => p.Beat == lastPaul.Beat && p != lastPaul)) 
            {
                paul = pauls.First(p => p.Beat == lastPaul.Beat && p != lastPaul);
            } else
            {
                paul = closest;
            }


            SelectionController.DeselectAll();


            foreach (BaseNote note in paul.notes)
                SelectionController.Select(note, true, true, false);

            SelectionController.SelectionChangedEvent?.Invoke();

            currentPaul = pauls.IndexOf(paul);

            lastPaul = paul;
        }

        public static void SelectAllPauls() 
        {
            foreach (Paul paul in pauls) 
            {
                foreach (BaseNote note in paul.notes)
                    SelectionController.Select(note, true, true, false);
            }
            SelectionController.SelectionChangedEvent?.Invoke();
        }

        public static void KeepFirstNotes()
        {
            
            List<BaseNote> notes = pauls.Where(p => p.notes.Any(n => SelectionController.SelectedObjects.Contains(n))).SelectMany(p => p.notes.Skip(1)).ToList();

            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);

            foreach (BaseObject beatmapObject in notes)
            {
                collection.DeleteObjectFix(beatmapObject, false);
            }

            BeatmapActionContainer.AddAction(new SelectionDeletedAction(notes));
        }
    }

    public static class PaulMaker
    {
        public static BaseArc GenerateArc(BaseNote from, BaseNote to, int? overrideAngle = null)
        {
            BaseNote closestGridSnap = Helper.GetClosestGridSnap(from);
            BaseNote closestGridSnap2 = Helper.GetClosestGridSnap(to);
            JSONNode jsonnode = new JSONObject();
            jsonnode["coordinates"] = from.GetRealPosition();
            jsonnode["tailCoordinates"] = to.GetRealPosition();
            BaseArc arc = new BaseArc {
                JsonTime = from.JsonTime,
                PosX = closestGridSnap.PosX,
                PosY = closestGridSnap.PosY, 
                Color = from.Color,
                CutDirection = overrideAngle.GetValueOrDefault(closestGridSnap.CutDirection),
                HeadControlPointLengthMultiplier = 1f,
                TailJsonTime = to.JsonTime,
                TailPosX = closestGridSnap2.PosX,
                TailPosY = closestGridSnap2.PosY,
                TailCutDirection = overrideAngle.GetValueOrDefault(closestGridSnap2.CutDirection),
                TailControlPointLengthMultiplier = 1f,
                MidAnchorMode = 0,
                CustomData = jsonnode };
            BeatmapObjectContainerCollection collectionForType = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            collectionForType.SpawnObjectFix(arc, true, true);
            return arc;
        }

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


        public static List<BaseObject> GeneratePoodle(BaseObject note1, BaseObject note2, int l_precision = 16, int endPreicision = 16, bool dots = false)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);

            float startTime = note1.SongBpmTime;
            float endTime = note2.SongBpmTime;

            float distanceInBeats = endTime - startTime;


            float npsStart = PaulmapperData.Instance.precision;
            float npsEnd = PaulmapperData.Instance.useEndPrecision ? PaulmapperData.Instance.endPrecision : PaulmapperData.Instance.precision;

            float precision = npsStart;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            while (distanceInBeats > 0 - 1 / precision)
            {
                BaseNote copy = (BaseNote)note1.Clone();

                copy.CustomData = new JSONObject();

                //copy.CustomData["_paul"] = startTime;
                copy.SongBpmTime = (endTime - distanceInBeats);
                if (copy.SongBpmTime > endTime)
                    break;
               
                collection.SpawnObjectFix(copy, false, true);
                copy.WriteCustom();

                spawnedBeatobjects.Add(copy);

                precision = Mathf.Lerp(npsEnd, npsStart, distanceInBeats / (endTime - startTime));
                distanceInBeats -= 1 / precision;
            }

            return spawnedBeatobjects;
        }

        /// <summary>
        /// Generate poodle from curves
        /// </summary>
        /// <param name="note1">First note</param>
        /// <param name="note2">Last note</param>
        /// <param name="splineInterpolatorx">Curve of the x-position</param>
        /// <param name="splineInterpolatory">Curve of the y-position</param>
        /// <param name="l_precision">Cursor precision</param>
        /// <param name="dots">Turn all notes to dots</param>
        /// <param name="colorDict">Dict of colors at time</param>
        /// <param name="dotTime">Time of dots for transitions</param>
        /// <returns></returns>
        public static List<BaseObject> GeneratePoodle(BaseObject note1, BaseObject note2, Curve splineInterpolatorx, Curve splineInterpolatory, int l_precision = 32, bool dots = false, Dictionary<float, Color> colorDict = null, List<float> dotTime = null, Dictionary<float, float> pointsDir = null)
        {
            //TimeKeeper TGP = new TimeKeeper();
            //TGP.Start();
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);

            float startTime = note1.SongBpmTime;
            float endTime = note2.SongBpmTime;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            BaseNote oldNote = null;
            int noteIndex = 1;


            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BaseNote note1Note = note1 as BaseNote;
                BaseNote copy = null;

                copy = (BaseNote)note1.Clone();

                copy.SongBpmTime = (endTime - distanceInBeats);
                if (copy.SongBpmTime > endTime)
                    break;

                float line = (originalDistance - distanceInBeats);


                var x = splineInterpolatorx.ValueAt(line);
                var y = splineInterpolatory.ValueAt(line);

                copy.CustomData = new JSONObject();
                JSONNode customData = copy.CustomData;

                copy.SetPosition(new Vector2((float)x, (float)y));

                //Color handling 
                if (colorDict != null && colorDict.Count > 0)
                {
                    copy.SetColor(LerpColorFromDict(colorDict, line));
                }

                if (dots)
                {
                    copy.CutDirection = 8;
                }

                JSONNode customData_old = null;
                if (PaulmapperData.Instance.rotateNotes)
                {
                    //Fix rotation
                    if (oldNote != null)
                    {
                        if (pointsDir != null)
                        {
                            //Directions are being forced
                            float ang = Helper.LerpDirFromDict(pointsDir, line - 1 / (float)l_precision);

                            //Set rotation
                            customData_old = oldNote.CustomData;
                            oldNote.CutDirection = 0;

                            if (PaulmapperData.Instance.vibro)
                            {
                                ang += 180 * (noteIndex % 2);
                            }

                            oldNote.SetRotation(ang);

                        } else
                        {
                            Vector2 op = oldNote.GetPosition();
                            Vector2 cp = copy.GetPosition();

                            float ang = Mathf.Atan2(cp.y - op.y, cp.x - op.x) * 180 / Mathf.PI;
                            ang += 90;


                            //Set rotation
                            customData_old = oldNote.CustomData;

                            if (PaulmapperData.Instance.vibro)
                            {
                                ang = Mathf.Atan2(Math.Abs(cp.y - op.y), Math.Abs(cp.x - op.x)) * 180 / Mathf.PI;
                                ang += 90;

                                ang += 180 * (noteIndex % 2);
                            }

                            oldNote.SetRotation(ang);
                        }
                    }
                }
                else if (PaulmapperData.Instance.vibro)
                {
                    copy.CutDirection = (noteIndex % 2);
                }



                if (dotTime != null && pointsDir == null)
                {
                    try
                    {
                        dotTime.Sort();
                        float closeDotTime = dotTime.OrderBy(d => Mathf.Abs(d - oldNote.SongBpmTime)).First();
                        if (Mathf.Abs(oldNote.SongBpmTime - closeDotTime) < 2 * PaulmapperData.Instance.transitionTime)
                        {
                            oldNote.CutDirection = 8;

                            if (!PaulmapperData.Instance.transitionRotation && customData_old != null)
                            {
                                oldNote.SetRotation(0);
                            }
                        }

                    }
                    catch
                    {

                    }

                }
                customData["_paul"] = startTime;

                copy.WriteCustom();

                collection.SpawnObjectFix(copy, false, false);
                spawnedBeatobjects.Add(copy);


                oldNote = copy;
                distanceInBeats -= 1 / (float)l_precision;

                noteIndex++;
            }

            if ((spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BaseNote).CustomDirection.HasValue)
            {
                if (pointsDir != null)
                {
                    (spawnedBeatobjects[spawnedBeatobjects.Count - 1] as BaseNote).SetRotation(pointsDir.Last().Value);
                } else
                {
                    (spawnedBeatobjects[spawnedBeatobjects.Count - 1] as BaseNote).SetRotation((spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BaseNote).CustomDirection.Value + (PaulmapperData.Instance.vibro ? 180 : 0));
                }
            }

            return spawnedBeatobjects;
        }

        /// <summary>
        /// Generate normal paul between two notes
        /// </summary>
        /// <param name="note1">First note</param>
        /// <param name="note2">Last note</param>
        /// <param name="l_precision">Cursor precision</param>
        /// <returns></returns>
        public static List<BaseObject> GeneratePaul(BaseObject note1, BaseObject note2, int l_precision)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(note1.ObjectType);

            float startTime = note1.SongBpmTime;
            float endTime = note2.SongBpmTime;

            float distanceInBeats = endTime - startTime;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BaseObject copy = null;

                copy = (BaseObject)note1.Clone();

                copy.SongBpmTime = (endTime - distanceInBeats);
                if (copy.SongBpmTime > endTime)
                    break;

                collection.SpawnObjectFix(copy, false, false);
                BaseObject beatmapObject = copy;
                spawnedBeatobjects.Add(beatmapObject);


                distanceInBeats -= 1 / (float)l_precision;
            }

            return spawnedBeatobjects;
        }

        /// <summary>
        /// Generate poodle from easing
        /// </summary>
        /// <param name="note1">First note of curve</param>
        /// <param name="note2">Last note of curve</param>
        /// <param name="easing">Easing</param>
        /// <param name="precision">Cursor precision</param>
        public static void GeneratePoodle(BaseObject note1, BaseObject note2, string easing = null, int precision = 32)
        {
            Dictionary<float, Color> DistColorDict = new Dictionary<float, Color>();

            if (Helper.TryGetColorFromObject(note1, out Color col1) && Helper.TryGetColorFromObject(note2, out Color col2))
            {
                DistColorDict.Add(0, col1);
                DistColorDict.Add(note2.SongBpmTime - note1.SongBpmTime, col2);
            }

            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(note1.ObjectType);

            Vector2 n1 = (note1 as BaseGrid).GetPosition();
            Vector2 n2 = (note2 as BaseGrid).GetPosition();

            float ang = Mathf.Atan2(n2.y - n1.y, n2.x - n1.x) * 180 / Mathf.PI;
            ang += 90;
            float noteRotation = ang;

            float startTime = note1.SongBpmTime;
            float endTime = note2.SongBpmTime;


            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            Vector2 note1pos = (note1 as BaseGrid).GetRealPosition();
            Vector2 note2pos = (note2 as BaseGrid).GetRealPosition();

            BaseGrid oldNote = null;
            int noteIndex = 1;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            while (distanceInBeats > 0 - 1 / (float)precision)
            {
                BaseGrid note1Note = note1 as BaseGrid;
                BaseGrid copy = (BaseGrid)note1.Clone();

                if (copy is BaseNote copyNote)
                    copyNote.CutDirection = 0;

                copy.SongBpmTime = endTime - distanceInBeats;
                if (copy.SongBpmTime > endTime)
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

                    copy.CustomData = new JSONObject();
                    JSONNode customData = copy.CustomData;

                    copy.CustomCoordinate = Vector2.Lerp(note1pos, note2pos, line);

                    if (DistColorDict != null && DistColorDict.Count > 0)
                    {
                        copy.CustomColor = PaulMaker.LerpColorFromDict(DistColorDict, copy.SongBpmTime - startTime);
                    }

                    if (copy is BaseNote)
                    {
                        if (PaulmapperData.Instance.rotateNotes)
                        {
                            (copy as BaseNote).SetRotation(noteRotation);
                        }
                        else if (PaulmapperData.Instance.vibro)
                        {
                            (copy as BaseNote).CutDirection = (noteIndex % 2);
                        }
                    }

                    customData["_paul"] = startTime;
                }


                copy.WriteCustom();
                collection.SpawnObjectFix(copy, false, false);


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
                collection.DeleteObjectFix(beatmapObject, false);
            }
            
            BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedBeatobjects, new List<BaseObject>() { note1, note2 }));

            foreach (BaseObject note in spawnedBeatobjects)
            {
                SelectionController.Select(note, true, true, false);
            }
            //beatmapObjectContainerCollection.DeleteObject(note2);
        }

        public static int CompareRound(float d1, float d2, float rounding)
        {
            if (EqualsRound(d1, d2, rounding))
                return 0;

            if (d1 > d2)
                return 1;
            else
                return -1;
        }

        public static bool EqualsRound(float d1, float d2, float rounding)
        {
            bool result = (Math.Abs(d2 - d1) < rounding);
            return result;
        }
    }

    public static class PaulActions
    {
        public static readonly Type[] actionMaps = new Type[]
        {
            typeof(CMInput.ICameraActions),
            typeof(CMInput.IBeatmapObjectsActions),
            typeof(CMInput.INodeEditorActions),
            typeof(CMInput.ISavingActions),
            typeof(CMInput.ITimelineActions),
            typeof(CMInput.IPlaybackActions)
        };

        public static Type[] actionMapsDisabled
        {
            get
            {
                return (from x in typeof(CMInput).GetNestedTypes()
                        where x.IsInterface && !actionMaps.Contains(x)
                        select x).ToArray<Type>();
            }
        }
    }
}
