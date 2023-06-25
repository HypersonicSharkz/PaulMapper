﻿using Beatmap.Base;
using Beatmap.Containers;
using Extreme.Mathematics.Curves;
using PaulMapper.PaulHelper;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using static PaulMapper.PaulMomenter;

namespace PaulMapper
{
    public class RealtimeCurve : MonoBehaviour
    {
        public Curve xCurve;
        public Curve yCurve;

        public Curve widthCurve;
        public Curve heightCurve;
        public Curve depthCurve;

        public Dictionary<float, Color> colorDist = new Dictionary<float, Color>();

        public List<BaseObject> initialObjects = new List<BaseObject>();

        public List<CurveParameter> curveParameters = new List<CurveParameter>();

        public BaseObject object1;
        public BaseObject object2;

        public List<BaseObject> curveObjects = new List<BaseObject>();

        protected BeatmapObjectContainerCollection beatmapObjectContainerCollection;
        protected EventGridContainer eventsContainer;
        protected TracksManager TracksManager;

        Track curveTrack;
        private bool actionMapsDisabled;

        public CurveParameter selectedCurvePoint;
        public Material mainMat;
        public Material selectionMat;

        List<BaseObject> originalCurveObjects = new List<BaseObject>();

        public virtual void InstantiateCurve(List<BaseObject> parameters)
        {
            beatmapObjectContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(parameters[0].ObjectType);
            eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Event) as EventGridContainer;

            TracksManager = FindObjectOfType<TracksManager>();
            BaseObject[] beatmapObjects = parameters.OrderBy(o => o.SongBpmTime).ToArray();


            //Materials are weird I think
            NoteContainer con = GameObject.FindObjectOfType<NoteContainer>(true);

            mainMat = con.GetComponentsInChildren<MeshRenderer>()[0].material;
            selectionMat = con.GetComponentsInChildren<MeshRenderer>()[1].material;

            if (beatmapObjects.Count() != beatmapObjects.Select(p => p.SongBpmTime).Distinct().Count())
            {
                Plugin.momenter.SetNotice("2 notes can't be on the same beat!", noticeType.Error);
                Destroy(this);
                return;
            }


            curveTrack = TracksManager.CreateTrack(0);

            object1 = beatmapObjects.First();
            object2 = beatmapObjects.Last();
            this.initialObjects = parameters;

            curveParameters = ObjectsToParameters(beatmapObjects.ToList());

            GetCurves(curveParameters, out xCurve, out yCurve);
            SpawnObjects();
            originalCurveObjects = curveObjects.Select(c => (BaseObject)c.Clone()).ToList();
            SpawnAnchorPoints();
        }

