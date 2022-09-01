using Extreme.Mathematics.Curves;
using PaulMapper.PaulHelper;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace PaulMapper
{
    class RealtimeWallCurve : RealtimeCurve
    {

        protected override void SpawnObjects()
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle);

            float startTime = object1.Time;
            float endTime = object2.Time;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BeatmapObject> spawnedBeatobjects = new List<BeatmapObject>();

            while (distanceInBeats > 0 - 1 / (float)PaulmapperData.Instance.precision)
            {
                BeatmapObstacle copy = null;
                if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
                {
                    copy = PaulHelper.PaulMaker.CopyV3Wall(object1.ConvertToJson());
                }
                else
                {
                    copy = new BeatmapObstacle(object1.ConvertToJson());
                }

                copy.Time = (endTime - distanceInBeats);
                if (copy.Time > endTime)
                    break;

                float line = (originalDistance - distanceInBeats);

                var x = xCurve.ValueAt(line);
                var y = yCurve.ValueAt(line);

                copy.CustomData = new JSONObject();
                JSONNode customData = copy.CustomData;

                if (PaulmapperData.Instance.fakeWalls)
                {
                    customData["_fake"] = true;

                    if (BeatSaberSongContainer.Instance.Map.Version == "3.0.0")
                        customData["uninteractable"] = true;
                    else
                        customData["_interactable"] = false;
                }

                collection.SpawnObject(copy, false, false);

                BeatmapObject beatmapObject = collection.UnsortedObjects.Last();
                spawnedBeatobjects.Add(beatmapObject);

                distanceInBeats -= 1 / (float)PaulmapperData.Instance.precision;
            }

            curveObjects = spawnedBeatobjects;
        }

        protected override void SpawnAnchorPoint(CurveParameter curveParameter)
        {
            base.SpawnAnchorPoint(curveParameter);


            curveParameter.anchorPoint.OnScroll += delegate (int dir, ScrollType scrollType) { AnchorPoint_OnScroll(curveParameter, dir, scrollType); };

        }

        float scalingMul = 0.01f;

        private void AnchorPoint_OnScroll(CurveParameter curveWallParameter, int dir, ScrollType scrollType)
        {
            switch (scrollType)
            {
                case ScrollType.Width:
                    curveWallParameter.scale.x += scalingMul * dir;
                    break;

                case ScrollType.Height: 
                    curveWallParameter.scale.y += scalingMul * dir;
                    break;

                case ScrollType.Duration:
                    curveWallParameter.scale.z += scalingMul * dir;
                    break;
            }
            
        }

        protected override void GetCurves(List<CurveParameter> beatmapNotes, out Curve curvex, out Curve curvey)
        {
            base.GetCurves(beatmapNotes, out curvex, out curvey);
        }

        protected override void UpdateObjects()
        {
            foreach (BeatmapObstacle wall in curveObjects)
            { 
                float time = wall.Time - curveObjects[0].Time;

                var x = xCurve.ValueAt(time);
                var y = yCurve.ValueAt(time);

                JSONNode customData = wall.CustomData;
                wall.SetPosition(new Vector2((float)x, (float)y));
                wall.SetScale(new Vector2((float)widthCurve.ValueAt(time), (float)heightCurve.ValueAt(time)));

                Color color = Color.white;
                //Color handling 
                if (colorDist != null && colorDist.Count > 0)
                {
                    color = PaulMaker.LerpColorFromDict(colorDist, time);
                    wall.SetColor(color);
                }

                BeatmapObjectContainer con;
                bool flag = beatmapObjectContainerCollection.LoadedContainers.TryGetValue(wall, out con);
                if (flag)
                {
                    con.UpdateGridPosition();

                    if (colorDist != null && colorDist.Count > 0)
                        (con as BeatmapObstacleContainer).SetColor(color);
                }
            }
        }

        protected override void UpdateMenuData(int id)
        {
            base.UpdateMenuData(id);
            CurveParameter parm = selectedCurvePoint;
        }
    }
}
