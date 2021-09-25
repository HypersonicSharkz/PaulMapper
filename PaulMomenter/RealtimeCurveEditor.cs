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

namespace PaulMapper
{
    public class RealtimeCurve : MonoBehaviour
    {
        public Curve xCurve;
        public Curve yCurve;
        public Dictionary<float, Color> colorDist = new Dictionary<float, Color>();

        List<BeatmapNote> initalNotes = new List<BeatmapNote>();

        public List<CurveParameter> curveParameters = new List<CurveParameter>();

        public BeatmapNote note1;
        public BeatmapNote note2;

        public List<BeatmapNote> poodleNotes = new List<BeatmapNote>();

        BeatmapObjectContainerCollection beatmapObjectContainerCollection;
        TracksManager TracksManager;

        Track curveTrack;
        private bool actionMapsDisabled;

        public void InstantiateCurve(List<BeatmapNote> parameters)
        {
            beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();
            TracksManager = FindObjectOfType<TracksManager>();
            BeatmapNote[] beatmapNotes = parameters.OrderBy(o => o.Time).ToArray();

            if (beatmapNotes.Count() != beatmapNotes.Select(p => p.Time).Distinct().Count())
            {
                Destroy(this);
                return;
            }


            curveTrack = TracksManager.CreateTrack(0);

            note1 = beatmapNotes[0];
            note2 = beatmapNotes.Last();
            this.initalNotes = parameters;

            curveParameters = NotesToParameters(beatmapNotes.ToList());

            GetCurves(curveParameters);
            SpawnNotes();
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

                    GetCurves(curveParameters);
                    UpdateNotes();
                }     
                
