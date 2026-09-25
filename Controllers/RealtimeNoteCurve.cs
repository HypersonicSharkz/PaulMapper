using Beatmap.Base;
using Beatmap.Containers;
using SimpleJSON;
using System;
using System.Linq;
using UnityEngine;

namespace PaulMapper
{
    class RealtimeNoteCurve : RealtimeCurve
    {
        float scalingMul = 0.1f;

        protected override void SpawnObjects()
        {
            /*curveObjects = PaulMaker.GeneratePoodle(object1,
                                                    object2,
                                                    PaulMapperData.INSTANCE.precision,
                                                    PaulMapperData.INSTANCE.useEndPrecision ? PaulMapperData.INSTANCE.endPrecision : PaulMapperData.INSTANCE.precision,
                                                    initialObjects.All(p => (p as BaseNote).CutDirection == 8));
            */

            curveObjects = PoodleGenerator.SpawnBasePoodle(object1, object2);

            base.SpawnObjects();
        }

        protected override void SpawnAnchorPoint(CurveParameter curveParameter)
        {
            base.SpawnAnchorPoint(curveParameter);

            curveParameter.anchorPoint.OnScroll += delegate (int dir, ScrollType scrollType) { AnchorPoint_OnScroll(curveParameter, dir, scrollType); };
        }
        
        private void AnchorPoint_OnScroll(CurveParameter curveParameter, int dir, ScrollType scrollType)
        {
            switch (scrollType)
            {
                case ScrollType.Width:
                    curveParameter.scale.x += scalingMul * dir;
                    break;

                case ScrollType.Height:
                    curveParameter.scale.y += scalingMul * dir;
                    break;

                case ScrollType.Duration:
                    curveParameter.scale.z += scalingMul * dir;
                    break;
            }

            if (PaulMapperData.INSTANCE.UsePointRotations && curveParameter.cutDirection.HasValue && (scrollType == ScrollType.Rotation))
            {
                curveParameter.cutDirection += 1 * (int)scrollType * dir;
            }

            UpdateAnchorPoints();
        }

