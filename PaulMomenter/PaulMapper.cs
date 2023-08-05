using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Extreme.Mathematics.Curves;
using System.Data;
using PaulMapper.PaulHelper;
using Beatmap.Base;
using System.Reflection;

namespace PaulMapper
{
    [Plugin("PaulMapper")]
    public class Plugin
    {
        public static bool useNewUI = false;

        public static PaulMapper momenter;
        internal static UIHandler uiHandler;

        public static bool UpToDate = true;

        [Init]
        private void Init()
        {
            //Debug.LogError("PaulMapper V0.3 - Loaded");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load("ChroMapper-PropEdit");
                if (assembly.GetName().Version < new Version("0.5.1.0"))
                {
                    Debug.LogError("Please update the ChroMapper PropEdit Plugin to get the new UI");
                    useNewUI = false;
                }
                else
                    useNewUI = true;
            }
            catch (Exception)
            {
                useNewUI = false;
            }

            if (useNewUI)
            {
                AddUIHandler();
            }

            ExtensionButtons.AddButton(UIHelper.LoadSprite("PaulMapper.Resources.Icon.png"), "Paul Mapper", () => { momenter?.ToggleUI(); });

            PaulmapperData.GetSaveData();
            CheckVersion();
        }

        private void AddUIHandler()
        {
            uiHandler = new UIHandler();
        }

        [Exit]
        private void Exit()
        {
            momenter?.paulmapperData?.SaveData();
        }

        private void CheckVersion()
        {
            SceneTransitionManager.Instance.StartCoroutine(GitHubUtils.GetLatestReleaseTag((tag) =>
            {
                if (string.IsNullOrEmpty(tag))
                    return;

                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();

                Version version = assembly.GetName().Version;
                Version githubVersion = new Version(tag.Replace("v", ""));

                if (version < githubVersion)
                {
                    UpToDate = false;
                }

            }));
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {    
            if (arg0.buildIndex == 3) //Mapper scene 
            {
                if (useNewUI)
                {
                    UpdateUIScene();
                }

                PaulFinder.pauls = new List<Paul>();

                if (momenter != null && momenter.isActiveAndEnabled)
                    return;

                momenter = new GameObject("PaulMomenter").AddComponent<PaulMapper>();
            }
        }

        private void UpdateUIScene()
        {
            var mapEditorUI = UnityEngine.Object.FindObjectOfType<MapEditorUI>();
            useNewUI = uiHandler.TryLoadPaulMapperWindow(mapEditorUI);
            uiHandler.TryLoadQuickMenu(mapEditorUI);
        }
    }


    public class PaulMapper : MonoBehaviour
    {
        public bool showGUI;

        public float min = 0.5f;
        public float max = 1.5f;

        public static AudioTimeSyncController ats;
        public static BeatmapObjectContainerCollection notesContainer;
        public static BeatmapObjectContainerCollection bpmChangesContainer;

        public PaulmapperData paulmapperData;

        public void ToggleUI()
        {
            if (Plugin.useNewUI)
            {
                Plugin.uiHandler.ToggleWindow();
            }
            else
            {
                showGUI = !showGUI;
            }
        }

        private void Start()
        {
            ats = BeatmapObjectContainerCollection.GetCollectionForType(0).AudioTimeSyncController;
            notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);
            bpmChangesContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.BpmChange);


            paulmapperData = PaulmapperData.Instance;