        private void Update()
        {
            bool anyIsHovering = curveParameters.Any(a => a.anchorPoint.isHovering);
            if (!actionMapsDisabled && anyIsHovering)
            {
                CMInputCallbackInstaller.DisableActionMaps(typeof(PaulMomenter), PaulActions.actionMapsDisabled);
                actionMapsDisabled = true;
            }
            else if (actionMapsDisabled && !anyIsHovering)
            {
                CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(PaulMomenter), PaulActions.actionMapsDisabled);
                actionMapsDisabled = false;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift))
                {
                    AddAnchorPoint();
                }    
            }

            if (xCurve != null && yCurve != null)
            {
                
                foreach (CurveParameter param in curveParameters)
                {
                    param.xPos = param.anchorPoint.GetAsParameter().x;
                    param.yPos = param.anchorPoint.GetAsParameter().y;
                }

                GetCurves(curveParameters, out xCurve, out yCurve);
                UpdateObjects();

                if (!SelectionController.HasSelectedObjects())
                {
                    FinishCurve();
                }
            }
        }

        public float GetRotationValueAtTime(float time, List<BaseObject> beatmapObjects)
        {
            //Get all relevant rotations
            IEnumerable<BaseEvent> rotations = eventsContainer.AllRotationEvents.Where(x => PaulMaker.CompareRound(x.SongBpmTime, beatmapObjects.First().SongBpmTime, 0.0001f) != -1 && PaulMaker.CompareRound(x.SongBpmTime, beatmapObjects.Last().SongBpmTime, 0.0001f) != 1).OrderBy(x => x.SongBpmTime);

            BaseEvent rotEvent = rotations.LastOrDefault(x => x.SongBpmTime <= time);
            if (rotEvent == null)
            {
                if (rotations.Count() == 1)
                {
                    rotEvent = rotations.First();
                }
                else
                    return -1;
            }

            float t1 = rotEvent.SongBpmTime;

            //Rotation at first note
            float rot1 = eventsContainer.AllRotationEvents.Where(x => x.SongBpmTime < t1).Sum(x => x.GetRotationDegreeFromValue().GetValueOrDefault());
            float rot2 = rot1 + rotEvent.GetRotationDegreeFromValue().GetValueOrDefault();


            //Get time of last rotation, or last note if it is further away
            float t2 = 0;
            BaseEvent rotEventEnd = rotations.FirstOrDefault(x => x.SongBpmTime >= time);

            if (rotEventEnd == null || rotEventEnd.SongBpmTime > beatmapObjects.Last().SongBpmTime)
                t2 = beatmapObjects.Last().SongBpmTime;
            else
                t2 = rotEventEnd.SongBpmTime;

            if (t1 == t2)
                return rot1;

            return Mathf.Lerp(rot1, rot2, (time - t1) / (t2 - t1));
        }

        protected virtual void UpdateObjects()
        {           
        }

        private void SpawnAnchorPoints()
        {
            //Spawn gameobject for each note
            foreach (CurveParameter point in curveParameters)
            {
                SpawnAnchorPoint(point);
            }

            //then delete notes
            
            foreach (BaseObject beatmapObject in initialObjects)
            {
                beatmapObjectContainerCollection.DeleteObject(beatmapObject, false);
            }

            BeatmapActionContainer.AddAction(new SelectionPastedAction(curveObjects, initialObjects));

            foreach (BaseObject note in curveObjects)
            {
                SelectionController.Select(note, true, true, false);
            }
            
        }

        protected virtual void SpawnAnchorPoint(CurveParameter curveParameter)
        {
            

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            CurveAnchorPoint point = sphere.AddComponent<CurveAnchorPoint>();
            point.mainMat = mainMat;
            point.selectionMat = selectionMat;
            point.param = curveParameter;

            float zPos = (curveParameter.time - PaulMomenter.ats.CurrentSongBpmTime) * EditorScaleController.EditorScale;

            sphere.transform.parent = curveTrack.ObjectParentTransform;
            sphere.transform.position = new Vector3(curveParameter.xPos, curveParameter.yPos, zPos) + point.parameterOffset;

            sphere.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);


            curveParameter.anchorPoint = point;

            if (curveParameter != curveParameters.Last() && curveParameter != curveParameters.First())
                point.OnMiddleClick += delegate (){ DeleteAnchorPoint(curveParameter); };

            point.OnLeftClick += delegate () {
                
                if (selectedCurvePoint != null)
                {
                    selectedCurvePoint.anchorPoint.matProp.SetFloat(Shader.PropertyToID("_Outline"), 0);
                    selectedCurvePoint.anchorPoint.matProp.SetColor(Shader.PropertyToID("_Color"), Color.magenta);
                    selectedCurvePoint.anchorPoint.UpdateMaterials();
                }
                    

                selectedCurvePoint = curveParameter;

                selectedCurvePoint.anchorPoint.matProp.SetFloat(Shader.PropertyToID("_Outline"), 0.05f);
                selectedCurvePoint.anchorPoint.matProp.SetColor(Shader.PropertyToID("_Color"), Color.green);
                selectedCurvePoint.anchorPoint.UpdateMaterials();
            };
        }
        float minTimeDif = 0.01f;
        private void AddAnchorPoint()
        {
            float time = curveObjects.First(o => o.SongBpmTime >= PaulMomenter.ats.CurrentSongBpmTime).SongBpmTime;
            if (!curveParameters.Any(c => Math.Abs(c.time - time) < minTimeDif))
            {
                if (PaulMomenter.ats.CurrentSongBpmTime < curveParameters.Last().time
                    && PaulMomenter.ats.CurrentSongBpmTime > curveParameters.First().time)
                {
                    //Is between first 2 points
                    CurveParameter newCP = new CurveParameter(curveObjects.First(o => o.SongBpmTime >= PaulMomenter.ats.CurrentSongBpmTime) as BaseGrid);
                    SpawnAnchorPoint(newCP);
                    curveParameters.Add(newCP);
                    curveParameters = curveParameters.OrderBy(c => c.time).ToList();
                }
            }


        }

        private void DeleteAnchorPoint(CurveParameter param)
        {
            selectedCurvePoint = null;
            curveParameters.Remove(param);
            Destroy(param.anchorPoint.gameObject);
        }

        protected virtual void SpawnObjects()
        {

        }

        private List<CurveParameter> ObjectsToParameters(List<BaseObject> beatmapObjects)
        {
            BaseObject[] beatmapNotesAnchor = null;
            if (beatmapObjects.Where(o => o.CustomData != null && o.CustomData.HasKey("_isAnchor")).Count() > 1)
            {
                //Curve already defined
                beatmapNotesAnchor = beatmapObjects.Where(o => o.CustomData != null && o.CustomData.HasKey("_isAnchor")).ToArray();
                return beatmapNotesAnchor.Select(n =>
                    new CurveParameter(n as BaseGrid)).OrderBy(c => c.time).ToList();
            } else
            {
                return beatmapObjects.Select(n =>
                    new CurveParameter(n as BaseGrid)).OrderBy(c => c.time).ToList();
            }
        }

        protected virtual void GetCurves(List<CurveParameter> beatmapNotes, out Curve curvex, out Curve curvey)
        {
            List<double> pointsx = new List<double>();

            List<double> pointsx_y = new List<double>();
            List<double> pointsy_y = new List<double>();

            float endTime = beatmapNotes.Last().time;
            float totalTime = beatmapNotes.Last().time - beatmapNotes[0].time;

            Dictionary<float, Color> DistColorDict = new Dictionary<float, Color>();

            foreach (CurveParameter note in beatmapNotes)
            {
                float startTime = note.time;

                float distanceInBeats = totalTime - (endTime - startTime);

                pointsx.Add(distanceInBeats);

                pointsx_y.Add(note.xPos);

                pointsy_y.Add(note.yPos);

                if (note.color != Color.clear)
                {
                    DistColorDict.Add(distanceInBeats, note.color);
                }

            }

            colorDist = DistColorDict;

            CubicSpline splinex = CubicSpline.CreateNatural(pointsx, pointsx_y);
            CubicSpline spliney = CubicSpline.CreateNatural(pointsx, pointsy_y);

            curvex = splinex;
            curvey = spliney;

            widthCurve = CubicSpline.CreateNatural(pointsx, beatmapNotes.Select(p => (double)p.scale.x).ToList());
            heightCurve = CubicSpline.CreateNatural(pointsx, beatmapNotes.Select(p => (double)p.scale.y).ToList());
            depthCurve = CubicSpline.CreateNatural(pointsx, beatmapNotes.Select(p => (double)p.scale.z).ToList());
        }

        void OnGUI()
        {
            if (selectedCurvePoint != null)
            {
                CurveParameter parm = selectedCurvePoint;

                Rect mainRect = PaulmapperData.Instance.windowRect.getRect();
                Rect newWindowRect = GUI.Window(1, new Rect(mainRect.x + mainRect.width, mainRect.y, 300, 180), UpdateMenuData, "Curve Point");
            }
        }

        protected virtual void UpdateMenuData(int id)
        {
            CurveParameter parm = selectedCurvePoint;

            GUI.Label(new Rect(5, 20, 140 - 5, 25), $"xPos:");
            if (GUI.Button(new Rect(150 + 5, 20, 150 - 5, 20), parm.xPos.ToString("0.00"), "Label"))
            {
                PersistentUI.Instance.ShowInputBox("Point X-Position", new Action<string>(delegate (string t)
                {
                    float x;
                    if (float.TryParse(t, out x))
                        parm.anchorPoint.transform.position = new Vector3(x + parm.anchorPoint.parameterOffset.x, parm.anchorPoint.transform.position.y, parm.anchorPoint.transform.position.z);
                }));
            }



            GUI.Label(new Rect(5, 35, 140 - 5, 25), $"yPos:");
            if (GUI.Button(new Rect(150 + 5, 35, 150 - 5, 20), parm.yPos.ToString("0.00"), "Label"))
            {
                PersistentUI.Instance.ShowInputBox("Point Y-Position", new Action<string>(delegate (string t)
                {
                    float y;
                    if (float.TryParse(t, out y))
                        parm.anchorPoint.transform.position = new Vector3(parm.anchorPoint.transform.position.x, y + parm.anchorPoint.parameterOffset.y, parm.anchorPoint.transform.position.z);
                }));
            }

            GUI.Label(new Rect(5, 50, 140 - 5, 25), $"Width:");
            if (GUI.Button(new Rect(150 + 5, 50, 150 - 5, 20), parm.scale.x.ToString("0.00"), "Label"))
            {
                PersistentUI.Instance.ShowInputBox("Point Width", new Action<string>(delegate (string t)
                {
                    float width;
                    if (float.TryParse(t, out width))
                        parm.scale.x = width;
                }));
            }

            GUI.Label(new Rect(5, 65, 140 - 5, 25), $"Height:");
            if (GUI.Button(new Rect(150 + 5, 65, 150 - 5, 20), parm.scale.y.ToString("0.00"), "Label"))
            {
                PersistentUI.Instance.ShowInputBox("Point Height", new Action<string>(delegate (string t)
                {
                    float height;
                    if (float.TryParse(t, out height))
                        parm.scale.y = height;
                }));
            }

            GUI.Label(new Rect(5, 80, 140 - 5, 25), $"Length:");
            if (GUI.Button(new Rect(150 + 5, 80, 150 - 5, 20), parm.scale.z.ToString("0.00"), "Label"))
            {
                PersistentUI.Instance.ShowInputBox("Point Length", new Action<string>(delegate (string t)
                {
                    float height;
                    if (float.TryParse(t, out height))
                        parm.scale.z = height;
                }));
            }



            GUI.Label(new Rect(5, 110, 140 - 5, 25), $"Color:");
            if (GUI.Button(new Rect(150 + 5, 110, 150 - 5, 40), parm.color.ToString(), "Label"))
            {
                PersistentUI.Instance.ShowColorInputBox("Mapper", "bookmark.update.color", new Action<Color?>(delegate (Color? color)
                {
                    parm.color = color.HasValue ? color.Value : Color.clear;
                }), parm.color);
            }
        }

        public void FinishCurve()
        {
            List<BeatmapAction> actions = new List<BeatmapAction>();
            bool dotStart = false;

            foreach (BaseObject obj in curveObjects)
            {
                if (obj.CustomData != null && obj.CustomData["_isAnchor"]) obj.CustomData.Remove("_isAnchor");

                actions.Add(new BeatmapObjectModifiedAction(obj, obj, originalCurveObjects[curveObjects.IndexOf(obj)]));

                if (dotStart || (obj is Beatmap.V3.V3ColorNote note && curveObjects.IndexOf(obj) > 0 && note.CutDirection == 8 && PaulmapperData.Instance.arcs))
                {
                    dotStart = true;

                    BaseArc arc = PaulMaker.GenerateArc(curveObjects[curveObjects.IndexOf(obj) - 1] as BaseNote, obj as BaseNote, 8);
                    actions.Add(new BeatmapObjectPlacementAction(arc, new List<BaseObject>(), "Arcs"));
                }
            }



            foreach (CurveParameter param in curveParameters)
            {
                //Set notes to anchor points for future editing
                try
                {
                    BaseObject noteForAnc = curveObjects.OrderBy(p => p.SongBpmTime).OrderBy(p => Math.Abs(param.time - p.SongBpmTime)).First();

                    if (noteForAnc.CustomData == null)
                    {
                        noteForAnc.CustomData = new JSONObject();
                    }
                    noteForAnc.CustomData["_isAnchor"] = true;
                }
                catch
                {

                }
                

                Destroy(param.anchorPoint.gameObject);
            }

            BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, true));

            Destroy(gameObject);

            
        }
    }

    public class CurveParameter
    {
        public CurveAnchorPoint anchorPoint;

        public float time;
        public float xPos;
        public float yPos;

        public float? cutDirection;

        public Color color;

        public Vector3 scale;

        public bool dotPoint;
        public float dotTime;

        public CurveParameter(float time, float xPos, float yPos, Color color)
        {
            this.time = time;
            this.xPos = xPos;
            this.yPos = yPos;
            this.color = color;
        }

        public CurveParameter(BaseGrid note)
        {
            Vector2 notePos = note.GetRealPosition();

            this.time = note.SongBpmTime;
            this.xPos = notePos.x;
            this.yPos = notePos.y;

            Color col = Color.clear;
            Helper.TryGetColorFromObject(note, out col);
            
            this.color = col;

            Helper.GetObjectScale(note, out scale);

            if (note.ObjectType == Beatmap.Enums.ObjectType.Note)
            {
                cutDirection = (note as BaseNote).GetNoteDirection();
                this.dotPoint = (note as BaseNote).CutDirection == 8;
                this.dotTime = PaulmapperData.Instance.transitionTime;
            }
        }

    }

    public enum ScrollType
    {
        None = 0,
        Rotation = 1,
        Width,
        Height = 15,
        Duration
    }

    public class CurveAnchorPoint : MonoBehaviour
    {
        public delegate void MouseAction();
        public event MouseAction OnMiddleClick;
        public event MouseAction OnLeftClick;

        public delegate void ScrollAction(int dir, ScrollType scrollType);
        public event ScrollAction OnScroll;

        private Vector3 screenPoint;
        private Vector3 offset;

        public Vector3 parameterOffset = new Vector3(0.5f, 2, 0);

        public bool isHovering;

        public Material mainMat;
        public Material selectionMat;

        public MaterialPropertyBlock matProp;

        public CurveParameter param;

        void Start()
        {
            GetComponent<Renderer>().materials = new Material[] { mainMat, selectionMat };

            matProp = new MaterialPropertyBlock();
            matProp.SetColor(Shader.PropertyToID("_OutlineColor"), Color.green);
            matProp.SetColor(Shader.PropertyToID("_Color"), Color.magenta);
            matProp.SetFloat(Shader.PropertyToID("_ObjectTime"), float.PositiveInfinity);
            matProp.SetFloat(Shader.PropertyToID("_Lit"), 0);


            UpdateMaterials();
        }

        public void UpdateMaterials()
        {
            GetComponent<Renderer>().SetPropertyBlock(matProp);
        }

        void OnMouseDown()
        {
            screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
        }
        
        void OnMouseOver()
        {
            isHovering = true;

            if (Input.GetMouseButtonDown(0))
            {
                if (OnLeftClick != null)
                    OnLeftClick();
            }

            if (Input.GetMouseButtonDown(2))
            {
                if (OnMiddleClick != null)
                    OnMiddleClick();
            }

            if (OnScroll != null)
            {
                ScrollType scrollType = ScrollType.None;

                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        scrollType = ScrollType.Height;
                    } else if (Input.GetKey(KeyCode.LeftControl))
                    {
                        scrollType = ScrollType.Width;
                    } else
                    {
                        scrollType = ScrollType.Rotation;
                    }

                    if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                    {
                        OnScroll(1, scrollType);
                    }
                    else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                    {
                        OnScroll(-1, scrollType);
                    }
                }
            }
        }

        void OnMouseExit()
        {
            if (isHovering)
            {
                isHovering = false;
            }
        }

        void OnMouseDrag()
        {
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);

            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
            transform.position = new Vector3(curPosition.x, curPosition.y, transform.position.z);
        }

        public Vector2 GetAsParameter()
        {
            return transform.position - parameterOffset;
        }


    }
}
