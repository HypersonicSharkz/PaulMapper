using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Shared;
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
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Obstacle);

            float startTime = object1.SongBpmTime;
            float endTime = object2.SongBpmTime;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            while (distanceInBeats > 0 - 1 / (float)PaulmapperData.Instance.precision)
            {
                BaseObstacle copy = null;
                copy = (BaseObstacle)object1.Clone();

                copy.SongBpmTime = (endTime - distanceInBeats);
                if (copy.SongBpmTime > endTime)
                    break;

                float line = (originalDistance - distanceInBeats);

                var x = xCurve.ValueAt(line);
                var y = yCurve.ValueAt(line);

                copy.CustomData = new JSONObject();
                JSONNode customData = copy.CustomData;

                if (PaulmapperData.Instance.fakeWalls)
                {
                    if (PaulmapperData.IsV3())
                    {
                        customData["uninteractable"] = true;
                    } 
                    else
                    {
                        customData["_fake"] = true;
                        customData["_interactable"] = false;
                    }
                }

                collection.SpawnObject(copy, false, false);

                BaseObject beatmapObject = collection.UnsortedObjects.Last() as BaseObject;
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

                case ScrollType.Rotation:
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
            foreach (BaseObstacle wall in curveObjects)
            { 
                float time = wall.SongBpmTime - curveObjects[0].SongBpmTime;

                var x = xCurve.ValueAt(time);
                var y = yCurve.ValueAt(time);

                JSONNode customData = wall.CustomData;
                wall.SetPosition(new Vector2((float)x, (float)y));
                wall.SetScale(new Vector3((float)widthCurve.ValueAt(time), (float)heightCurve.ValueAt(time), (float)depthCurve.ValueAt(time)));

                float rotAtTime = GetRotationValueAtTime(wall.SongBpmTime, curveObjects);
                if (rotAtTime != -1)
                    wall.CustomWorldRotation = new Vector3(0, rotAtTime, 0);

                Color color = Color.white;
                //Color handling 
                if (colorDist != null && colorDist.Count > 0)
                {
                    color = PaulMaker.LerpColorFromDict(colorDist, time);
                    wall.SetColor(color);
                }

                ObjectContainer con;
                bool flag = beatmapObjectContainerCollection.LoadedContainers.TryGetValue(wall, out con);
                if (flag)
                {
                    con.UpdateGridPosition();

                    if (colorDist != null && colorDist.Count > 0)
                        (con as ObstacleContainer).SetColor(color);
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
