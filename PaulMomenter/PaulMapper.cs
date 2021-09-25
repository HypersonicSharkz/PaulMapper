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
using System.IO;

namespace PaulMapper
{
    [Plugin("PaulMapper")]
    public class Plugin
    {
        public static PaulMomenter momenter;

        [Init]
        private void Init()
        {
            //Debug.LogError("PaulMapper V0.3 - Loaded");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            
        }

        [Exit]
        private void Exit()
        {
            momenter?.paulmapperData?.SaveData();
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

        public float min = 0.5f;
        public float max = 1.5f;

        public static AudioTimeSyncController ats;
        public static NotesContainer notesContainer;

        public PaulmapperData paulmapperData;

        private void Start()
        {
            ats = BeatmapObjectContainerCollection.GetCollectionForType(0).AudioTimeSyncController;
            notesContainer = UnityEngine.Object.FindObjectOfType<NotesContainer>();


            paulmapperData = PaulmapperData.GetSaveData();

            try
            {
                windowRect = paulmapperData.windowRect.getRect();
            } catch
            {
                paulmapperData.windowRect = new SerializableRect(windowRect);
                paulmapperData.SaveData();
            }
            
        }

        public static float guiX = 200;
        public static float guiWidth = 140;

        public static Rect windowRect = new Rect(guiX, 10, guiWidth, 440);

        void UpdatePaulMapperWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUI.Label(new Rect((guiWidth - 110) / 2, 40, 80, 20), "Precision: ");

            GUI.SetNextControlName("Precision");
            string text = GUI.TextField(new Rect(80 + (guiWidth - 110) / 2, 40, 30, 20), $"{paulmapperData.precision}");

            paulmapperData.vibro = GUI.Toggle(new Rect(5, 80, guiWidth / 2 - 10, 20), paulmapperData.vibro, "Vibro");
            paulmapperData.rotateNotes = GUI.Toggle(new Rect(guiWidth / 2 + 5, 80, guiWidth / 2 - 10, 20), paulmapperData.rotateNotes, "Rotate");


            if (GUI.Button(new Rect(5, 130, guiWidth - 10, 30), "Mirror"))
            {
                MirrorSelected();
            }

            GUI.Label(new Rect((guiWidth - 110) / 2, 170, 80, 20), "Transition time: ");

            GUI.SetNextControlName("Precision");
            string tt = GUI.TextField(new Rect(80 + (guiWidth - 110) / 2, 170, 30, 20), $"{paulmapperData.transitionTime.ToString("0.0")}");

            paulmapperData.transitionRotation = GUI.Toggle(new Rect(20, 190, guiWidth - 10, 20), paulmapperData.transitionRotation, "Keep Rotation");




            if (GUI.Button(new Rect(5, 220, guiWidth - 10, 20), "Find All Pauls"))
            {
                List<BeatmapNote> allNotes = (from BeatmapNote it in notesContainer.LoadedObjects
                                              orderby it.Time
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

                        try
                        {
                            Paul paul = PaulFinder.pauls.Last(p => p.Beat < ats.CurrentBeat);

                            PaulFinder.GoToPaul(paul);
                        }
                        catch
                        {
                            PaulFinder.GoToPaul(PaulFinder.pauls.Last());
                        }
                    }



                    if (GUI.Button(new Rect(guiWidth - (5 + 40), 290, 40, 20), ">"))
                    {
                        //Go to next paul
                        try
                        {
                            Paul paul = PaulFinder.pauls.First(p => p.Beat > ats.CurrentBeat);
                            PaulFinder.GoToPaul(paul);
                        }
                        catch
                        {
                            PaulFinder.GoToPaul(PaulFinder.pauls.First());
                        }
                            
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

            if (!int.TryParse(text, out paulmapperData.precision))
                return;

            if (!float.TryParse(tt, out paulmapperData.transitionTime))
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

        public bool advancedQuickMenu = false;

        private void OnGUI()
        {
            if (showGUI)
            {
                Rect newWindowRect = GUI.Window(0, windowRect, UpdatePaulMapperWindow, "Paul Menu");

                if (newWindowRect.x > 0 &&
                    newWindowRect.x < Screen.width - guiWidth)
                {
                    windowRect.x = newWindowRect.x;
                }

                if (
                    newWindowRect.y > 0 &&
                    newWindowRect.y < Screen.height - 440)
                {
                    windowRect.y = newWindowRect.y;
                }
                

                paulmapperData.windowRect.setRect(windowRect);

                //GUI.Box(new Rect(guiX, 10, guiWidth, 440), "Paul Menu");

                Rect windowTotalRect = new Rect(windowRect);


                //useMappingExtensions = GUI.Toggle(new Rect(guiX, 120, 80, 20), useMappingExtensions, "Mapping Ext.");

                
                if (SelectionController.SelectedObjects.Count == 2 && SelectionController.SelectedObjects.All(s => s.BeatmapType == BeatmapObject.ObjectType.Note))
                {
                    BeatmapNote beatmapObject1 = SelectionController.SelectedObjects.First() as BeatmapNote;
                    BeatmapNote beatmapObject2 = SelectionController.SelectedObjects.Last() as BeatmapNote;
                    if ( (beatmapObject1.CutDirection == beatmapObject2.CutDirection || paulmapperData.rotateNotes) && beatmapObject1.Time != beatmapObject2.Time)
                    {
                        windowTotalRect.width += guiWidth;

                        BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o.Time).ToArray();

                        GUI.Box(new Rect(windowRect.x + guiWidth, windowRect.y, guiWidth, windowRect.height), "Quick Menu");
                        float xPos = windowRect.x + guiWidth + 10;
                        float yPos = windowRect.y + 30;

                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "Linear"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], null, paulmapperData.precision);
                        }

                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "SineIn"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInSine", paulmapperData.precision);
                        }

                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "SineOut"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutSine", paulmapperData.precision);
                        }

                        if (advancedQuickMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "SineInOut"))
                            {
                                GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutSine", paulmapperData.precision);
                            }
                        }




                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "QuadIn"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInQuad", paulmapperData.precision);
                        }

                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "QuadOut"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutQuad", paulmapperData.precision);
                        }

                        if (advancedQuickMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "QuadInOut"))
                            {
                                GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutQuad", paulmapperData.precision);
                            }
                        }




                        yPos += 30;

                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "CubicIn"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "CubicIn", paulmapperData.precision);
                        }
                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "CubicOut"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "CubicOut", paulmapperData.precision);
                        }

                        if (advancedQuickMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "CubicInOut"))
                            {
                                GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "CubicInOut", paulmapperData.precision);
                            }
                        }

                        yPos += 30;

                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "ExpIn"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "ExpIn", paulmapperData.precision);
                        }

                        yPos += 30;

                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "ExpOut"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "ExpOut", paulmapperData.precision);
                        }

                        if (advancedQuickMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "ExpInOut"))
                            {
                                GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "ExpInOut", paulmapperData.precision);
                            }
                        }

                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BackIn"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInBack", paulmapperData.precision);
                        }
                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BackOut"))
                        {
                            GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutBack", paulmapperData.precision);
                        }

                        if (advancedQuickMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BackInOut"))
                            {
                                GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutBack", paulmapperData.precision);
                            }
                        }

                        if (advancedQuickMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BounceIn"))
                            {
                                GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInBounce", paulmapperData.precision);
                            }
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BounceOut"))
                            {
                                GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutBounce", paulmapperData.precision);
                            }
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BounceInOut"))
                            {
                                GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutBounce", paulmapperData.precision);
                            }
                        }

                        yPos += 60;

                        //advancedQuickMenu = GUI.Toggle(new Rect(xPos, yPos, 80, 20), advancedQuickMenu, "Advanced");

                    }
                }

                if (windowTotalRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)) ||
                    GUI.GetNameOfFocusedControl() == "Precision")
                {
                    if (!isHovering)
                    {
                        isHovering = true;
                        CMInputCallbackInstaller.DisableActionMaps(typeof(PaulMomenter), actionMapsDisabled);
                    }
                }
                else
                {
                    if (isHovering)
                    {
                        isHovering = false;
                        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(PaulMomenter), actionMapsDisabled);
                    }
                }
            }
        }

        private void OnDisable()
        {
            paulmapperData.SaveData();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                showGUI = !showGUI;
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                //TimeKeeper TP = new TimeKeeper();
                //TP.Start();

                BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o.Time).ToArray();
                BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();
                List<BeatmapObject> spawnedNotes = new List<BeatmapObject>();

                if (beatmapObjects.Count() != beatmapObjects.Select(p => p.Time).Distinct().Count())
                    return;

                //Normal paul
                if (beatmapObjects.All(o => !((o as BeatmapNote).CustomData != null && (o as BeatmapNote).CustomData.HasKey("_position")) &&
                                            (o as BeatmapNote).CutDirection == (beatmapObjects[0] as BeatmapNote).CutDirection &&
                                            (o as BeatmapNote).LineLayer == (beatmapObjects[0] as BeatmapNote).LineLayer &&
                                            (o as BeatmapNote).LineIndex == (beatmapObjects[0] as BeatmapNote).LineIndex
                ))
                {
                    spawnedNotes = GeneratePaul(beatmapObjects[0], beatmapObjects.Last(), paulmapperData.precision);

                } 
                else
                {
                    //A poodle
                    List<double> pointsx = new List<double>();

                    List<double> pointsx_y = new List<double>();
                    List<double> pointsy_y = new List<double>();

                    float endTime = beatmapObjects.Last().Time;
                    float totalTime = beatmapObjects.Last().Time - beatmapObjects[0].Time;

                    Dictionary<float, Color> DistColorDict = new Dictionary<float, Color>();

                    foreach (BeatmapObject beatmapObject in beatmapObjects)
                    {
                        float startTime = beatmapObject.Time;

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

                    List<float> dotTimes = null;
                    if (paulmapperData.autoDot)
                    {
                        dotTimes = beatmapObjects.Where(p => (p as BeatmapNote).CutDirection == 8).Select(p => p.Time - paulmapperData.transitionTime).ToList();
                    }

                    CubicSpline splinex = CubicSpline.CreateNatural(pointsx, pointsx_y);
                    CubicSpline spliney = CubicSpline.CreateNatural(pointsx, pointsy_y);

                    spawnedNotes = GeneratePoodle(beatmapObjects[0], beatmapObjects.Last(), splinex, spliney, paulmapperData.precision, beatmapObjects.All(o => (o as BeatmapNote).CutDirection == 8), DistColorDict, dotTimes);
                }


                foreach (BeatmapObject beatmapObject in beatmapObjects)
                {
                    beatmapObjectContainerCollection.DeleteObject(beatmapObject, false);
                }

                BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedNotes, beatmapObjects));

                foreach (BeatmapObject note in spawnedNotes)
                {
                    SelectionController.Select(note, true, true, false);
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
                    bool precisionWidth = obstacle.Width >= 1000;
                    int __state = obstacle.LineIndex;

                    if (obstacle.CustomData != null)
                    {
                        if (obstacle.CustomData.HasKey("_position"))
                        {
                            Vector2 oldPosition = obstacle.CustomData["_position"];
                            Vector2 flipped = new Vector2(oldPosition.x * -1f, oldPosition.y);
                            
                            if (obstacle.CustomData.HasKey("_scale"))
                            {
                                Vector2 scale = obstacle.CustomData["_scale"];
                                flipped.x -= scale.x;
                            }
                            else
                            {
                                flipped.x -= (float)obstacle.Width;
                            }
                            obstacle.CustomData["_position"] = flipped;
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
                            int newWidth = obstacle.Width;
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
                            obstacle.LineIndex = newIndex;
                        }
                        else
                        {
                            int mirrorLane = (__state - 2) * -1 + 2;
                            obstacle.LineIndex = mirrorLane - obstacle.Width;
                        }
                    }
                }
                else
                {
                    BeatmapNote note;
                    bool flag11 = (note = (con as BeatmapNote)) != null;
                    if (flag11)
                    {
                        bool flag12 = note.CustomData != null;
                        if (flag12)
                        {
                            bool flag13 = note.CustomData.HasKey("_position");
                            if (flag13)
                            {
                                Vector2 oldPosition2 = note.CustomData["_position"];
                                Vector2 flipped2 = new Vector2((oldPosition2.x + 0.5f) * -1f - 0.5f, oldPosition2.y);
                                note.CustomData["_position"] = flipped2;
                            }
                        }
                        else
                        {
                            int __state2 = note.LineIndex;
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
                                note.LineIndex = newIndex2;
                            }
                            else
                            {
                                int mirrorLane2 = (int)(((float)__state2 - 1.5f) * -1f + 1.5f);
                                note.LineIndex = mirrorLane2;
                            }
                        }
                        if (note.Type != 3)
                        {
                            note.Type = ((note.Type == 0) ? 1 : 0);
                            if (note.CustomData != null && note.CustomData.HasKey("_cutDirection"))
                            {

                                note.CustomData["_cutDirection"] = -note.CustomData["_cutDirection"].AsFloat;

                            } else
                            {
                                if (this.CutDirectionToMirrored.ContainsKey(note.CutDirection))
                                {
                                    note.CutDirection = this.CutDirectionToMirrored[note.CutDirection];
                                }
                            }

                        }
                    }
                }

                allActions.Add(new BeatmapObjectModifiedAction(con, con, original, "e", true));
            }
            foreach (BeatmapObject unique in SelectionController.SelectedObjects.DistinctBy(x => x.BeatmapType))
            {
                BeatmapObjectContainerCollection.GetCollectionForType(unique.BeatmapType).RefreshPool(true);
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

            float startTime = note1.Time;
            float endTime = note2.Time;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BeatmapNote copy = new BeatmapNote(note1.ConvertToJson());
                copy.Time = (endTime - distanceInBeats);


                beatmapObjectContainerCollection.SpawnObject(copy, false, false);
                BeatmapObject beatmapObject = beatmapObjectContainerCollection.UnsortedObjects[beatmapObjectContainerCollection.UnsortedObjects.Count - 1];
                spawnedBeatobjects.Add(beatmapObject);


                distanceInBeats -= 1 / (float)l_precision;
            }

            return spawnedBeatobjects;
        }

        private List<BeatmapObject> GeneratePoodle(BeatmapObject note1, BeatmapObject note2, Curve splineInterpolatorx, Curve splineInterpolatory, int l_precision = 32, bool dots = false, Dictionary<float,Color> colorDict = null, List<float> dotTime = null)
        {
            //TimeKeeper TGP = new TimeKeeper();
            //TGP.Start();
            BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();


            float startTime = note1.Time;
            float endTime = note2.Time;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            BeatmapNote oldNote = null;
            int noteIndex = 1;

            while (distanceInBeats > 0 - 1 / (float)l_precision)
            {
                BeatmapNote copy = new BeatmapNote(note1.ConvertToJson());
                copy.Time = (endTime - distanceInBeats);

                float line = (originalDistance - distanceInBeats);

                var x = splineInterpolatorx.ValueAt(line);
                var y = splineInterpolatory.ValueAt(line);

                copy.CustomData = new JSONObject();
                JSONNode customData = copy.CustomData;

                customData["_position"] = new Vector2((float)x, (float)y);


                //Color handling 
                if (colorDict != null && colorDict.Count > 0)
                {
                    customData["_color"] = LerpColorFromDict(colorDict, line);
                }

                if (dots)
                {
                    copy.CutDirection = 8;
                }

                JSONNode customData_old = null;
                if (paulmapperData.rotateNotes)
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
                        customData_old = oldNote.CustomData;
                        customData_old["_cutDirection"] = ang;

                    }
                }
                else if (paulmapperData.vibro)
                {
                    copy.CutDirection = (noteIndex % 2);
                }



                if (dotTime != null)
                {
                    try
                    {
                        dotTime.Sort();
                        float closeDotTime = dotTime.Last(d => oldNote.Time > d);
                        if (oldNote.Time - closeDotTime < 2 * paulmapperData.transitionTime)
                        {
                            oldNote.CutDirection = 8;
                            
                            if (!paulmapperData.transitionRotation && customData_old != null)
                            {
                                customData_old["_cutDirection"] = 0;
                            }
                        }

                    }
                    catch
                    {
                       
                    }

                }


                //}


                beatmapObjectContainerCollection.SpawnObject(copy, false, false);


                BeatmapObject beatmapObject = beatmapObjectContainerCollection.UnsortedObjects[beatmapObjectContainerCollection.UnsortedObjects.Count - 1];
                spawnedBeatobjects.Add(beatmapObject);


                oldNote = copy;
                distanceInBeats -= 1 / (float)l_precision;

                noteIndex++;
            }

            if (spawnedBeatobjects[spawnedBeatobjects.Count - 2].CustomData.HasKey("_cutDirection"))
                spawnedBeatobjects[spawnedBeatobjects.Count - 1].CustomData["_cutDirection"] = spawnedBeatobjects[spawnedBeatobjects.Count - 2].CustomData["_cutDirection"];

            else if ((spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BeatmapNote).CutDirection > 1000)
                (spawnedBeatobjects[spawnedBeatobjects.Count - 1] as BeatmapNote).CutDirection = (spawnedBeatobjects[spawnedBeatobjects.Count - 2] as BeatmapNote).CutDirection;

            if (dotTime != null && dotTime.Count > 0 && endTime.ToString() == (dotTime.Last() + paulmapperData.transitionTime).ToString())
            {
                (spawnedBeatobjects.Last() as BeatmapNote).CutDirection = 8;
            }
                

            //TGP.Complete("GeneratePoodle");

            return spawnedBeatobjects;
        }

        private void GeneratePoodle(BeatmapObject note1, BeatmapObject note2, string easing = null, int precision = 32)
        {

            if ((note1 as BeatmapNote).CutDirection == (note2 as BeatmapNote).CutDirection)
            { 
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
                    BeatmapNote copy = new BeatmapNote(note1.ConvertToJson());
                    copy.Time = endTime - distanceInBeats;

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

                        if (paulmapperData.rotateNotes)
                        {
                            customData["_cutDirection"] = noteRotation;
                        }
                        else if (paulmapperData.vibro)
                        {
                            copy.CutDirection = (noteIndex % 2);
                        }
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

                foreach (BeatmapObject beatmapObject in new List<BeatmapObject>() { note1, note2} )
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
    }
}
