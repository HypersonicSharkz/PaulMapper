using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PaulMapper
{
    static class WorldRotationHelper
    {
        public static float? GetRotationValueAtTime(float time, List<BaseGrid> beatmapObjects)
        {
            //Get all relevant rotations
            RotationEventGridContainer rotationsContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.RotationEvent) as RotationEventGridContainer;
            IEnumerable<BaseRotationEvent> rotations = rotationsContainer.MapObjects.Where(x => MathUtil.CompareRound(x.SongBpmTime, beatmapObjects.First().SongBpmTime, 0.0001f) != -1 && MathUtil.CompareRound(x.SongBpmTime, beatmapObjects.Last().SongBpmTime, 0.0001f) != 1).OrderBy(x => x.SongBpmTime);

            BaseRotationEvent rotEvent = rotations.LastOrDefault(x => x.SongBpmTime <= time);
            if (rotEvent == null)
            {
                if (rotations.Count() == 1)
                {
                    rotEvent = rotations.First();
                }
                else
                    return null;
            }

            float t1 = rotEvent.SongBpmTime;

            //Rotation at first note
            float rot1 = rotationsContainer.MapObjects.Where(x => x.SongBpmTime < t1).Sum(x => x.Rotation);
            float rot2 = rot1 + rotEvent.Rotation;


            //Get time of last rotation, or last note if it is further away
            float t2 = 0;
            BaseRotationEvent rotEventEnd = rotations.FirstOrDefault(x => x.SongBpmTime >= time);

            if (rotEventEnd == null || rotEventEnd.SongBpmTime > beatmapObjects.Last().SongBpmTime)
                t2 = beatmapObjects.Last().SongBpmTime;
            else
                t2 = rotEventEnd.SongBpmTime;

            if (t1 == t2)
                return rot1;

            return Mathf.Lerp(rot1, rot2, (time - t1) / (t2 - t1));
        }
    }
}
