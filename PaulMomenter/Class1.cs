using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;
using System.Text.RegularExpressions;
using Extreme.Mathematics.Curves;

namespace PaulMapper
{
    [Plugin("PaulMapper")]
    public class Plugin
    {
        public PaulMomenter momenter; 

        [Init]
        private void Init()
        {
            Debug.LogError("PaulMapper V0.1 - Loaded");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.buildIndex == 3) //Mapper scene 
            {
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

        private void OnGUI()
        {
            if (showGUI)
            {
                GUI.Box(new Rect(10, 10, 140, 440), "Paul Menu");

                GUI.Label(new Rect(10, 40, 80, 20), "Precision: ");
                string text = GUI.TextField(new Rect(90, 40, 30, 20), $"{precision}");

                vibro = GUI.Toggle(new Rect(10, 80, 55, 20), vibro, "Vibro");
                rotateNotes = GUI.Toggle(new Rect(70, 80, 80, 20), rotateNotes, "Rotation");

                if (!int.TryParse(text, out precision))
                    return;
                
                if (SelectionController.SelectedObjects.Count == 2 && SelectionController.SelectedObjects.All(s => s.beatmapType == BeatmapObject.Type.NOTE))
                {
                    BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o._time).ToArray();

                    if (GUI.Button(new Rect(20, 70, 120, 20), "Linear"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], null, precision);
                    }

                    if (GUI.Button(new Rect(20, 100, 120, 20), "ExpIn"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "ExpIn", precision);
                    }

                    if (GUI.Button(new Rect(20, 130, 120, 20), "ExpOut"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "ExpOut", precision);
                    }

                    if (GUI.Button(new Rect(20, 160, 120, 20), "ExpInOut"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "ExpInOut", precision);
                    }

                    if (GUI.Button(new Rect(20, 190, 120, 20), "CubicIn"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "CubicIn", precision);
                    }
                    if (GUI.Button(new Rect(20, 220, 120, 20), "CubicOut"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "CubicOut", precision);
                    }
                    if (GUI.Button(new Rect(20, 250, 120, 20), "CubicInOut"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "CubicInOut", precision);
                    }


                    if (GUI.Button(new Rect(20, 280, 120, 20), "easeInBack"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeInBack", precision);
                    }
                    if (GUI.Button(new Rect(20, 310, 120, 20), "easeOutBack"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeOutBack", precision);
                    }
                    if (GUI.Button(new Rect(20, 340, 120, 20), "easeInOutBack"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeInOutBack", precision);
                    }


                    if (GUI.Button(new Rect(20, 370, 120, 20), "easeInBounce"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeInBounce", precision);
                    }
                    if (GUI.Button(new Rect(20, 400, 120, 20), "easeOutBounce"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeOutBounce", precision);
                    }
                    if (GUI.Button(new Rect(20, 430, 120, 20), "easeInOutBounce"))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeInOutBounce", precision);
                    }
                }
            }
        }

        private bool TryGetColorFromObject(BeatmapObject beatmapObject, out Color color)
        {
            color = Color.clear;
            if (beatmapObject.beatmapType != BeatmapObject.Type.NOTE)
                return false;

            JSONNode customData = beatmapObject._customData;
            if (customData != null && customData.HasKey("_color"))
            {
                color = customData["_color"];
                return true;
            } 
            
            /*else
            {
                if ((beatmapObject as BeatmapNote)._type == 0)
                {
                    color = Color.red;
                    return true;
                }
                else if ((beatmapObject as BeatmapNote)._type == 1)
                {
                    color = Color.blue;
                    return true;
                }
            }*/

            return false;
        }



        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                showGUI = !showGUI;
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o._time).ToArray();
                float endTime = beatmapObjects.Last()._time;
                float totalTime = beatmapObjects.Last()._time - beatmapObjects[0]._time;

                Dictionary<float, Color> colorDict = new Dictionary<float, Color>();



                foreach (BeatmapObject beatmapObject in beatmapObjects)
                {
                    Color color = Color.clear;
                    if (TryGetColorFromObject(beatmapObject, out color))
                    {
                        if (color != new Color(0, 0, 0, 0))
                        {
                            float startTime = beatmapObject._time;

                            float distanceInBeats = totalTime - (endTime - startTime);

                            colorDict.Add(distanceInBeats, color);
                        }
                    }
                }

                foreach (BeatmapObject beatmapObject in beatmapObjects)
                {
                    float startTime = beatmapObject._time;

                    float distanceInBeats = totalTime - (endTime - startTime);


                    JSONNode customData = beatmapObject._customData;
                    if (customData != null)
                        customData["_color"] = LerpColorFromDict(colorDict, distanceInBeats);
                }

            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o._time).ToArray();

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
                    if (TryGetColorFromObject(beatmapObject, out color))
                    {
                        if (color != new Color(0,0,0,0))
                        {
                            DistColorDict.Add(distanceInBeats, color);
                        }
                    }

                }

                CubicSpline splinex = CubicSpline.CreateNatural(pointsx, pointsx_y);

                CubicSpline spliney = CubicSpline.CreateNatural(pointsx, pointsy_y);


                List<BeatmapObject> spawnedNotes = GeneratePaul(beatmapObjects[0], beatmapObjects.Last(), splinex, spliney, precision, beatmapObjects.All(o => (o as BeatmapNote)._cutDirection == 8), DistColorDict);

                BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();

                foreach (BeatmapObject beatmapObject in beatmapObjects)
                {
                    beatmapObjectContainerCollection.DeleteObject(beatmapObject, false);
                }

                BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedNotes, beatmapObjects));

                foreach (BeatmapObject note in spawnedNotes)
                {
                    SelectionController.Select(note, true);
                }
                
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                
                if (SelectionController.SelectedObjects.Count == 2 && SelectionController.SelectedObjects.All(s => s.beatmapType == BeatmapObject.Type.NOTE))
                {
                    BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o._time).ToArray();

                    if (Input.GetKeyDown(KeyCode.Alpha1))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1]);
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha2))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "ExpIn");
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha3))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "ExpOut");
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha4))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "ExpInOut");
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha5))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "CubicIn");
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha6))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "CubicOut");
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha7))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "CubicInOut");
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha7))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "CubicInOut");
                    }

                    //Cursed
                    else if (Input.GetKeyDown(KeyCode.Alpha8))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeInBack");
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha9))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeOutBack");
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha0))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeInOutBack");
                    }

                    //Omega cursed
                    else if (Input.GetKeyDown(KeyCode.Keypad7))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeInBounce");
                    }
                    else if (Input.GetKeyDown(KeyCode.Keypad8))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeOutBounce");
                    }
                    else if (Input.GetKeyDown(KeyCode.Keypad9))
                    {
                        GeneratePaul(beatmapObjects[0], beatmapObjects[1], "easeInOutBounce");
                    }
                }
            
                else if (SelectionController.SelectedObjects.Count == 2 && SelectionController.SelectedObjects.All(s => s.beatmapType != BeatmapObject.Type.NOTE))
                {
                    BeatmapObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o._time).ToArray();

                    MapEvent startEvent = beatmapObjects.First() as MapEvent;
                    MapEvent endEvent = beatmapObjects.Last() as MapEvent;

                    BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();
                    TracksManager tracksManager = UnityEngine.Object.FindObjectOfType<TracksManager>();

                    float startTime = startEvent._time;
                    float endTime = endEvent._time;

                    float distanceInBeats = endTime - startTime;
                    float originalDistance = distanceInBeats;


                    List<MapEvent> spawnedBeatobjects = new List<MapEvent>();

                    while (distanceInBeats > 0)
                    {
                        MapEvent copy = new MapEvent((endTime - distanceInBeats), startEvent._type, startEvent._value);

                        beatmapObjectContainerCollection.SpawnObject(copy, true, true);

                        distanceInBeats -= 1 / (float)precision;
                    }

                    tracksManager.RefreshTracks();
                    /*

                    BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedBeatobjects, null));

                    foreach (MapEvent note in spawnedBeatobjects)
                    {
                        SelectionController.Select(note, true);
                    }*/
                }
            }
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

        private List<BeatmapObject> GeneratePaul(BeatmapObject note1, BeatmapObject note2, CubicSpline splineInterpolatorx, CubicSpline splineInterpolatory, int l_precision = 32, bool dots = false, Dictionary<float,Color> colorDict = null)
        {
            AudioTimeSyncController audioTimeSyncController = UnityEngine.Object.FindObjectOfType<AudioTimeSyncController>();
            BeatmapObjectContainerCollection beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();



            float startTime = note1._time;
            float endTime = note2._time;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            Vector2 note1pos = (note1 as BeatmapNote).GetPosition();

            Vector2 note2pos = (note2 as BeatmapNote).GetPosition();


            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            BeatmapNote oldNote = null;
            int noteIndex = 1;
            int startCutDir = (note1 as BeatmapNote)._cutDirection;

            while (distanceInBeats > 0)
            {
                BeatmapNote copy = new BeatmapNote(note1.ConvertToJSON());
                copy._time = (float)(endTime - distanceInBeats);

                float line = (originalDistance - distanceInBeats);

                var x = splineInterpolatorx.ValueAt(line);
                var y = splineInterpolatory.ValueAt(line);

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
                else if(vibro)
                {
                    copy._cutDirection = (noteIndex % 2);
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

            /*
            beatmapObjectContainerCollection.DeleteObject(note1, false);
            BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedBeatobjects, new List<BeatmapObject>() { note1 }));

            beatmapObjectContainerCollection.RefreshPool(true);

            SelectionController.SelectBetween(note1, note2);*/

            return spawnedBeatobjects;

            //beatmapObjectContainerCollection.DeleteObject(note2);
        }

        private void GeneratePaul(BeatmapObject note1, BeatmapObject note2, string easing = null, int precision = 32)
        {

            if ((note1 as BeatmapNote)._cutDirection == (note2 as BeatmapNote)._cutDirection)
            {
                AudioTimeSyncController audioTimeSyncController = UnityEngine.Object.FindObjectOfType<AudioTimeSyncController>();
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

                    beatmapObjectContainerCollection.SpawnObject(copy, true, false);
                   

                    BeatmapObject beatmapObject = beatmapObjectContainerCollection.UnsortedObjects[beatmapObjectContainerCollection.UnsortedObjects.Count - 1];
                    spawnedBeatobjects.Add(beatmapObject);

                    
                    
                    distanceInBeats -= 1 / (float)precision;
                }


                BeatmapActionContainer.AddAction(new SelectionPastedAction(spawnedBeatobjects, null));

                beatmapObjectContainerCollection.RefreshPool(true);

                foreach (BeatmapObject note in spawnedBeatobjects)
                {
                    SelectionController.Select(note, true);
                }
                //beatmapObjectContainerCollection.DeleteObject(note2);


            }
        }
    }
}
