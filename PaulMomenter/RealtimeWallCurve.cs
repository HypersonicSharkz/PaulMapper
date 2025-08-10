﻿using Beatmap.Base;
using Beatmap.Containers;
using Extreme.Mathematics.Curves;
using PaulMapper.PaulHelper;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PaulMapper
{
    class RealtimeWallCurve : RealtimeCurve
    {

        protected override void SpawnObjects()
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Obstacle);

            float startTime = object1.JsonTime;
            float endTime = object2.JsonTime;

            float distanceInBeats = endTime - startTime;
            float originalDistance = distanceInBeats;

            float npsStart = PaulMapperData.Instance.precision;
            float npsEnd = PaulMapperData.Instance.useEndPrecision ? PaulMapperData.Instance.endPrecision : PaulMapperData.Instance.precision;

            float precision = npsStart;

            List<BaseObject> spawnedBeatobjects = new List<BaseObject>();

            while (distanceInBeats > 0 - 1 / precision)
            {
                BaseObstacle copy = null;
                copy = (BaseObstacle)object1.Clone();

                copy.JsonTime = (endTime - distanceInBeats);
                if (copy.JsonTime > endTime)
                    break;

                float line = (originalDistance - distanceInBeats);

                var x = xCurve.ValueAt(line);
                var y = yCurve.ValueAt(line);

                copy.CustomData = new JSONObject();
                JSONNode customData = copy.CustomData;

                if (PaulMapperData.Instance.fakeWalls)
                {
                    if (PaulMapperData.IsV3())
                    {
                        customData["uninteractable"] = true;
                    } 
                    else
                    {
                        customData["_fake"] = true;
                        customData["_interactable"] = false;
                    }
                }

                if (copy.CustomWorldRotation != null)
                {
                    Vector3 rot = copy.CustomWorldRotation.ReadVector3(new Vector3(0, 0, 0));
                    copy.CustomWorldRotation = new Vector3(rot.x, rot.y, 0);
                }

                copy.WriteCustom();
                collection.SpawnObjectFix(copy, false, true);

                BaseObject beatmapObject = copy;
                spawnedBeatobjects.Add(beatmapObject);

                precision = Mathf.Lerp(npsEnd, npsStart, distanceInBeats / (endTime - startTime));
                distanceInBeats -= 1 / precision;
            }

            curveObjects = spawnedBeatobjects;
            base.SpawnObjects();
        }

        protected override void SpawnAnchorPoint(CurveParameter curveParameter)
        {
            base.SpawnAnchorPoint(curveParameter);


            curveParameter.anchorPoint.OnScroll += (int dir, ScrollType scrollType) => AnchorPoint_OnScroll(curveParameter, dir, scrollType);

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
            UpdateAnchorPoints();
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

                float? rotAtTime = Helper.GetRotationValueAtTime(wall.SongBpmTime, curveObjects);
                if (rotAtTime.HasValue)
                    wall.CustomWorldRotation = new Vector3(0, rotAtTime.Value, 0);

                //Local rotation
                //First get the two points before and after note
                CurveParameter paramBefore = curveParameters.Last(p => p.time <= wall.SongBpmTime);
                CurveParameter paramAfter = curveParameters.First(p => p.time >= wall.SongBpmTime);

                float lerpTime = 1;
                if (paramBefore != paramAfter)
                    lerpTime = (wall.SongBpmTime - paramBefore.time) / (paramAfter.time - paramBefore.time);

                float rotX = Mathf.Lerp(paramBefore.rotation.x, paramAfter.rotation.x, lerpTime);
                float rotY = Mathf.Lerp(paramBefore.rotation.y, paramAfter.rotation.y, lerpTime);
                float rotZ = Mathf.Lerp(paramBefore.rotation.z, paramAfter.rotation.z, lerpTime);

                wall.CustomLocalRotation = new Vector3(rotX, rotY, rotZ);

                Color color = Color.white;
                //Color handling 
                if (colorDist != null && colorDist.Count > 0)
                {
                    color = PaulMaker.LerpColorFromDict(colorDist, time);
                    wall.SetColor(color);
                }

                wall.WriteCustom();

                if (beatmapObjectContainerCollection.LoadedContainers.TryGetValue(wall, out ObjectContainer con))
                {
                    con.UpdateGridPosition();
                    if (wall.CustomLocalRotation == null || wall.CustomLocalRotation.ReadVector3() == Vector3.zero)
                        con.Animator.LocalTarget.localEulerAngles = Vector3.zero;

                    if (colorDist != null && colorDist.Count > 0)
                        (con as ObstacleContainer).SetColor(color);
                }
            }
        }
    }
}