                if (!SelectionController.HasSelectedObjects())
                {
                    FinishCurve();
                }
            }
        }

        private void UpdateNotes()
        {
            BeatmapNote oldNote = null;
            foreach (BeatmapNote note in poodleNotes)
            {
                float time = note.Time - poodleNotes[0].Time;

                var x = xCurve.ValueAt(time);
                var y = yCurve.ValueAt(time);

                JSONNode customData = note.CustomData;
                customData["_position"] = new Vector2((float)x, (float)y);

                //Color handling 
                if (colorDist != null && colorDist.Count > 0)
                {
                    customData["_color"] = PaulMaker.LerpColorFromDict(colorDist, time);
                }

                //Now update direction
                JSONNode customData_old = null;
                if (PaulmapperData.Instance.rotateNotes)
                {
                    //Fix rotation
                    if (oldNote != null)
                    {
                        //Find angle for old object to face new one
                        Vector2 op = oldNote.GetRealPosition();
                        Vector2 cp = note.GetRealPosition();

                        float ang = Mathf.Atan2(cp.y - op.y, cp.x - op.x) * 180 / Mathf.PI;
                        ang += 90;


                        //Set rotation
                        customData_old = oldNote.CustomData;
                        customData_old["_cutDirection"] = ang;
                    }
                }
                if (note == poodleNotes.Last())
                {
                    customData["_cutDirection"] = customData_old["_cutDirection"];
                }


                if (dotTimes != null)
                {
                    try
                    {
                        dotTimes.Sort();
                        float closeDotTime = dotTimes.Last(d => oldNote.Time > d);
                        if (oldNote.Time - closeDotTime < 2 * PaulmapperData.Instance.transitionTime)
                        {
                            oldNote.CutDirection = 8;

                            if (!PaulmapperData.Instance.transitionRotation && customData_old != null)
                            {
                                customData_old["_cutDirection"] = 0;
                            }
                        }

                    }
                    catch
                    {

                    }

                }



                oldNote = note;

                BeatmapObjectContainer con;
                bool flag = beatmapObjectContainerCollection.LoadedContainers.TryGetValue(note, out con);
                if (flag)
                {
                    con.UpdateGridPosition();
                    BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);
                    NotesContainer notesContainer = collection as NotesContainer;
                    notesContainer.RefreshSpecialAngles(note, false, false);
                }
            }

           
        }

        private void SpawnAnchorPoints()
        {
            //Spawn gameobject for each note
            foreach (CurveParameter point in curveParameters)
            {
                SpawnAnchorPoint(point);
            }

            //then delete notes
            
            foreach (BeatmapObject beatmapObject in initalNotes)
            {
                beatmapObjectContainerCollection.DeleteObject(beatmapObject, false);
            }
            
            BeatmapActionContainer.AddAction(new SelectionPastedAction(poodleNotes, initalNotes));

           
            foreach (BeatmapObject note in poodleNotes)
            {
                SelectionController.Select(note, true, true, false);
            }
            
        }

        private void SpawnAnchorPoint(CurveParameter curveParameter)
        {
            float zPos = (curveParameter.time - PaulMomenter.ats.CurrentBeat) * EditorScaleController.EditorScale;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            CurveAnchorPoint point = sphere.AddComponent<CurveAnchorPoint>();

            sphere.transform.parent = curveTrack.ObjectParentTransform;
            sphere.transform.position = new Vector3(curveParameter.xPos, curveParameter.yPos, zPos) + point.parameterOffset;

            sphere.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);


            curveParameter.anchorPoint = point;

            if (curveParameter != curveParameters.Last() && curveParameter != curveParameters.First())
                point.OnMiddleClick += delegate (){ DeleteAnchorPoint(curveParameter); };
        }

        private void AddAnchorPoint()
        {
            if (!curveParameters.Any(c => c.time == PaulMomenter.ats.CurrentBeat))
            {
                if (PaulMomenter.ats.CurrentBeat < curveParameters.Last().time
                    && PaulMomenter.ats.CurrentBeat > curveParameters.First().time)
                {
                    //Is between first 2 points
                    CurveParameter newCP = new CurveParameter(PaulMomenter.ats.CurrentBeat, 0, 0, Color.clear);
                    SpawnAnchorPoint(newCP);
                    curveParameters.Add(newCP);
                    curveParameters = curveParameters.OrderBy(c => c.time).ToList();
                }
            }


        }

        private void DeleteAnchorPoint(CurveParameter param)
        {
            curveParameters.Remove(param);
            Destroy(param.anchorPoint.gameObject);
        }

        List<float> dotTimes = null;
        private void SpawnNotes()
        {
            
            if (PaulmapperData.Instance.autoDot)
            {
                dotTimes = initalNotes.Where(p => (p as BeatmapNote).CutDirection == 8).Select(p => p.Time - PaulmapperData.Instance.transitionTime).ToList();
            }

            poodleNotes = PaulMaker.GeneratePoodle(
                    note1, note2, 
                    xCurve, yCurve, 
                    PaulmapperData.Instance.precision, 
                    initalNotes.All(p => p.CutDirection == 8), 
                    colorDist, dotTimes
                ).Cast<BeatmapNote>().ToList();
        }

        private List<CurveParameter> NotesToParameters(List<BeatmapNote> beatmapNotes)
        {
            return beatmapNotes.Select(n =>
                new CurveParameter(n)).OrderBy(c => c.time).ToList();
        }

        private void GetCurves(List<CurveParameter> beatmapNotes)
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

            CubicSpline splinex = CubicSpline.CreateNatural(pointsx, pointsx_y);
            CubicSpline spliney = CubicSpline.CreateNatural(pointsx, pointsy_y);

            xCurve = splinex;
            yCurve = spliney;
        }

        public void FinishCurve()
        {
            foreach (CurveParameter param in curveParameters)
            {
                Destroy(param.anchorPoint.gameObject);
            }
            Destroy(gameObject);
        }
    }

    public class CurveParameter
    {
        public CurveAnchorPoint anchorPoint;

        public float time;
        public float xPos;
        public float yPos;

        public Color color;

        public CurveParameter(float time, float xPos, float yPos, Color color)
        {
            this.time = time;
            this.xPos = xPos;
            this.yPos = yPos;
            this.color = color;
        }

        public CurveParameter(BeatmapNote note)
        {
            Vector2 notePos = note.GetRealPosition();

            this.time = note.Time;
            this.xPos = notePos.x;
            this.yPos = notePos.y;

            Color color = Color.clear;
            Helper.TryGetColorFromObject(note, out color);

            this.color = color;
        }
    }

    public class CurveAnchorPoint : MonoBehaviour
    {
        public delegate void DeleteAnchorPoint();
        public event DeleteAnchorPoint OnMiddleClick;

        private Vector3 screenPoint;
        private Vector3 offset;

        public Vector3 parameterOffset = new Vector3(0.5f, 2, 0);

        public bool isHovering;

        void OnMouseDown()
        {
            screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
        }
        
        void OnMouseOver()
        {
            if (!isHovering)
            {
                isHovering = true;
            }
            if (Input.GetMouseButtonDown(2))
            {
                if (OnMiddleClick != null)
                    OnMiddleClick();
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