        protected override void UpdateObjects()
        {
            BaseNote oldNote = null;
            int noteIndex = 0;
            foreach (BaseNote note in curveObjects)
            {
                noteIndex++;

                float time = note.SongBpmTime - curveObjects[0].SongBpmTime;

                var x = xCurve.ValueAt(time);
                var y = yCurve.ValueAt(time);

                JSONNode customData = note.CustomData;
                note.CustomCoordinate = new Vector2((float)x, (float)y);
                if (PaulMapperData.INSTANCE.UseScale)
                {
                    note.SetScale(new Vector3((float)widthCurve.ValueAt(time), (float)heightCurve.ValueAt(time), (float)depthCurve.ValueAt(time)));
                }

                float? rotAtTime = WorldRotationHelper.GetRotationValueAtTime(note.SongBpmTime, curveObjects);
                if (rotAtTime.HasValue)
                    note.CustomWorldRotation = new Vector3(0, rotAtTime.Value, 0);

                Color color = Color.white;
                //Color handling 
                if (colorDist != null && colorDist.Count > 0)
                {
                    color = ColorHelper.LerpColorFromDict(colorDist, time);
                    note.CustomColor = color;
                }

                //Now update direction
                JSONNode customData_old = null;
                if (PaulMapperData.INSTANCE.RotateNotes)
                {
                    //Fix rotation
                    if (oldNote != null)
                    {

                        if (PaulMapperData.INSTANCE.UsePointRotations)
                        {
                            //Directions are being forced

                            //First get the two points before and after note
                            CurveParameter paramBefore = curveParameters.Last(p => p.time < note.SongBpmTime);
                            CurveParameter paramAfter = curveParameters.First(p => p.time >= note.SongBpmTime);

                            float lerpTime = (note.SongBpmTime - paramBefore.time) / (paramAfter.time - paramBefore.time);

                            float ang = Mathf.Lerp(paramBefore.cutDirection.Value, paramAfter.cutDirection.Value, lerpTime);

                            //Set rotation
                            customData_old = oldNote.CustomData;
                            oldNote.CutDirection = 0;

                            if (PaulMapperData.INSTANCE.Vibro)
                            {
                                ang += 180 * (noteIndex % 2);
                            }

                            oldNote.SetRotation(ang);

                        }
                        else
                        {
                            oldNote.CutDirection = 0;

                            Vector2 op = oldNote.GetPosition();
                            Vector2 cp = note.GetPosition();

                            float xPos = cp.x;
                            float yPos = cp.y;


                            if (PaulMapperData.INSTANCE.AdjustToWorldRotation && rotAtTime.HasValue)
                            {
                                float oldWorldRot = WorldRotationHelper.GetRotationValueAtTime(oldNote.SongBpmTime, curveObjects) ?? 0;
                                float rotDif = (rotAtTime.Value - oldWorldRot) * (Mathf.PI / 180f);

                                xPos += Mathf.Cos(Mathf.PI / 2 - rotDif);// * (note.SongBpmTime - oldNote.SongBpmTime);
                            }

                            float ang = Mathf.Atan2(yPos - op.y, xPos - op.x) * 180 / Mathf.PI;
                            ang += 90;


                            //Set rotation
                            customData_old = oldNote.CustomData;

                            if (PaulMapperData.INSTANCE.Vibro)
                            {
                                ang = Mathf.Atan2(Math.Abs(cp.y - op.y), Math.Abs(cp.x - op.x)) * 180 / Mathf.PI;
                                ang += 90;

                                ang += 180 * (noteIndex % 2);
                            }

                            oldNote.SetRotation(ang);
                        }
                        //Now check dots
                        if (curveParameters.Any(c => c.dotPoint && Math.Abs(oldNote.SongBpmTime - c.time) < c.dotTime))
                        {
                            oldNote.CutDirection = 8;
                            if (!PaulMapperData.INSTANCE.TransitionRotation)
                                oldNote.SetRotation(0);
                        }
                    }

                    
                    if (note == curveObjects.Last())
                    {
                        note.CutDirection = oldNote.CutDirection;
                        if (PaulMapperData.INSTANCE.Vibro)
                            note.SetRotation(oldNote.GetNoteDirection() + 180f);
                        else
                        {
                            note.SetRotation(oldNote.GetNoteDirection());
                        }
                    }
                }
                else if (PaulMapperData.INSTANCE.Vibro)
                {
                    note.SetRotation(180 * (noteIndex % 2));
                }

                if (PaulMapperData.IsV3())
                {
                    if (PaulMapperData.INSTANCE.DisableBadCutDirection)
                    {
                        customData["disableBadCutDirection"] = true;
                    }
                    if (PaulMapperData.INSTANCE.DisableBadCutSpeed)
                    {
                        customData["disableBadCutSpeed"] = true;
                    }
                    if (PaulMapperData.INSTANCE.DisableBadCutSaberType)
                    {
                        customData["disableBadCutSaber"] = true;
                    }
                }

                note.WriteCustom();

                oldNote = note;
            }

            foreach (BaseNote note in curveObjects)
            {
                UpdateGraphics(note, note.CustomColor);
            }
        }

        private void UpdateGraphics(BaseNote note, Color? color)
        {
            ObjectContainer con;
            if (note != null && beatmapObjectContainerCollection.LoadedContainers.TryGetValue(note, out con))
            {
                con.UpdateGridPosition();
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(note.ObjectType).RefreshSpecialAngles(note, false, false);

                if (colorDist != null && colorDist.Count > 0)
                    (con as NoteContainer).SetColor(color);

                SetNoteCut(con as NoteContainer);
            }
        }

        public void SetNoteCut(NoteContainer note)
        {
            bool flag = note.NoteData.Type != 3;
            if (flag)
            {
                bool flag2 = note.NoteData.CutDirection != 8;
                if (flag2)
                {
                    note.SetArrow();
                }
                else
                {
                    note.SetDot();
                }
            }
        }
    }
}
