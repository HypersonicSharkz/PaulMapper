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
using System.Collections.ObjectModel;

namespace PaulMapper
{
    [Plugin("PaulMapper")]
    public class Plugin
    {
        public static PaulMapper momenter;

        public static bool UpToDate = true;

        [Init]
        private void Init()
        {
            //Debug.LogError("PaulMapper V0.3 - Loaded");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            ExtensionButtons.AddButton(UIHelper.LoadSprite("PaulMapper.Resources.Icon.png"), "Paul Mapper", () => { momenter?.ToggleUI(); });

            PaulMapperData.GetSaveData();
            CheckVersion();
        }

        [Exit]
        private void Exit()
        {
            momenter?.paulmapperData?.SaveData();
        }

        public static void CheckVersion()
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
                PaulFinder.pauls = new List<Paul>();

                if (momenter == null || !momenter.isActiveAndEnabled)
                {
                    momenter = new GameObject("PaulMomenter").AddComponent<PaulMapper>();
                }
            }
        }
    }


    public class PaulMapper : MonoBehaviour
    {
        public float min = 0.5f;
        public float max = 1.5f;

        public static AudioTimeSyncController ats;
        public static BeatmapObjectContainerCollection notesContainer;
        public static BeatmapObjectContainerCollection bpmChangesContainer;

        internal static UIHandler uiHandler = new UIHandler();

        public PaulMapperData paulmapperData;

        public void ToggleUI()
        {
            uiHandler.ToggleWindow();
        }

        private void Start()
        {
            ats = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note).AudioTimeSyncController;
            notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);
            bpmChangesContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.BpmChange);


            paulmapperData = PaulMapperData.Instance;

            UpdateUIScene();
        }

        private void UpdateUIScene()
        {
            var mapEditorUI = UnityEngine.Object.FindObjectOfType<MapEditorUI>();
            uiHandler.TryLoadPaulMapperWindow(mapEditorUI);
            uiHandler.TryLoadQuickMenu(mapEditorUI);
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

                    float startTime = walls[0].JsonTime;
                    float endTime = walls[1].JsonTime;

                    float distanceInBeats = endTime - startTime;
                    float originalDistance = distanceInBeats;

                    while (distanceInBeats > 0 - 1 / (float)precision)
                    {
                        BaseObstacle copy = (BaseObstacle)walls[0].Clone();
                        copy.JsonTime = (endTime - distanceInBeats);


                        collection.SpawnObjectFix(copy, false, false);
                        BaseObject beatmapObject = copy;
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

        public void SetNotice(string p_notice, noticeType noticeType)
        {
            uiHandler.SetNotice(p_notice, noticeType);
        }

        private void Update()
        {
            uiHandler.UpdateUI();

            //Update checker
            if (Plugin.UpToDate && Time.time - lastUpdateCheck > 60)
            {
                lastUpdateCheck = Time.time;
                Plugin.CheckVersion();
                Debug.Log("CHECKING");
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                ToggleUI();
            }

            if (!RealtimeCurve.Editing)
            {
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
        private float lastUpdateCheck;

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
