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

        public static List<Paul> FindAllPauls(List<BeatmapNote> allNotes)
        {
            List<BeatmapNote> notesLeft = allNotes.Where(n => n.Type == 0).ToList();
            List<BeatmapNote> notesRight = allNotes.Where(n => n.Type == 1).ToList();

            List<Paul> pauls = new List<Paul>();

            List<Paul> leftPauls = FindPauls(notesLeft);
            leftPauls.ForEach(p => p.notes.ForEach(n => n.CustomData["_paul"] = $"{p.notes.First().Time}" ));
            pauls.AddRange(leftPauls);

            List<Paul> rightPauls = FindPauls(notesRight);
            rightPauls.ForEach(p => p.notes.ForEach(n => n.CustomData["_paul"] = $"{p.notes.First().Time}"));
            pauls.AddRange(rightPauls);

            //Debug.LogError("Found: " + pauls.Count + " Pauls");

            return pauls;
        }

        public static List<Paul> FindPauls(List<BeatmapNote> notesOneSide)
        {
            if (notesOneSide.Count <= 0)
                return new List<Paul>();

            //Find closest notes

            BeatmapNote oldNote = notesOneSide[0];

            List<Paul> foundPauls = new List<Paul>();

            bool paul = false;

            List<BeatmapNote> groupedNotes = new List<BeatmapNote>();
            float lastPrecision = 0;

            foreach (BeatmapNote note in notesOneSide)
            {
                if (note.Time != oldNote.Time)
                {
                    float dist = note.Time - oldNote.Time;

                    if (lastPrecision != 0)
                    {

                        if (dist > lastPrecision - 0.01 && dist < lastPrecision + 0.01 && notesOneSide.IndexOf(note) != notesOneSide.Count - 1)
                        {
                            //Is still part of a paul
                            if (!paul)
                            {
                                paul = true;
                                groupedNotes = new List<BeatmapNote>();
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

            PaulMomenter.ats.MoveToTimeInBeats(paul.notes[0].Time);


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


            foreach (BeatmapNote note in paul.notes)
                SelectionController.Select(note, true, true, true);

            currentPaul = pauls.IndexOf(paul);

            lastPaul = paul;
        }

        public static void SelectAllPauls() 
        {
            foreach (Paul paul in pauls) 
            {
                foreach (BeatmapNote note in paul.notes)
                    SelectionController.Select(note, true, true, true);
            }
        }

        public static void KeepFirstNotes()
        {
            
            List<BeatmapNote> notes = pauls.Where(p => p.notes.Any(n => SelectionController.SelectedObjects.Contains(n))).SelectMany(p => p.notes.Skip(1)).ToList();

            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);

            foreach (BeatmapObject beatmapObject in notes)
            {
                collection.DeleteObject(beatmapObject, false);
            }

            BeatmapActionContainer.AddAction(new SelectionDeletedAction(notes));
        }
    }

    public static class PaulMaker
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


        public static List<BeatmapObject> GeneratePoodle(BeatmapObject note1, BeatmapObject note2, int l_precision = 32, bool dots = false)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);

            float startTime = note1.Time;
            float endTime = note2.Time;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BeatmapNote note1Note = note1 as BeatmapNote;
                BeatmapNote copy = null;
                if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
                {
                    if (note1Note.Type == BeatmapNote.NoteTypeBomb)
                    {
                        copy = CopyV3Bomb(note1.ConvertToJson());
                    } else
                    {
                        copy = CopyV3Note(note1.ConvertToJson());
                    }
                }
                else
                {
                    copy = new BeatmapNote(note1Note.Time, note1Note.LineIndex, note1Note.LineLayer, note1Note.Type, note1Note.CutDirection, note1Note.CustomData);
                }

                copy.CustomData = new JSONObject();

                copy.Time = (endTime - distanceInBeats);
                if (copy.Time > endTime)
                    break;

                collection.SpawnObject(copy, false, false);

                BeatmapObject beatmapObject = collection.UnsortedObjects.Last();
                spawnedBeatobjects.Add(beatmapObject);

                distanceInBeats -= 1 / (float)l_precision;
            }

            return spawnedBeatobjects;
        }

        internal static BeatmapNote CopyV3Note(JSONNode data)
        {
            return new BeatmapColorNote(data);
        }

        internal static BeatmapNote CopyV3Bomb(JSONNode data)
        {
            return new BeatmapBombNote(data);
        }

        internal static BeatmapObstacle CopyV3Wall(JSONNode data)
        {
            return new BeatmapObstacleV3(data);
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
        public static List<BeatmapObject> GeneratePoodle(BeatmapObject note1, BeatmapObject note2, Curve splineInterpolatorx, Curve splineInterpolatory, int l_precision = 32, bool dots = false, Dictionary<float, Color> colorDict = null, List<float> dotTime = null, Dictionary<float, float> pointsDir = null)
        {
            //TimeKeeper TGP = new TimeKeeper();
            //TGP.Start();
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);

            float startTime = note1.Time;
            float endTime = note2.Time;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            BeatmapNote oldNote = null;
            int noteIndex = 1;


            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BeatmapNote note1Note = note1 as BeatmapNote;
                BeatmapNote copy = null;
                if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
                {
                    copy = new BeatmapColorNote(note1.ConvertToJson());
                }
                else
                {
                    copy = new BeatmapNote(note1Note.Time, note1Note.LineIndex, note1Note.LineLayer, note1Note.Type, note1Note.CutDirection, note1Note.CustomData);
                }

                copy.Time = (endTime - distanceInBeats);
                if (copy.Time > endTime)
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
                        float closeDotTime = dotTime.OrderBy(d => Mathf.Abs(d - oldNote.Time)).First();
                        if (Mathf.Abs(oldNote.Time - closeDotTime) < 2 * PaulmapperData.Instance.transitionTime)
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


                BeatmapObject beatmapObject = collection.UnsortedObjects.Last();
                spawnedBeatobjects.Add(beatmapObject);


                oldNote = copy;
                distanceInBeats -= 1 / (float)l_precision;

                noteIndex++;
            }

            /*
            if (spawnedBeatobjects[spawnedBeatobjects.Count - 2].CustomData.HasKey("_cutDirection")) 
            {
                if (pointsDir != null)
                    spawnedBeatobjects[spawnedBeatobjects.Count - 1].CustomData["_cutDirection"] = pointsDir.Last().Value;
                else
                    spawnedBeatobjects[spawnedBeatobjects.Count - 1].CustomData["_cutDirection"] = spawnedBeatobjects[spawnedBeatobjects.Count - 2].CustomData["_cutDirection"] + (PaulmapperData.Instance.vibro ? 180 : 0);
            }
                

            else if ((spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BeatmapNote).CutDirection > 1000)
                (spawnedBeatobjects[spawnedBeatobjects.Count - 1] as BeatmapNote).CutDirection = (spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BeatmapNote).CutDirection;

            if (dotTime != null && dotTime.Count > 0 && endTime.ToString() == (dotTime.Last() + PaulmapperData.Instance.transitionTime).ToString())
            {
                (spawnedBeatobjects.Last() as BeatmapNote).CutDirection = 8;
            }*/

            if (spawnedBeatobjects[spawnedBeatobjects.Count - 2].CustomData.HasKey("_cutDirection"))
            {
                if (pointsDir != null)
                {
                    spawnedBeatobjects[spawnedBeatobjects.Count - 1].SetRotation(pointsDir.Last().Value);
                } else
                {
                    spawnedBeatobjects[spawnedBeatobjects.Count - 1].SetRotation(spawnedBeatobjects[spawnedBeatobjects.Count - 2].CustomData["_cutDirection"] + (PaulmapperData.Instance.vibro ? 180 : 0));
                }
            }


            //TGP.Complete("GeneratePoodle");

            return spawnedBeatobjects;
        }

        /// <summary>
        /// Generate normal paul between two notes
        /// </summary>
        /// <param name="note1">First note</param>
        /// <param name="note2">Last note</param>
        /// <param name="l_precision">Cursor precision</param>
        /// <returns></returns>
        public static List<BeatmapObject> GeneratePaul(BeatmapObject note1, BeatmapObject note2, int l_precision)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(note1.BeatmapType);

            float startTime = note1.Time;
            float endTime = note2.Time;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BeatmapObject copy = null;

                if (note1.BeatmapType == BeatmapObject.ObjectType.Note)
                {
                    if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
                    {
                        if ((note1 as BeatmapNote).Type == BeatmapNote.NoteTypeBomb)
                        {
                            copy = CopyV3Bomb(note1.ConvertToJson());
                        }
                        else
                        {
                            copy = CopyV3Note(note1.ConvertToJson());
                        }
                    }
                    else
                    {
                        copy = new BeatmapNote(note1.ConvertToJson());
                    }
                } else if (note1.BeatmapType == BeatmapObject.ObjectType.Obstacle)
                {
                    if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
                    {
                         copy = CopyV3Wall(note1.ConvertToJson());
                    }
                    else
                    {
                        copy = new BeatmapObstacle(note1.ConvertToJson());
                    }
                }



                copy.Time = (endTime - distanceInBeats);
                if (copy.Time > endTime)
                    break;

                collection.SpawnObject(copy, false, false);
                BeatmapObject beatmapObject = collection.UnsortedObjects.Last();
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
        public static void GeneratePoodle(BeatmapObject note1, BeatmapObject note2, string easing = null, int precision = 32)
        {
            Dictionary<float, Color> DistColorDict = new Dictionary<float, Color>();

            if (Helper.TryGetColorFromObject(note1, out Color col1) && Helper.TryGetColorFromObject(note2, out Color col2))
            {
                DistColorDict.Add(0, col1);
                DistColorDict.Add(note2.Time - note1.Time, col2);
            }

            BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();

            Vector2 n1 = (note1 as BeatmapNote).GetPosition();
            Vector2 n2 = (note2 as BeatmapNote).GetPosition();

            float ang = Mathf.Atan2(n2.y - n1.y, n2.x - n1.x) * 180 / Mathf.PI;
            ang += 90;
            float noteRotation = ang;

            float startTime = note1.Time;
            float endTime = note2.Time;


            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            Vector2 note1pos = (note1 as BeatmapNote).GetRealPosition();
            Vector2 note2pos = (note2 as BeatmapNote).GetRealPosition();

            BeatmapNote oldNote = null;
            int noteIndex = 1;

            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            while (distanceInBeats > 0 - 1 / (float)precision)
            {
                BeatmapNote note1Note = note1 as BeatmapNote;
                BeatmapNote copy = new BeatmapNote(note1Note.Time, note1Note.LineIndex, note1Note.LineLayer, note1Note.Type, note1Note.CutDirection, note1Note.CustomData);

                copy.Time = endTime - distanceInBeats;
                if (copy.Time > endTime)
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

                    customData["_position"] = Vector2.Lerp(note1pos, note2pos, line);
                    if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0") customData["coordinates"] = Vector2.Lerp(note1pos, note2pos, line);

                    if (DistColorDict != null && DistColorDict.Count > 0)
                    {
                        customData["_color"] = PaulMaker.LerpColorFromDict(DistColorDict, copy.Time - startTime);
                    }

                    if (PaulmapperData.Instance.rotateNotes)
                    {
                        customData["_cutDirection"] = noteRotation;
                    }
                    else if (PaulmapperData.Instance.vibro)
                    {
                        copy.CutDirection = (noteIndex % 2);
                    }

                    customData["_paul"] = startTime;
                }

                    

                beatmapObjectContainerCollection.SpawnObject(copy, false, false);


                BeatmapObject beatmapObject = beatmapObjectContainerCollection.UnsortedObjects[beatmapObjectContainerCollection.UnsortedObjects.Count - 1];
                spawnedBeatobjects.Add(beatmapObject);


                oldNote = copy;
                distanceInBeats -= 1 / (float)precision;
                noteIndex += 1;
            }

            if (spawnedBeatobjects[spawnedBeatobjects.Count - 2].CustomData.HasKey("_cutDirection"))
                spawnedBeatobjects[spawnedBeatobjects.Count - 1].CustomData["_cutDirection"] = spawnedBeatobjects[spawnedBeatobjects.Count - 2].CustomData["_cutDirection"];

            foreach (BeatmapObject beatmapObject in new List<BeatmapObject>() { note1, note2 })
            {
                beatmapObjectContainerCollection.DeleteObject(beatmapObject, false);
            }

            BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedBeatobjects, new List<BeatmapObject>() { note1, note2 }));

            foreach (BeatmapObject note in spawnedBeatobjects)
            {
                SelectionController.Select(note, true, true, false);
            }
            //beatmapObjectContainerCollection.DeleteObject(note2);

            
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
