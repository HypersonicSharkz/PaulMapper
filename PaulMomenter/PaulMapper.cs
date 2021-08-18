using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;
using Extreme.Mathematics.Curves;
using System.Data;
using Discord;
using PaulMapper.PaulHelper;

namespace PaulMapper
{
    [Plugin("PaulMapper")]
    public class Plugin
    {
        public static PaulMomenter momenter;

        [Init]
        private void Init()
        {
            Debug.LogError("PaulMapper V0.3 - Loaded");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.buildIndex == 3) //Mapper scene 
            {
                PaulFinder.pauls = new List<Paul>();

                if (momenter != null && momenter.isActiveAndEnabled)
                    return;

                momenter = new GameObject("PaulMomenter").AddComponent<PaulMomenter>();
            }

        }

    }


    public class PaulMomenter : MonoBehaviour
    {
        public bool showGUI;
        public int precision = 32;
        public bool vibro = false;
        public bool rotateNotes = true;
        public bool useMappingExtensions = false;

        public float min = 0.5f;
        public float max = 1.5f;

        public static AudioTimeSyncController ats;
        public static NotesContainer notesContainer;


        private void Start()
        {
            ats = BeatmapObjectContainerCollection.GetCollectionForType(0).AudioTimeSyncController;
            notesContainer = UnityEngine.Object.FindObjectOfType<NotesContainer>();
        }

        public static float guiX = 200;
        public static float guiWidth = 140;

        public static Rect windowRect = new Rect(guiX, 10, guiWidth, 440);

        void DoMyWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUI.Label(new Rect((guiWidth - 110) / 2, 40, 80, 20), "Precision: ");

            GUI.SetNextControlName("Precision");
            string text = GUI.TextField(new Rect(80 + (guiWidth - 110) / 2, 40, 30, 20), $"{precision}");

            vibro = GUI.Toggle(new Rect(5, 80, guiWidth / 2 - 10, 20), vibro, "Vibro");
            rotateNotes = GUI.Toggle(new Rect(guiWidth / 2 + 5, 80, guiWidth / 2 - 10, 20), rotateNotes, "Rotate");


            if (GUI.Button(new Rect(5, 130, guiWidth - 10, 30), "Mirror"))
            {
                MirrorSelected();
            }


            if (GUI.Button(new Rect(5, 220, guiWidth - 10, 20), "Find All Pauls"))
            {
                List<BeatmapNote> allNotes = (from BeatmapNote it in notesContainer.LoadedObjects
                                              orderby it._time
                                              select it).ToList();

                PaulFinder.pauls = PaulFinder.FindAllPauls(allNotes).OrderBy(p => p.Beat).ToList();
            }

            if (PaulFinder.pauls.Count > 0)
            {
                if (GUI.Button(new Rect(5, 250, guiWidth - 10, 20), "GoTo Paul"))
                {
                    PersistentUI.Instance.ShowInputBox("Go to paul", (Action<string>)delegate (string result)
                    {
                        int paulNumber = 0;
                        if (int.TryParse(result, out paulNumber))
                        {
                            PaulFinder.GoToPaul(PaulFinder.pauls[paulNumber - 1]);
                        }

                    }, "0");
                }


                try
                {
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(0, 290, guiWidth, 20), $"{PaulFinder.currentPaul + 1}/{PaulFinder.pauls.Count}", style);

                    if (GUI.Button(new Rect(5, 290, 40, 20), "<"))
                    {
                        //Go to last paul

                        Paul paul = PaulFinder.pauls.Last(p => p.notes[0]._time < ats.CurrentBeat);

                        PaulFinder.GoToPaul(paul);
                    }



                    if (GUI.Button(new Rect(guiWidth - (5 + 40), 290, 40, 20), ">"))
                    {
                        //Go to next paul
                        Paul paul = PaulFinder.pauls.First(p => p.notes[0]._time > ats.CurrentBeat);

                        PaulFinder.GoToPaul(paul);
                    }

                    if (GUI.Button(new Rect(5, 320, guiWidth - 10, 20), "Select Current"))
                    {
                        PaulFinder.SelectCurrentPaul();
                    }
                }
                catch
                {

                }
            }

            if (!int.TryParse(text, out precision))
                return;

        }

        private bool isHovering;

        private readonly Type[] actionMaps = new Type[]
        {
            typeof(CMInput.ICameraActions),
            typeof(CMInput.IBeatmapObjectsActions),
            typeof(CMInput.INodeEditorActions),
            typeof(CMInput.ISavingActions),
            typeof(CMInput.ITimelineActions)
        };

        private Type[] actionMapsDisabled
        {
            get
            {
                return (from x in typeof(CMInput).GetNestedTypes()
                        where x.IsInterface && !actionMaps.Contains(x)
                        select x).ToArray<Type>();
            }
        }

        private void OnGUI()
        {
            if (showGUI)
            {
                
                windowRect = GUI.Window(0, windowRect, DoMyWindow, "Paul Menu");
                //GUI.Box(new Rect(guiX, 10, guiWidth, 440), "Paul Menu");
                
                if (windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)) ||
                    GUI.GetNameOfFocusedControl() == "Precision")
                {
                    if (!isHovering)
                    {
                        isHovering = true;
                        CMInputCallbackInstaller.DisableActionMaps(typeof(PaulMomenter), actionMapsDisabled);
                    }
                } else
                {
                    if (isHovering)
                    {
                        isHovering = false;
                        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(PaulMomenter), actionMapsDisabled);
                    }
                }

                //useMappingExtensions = GUI.Toggle(new Rect(guiX, 120, 80, 20), useMappingExtensions, "Mapping Ext.");

                if (SelectionController.SelectedObjects.Count == 2 && SelectionController.SelectedObjects.All(s => s.beatmapType == BeatmapObject.Type.NOTE))
                {
                    BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o._time).ToArray();

                    GUI.Box(new Rect(windowRect.x + guiWidth, windowRect.y, guiWidth, windowRect.height), "Quick Menu");
                    float xPos = windowRect.x + guiWidth + 10;
                    float yPos = windowRect.y + 30;

                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "Linear"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], null, precision);
                    }

                    yPos += 30;

                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "ExpIn"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "ExpIn", precision);
                    }

                    yPos += 30;

                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "ExpOut"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "ExpOut", precision);
                    }

                    yPos += 30;

                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "ExpInOut"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "ExpInOut", precision);
                    }

                    yPos += 30;

                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "CubicIn"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "CubicIn", precision);
                    }
                    yPos += 30;
                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "CubicOut"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "CubicOut", precision);
                    }
                    yPos += 30;
                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "CubicInOut"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "CubicInOut", precision);
                    }

                    yPos += 30;
                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "easeInBack"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInBack", precision);
                    }
                    yPos += 30;
                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "easeOutBack"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutBack", precision);
                    }
                    yPos += 30;
                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "easeInOutBack"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutBack", precision);
                    }

                    yPos += 30;
                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "easeInBounce"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInBounce", precision);
                    }
                    yPos += 30;
                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "easeOutBounce"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutBounce", precision);
                    }
                    yPos += 30;
                    if (GUI.Button(new Rect(xPos, yPos, 120, 20), "easeInOutBounce"))
                    {
                        GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutBounce", precision);
                    }
                }
            }
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                showGUI = !showGUI;
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o._time).ToArray();

                foreach (BeatmapObject beatmapObject in beatmapObjects)
                {
                    float rand = UnityEngine.Random.Range(min, max);
                    if (beatmapObject._customData != null && beatmapObject._customData.HasKey("_color"))
                    {
                        beatmapObject._customData["_color"] = new Color(beatmapObject._customData["_color"][0] * rand, beatmapObject._customData["_color"][1] * rand, beatmapObject._customData["_color"][2] * rand, beatmapObject._customData["_color"][3]);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                //TimeKeeper TP = new TimeKeeper();
                //TP.Start();

                BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o._time).ToArray();
                BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();
                List<BeatmapObject> spawnedNotes = new List<BeatmapObject>();

                //Normal paul
                if (beatmapObjects.All(o => !((o as BeatmapNote)._customData != null && (o as BeatmapNote)._customData.HasKey("_position")) &&
                                            (o as BeatmapNote)._cutDirection == (beatmapObjects[0] as BeatmapNote)._cutDirection &&
                                            (o as BeatmapNote)._lineLayer == (beatmapObjects[0] as BeatmapNote)._lineLayer &&
                                            (o as BeatmapNote)._lineIndex == (beatmapObjects[0] as BeatmapNote)._lineIndex
                ))
                {
                    spawnedNotes = GeneratePaul(beatmapObjects[0], beatmapObjects.Last(), precision);

                } 
                else
                {
                    //A poodle
                    List<double> pointsx = new List<double>();

                    List<double> pointsx_y = new List<double>();
                    List<double> pointsy_y = new List<double>();

                    float endTime = beatmapObjects.Last()._time;
                    float totalTime = beatmapObjects.Last()._time - beatmapObjects[0]._time;

                    Dictionary<float, Color> DistColorDict = new Dictionary<float, Color>();

                    foreach (BeatmapObject beatmapObject in beatmapObjects)
                    {
                        float startTime = beatmapObject._time;

                        float distanceInBeats = totalTime - (endTime - startTime);

                        pointsx.Add(distanceInBeats);

                        pointsx_y.Add((beatmapObject as BeatmapNote).GetRealPosition().x);

                        pointsy_y.Add((beatmapObject as BeatmapNote).GetRealPosition().y);


                        Color color = Color.clear;
                        if (Helper.TryGetColorFromObject(beatmapObject, out color))
                        {
                            DistColorDict.Add(distanceInBeats, color);
                        }

                    }

                    CubicSpline splinex = CubicSpline.CreateNatural(pointsx, pointsx_y);
                    CubicSpline spliney = CubicSpline.CreateNatural(pointsx, pointsy_y);

                    spawnedNotes = GeneratePoodle(beatmapObjects[0], beatmapObjects.Last(), splinex, spliney, precision, beatmapObjects.All(o => (o as BeatmapNote)._cutDirection == 8), DistColorDict);
                }


                foreach (BeatmapObject beatmapObject in beatmapObjects)
                {
                    beatmapObjectContainerCollection.DeleteObject(beatmapObject, false);
                }

                BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedNotes, beatmapObjects));

                foreach (BeatmapObject note in spawnedNotes)
                {
                    SelectionController.Select(note, true);
                }

                //TP.Complete("Paul");

            }
        }

        private Dictionary<int, int> CutDirectionToMirrored = new Dictionary<int, int>
        {
            {
                6,
                7
            },
            {
                7,
                6
            },
            {
                4,
                5
            },
            {
                5,
                4
            },
            {
                3,
                2
            },
            {
                2,
                3
            }
        };
        public void MirrorSelected()
        {
            List<BeatmapAction> allActions = new List<BeatmapAction>();
            foreach (BeatmapObject con in SelectionController.SelectedObjects)
            {
                BeatmapObject original = BeatmapObject.GenerateCopy<BeatmapObject>(con);
                BeatmapObstacle obstacle;
                if ((obstacle = (con as BeatmapObstacle)) != null)
                {
                    bool precisionWidth = obstacle._width >= 1000;
                    int __state = obstacle._lineIndex;

                    if (obstacle._customData != null)
                    {
                        if (obstacle._customData.HasKey("_position"))
                        {
                            Vector2 oldPosition = obstacle._customData["_position"];
                            Vector2 flipped = new Vector2(oldPosition.x * -1f, oldPosition.y);
                            
                            if (obstacle._customData.HasKey("_scale"))
                            {
                                Vector2 scale = obstacle._customData["_scale"];
                                flipped.x -= scale.x;
                            }
                            else
                            {
                                flipped.x -= (float)obstacle._width;
                            }
                            obstacle._customData["_position"] = flipped;
                        }
                    }
                    else
                    {
                        bool flag6 = __state >= 1000 || __state <= -1000 || precisionWidth;
                        if (flag6)
                        {
                            int newIndex = __state;
                            bool flag7 = newIndex <= -1000;
                            if (flag7)
                            {
                                newIndex += 1000;
                            }
                            else
                            {
                                bool flag8 = newIndex >= 1000;
                                if (flag8)
                                {
                                    newIndex -= 1000;
                                }
                                else
                                {
                                    newIndex *= 1000;
                                }
                            }
                            newIndex = (newIndex - 2000) * -1 + 2000;
                            int newWidth = obstacle._width;
                            bool flag9 = newWidth < 1000;
                            if (flag9)
                            {
                                newWidth *= 1000;
                            }
                            else
                            {
                                newWidth -= 1000;
                            }
                            newIndex -= newWidth;
                            bool flag10 = newIndex < 0;
                            if (flag10)
                            {
                                newIndex -= 1000;
                            }
                            else
                            {
                                newIndex += 1000;
                            }
                            obstacle._lineIndex = newIndex;
                        }
                        else
                        {
                            int mirrorLane = (__state - 2) * -1 + 2;
                            obstacle._lineIndex = mirrorLane - obstacle._width;
                        }
                    }
                }
                else
                {
                    BeatmapNote note;
                    bool flag11 = (note = (con as BeatmapNote)) != null;
                    if (flag11)
                    {
                        bool flag12 = note._customData != null;
                        if (flag12)
                        {
                            bool flag13 = note._customData.HasKey("_position");
                            if (flag13)
                            {
                                Vector2 oldPosition2 = note._customData["_position"];
                                Vector2 flipped2 = new Vector2((oldPosition2.x + 0.5f) * -1f - 0.5f, oldPosition2.y);
                                note._customData["_position"] = flipped2;
                            }
                        }
                        else
                        {
                            int __state2 = note._lineIndex;
                            bool flag14 = __state2 > 3 || __state2 < 0;
                            if (flag14)
                            {
                                int newIndex2 = __state2;
                                bool flag15 = newIndex2 <= -1000;
                                if (flag15)
                                {
                                    newIndex2 += 1000;
                                }
                                else
                                {
                                    bool flag16 = newIndex2 >= 1000;
                                    if (flag16)
                                    {
                                        newIndex2 -= 1000;
                                    }
                                }
                                newIndex2 = (newIndex2 - 1500) * -1 + 1500;
                                bool flag17 = newIndex2 < 0;
                                if (flag17)
                                {
                                    newIndex2 -= 1000;
                                }
                                else
                                {
                                    newIndex2 += 1000;
                                }
                                note._lineIndex = newIndex2;
                            }
                            else
                            {
                                int mirrorLane2 = (int)(((float)__state2 - 1.5f) * -1f + 1.5f);
                                note._lineIndex = mirrorLane2;
                            }
                        }
                        if (note._type != 3)
                        {
                            note._type = ((note._type == 0) ? 1 : 0);
                            if (note._customData != null && note._customData.HasKey("_cutDirection"))
                            {

                                note._customData["_cutDirection"] = -note._customData["_cutDirection"].AsFloat;

                            } else
                            {
                                if (this.CutDirectionToMirrored.ContainsKey(note._cutDirection))
                                {
                                    note._cutDirection = this.CutDirectionToMirrored[note._cutDirection];
                                }
                            }

                        }
                    }
                }

                allActions.Add(new BeatmapObjectModifiedAction(con, con, original, "e", true));
            }
            foreach (BeatmapObject unique in SelectionController.SelectedObjects.DistinctBy(x => x.beatmapType))
            {
                BeatmapObjectContainerCollection.GetCollectionForType(unique.beatmapType).RefreshPool(true);
            }
            BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true, "Mirrored a selection of objects."));

        }

        public class TimeKeeper
        {
            public DateTime StartTime;
            public void Start() => StartTime = DateTime.Now;
            public void Complete(string action) => Debug.LogError($"{action} Completed in {(DateTime.Now - StartTime).TotalSeconds} seconds");
        }

        private Color LerpColorFromDict(Dictionary<float, Color> colorDict, float dist)
        {
            if (colorDict.ContainsKey(dist))
            {
                Color lerpedColor;
                if (colorDict.TryGetValue(dist, out lerpedColor))
                    return lerpedColor;
            }
                
            for (int i = 0; i < colorDict.Count - 1; i++)
            {
                if (dist > colorDict.ToList()[i].Key && dist < colorDict.ToList()[i+1].Key)
                {
                    Color colorStart = colorDict.ToList()[i].Value;
                    Color colorEnd = colorDict.ToList()[i+1].Value;

                    float startDist = colorDict.ToList()[i].Key;
                    float endDist = colorDict.ToList()[i+1].Key - startDist;

                    float t = (dist - startDist) / endDist;
                    Color lerpedColor = Color.Lerp(colorStart, colorEnd, t);

                    return lerpedColor;
                }

                
            }
            return Color.white;
        }

        private List<BeatmapObject> GeneratePaul(BeatmapObject note1, BeatmapObject note2, int l_precision)
        {
            BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();

            float startTime = note1._time;
            float endTime = note2._time;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BeatmapNote copy = new BeatmapNote(note1.ConvertToJSON());
                copy._time = (endTime - distanceInBeats);


                beatmapObjectContainerCollection.SpawnObject(copy, false, false);
                BeatmapObject beatmapObject = beatmapObjectContainerCollection.UnsortedObjects[beatmapObjectContainerCollection.UnsortedObjects.Count - 1];
                spawnedBeatobjects.Add(beatmapObject);


                distanceInBeats -= 1 / (float)l_precision;
            }

            return spawnedBeatobjects;
        }

        private List<BeatmapObject> GeneratePoodle(BeatmapObject note1, BeatmapObject note2, Curve splineInterpolatorx, Curve splineInterpolatory, int l_precision = 32, bool dots = false, Dictionary<float,Color> colorDict = null)
        {
            //TimeKeeper TGP = new TimeKeeper();
            //TGP.Start();
            BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();


            float startTime = note1._time;
            float endTime = note2._time;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            BeatmapNote oldNote = null;
            int noteIndex = 1;

            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BeatmapNote copy = new BeatmapNote(note1.ConvertToJSON());
                copy._time = (endTime - distanceInBeats);

                float line = (originalDistance - distanceInBeats);

                var x = splineInterpolatorx.ValueAt(line);
                var y = splineInterpolatory.ValueAt(line);

                if (useMappingExtensions)
                {
                    if (x + 3 < 1)
                        copy._lineIndex = (int)Math.Floor(1000 + 1000 * x);
                    else
                        copy._lineIndex = (int)Math.Floor((x + 3) * 1000);




                    if (y + 1 < 1)
                        copy._lineLayer = (int)Math.Floor(-3000 + 1000 * y);
                    else
                        copy._lineLayer = (int)Math.Floor((y + 1) * 1000);



                    if (rotateNotes)
                    {
                        //Fix rotation
                        if (oldNote != null)
                        {
                            float line_old = (originalDistance - (distanceInBeats + 1 / (float)l_precision));

                            var x_old = splineInterpolatorx.ValueAt(line_old);
                            var y_old = splineInterpolatory.ValueAt(line_old);

                            //Find angle for old object to face new one
                            Vector2 op = new Vector2((float)x_old, (float)y_old);
                            Vector2 cp = new Vector2((float)x, (float)y);
                            float ang = Mathf.Atan2(cp.y - op.y, cp.x - op.x) * -180 / Mathf.PI;
                            ang += 270;


                            //Set rotation
                            oldNote._cutDirection = (int)(1000 + ang);

                        }
                    }
                    else if (vibro)
                    {
                        oldNote._cutDirection = (noteIndex % 2);
                    }

                } 
                else
                {
                    copy._customData = new JSONObject();
                    JSONNode customData = copy._customData;

                    customData["_position"] = new Vector2((float)x, (float)y);


                    //Color handling 
                    if (colorDict != null && colorDict.Count > 0)
                    {
                        customData["_color"] = LerpColorFromDict(colorDict, line);
                    }

                    if (dots)
                    {
                        copy._cutDirection = 8;
                    }

                    if (rotateNotes)
                    {
                        //Fix rotation
                        if (oldNote != null)
                        {
                            //Find angle for old object to face new one
                            Vector2 op = oldNote.GetPosition();
                            Vector2 cp = copy.GetPosition();
                            float ang = Mathf.Atan2(cp.y - op.y, cp.x - op.x) * 180 / Mathf.PI;
                            ang += 90;


                            //Set rotation
                            JSONNode customData_old = oldNote._customData;
                            customData_old["_cutDirection"] = ang;

                        }
                    }
                    else if (vibro)
                    {
                        copy._cutDirection = (noteIndex % 2);
                    }
                }


                beatmapObjectContainerCollection.SpawnObject(copy, false, false);


                BeatmapObject beatmapObject = beatmapObjectContainerCollection.UnsortedObjects[beatmapObjectContainerCollection.UnsortedObjects.Count - 1];
                spawnedBeatobjects.Add(beatmapObject);


                oldNote = copy;
                distanceInBeats -= 1 / (float)l_precision;

                noteIndex++;

            }

            if (spawnedBeatobjects[spawnedBeatobjects.Count - 2]._customData.HasKey("_cutDirection"))
                spawnedBeatobjects[spawnedBeatobjects.Count - 1]._customData["_cutDirection"] = spawnedBeatobjects[spawnedBeatobjects.Count - 2]._customData["_cutDirection"];

            else if ((spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BeatmapNote)._cutDirection > 1000)
                (spawnedBeatobjects[spawnedBeatobjects.Count - 1] as BeatmapNote)._cutDirection = (spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BeatmapNote)._cutDirection;

            //TGP.Complete("GeneratePoodle");

            return spawnedBeatobjects;
        }

        private void GeneratePoodle(BeatmapObject note1, BeatmapObject note2, string easing = null, int precision = 32)
        {

            if ((note1 as BeatmapNote)._cutDirection == (note2 as BeatmapNote)._cutDirection)
            { 
                BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();
                


                float startTime = note1._time;
                float endTime = note2._time;


                float distanceInBeats = endTime - startTime;
                float originalDistance = distanceInBeats;

                Vector2 note1pos = (note1 as BeatmapNote).GetRealPosition();
                Vector2 note2pos = (note2 as BeatmapNote).GetRealPosition();

                List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

                while (distanceInBeats > 0)
                {
                    BeatmapNote copy = new BeatmapNote(note1.ConvertToJSON());
                    copy._time = endTime - distanceInBeats;

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
                            }
                        }
                        
                        JSONNode customData = copy.GetOrCreateCustomData();
                        customData["_position"] = Vector2.Lerp(note1pos, note2pos, line);
                    }

                    beatmapObjectContainerCollection.SpawnObject(copy, false, false);


                    BeatmapObject beatmapObject = beatmapObjectContainerCollection.UnsortedObjects[beatmapObjectContainerCollection.UnsortedObjects.Count - 1];
                    spawnedBeatobjects.Add(beatmapObject);

                    
                    
                    distanceInBeats -= 1 / (float)precision;
                }


                foreach (BeatmapObject beatmapObject in new List<BeatmapObject>() { note1, note2} )
                {
                    beatmapObjectContainerCollection.DeleteObject(beatmapObject, false);
                }

                BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedBeatobjects, new List<BeatmapObject>() { note1, note2 }));

                foreach (BeatmapObject note in spawnedBeatobjects)
                {
                    SelectionController.Select(note, true);
                }
                //beatmapObjectContainerCollection.DeleteObject(note2);


            }
        }
    }
}