            try
            {
                windowRect = paulmapperData.windowRect.getRect();
            }
            catch
            {
                paulmapperData.windowRect = new SerializableRect(windowRect);
                paulmapperData.SaveData();
            }
        }

        public static float guiX = 200;
        public static float guiWidth = 140;

        public static Rect windowRect = new Rect(guiX, 10, guiWidth, 450);

        void UpdatePaulMapperWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUI.Label(new Rect((guiWidth - 110) / 2, 40, 80, 20), "Precision: ");

            GUI.SetNextControlName("Precision");
            string text = GUI.TextField(new Rect(80 + (guiWidth - 110) / 2, 40, 30, 20), $"{paulmapperData.precision}");

            try
            {
                float bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;

                float nps = (bpm / 60) / (1 / (float)paulmapperData.precision);
                if (nps > 60)
                    GUI.Label(new Rect(5, 60, guiWidth - 10, 25), $"NPS: {nps.ToString("0.00")} !! lag");
                else
                    GUI.Label(new Rect(5, 60, guiWidth - 10, 25), $"NPS: {nps.ToString("0.00")}");
            }
            catch
            {
                GUI.Label(new Rect(5, 60, guiWidth - 10, 20), $"NPS: Infinity");
            }

            paulmapperData.vibro = GUI.Toggle(new Rect(5, 80, guiWidth / 2 - 10, 20), paulmapperData.vibro, "Vibro");
            paulmapperData.rotateNotes = GUI.Toggle(new Rect(guiWidth / 2 + 5, 80, guiWidth / 2 - 10, 20), paulmapperData.rotateNotes, "Rotate");

            GUI.Label(new Rect((guiWidth - 110) / 2, 170, 80, 20), "Transition time: ");

            GUI.SetNextControlName("Precision");
            string tt = GUI.TextField(new Rect(80 + (guiWidth - 110) / 2, 170, 30, 20), $"{paulmapperData.transitionTime.ToString("0.0")}");

            paulmapperData.transitionRotation = GUI.Toggle(new Rect(20, 190, guiWidth - 10, 20), paulmapperData.transitionRotation, "Keep Rotation");

            /*
            if (GUI.Button(new Rect(5, 130, guiWidth - 10, 20), "Mirror"))
            {
                MirrorSelected();
            }*/

            if (advancedMenu)
            {
                if (GUI.Button(new Rect(5, 150, guiWidth - 10, 20), "Fix"))
                {
                    notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);
                    List<BaseNote> allNotes = (from BaseNote it in notesContainer.LoadedObjects
                                                  orderby it.SongBpmTime
                                               select it).ToList();

                    foreach (BaseNote note in allNotes)
                    {
                        if (note.CustomData.HasKey("_paul"))
                        {
                            note.PosX = 4 * (int)(note.Type * 2 - 1);
                        }
                    }
                }
            } 
            else 
            {
                if (GUI.Button(new Rect(5, 100, guiWidth - 10, 25), "Settings"))
                {
                    showSettings = !showSettings;
                }

                if (BeatSaberSongContainer.Instance.Map.Version == "3.2.0")
                {
                    if (GUI.Button(new Rect(5, 140, guiWidth - 10, 25), "Arc"))
                    {
                        SpawnPrecisionArc();
                    }
                }
            }

            if (GUI.Button(new Rect(5, 220, guiWidth - 10, 20), "Find All Pauls"))
            {
                List<BaseNote> allNotes = (from BaseNote it in notesContainer.LoadedObjects
                                              orderby it.SongBpmTime
                                           select it).ToList();

                PaulFinder.pauls = PaulFinder.FindAllPauls(allNotes).OrderBy(p => p.Beat).ToList();
            }

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;

            if (PaulFinder.pauls.Count > 0)
            {
                if (advancedMenu)
                {
                    if (GUI.Button(new Rect(5, 10, guiWidth - 10, 20), "Change Prec. Of every poodle"))
                    {
                        PersistentUI.Instance.ShowDialogBox("Are you sure you wanna change the precision of every selected poodle?", (int result) =>
                        {
                            if (result != 0)
                                return;

                            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);
                            List<BaseObject> spawnedNotes = new List<BaseObject>();
                            List<BaseObject> beatmapObjects = new List<BaseObject>();

                            foreach (Paul paul in PaulFinder.pauls.Where(p => p.notes.Any(n => SelectionController.SelectedObjects.Contains(n))))
                            {
                                paulmapperData.transitionTime = 1f / (1.5f * (float)paulmapperData.precision);

                                spawnedNotes.AddRange(CreatePoodle(paul.notes.ToArray(), true, paulmapperData.precision, float.Epsilon).ToArray());
                                beatmapObjects.AddRange(paul.notes.ToArray());
                            }

                            foreach (BaseObject beatmapObject in beatmapObjects)
                            {
                                collection.DeleteObjectFix(beatmapObject, false);
                            }

                            BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedNotes, beatmapObjects));

                            foreach (BaseObject note in spawnedNotes)
                            {
                                SelectionController.Select(note, true, true, false);
                            }

                        }, PersistentUI.DialogBoxPresetType.YesNo);
                    }
                }


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

                    GUI.Label(new Rect(0, 290, guiWidth, 20), $"{PaulFinder.currentPaul + 1}/{PaulFinder.pauls.Count}", style);

                    if (GUI.Button(new Rect(5, 290, 40, 20), "<"))
                    {
                        //Go to last paul

                        try
                        {
                            Paul paul = PaulFinder.pauls.Last(p => p.Beat < ats.CurrentSongBpmTime);

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
                            Paul paul = PaulFinder.pauls.First(p => p.Beat > ats.CurrentSongBpmTime);
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

                    if (GUI.Button(new Rect(5, 350, guiWidth - 10, 20), "Select All Pauls"))
                    {
                        PaulFinder.SelectAllPauls();
                    }

                    if (advancedMenu)
                    {
                        if (GUI.Button(new Rect(5, 380, guiWidth - 10, 20), "Keep First Notes"))
                        {
                            PersistentUI.Instance.ShowDialogBox("Are you sure you want to remove all but first note of selected poodles?", (int result) =>
                            {
                                if (result != 0)
                                    return;

                                PaulFinder.KeepFirstNotes();
                            }, PersistentUI.DialogBoxPresetType.YesNo);
                        }
                    }
                }
                catch
                {

                }
            }

            if (SelectionController.SelectedObjects.Count > 0)
            {
                float selectionDir = selectionDur() * 1000;
                GUI.Label(new Rect(0, 370, guiWidth, 20), $"Length: {(selectionDir).ToString("0.0")}ms", style);


                //Average swing speed
                if (SelectionController.SelectedObjects.All(s => s.ObjectType == Beatmap.Enums.ObjectType.Note))
                {
                    BaseNote last = null;
                    float summedChange = 0;
                    foreach (BaseNote beatmapObject in SelectionController.SelectedObjects.OrderBy(s => s.SongBpmTime).Cast<BaseNote>())
                    {
                        if (last != null)
                        {
                            summedChange += Vector2.Distance(beatmapObject.GetRealPosition(), last.GetRealPosition());
                        }
                        last = beatmapObject;
                    }

                    float spd = summedChange / (selectionDir / 1000);
                    GUI.Label(new Rect(0, 390, guiWidth, 20), $"AVG. SPD: {spd.ToString("0.0")}", style);
                }
            }



            if (!string.IsNullOrEmpty(notice))
                GUI.TextArea(new Rect(0, 400, guiWidth, 50), $"{notice}", style);
            else if (!Plugin.UpToDate)
                if (GUI.Button(new Rect(0, 400, guiWidth, 50), $"New Version Of Paulmapper is available", style))
                    System.Diagnostics.Process.Start("https://github.com/HypersonicSharkz/PaulMapper/releases");

            if (!int.TryParse(text, out paulmapperData.precision))
                return;
            paulmapperData.precision = Math.Max(1, paulmapperData.precision);

            if (!float.TryParse(tt, out paulmapperData.transitionTime))
                return;
        }

        public static void SpawnPrecisionArc()
        {
            if (SelectionController.SelectedObjects.Count < 2) { /*SetNotice("Select at least two notes", noticeType.Error);*/ return; }

            if (!SelectionController.SelectedObjects.All(n => n.ObjectType == Beatmap.Enums.ObjectType.Note)) { /*SetNotice("Select only notes", noticeType.Error);*/ return; }

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

        private float selectionDur()
        {
            if (SelectionController.SelectedObjects.All(o => o.ObjectType == Beatmap.Enums.ObjectType.Note))
            {
                List<BaseNote> notes = SelectionController.SelectedObjects.OrderBy(n => n.SongBpmTime).Cast<BaseNote>().ToList();
                return ats.GetSecondsFromBeat(notes.Last().SongBpmTime - notes.First().SongBpmTime);
            }
            return 0;
        }

        string notice;

        private bool isHovering;

        public static bool advancedMenu = false;
        public static bool showSettings = false;

        private void UpdateSettingsMenu(int windowId)
        {
            paulmapperData.usePointRotations = GUI.Toggle(new Rect(guiWidth / 2 + 5, 20, guiWidth / 2 - 10, 20), paulmapperData.usePointRotations, "Force");
            paulmapperData.useScale = GUI.Toggle(new Rect(5, 20, guiWidth / 2 - 10, 20), paulmapperData.useScale, "Scale");
            paulmapperData.fakeWalls = GUI.Toggle(new Rect(5, 40, guiWidth / 2 - 10, 20), paulmapperData.fakeWalls, "Fake");

            if (BeatSaberSongContainer.Instance.Map.Version == "3.2.0")
            {
                paulmapperData.arcs = GUI.Toggle(new Rect(guiWidth / 2 + 5, 40, guiWidth / 2 - 10, 20), paulmapperData.arcs, "Arcs");
            }

            GUI.Label(new Rect((guiWidth - 110) / 2, 60, 80, 20), "Wall Rot: ");
            GUI.SetNextControlName("Precision");
            string text = GUI.TextField(new Rect(80 + (guiWidth - 110) / 2, 60, 30, 20), $"{paulmapperData.wallRotationAmount}");

            GUI.Label(new Rect(5, 140, guiWidth - 10, 20), "Disable Badcuts:");
            paulmapperData.disableBadCutDirection = GUI.Toggle(new Rect(5, 160, guiWidth - 10, 20), paulmapperData.disableBadCutDirection, "Direction");
            paulmapperData.disableBadCutSaberType = GUI.Toggle(new Rect(5, 180, guiWidth - 10, 20), paulmapperData.disableBadCutSaberType, "Saber Type");
            paulmapperData.disableBadCutSpeed = GUI.Toggle(new Rect(5, 200, guiWidth - 10, 20), paulmapperData.disableBadCutSpeed, "Speed");

            if (!int.TryParse(text, out paulmapperData.wallRotationAmount))
                return;

            paulmapperData.wallRotationAmount = Math.Max(1, paulmapperData.wallRotationAmount);
        }

        private void OnGUI()
        {
            if (!Plugin.useNewUI && showGUI)
            {
                Rect newWindowRect = GUI.Window(0, windowRect, UpdatePaulMapperWindow, advancedMenu ? "Advanced Paul Menu" : "Paul Menu");
                Rect settingsWindowRect = Rect.zero;

                if (showSettings)
                {
                    settingsWindowRect = GUI.Window(2, new Rect(newWindowRect.x + newWindowRect.width, newWindowRect.y + 185, guiWidth, 250), UpdateSettingsMenu, "Settings");
                }

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


                if (SelectionController.SelectedObjects.Count == 2 && SelectionController.SelectedObjects.All(s => s.ObjectType == Beatmap.Enums.ObjectType.Note))
                {
                    BaseNote beatmapObject1 = SelectionController.SelectedObjects.First() as BaseNote;
                    BaseNote beatmapObject2 = SelectionController.SelectedObjects.Last() as BaseNote;
                    if ((beatmapObject1.CutDirection == beatmapObject2.CutDirection || paulmapperData.rotateNotes) && beatmapObject1.SongBpmTime != beatmapObject2.SongBpmTime)
                    {
                        windowTotalRect.width += guiWidth;

                        BaseObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o.SongBpmTime).ToArray();

                        GUI.Box(new Rect(windowRect.x + guiWidth, windowRect.y, guiWidth, windowRect.height), "Quick Menu");
                        float xPos = windowRect.x + guiWidth + 10;
                        float yPos = windowRect.y + 30;

                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "Linear"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], null, paulmapperData.precision);
                        }

                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "SineIn"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInSine", paulmapperData.precision);
                        }

                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "SineOut"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutSine", paulmapperData.precision);
                        }

                        if (advancedMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "SineInOut"))
                            {
                                PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutSine", paulmapperData.precision);
                            }
                        }

                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "QuadIn"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInQuad", paulmapperData.precision);
                        }

                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "QuadOut"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutQuad", paulmapperData.precision);
                        }

                        if (advancedMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "QuadInOut"))
                            {
                                PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutQuad", paulmapperData.precision);
                            }
                        }




                        yPos += 30;

                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "CubicIn"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "CubicIn", paulmapperData.precision);
                        }
                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "CubicOut"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "CubicOut", paulmapperData.precision);
                        }

                        if (advancedMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "CubicInOut"))
                            {
                                PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "CubicInOut", paulmapperData.precision);
                            }
                        }

                        yPos += 30;

                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "ExpIn"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "ExpIn", paulmapperData.precision);
                        }

                        yPos += 30;

                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "ExpOut"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "ExpOut", paulmapperData.precision);
                        }

                        if (advancedMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "ExpInOut"))
                            {
                                PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "ExpInOut", paulmapperData.precision);
                            }
                        }

                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BackIn"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInBack", paulmapperData.precision);
                        }
                        yPos += 30;
                        if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BackOut"))
                        {
                            PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutBack", paulmapperData.precision);
                        }

                        if (advancedMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BackInOut"))
                            {
                                PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutBack", paulmapperData.precision);
                            }
                        }

                        if (advancedMenu)
                        {
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BounceIn"))
                            {
                                PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInBounce", paulmapperData.precision);
                            }
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BounceOut"))
                            {
                                PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeOutBounce", paulmapperData.precision);
                            }
                            yPos += 30;
                            if (GUI.Button(new Rect(xPos, yPos, 120, 20), "BounceInOut"))
                            {
                                PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], "easeInOutBounce", paulmapperData.precision);
                            }
                        }

                        yPos += 60;
                    }
                }

                if (windowTotalRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)) ||
                    (showSettings && settingsWindowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y))) ||
                    GUI.GetNameOfFocusedControl() == "Precision")
                {
                    if (!isHovering)
                    {
                        isHovering = true;
                        CMInputCallbackInstaller.DisableActionMaps(typeof(PaulMapper), PaulActions.actionMapsDisabled);
                    }
                }
                else
                {
                    if (isHovering)
                    {
                        isHovering = false;
                        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(PaulMapper), PaulActions.actionMapsDisabled);
                    }
                }
            }
        }

        private void OnDisable()
        {
            paulmapperData.SaveData();
        }

        public List<BaseObject> CreatePoodle(BaseObject[] beatmapObjects, bool autoDot, int precision, float transitionTime)
        {
            if (beatmapObjects.Count() != beatmapObjects.Select(p => p.SongBpmTime).Distinct().Count())
                return null;

            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(beatmapObjects[0].ObjectType);

            List<BaseObject> spawnedNotes = new List<BaseObject>();

            if (beatmapObjects.All(b => b.ObjectType == Beatmap.Enums.ObjectType.Note))
            {
                //Normal paul
                if (beatmapObjects.All(o => !((o as BaseNote).CustomData != null && (o as BaseNote).CustomData.HasKey("_position")) &&
                                            (o as BaseNote).CutDirection == (beatmapObjects[0] as BaseNote).CutDirection &&
                                            (o as BaseNote).PosX == (beatmapObjects[0] as BaseNote).PosX &&
                                            (o as BaseNote).PosY == (beatmapObjects[0] as BaseNote).PosY
                ) && !paulmapperData.vibro)
                {
                    spawnedNotes = PaulMaker.GeneratePaul(beatmapObjects[0], beatmapObjects.Last(), precision);

                }
                else
                {
                    //A poodle
                    List<double> pointsx = new List<double>();

                    List<double> pointsx_y = new List<double>();
                    List<double> pointsy_y = new List<double>();

                    Dictionary<float, float> points_dir = new Dictionary<float, float>();

                    float endTime = beatmapObjects.Last().SongBpmTime;
                    float totalTime = beatmapObjects.Last().SongBpmTime - beatmapObjects[0].SongBpmTime;

                    Dictionary<float, Color> DistColorDict = new Dictionary<float, Color>();

                    foreach (BaseObject beatmapObject in beatmapObjects)
                    {
                        float startTime = beatmapObject.SongBpmTime;

                        float distanceInBeats = totalTime - (endTime - startTime);

                        pointsx.Add(distanceInBeats);

                        pointsx_y.Add((beatmapObject as BaseNote).GetRealPosition().x);

                        pointsy_y.Add((beatmapObject as BaseNote).GetRealPosition().y);

                        if ((beatmapObject as BaseNote).CutDirection != 8)
                            points_dir.Add(distanceInBeats, (beatmapObject as BaseNote).GetNoteDirection());

                        if (Helper.TryGetColorFromObject(beatmapObject, out Color color))
                        {
                            DistColorDict.Add(distanceInBeats, color);
                        }

                    }

                    List<float> dotTimes = null;
                    if (autoDot)
                    {
                        dotTimes = beatmapObjects.Where(p => (p as BaseNote).CutDirection == 8).Select(p => p.SongBpmTime - transitionTime).ToList();
                    }

                    CubicSpline splinex = CubicSpline.CreateNatural(pointsx, pointsx_y);
                    CubicSpline spliney = CubicSpline.CreateNatural(pointsx, pointsy_y);

                    spawnedNotes = PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects.Last(), splinex, spliney, precision, beatmapObjects.All(o => (o as BaseNote).CutDirection == 8), DistColorDict, dotTimes, paulmapperData.usePointRotations ? points_dir : null);
                }

                //TP.Complete("Paul");
            }
            else if (beatmapObjects.All(b => b.ObjectType == Beatmap.Enums.ObjectType.Obstacle) && beatmapObjects.Length == 2)
            {
                List<BaseObstacle> walls = beatmapObjects.Cast<BaseObstacle>().ToList();

                if (walls.All(w => w.Type == walls[0].Type && w.Width == walls[0].Width && w.PosX == walls[0].PosX))
                {

                    float startTime = walls[0].SongBpmTime;
                    float endTime = walls[1].SongBpmTime;

                    float distanceInBeats = endTime - startTime;
                    float originalDistance = distanceInBeats;

                    while (distanceInBeats > 0 - 1 / (float)precision)
                    {
                        BaseObstacle copy = (BaseObstacle)walls[0].Clone();
                        copy.SongBpmTime = (endTime - distanceInBeats);


                        collection.SpawnObjectFix(copy, false, false);
                        BaseObject beatmapObject = collection.UnsortedObjects.Last() as BaseObject;
                        spawnedNotes.Add(beatmapObject);


                        distanceInBeats -= 1 / (float)precision;
                    }
                }
            }
            else
            {
                return null;
            }

            return spawnedNotes;
        }


        string[] symbols = new string[] { "", "⚠", "‼️" };
        float lastNoticeTime;
        public void SetNotice(string p_notice, noticeType noticeType)
        {
            if (Plugin.useNewUI)
            {
                Plugin.uiHandler.SetNotice(p_notice, noticeType);
            }
            else
            {
                string initalText = symbols[(int)noticeType] + ": " + p_notice;
                int lastWhiteSpace = 0;

                int lines = 0;
                List<char> copied = initalText.ToList();
                for (int i = 0; i < copied.Count; i++)
                {

                    if (char.IsWhiteSpace(copied[i]))
                    {
                        lastWhiteSpace = i;
                    }

                    if (i % 20 == 0)
                    {
                        initalText.Insert(lastWhiteSpace + lines, "\n");
                        lines++;
                    }
                }

                notice = initalText;
                lastNoticeTime = Time.time;
            }
        }

        private void Update()
        {
            if (Plugin.useNewUI)
                Plugin.uiHandler.UpdateUI();

            //Notice update
            if (Time.time - lastNoticeTime > 10)
                notice = string.Empty;

            /*
            if (Input.GetKeyDown(KeyCode.F10))
            {
                advancedMenu = false;
                if (Input.GetKey(KeyCode.LeftShift) && !showGUI)
                {
                    advancedMenu = true;
                    isHovering = false;
                    CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(PaulMapper), PaulActions.actionMapsDisabled);
                }

                showGUI = !showGUI;
            }*/

            if (Input.GetKeyDown(KeyCode.F10))
            {
                ToggleUI();
            }


            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    Helper.RotateWalls(false, true);
                }
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    Helper.RotateWalls(true, true);
                }                
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    Helper.RotateWalls(false, false);
                }
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    Helper.RotateWalls(true, false);
                }
            }


            if (Input.GetKeyDown(KeyCode.F9))
            {
                //TimeKeeper TP = new TimeKeeper();
                //TP.Start();

                BaseObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o.SongBpmTime).ToArray();

                if (beatmapObjects.Length != 2)
                {
                    SetNotice("Select only two notes", noticeType.Error);
                    return;
                }

                if (beatmapObjects[1].SongBpmTime - beatmapObjects[0].SongBpmTime < 1 / paulmapperData.precision)
                {
                    SetNotice("Notes are closer than precision", noticeType.Error);
                    return;
                }

                if (beatmapObjects.All(b => b.ObjectType == Beatmap.Enums.ObjectType.Note))
                {
                    if (!beatmapObjects.All(o => (o as BaseNote).CutDirection == (beatmapObjects[0] as BaseNote).CutDirection &&
                                                (o as BaseNote).PosY == (beatmapObjects[0] as BaseNote).PosY &&
                                                (o as BaseNote).PosX == (beatmapObjects[0] as BaseNote).PosX))
                    {
                        SetNotice("Notes can not be made into a paul", noticeType.Error);
                        return;
                    }
                } else if (beatmapObjects.All(b => b.ObjectType == Beatmap.Enums.ObjectType.Obstacle)) 
                {
                    if (!beatmapObjects.All(o =>
                            (o as BaseObstacle).Width == (beatmapObjects[0] as BaseObstacle).Width &&
                            (o as BaseObstacle).PosX == (beatmapObjects[0] as BaseObstacle).PosX))
                    {
                        SetNotice("Walls can not be made into a paul", noticeType.Error);
                        return;
                    }
                }


                List<BaseObject> spawnedNotes = PaulMaker.GeneratePaul(beatmapObjects[0], beatmapObjects[1], paulmapperData.precision);

                BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(beatmapObjects[0].ObjectType);

                foreach (BaseObject beatmapObject in beatmapObjects)
                {
                    collection.DeleteObjectFix(beatmapObject, false);
                }

                BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedNotes, beatmapObjects));

                foreach (BaseObject note in spawnedNotes)
                {
                    SelectionController.Select(note, true, true, false);
                }
            }

            if (Input.GetKeyDown(KeyCode.F12))
            {
                BaseObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o.SongBpmTime).ToArray();

                if (beatmapObjects.Length < 2)
                {
                    SetNotice("Select at least 2 points", noticeType.Error);
                    return;
                }
                if (beatmapObjects.Length == 2)
                {
                    if (beatmapObjects[1].SongBpmTime - beatmapObjects[0].SongBpmTime < 1 / paulmapperData.precision)
                    {
                        SetNotice("Points are closer than precision", noticeType.Error);
                        return;
                    }
                }   

                if (beatmapObjects.All(b => b.ObjectType == Beatmap.Enums.ObjectType.Note))
                {
                    GameObject gameObject = new GameObject("Curve");
                    RealtimeCurve curve = gameObject.AddComponent<RealtimeNoteCurve>();
                    curve.InstantiateCurve(beatmapObjects.ToList());
                }
                else if (beatmapObjects.All(b => b.ObjectType == Beatmap.Enums.ObjectType.Obstacle))
                {
                    GameObject gameObject = new GameObject("Curve");
                    RealtimeCurve curve = gameObject.AddComponent<RealtimeWallCurve>();
                    curve.InstantiateCurve(beatmapObjects.ToList());
                } else
                {
                    SetNotice("Only select objects of same type", noticeType.Error);
                }

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
            foreach (BaseObject con in SelectionController.SelectedObjects)
            {
                BaseObject original = (BaseObject)con.Clone();
                if (con is BaseObstacle obstacle)
                {
                    bool precisionWidth = obstacle.Width >= 1000;
                    int __state = obstacle.PosX;

                    if (obstacle.CustomData != null)
                    {
                        if (obstacle.CustomData.HasKey("coordinates"))
                        {
                            Vector2 oldPosition = obstacle.CustomData["coordinates"];
                            Vector2 flipped = new Vector2(oldPosition.x * -1f, oldPosition.y);

                            if (obstacle.CustomData.HasKey("size"))
                            {
                                Vector2 scale = obstacle.CustomData["size"];
                                flipped.x -= scale.x;
                            }
                            else
                            {
                                flipped.x -= (float)obstacle.Width;
                            }
                            obstacle.CustomData["coordinates"] = flipped;
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
                            obstacle.PosX = newIndex;
                        }
                        else
                        {
                            int mirrorLane = (__state - 2) * -1 + 2;
                            obstacle.PosX = mirrorLane - obstacle.Width;
                        }
                    }
                }
                else
                {
                    if (con is BaseNote note)
                    {
                        if (note.CustomCoordinate != null)
                        {
                            Vector2 oldPosition2 = ((Vector2?)note.CustomCoordinate).Value;
                            Vector2 flipped2 = new Vector2((oldPosition2.x + 0.5f) * -1f - 0.5f, oldPosition2.y);
                            note.CustomCoordinate = flipped2;
                        }
                        else
                        {
                            int __state2 = note.PosX;
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
                                note.PosX = newIndex2;
                            }
                            else
                            {
                                int mirrorLane2 = (int)(((float)__state2 - 1.5f) * -1f + 1.5f);
                                note.PosX = mirrorLane2;
                            }
                        }
                        if (note.Type != 3)
                        {
                            note.Type = ((note.Type == 0) ? 1 : 0);
                            if (note.CustomData != null)
                            {
                                if (note.CustomDirection.HasValue)
                                {
                                    note.SetRotation(-note.CustomDirection.Value);
                                }
                            }
                            else
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
            foreach (BaseObject unique in SelectionController.SelectedObjects.DistinctBy(x => x.ObjectType))
            {
                BeatmapObjectContainerCollection.GetCollectionForType(unique.ObjectType).RefreshPool(true);
            }
            BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true, "Mirrored a selection of objects."));

        }
    }
}
