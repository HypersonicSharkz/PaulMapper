using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using Extreme.Mathematics.Curves;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PaulMapper.PaulHelper
{
    public static class PaulFinder
    {
        public static List<Paul> pauls = new List<Paul>();
        public static int currentPaul = 0;

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

            //Debug.LogError("Found: " + pauls.Count + " Pauls");

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
                                groupedNotes = new List<BaseNote>();
                                groupedNotes.Add(notesOneSide[notesOneSide.IndexOf(note) - 2]);
                                groupedNotes.Add(oldNote);
                                groupedNotes.Add(note);
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

            PaulMomenter.ats.MoveToTimeInBeats(paul.notes[0].SongBpmTime);


        }

        static Paul lastPaul;

        public static void SelectCurrentPaul()
        {
            Paul paul = null;

            Paul closest = pauls.OrderBy(p => Math.Abs(p.Beat - PaulMomenter.ats.CurrentBeat) ).First();
            if (lastPaul != null && closest == lastPaul && pauls.Any(p => p.Beat == lastPaul.Beat && p != lastPaul)) 
            {
                paul = pauls.First(p => p.Beat == lastPaul.Beat && p != lastPaul);
            } else
            {
                paul = closest;
            }


            SelectionController.DeselectAll();


            foreach (BaseNote note in paul.notes)
                SelectionController.Select(note, true, true, true);

            currentPaul = pauls.IndexOf(paul);

            lastPaul = paul;
        }

        public static void SelectAllPauls() 
        {
            foreach (Paul paul in pauls) 
            {
                foreach (BaseNote note in paul.notes)
                    SelectionController.Select(note, true, true, true);
            }
        }

        public static void KeepFirstNotes()
        {
            
            List<BaseNote> notes = pauls.Where(p => p.notes.Any(n => SelectionController.SelectedObjects.Contains(n))).SelectMany(p => p.notes.Skip(1)).ToList();

            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);

            foreach (BaseObject beatmapObject in notes)
            {
                collection.DeleteObject(beatmapObject, false);
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
            V3Arc obj = new V3Arc(from.SongBpmTime, closestGridSnap.PosX, closestGridSnap.PosY, from.Color, overrideAngle.GetValueOrDefault(closestGridSnap.CutDirection), 1f, to.SongBpmTime, closestGridSnap2.PosX, closestGridSnap2.PosY, overrideAngle.GetValueOrDefault(closestGridSnap2.CutDirection), 1f, 0, jsonnode);
            BeatmapObjectContainerCollection collectionForType = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            collectionForType.SpawnObject(obj, true, true);
            return collectionForType.UnsortedObjects.Last() as BaseArc;
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


        public static List<BaseObject> GeneratePoodle(BaseObject note1, BaseObject note2, int l_precision = 32, bool dots = false)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);

            float startTime = note1.SongBpmTime;
            float endTime = note2.SongBpmTime;

            float distanceInBeats = endTime - startTime;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BaseNote note1Note = note1 as BaseNote;
                BaseNote copy = null;

                copy = (BaseNote)note1Note.Clone();

                copy.CustomData = new JSONObject();

                //copy.CustomData["_paul"] = startTime;
                copy.SongBpmTime = (endTime - distanceInBeats);
                if (copy.SongBpmTime > endTime)
                    break;

                collection.SpawnObject(copy, false, false);

                BaseObject beatmapObject = collection.UnsortedObjects.Last();
                spawnedBeatobjects.Add(beatmapObject);

                distanceInBeats -= 1 / (float)l_precision;
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

                //customData["_position"] = new Vector2((float)x, (float)y);
                //if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0") customData["coordinates"] = new Vector2((float)x, (float)y);

                //Color handling 
                if (colorDict != null && colorDict.Count > 0)
                {
                    copy.SetColor(LerpColorFromDict(colorDict, line));
                    //customData["_color"] = LerpColorFromDict(colorDict, line);
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
                            //customData_old["_cutDirection"] = ang;

                        } else
                        {
                            //Find angle for old object to face new one
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
                            //customData_old["_cutDirection"] = ang;
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
                                //customData_old["_cutDirection"] = 0;
                            }
                        }

                    }
                    catch
                    {

                    }

                }


                //}
                customData["_paul"] = startTime;


                collection.SpawnObject(copy, false, false);


                BaseObject beatmapObject = collection.UnsortedObjects.Last();
                spawnedBeatobjects.Add(beatmapObject);


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
            float originalDistance = distanceInBeats;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BaseObject copy = null;

                copy = (BaseObject)note1.Clone();

                copy.SongBpmTime = (endTime - distanceInBeats);
                if (copy.SongBpmTime > endTime)
                    break;

                collection.SpawnObject(copy, false, false);
                BaseObject beatmapObject = collection.UnsortedObjects.Last();
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

            BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();

            Vector2 n1 = (note1 as BaseNote).GetPosition();
            Vector2 n2 = (note2 as BaseNote).GetPosition();

            float ang = Mathf.Atan2(n2.y - n1.y, n2.x - n1.x) * 180 / Mathf.PI;
            ang += 90;
            float noteRotation = ang;

            float startTime = note1.SongBpmTime;
            float endTime = note2.SongBpmTime;


            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            Vector2 note1pos = (note1 as BaseNote).GetRealPosition();
            Vector2 note2pos = (note2 as BaseNote).GetRealPosition();

            BaseNote oldNote = null;
            int noteIndex = 1;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            while (distanceInBeats > 0 - 1 / (float)precision)
            {
                BaseNote note1Note = note1 as BaseNote;
                BaseNote copy = (BaseNote)note1.Clone();
                copy.CutDirection = 0;

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

                    if (PaulmapperData.Instance.rotateNotes)
                    {
                        (copy as BaseNote).SetRotation(noteRotation);
                    }
                    else if (PaulmapperData.Instance.vibro)
                    {
                        copy.CutDirection = (noteIndex % 2);
                    }

                    customData["_paul"] = startTime;
                }

                    

                beatmapObjectContainerCollection.SpawnObject(copy, false, false);


                BaseObject beatmapObject = beatmapObjectContainerCollection.UnsortedObjects[beatmapObjectContainerCollection.UnsortedObjects.Count - 1];
                spawnedBeatobjects.Add(beatmapObject);


                oldNote = copy;
                distanceInBeats -= 1 / (float)precision;
                noteIndex += 1;
            }

            if ((spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BaseNote).CustomDirection.HasValue)
                (spawnedBeatobjects[spawnedBeatobjects.Count - 1] as BaseNote).SetRotation((spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BaseNote).CustomDirection.Value);

            foreach (BaseObject beatmapObject in new List<BaseObject>() { note1, note2 })
            {
                beatmapObjectContainerCollection.DeleteObject(beatmapObject, false);
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
            typeof(CMInput.ITimelineActions)
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
