using Beatmap.Base;
using Beatmap.Containers;
using Extreme.Mathematics.Curves;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PaulMapper
{
    public class RealtimeCurve : MonoBehaviour
    {
        public static bool Editing;

        public Curve xCurve;
        public Curve yCurve;

        public Curve widthCurve;
        public Curve heightCurve;
        public Curve depthCurve;

        public Dictionary<float, Color> colorDist = new Dictionary<float, Color>();

        public List<BaseGrid> initialObjects = new List<BaseGrid>();

        public List<CurveParameter> curveParameters = new List<CurveParameter>();

        public BaseGrid object1;
        public BaseGrid object2;

        public List<BaseGrid> curveObjects = new List<BaseGrid>();

        protected BeatmapObjectContainerCollection beatmapObjectContainerCollection;
        protected EventGridContainer eventsContainer;
        protected TracksManager TracksManager;

        Track curveTrack;
        private bool actionMapsDisabled;

        public CurveParameter selectedCurvePoint;
        public Material mainMat;
        public Material selectionMat;

        List<BaseGrid> originalCurveObjects = new List<BaseGrid>();

        private void Start()
        {
            RealtimeCurve.Editing = true;
            StartCurvePointEditor();

            if (Plugin.addAnchor != null)
                Plugin.addAnchor.performed += AddAnchorPoint;
        }

        private void OnDisable()
        {
            Debug.Log("Curve Object Destroyed");

            if (Plugin.addAnchor != null)
                Plugin.addAnchor.performed -= AddAnchorPoint;

            FinishCurveEditor();
        }

        private void StartCurvePointEditor()
        {
            CurvePointEditor.ParameterChanged += CurvePointEditor_ParameterChanged;
        }

        public virtual void InstantiateCurve(List<BaseGrid> parameters)
        {
            beatmapObjectContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(parameters[0].ObjectType);
            eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Event) as EventGridContainer;

            TracksManager = FindObjectOfType<TracksManager>();
            BaseGrid[] beatmapObjects = parameters.OrderBy(o => o.SongBpmTime).ToArray();


            //Materials are weird I think
            NoteContainer con = GameObject.FindObjectOfType<NoteContainer>(true);

            mainMat = con.GetComponentsInChildren<MeshRenderer>()[0].material;
            selectionMat = con.GetComponentsInChildren<MeshRenderer>()[1].material;

            if (beatmapObjects.Count() != beatmapObjects.Select(p => p.SongBpmTime).Distinct().Count())
            {
                Plugin.paulMapper?.SetNotice("2 notes can't be on the same beat!", noticeType.Error);
                Destroy(this);
                return;
            }


            curveTrack = TracksManager.CreateTrack(0);

            object1 = (BaseGrid)beatmapObjects.First().Clone();
            object2 = (BaseGrid)beatmapObjects.Last().Clone();
            this.initialObjects = parameters;

            curveParameters = ObjectsToParameters(beatmapObjects.ToList());

            GetCurves(curveParameters, out xCurve, out yCurve);

            //then delete notes
            foreach (BaseObject beatmapObject in initialObjects)
            {
                beatmapObjectContainerCollection.DeleteObject(beatmapObject, false);
            }

            SpawnObjects();
            originalCurveObjects = curveObjects.Select(c => (BaseGrid)c.Clone()).ToList();
            BeatmapActionContainer.AddAction(new SelectionPastedAction(curveObjects, initialObjects));

            SpawnAnchorPoints();

            PaulMapper.uiHandler.UpdateSelectionUI();
        }

        private void Update()
        {
            bool anyIsHovering = curveParameters.Any(a => a.anchorPoint.isHovering);
            if (!actionMapsDisabled && anyIsHovering)
            {
                CMInputCallbackInstaller.DisableActionMaps(typeof(PaulMapper), PaulActions.actionMapsDisabled);
                actionMapsDisabled = true;
            }
            else if (actionMapsDisabled && !anyIsHovering)
            {
                CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(PaulMapper), PaulActions.actionMapsDisabled);
                actionMapsDisabled = false;
            }

            if (xCurve != null && yCurve != null)
            {
                if (!SelectionController.HasSelectedObjects())
                {
                    FinishCurve();
                }
            }

            if (selectedCurvePoint != null)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        selectedCurvePoint.rotation += new Vector3(0, 0, PaulMapperData.INSTANCE.WallRotationAmount);
                        UpdateAnchorPoints();
                    }
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        selectedCurvePoint.rotation += new Vector3(0, 0, -PaulMapperData.INSTANCE.WallRotationAmount);
                        UpdateAnchorPoints();
                    }
                }

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        selectedCurvePoint.rotation += new Vector3(PaulMapperData.INSTANCE.WallRotationAmount, 0, 0);
                        UpdateAnchorPoints();
                    }
                }

                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        selectedCurvePoint.rotation += new Vector3(-PaulMapperData.INSTANCE.WallRotationAmount, 0, 0);
                        UpdateAnchorPoints();
                    }
                }
            }
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

            foreach (BaseObject note in curveObjects)
            {
                SelectionController.Select(note, true, true, false);
            }

            UpdateQuickMenu();
        }

        private void UpdateQuickMenu()
        {
            PaulMapper.uiHandler.UpdateQuickMenu();
        }

        protected virtual void SpawnAnchorPoint(CurveParameter curveParameter)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            CurveAnchorPoint point = sphere.AddComponent<CurveAnchorPoint>();
            point.mainMat = mainMat;
            point.selectionMat = selectionMat;
            point.param = curveParameter;

            float zPos = (curveParameter.time - PaulMapper.ats.CurrentSongBpmTime) * EditorScaleController.EditorScale;

            sphere.transform.parent = curveTrack.ObjectParentTransform;
            sphere.transform.position = (new Vector3(curveParameter.xPos, curveParameter.yPos, zPos) + point.parameterOffset) * 0.6f + new Vector3(0,0,1);

            sphere.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);


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

                //Create new UI

            };

            point.OnPointDrag += delegate ()
            {
                UpdateAnchorPoints();
            };
        }
        float minTimeDif = 0.01f;

        protected void UpdateAnchorPoints()
        {
            if (selectedCurvePoint != null)
            {
                selectedCurvePoint.xPos = selectedCurvePoint.anchorPoint.GetAsParameter().x;
                selectedCurvePoint.yPos = selectedCurvePoint.anchorPoint.GetAsParameter().y;
            }

            UpdateCurvePointEditor(selectedCurvePoint);

            GetCurves(curveParameters, out xCurve, out yCurve);
            UpdateObjects();

            PaulMapper.uiHandler.UpdateSelectionUI();
        }

        private void UpdateCurvePointEditor(CurveParameter p)
        {
            CurvePointEditor.UpdatePoint(p);
        }

        private void AddAnchorPoint(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            Debug.Log("Adding Anchor Point");

            float time = curveObjects.First(o => o.SongBpmTime >= PaulMapper.ats.CurrentSongBpmTime).SongBpmTime;
            if (!curveParameters.Any(c => Math.Abs(c.time - time) < minTimeDif))
            {
                if (PaulMapper.ats.CurrentSongBpmTime < curveParameters.Last().time
                    && PaulMapper.ats.CurrentSongBpmTime > curveParameters.First().time)
                {
                    //Is between first 2 points
                    CurveParameter newCP = new CurveParameter(curveObjects.First(o => o.SongBpmTime >= PaulMapper.ats.CurrentSongBpmTime) as BaseGrid);
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
            UpdateAnchorPoints();
        }

        protected virtual void SpawnObjects()
        {
            UpdateAnchorPoints();

            foreach (CurveParameter param in curveParameters)
            {
                BaseObject noteForAnc = curveObjects.OrderBy(p => p.SongBpmTime).OrderBy(p => Math.Abs(param.time - p.SongBpmTime)).First();

                if (noteForAnc.CustomData == null)
                {
                    noteForAnc.CustomData = new JSONObject();
                }
                noteForAnc.CustomData["_isAnchor"] = true;

                noteForAnc.WriteCustom();
            }
        }

        private List<CurveParameter> ObjectsToParameters(List<BaseGrid> beatmapObjects)
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

        public void FinishCurve()
        {
            List<BeatmapAction> actions = new List<BeatmapAction>();
            bool dotStart = false;
            Debug.Log("Finish");
            foreach (BaseGrid obj in curveObjects)
            {
                if (!BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).ContainsObject(obj))
                {
                    Debug.Log("Note not found in collection? Possible undo");
                    continue;
                }

                actions.Add(new BeatmapObjectUpdatedAction(obj, originalCurveObjects[curveObjects.IndexOf(obj)]));

                if (obj.CustomData != null && obj.CustomData["_isAnchor"]) obj.CustomData.Remove("_isAnchor");

                obj.WriteCustom();

                if (dotStart || (obj is BaseNote note && curveObjects.IndexOf(obj) > 0 && note.CutDirection == 8 && PaulMapperData.INSTANCE.Arcs))
                {
                    dotStart = true;

                    BaseArc arc = PoodleGenerator.GenerateArc(curveObjects[curveObjects.IndexOf(obj) - 1] as BaseNote, obj as BaseNote, 8);
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

                    noteForAnc.WriteCustom();
                }
                catch
                {

                }
                

                Destroy(param.anchorPoint.gameObject);
            }

            if (actions.Count > 0)
                BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, true));
            
            Destroy(gameObject);
        }

        private void FinishCurveEditor()
        {
            RealtimeCurve.Editing = false;
            CurvePointEditor.UpdatePoint(null);
            CurvePointEditor.ParameterChanged -= CurvePointEditor_ParameterChanged;
        }

        private void CurvePointEditor_ParameterChanged()
        {
            UpdateAnchorPoints();
        }
    }

    public class CurveParameter
    {
        public Beatmap.Enums.ObjectType type;

        public CurveAnchorPoint anchorPoint;

        public float time;
        public float xPos;
        public float yPos;

        public float? cutDirection;

        public Color color;
        public Vector3 rotation;

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

            Color col = note.GetColor();
            this.color = col;

            scale = note.GetObjectScale();

            type = note.ObjectType;

            if (type == Beatmap.Enums.ObjectType.Note)
            {
                cutDirection = (note as BaseNote).GetNoteDirection();
                this.dotPoint = (note as BaseNote).CutDirection == 8;
                this.dotTime = PaulMapperData.INSTANCE.TransitionTime;
            }

            rotation = note.GetRotation();
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
        public event MouseAction OnPointDrag;

        public delegate void ScrollAction(int dir, ScrollType scrollType);
        public event ScrollAction OnScroll;

        private Vector3 screenPoint;
        private Vector3 offset;

        public Vector3 parameterOffset = new Vector3(0.5f, 3, 0);

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
                ScrollType scrollType = ScrollType.Rotation;

                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                        scrollType = ScrollType.Height;
                    else if (Input.GetKey(KeyCode.LeftControl))
                        scrollType = ScrollType.Width;

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

            if (OnPointDrag != null)
                OnPointDrag();
        }

        public Vector2 GetAsParameter()
        {
            return (transform.position) / 0.6f - parameterOffset;
        }


    }
}
