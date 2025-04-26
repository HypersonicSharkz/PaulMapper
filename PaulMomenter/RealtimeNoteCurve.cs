using Beatmap.Base;
using Beatmap.Containers;
using PaulMapper.PaulHelper;
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
            /*curveObjects = PaulMaker.GeneratePoodle(
                    object1, object2,
                    xCurve, yCurve,
                    PaulmapperData.Instance.precision,
                    initialObjects.All(p => (p as BeatmapNote).CutDirection == 8),
                    colorDist, new List<float>()
                ).ToList();*/
            curveObjects = PaulMaker.GeneratePoodle(object1,
                                                    object2,
                                                    PaulmapperData.Instance.precision,
                                                    PaulmapperData.Instance.useEndPrecision ? PaulmapperData.Instance.endPrecision : PaulmapperData.Instance.precision,
                                                    initialObjects.All(p => (p as BaseNote).CutDirection == 8));

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

            if (PaulmapperData.Instance.usePointRotations && curveParameter.cutDirection.HasValue && (scrollType == ScrollType.Rotation))
            {
                curveParameter.cutDirection += 1 * (int)scrollType * dir;
            }

            UpdateAnchorPoints();
        }


        protected override void UpdateMenuData(int id)
        {
            base.UpdateMenuData(id);

            CurveParameter parm = selectedCurvePoint;

            if (PaulmapperData.Instance.usePointRotations && parm.cutDirection.HasValue)
            {
                GUI.Label(new Rect(5, 95, 140 - 5, 25), $"Cut Direction:");
                if (GUI.Button(new Rect(150 + 5, 95, 150 - 5, 20), parm.cutDirection.Value.ToString("0.00"), "Label"))
                {
                    PersistentUI.Instance.ShowInputBox("Force Cut Direction", new Action<string>(delegate (string t)
                    {
                        float cutDir;
                        if (float.TryParse(t, out cutDir))
                        {
                            parm.cutDirection = cutDir;
                            UpdateAnchorPoints();
                        }
                    }));
                }

            }

            GUI.Label(new Rect(5, 145, 140 - 5, 25), $"Dot Point:");
            parm.dotPoint = GUI.Toggle(new Rect(150 + 5, 145, 150 - 5, 20), parm.dotPoint, "");

            if (parm.dotPoint)
            {
                GUI.Label(new Rect(5, 160, 140 - 5, 25), $"Dot Time:");
                if (GUI.Button(new Rect(150 + 5, 160, 150 - 5, 20), parm.dotTime.ToString("0.00"), "Label"))
                {
                    PersistentUI.Instance.ShowInputBox("Dot Time", new Action<string>(delegate (string t)
                    {
                        float time;
                        if (float.TryParse(t, out time))
                        {
                            parm.dotTime = time;
                            UpdateAnchorPoints();
                        }
                    }));
                }
            }
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
                note.SetPosition(new Vector2((float)x, (float)y));
                if (PaulmapperData.Instance.useScale)
                {
                    note.SetScale(new Vector3((float)widthCurve.ValueAt(time), (float)heightCurve.ValueAt(time), (float)depthCurve.ValueAt(time)));
                }

                float? rotAtTime = Helper.GetRotationValueAtTime(note.SongBpmTime, curveObjects);
                if (rotAtTime.HasValue)
                    note.CustomWorldRotation = new Vector3(0, rotAtTime.Value, 0);

                Color color = Color.white;
                //Color handling 
                if (colorDist != null && colorDist.Count > 0)
                {
                    color = PaulMaker.LerpColorFromDict(colorDist, time);
                    note.SetColor(color);
                }

                //Now update direction
                JSONNode customData_old = null;
                if (PaulmapperData.Instance.rotateNotes)
                {
                    //Fix rotation
                    if (oldNote != null)
                    {

                        if (PaulmapperData.Instance.usePointRotations)
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

                            if (PaulmapperData.Instance.vibro)
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


                            if (PaulmapperData.Instance.adjustToWorldRotation && rotAtTime.HasValue)
                            {
                                float oldWorldRot = Helper.GetRotationValueAtTime(oldNote.SongBpmTime, curveObjects) ?? 0;
                                float rotDif = (rotAtTime.Value - oldWorldRot) * (Mathf.PI / 180f);

                                xPos += Mathf.Cos(Mathf.PI / 2 - rotDif);// * (note.SongBpmTime - oldNote.SongBpmTime);
                            }

                            float ang = Mathf.Atan2(yPos - op.y, xPos - op.x) * 180 / Mathf.PI;
                            ang += 90;


                            //Set rotation
                            customData_old = oldNote.CustomData;

                            if (PaulmapperData.Instance.vibro)
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
                            if (!PaulmapperData.Instance.transitionRotation)
                                oldNote.SetRotation(0);
                        }
                    }

                    
                    if (note == curveObjects.Last())
                    {
                        note.CutDirection = oldNote.CutDirection;
                        if (PaulmapperData.Instance.vibro)
                            note.SetRotation(oldNote.GetNoteDirection() + 180f);
                        else
                        {
                            note.SetRotation(oldNote.GetNoteDirection());
                        }
                    }
                }
                else if (PaulmapperData.Instance.vibro)
                {
                    note.SetRotation(180 * (noteIndex % 2));
                }

                if (PaulmapperData.IsV3())
                {
                    if (PaulmapperData.Instance.disableBadCutDirection)
                    {
                        customData["disableBadCutDirection"] = true;
                    }
                    if (PaulmapperData.Instance.disableBadCutSpeed)
                    {
                        customData["disableBadCutSpeed"] = true;
                    }
                    if (PaulmapperData.Instance.disableBadCutSaberType)
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
                    note.SetArrowVisible(true);
                    note.SetDotVisible(false);
                }
                else
                {
                    note.SetArrowVisible(false);
                    note.SetDotVisible(true);
                }
            }
            else
            {
                note.SetArrowVisible(false);
                note.SetDotVisible(false);
            }
        }
    }
}
