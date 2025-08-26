using Beatmap.Base;
using ChroMapper_PropEdit.Components;
using ChroMapper_PropEdit.UserInterface;
using PaulMapper.PaulHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaulMapper
{
    internal class UIHandler
    {
        public Window paulWindow;
        public GameObject panel;
        public ScrollBox scrollbox;

        public GameObject npsLabel;
        public GameObject lengthLabel;
        public GameObject speedLabel;

        public Window quickWindow;

        private float lastNoticeTime = float.NegativeInfinity;
        public TextMeshProUGUI noticeLabel;

        private GameObject navigateButtons;

        private UIButton gotoPaul;
        private UIButton selectCurrent;
        private UIButton selectAll;
        private TextMeshProUGUI paulNavNumber;

        public UIHandler()
        {
            
        }

        public void UpdateNPS()
        {
            float bpm = BeatSaberSongContainer.Instance.Map.BpmAtJsonTime(PaulMapper.ats.CurrentJsonTime) ?? 100;
            float nps = (bpm / 60) / (1 / (float)PaulMapperData.Instance.precision);
            float npsEnd = PaulMapperData.Instance.useEndPrecision ? (bpm / 60) / (1 / (float)PaulMapperData.Instance.endPrecision) : nps;

            var tmp = npsLabel.GetComponent<TextMeshProUGUI>();
            tmp.text = "NPS " + nps.ToString("0.00") + (PaulMapperData.Instance.useEndPrecision ? (" -> " + npsEnd.ToString("0.00")) : "");
            tmp.color = LerpErrorColor(40f, 80f, Math.Max(nps, npsEnd));
        }

        public void UpdateQuickMenu()
        {
            if (!PaulMapperData.Instance.enableQuickMenu)
            {
                if (quickWindow.gameObject.activeSelf)
                    quickWindow.Toggle();

                return;
            }

            if (SelectionController.SelectedObjects.Count == 2)
            {
                if (SelectionController.SelectedObjects.All(s => s.ObjectType == Beatmap.Enums.ObjectType.Note))
                {
                    BaseNote beatmapObject1 = SelectionController.SelectedObjects.First() as BaseNote;
                    BaseNote beatmapObject2 = SelectionController.SelectedObjects.Last() as BaseNote;
                    if ((beatmapObject1.CutDirection == beatmapObject2.CutDirection || PaulMapperData.Instance.rotateNotes) && beatmapObject1.SongBpmTime != beatmapObject2.SongBpmTime)
                    {
                        if (!quickWindow.gameObject.activeSelf)
                            quickWindow.Toggle();
                    }
                }
                else if (SelectionController.SelectedObjects.All(s => s.ObjectType == Beatmap.Enums.ObjectType.Obstacle))
                {
                    BaseObstacle beatmapObject1 = SelectionController.SelectedObjects.First() as BaseObstacle;
                    BaseObstacle beatmapObject2 = SelectionController.SelectedObjects.Last() as BaseObstacle;
                    if (beatmapObject1.SongBpmTime != beatmapObject2.SongBpmTime)
                    {
                        if (!quickWindow.gameObject.activeSelf)
                            quickWindow.Toggle();
                    }
                }
            }
            else
            {
                if (quickWindow.gameObject.activeSelf)
                    quickWindow.Toggle();
            }
        }

        public void UpdateSelectionUI()
        {
            if (SelectionController.SelectedObjects.Count > 1)
            {
                float selectionDir = selectionDur() * 1000;
                var tmp = lengthLabel.GetComponent<TextMeshProUGUI>();
                tmp.text = "Length " + selectionDir.ToString("0.0") + "ms";
                tmp.color = LerpErrorColor(800f, 1500f, selectionDir);

                //Average swing speed
                if (SelectionController.SelectedObjects.All(s => s.ObjectType == Beatmap.Enums.ObjectType.Note))
                {
                    BaseNote last = null;
                    float summedChange = 0;
                    foreach (BaseNote beatmapObject in SelectionController.SelectedObjects.OrderBy(s => s.SongBpmTime).Cast<BaseNote>())
                    {
                        if (last != null)
                        {
                            summedChange += Vector2.Distance(beatmapObject.GetRealPosition(), last.GetRealPosition());
                        }
                        last = beatmapObject;
                    }

                    float spd = summedChange / (selectionDir / 1000);
                    var tmp_speed = speedLabel.GetComponent<TextMeshProUGUI>();
                    tmp_speed.text = "Average Swing Speed " + spd.ToString("0.0");
                    tmp_speed.color = LerpErrorColor(5, 2, spd);
                }
            }
            else
            {
                var tmp_speed = speedLabel.GetComponent<TextMeshProUGUI>();
                tmp_speed.text = "";
                var tmp = lengthLabel.GetComponent<TextMeshProUGUI>();
                tmp.text = "";
            }
        }

        public void UpdateUI()
        {
            if (Time.time - lastNoticeTime > 10)
            {
                if (!Plugin.UpToDate)
                {
                    SetNotice("New Version Of <color=#5495ff><u><link=github>Paul Mapper</link></u></color> is available", noticeType.None);
                }
                else
                {
                    noticeLabel.text = "";
                }
            }
        }

        public void SetNotice(string notice, noticeType noticeType)
        {
            lastNoticeTime = Time.time;
            noticeLabel.text = notice;
            switch (noticeType)
            {
                case noticeType.None:
                    noticeLabel.color = Color.white;
                    break;

                case noticeType.Warning:
                    noticeLabel.color = new Color(0.641f, 0.494f, 0.308f);
                    break;

                case noticeType.Error:
                    noticeLabel.color = new Color(0.729f, 0.106f, 0.114f);
                    break;
            }
        }
        private Color LerpErrorColor(float min, float max, float val)
        {
            float t = 0;
            if (min < max)
            {
                if (val < min)
                    t = 0f;
                if (val > max)
                    t = 1f;
                else
                    t = (val - min) / (max - min);
            }
            else
            {
                if (val > min)
                    t = 0f;
                if (val < max)
                    t = 1f;
                else
                    t = (val - min) / (max - min);
            }

            return Color.Lerp(new Color(0.553f, 0.882f, 0.502f), new Color(0.729f, 0.106f, 0.114f), t);
        }

        private float selectionDur()
        {
            if (SelectionController.SelectedObjects.All(o => o.ObjectType == Beatmap.Enums.ObjectType.Note))
            {
                List<BaseNote> notes = SelectionController.SelectedObjects.OrderBy(n => n.SongBpmTime).Cast<BaseNote>().ToList();
                return PaulMapper.ats.GetSecondsFromBeat(notes.Last().SongBpmTime - notes.First().SongBpmTime);
            }
            return 0;
        }

        public bool TryLoadQuickMenu(MapEditorUI mapEditorUI)
        {
            try
            {
                var parent = mapEditorUI.MainUIGroup[5].gameObject;
                quickWindow = Window.Create("PaulMapperQuickMenu", "", parent, new Vector2(160, 300));

                var container = UI.AddChild(quickWindow.gameObject, "Quick Container");
                UI.AttachTransform(container, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1));
                var sb = ScrollBox.Create(container);

                var u = UI.AddButton(sb.content, "Linear", () => {GenerateQuickPoodle("Linear");});
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Sine In", () => { GenerateQuickPoodle("easeInSine"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Sine Out", () => { GenerateQuickPoodle("easeOutSine"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Quad In", () => { GenerateQuickPoodle("easeInQuad"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Quad Out", () => { GenerateQuickPoodle("easeOutQuad"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Cubic In", () => { GenerateQuickPoodle("CubicIn"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Cubic Out", () => { GenerateQuickPoodle("CubicOut"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Exp In", () => { GenerateQuickPoodle("ExpIn"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Exp Out", () => { GenerateQuickPoodle("ExpOut"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Back In", () => { GenerateQuickPoodle("easeInBack"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                u = UI.AddButton(sb.content, "Back Out", () => { GenerateQuickPoodle("easeOutBack"); });
                UI.AttachTransform(u.gameObject, new Vector2(0, 20), Vector2.zero);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        private void GenerateQuickPoodle(string type)
        {
            if (SelectionController.SelectedObjects.Count == 2 && SelectionController.SelectedObjects.All(s => s.ObjectType == Beatmap.Enums.ObjectType.Note))
            {
                BaseNote beatmapObject1 = SelectionController.SelectedObjects.First() as BaseNote;
                BaseNote beatmapObject2 = SelectionController.SelectedObjects.Last() as BaseNote;
                if ((beatmapObject1.CutDirection == beatmapObject2.CutDirection || PaulMapperData.Instance.rotateNotes) && beatmapObject1.SongBpmTime != beatmapObject2.SongBpmTime)
                {
                    BaseObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o.SongBpmTime).ToArray();

                    PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], type, PaulMapperData.Instance.precision);
                }
            }
            else if (SelectionController.SelectedObjects.All(s => s.ObjectType == Beatmap.Enums.ObjectType.Obstacle))
            {
                BaseObstacle beatmapObject1 = SelectionController.SelectedObjects.First() as BaseObstacle;
                BaseObstacle beatmapObject2 = SelectionController.SelectedObjects.Last() as BaseObstacle;
                if (beatmapObject1.SongBpmTime != beatmapObject2.SongBpmTime)
                {
                    BaseObject[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o.SongBpmTime).ToArray();
                    PaulMaker.GeneratePoodle(beatmapObjects[0], beatmapObjects[1], type, PaulMapperData.Instance.precision);
                }
            }
        }

        public bool TryLoadPaulMapperWindow(MapEditorUI mapEditorUI)
        {
            try
            {
                var parent = mapEditorUI.MainUIGroup[5].gameObject;

                paulWindow = Window.Create("PaulMapper", "Paul Mapper", parent, new Vector2(170, 270));

                var button = UI.AddButton(paulWindow.title, "X", ToggleWindow);
                UI.AttachTransform(button.gameObject, pos: new Vector2(-25, -14), size: new Vector2(30, 30), anchor_min: new Vector2(1, 1), anchor_max: new Vector2(1, 1));

                var container = UI.AddChild(paulWindow.gameObject, "Settings Scroll Container");
                UI.AttachTransform(container, new Vector2(-10, -60), new Vector2(0, -5), new Vector2(0, 0), new Vector2(1, 1));
                {
                    var image = container.AddComponent<Image>();
                    image.sprite = PersistentUI.Instance.Sprites.Background;
                    image.type = Image.Type.Sliced;
                    image.color = new Color(0.1f, 0.1f, 0.1f, 1);
                }


                var footer = UI.AddChild(paulWindow.gameObject, "Footer");
                UI.AttachTransform(footer, new Vector2(-10, 20), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
                noticeLabel = UI.AddLabel(footer, "Error Notice Label", "", new Vector2(60, 5), null, null, 12, null, TextAlignmentOptions.BottomLeft).GetComponent<TextMeshProUGUI>();
                ClickableLabel click = noticeLabel.gameObject.AddComponent<ClickableLabel>();
                click.OnClick += NoticeLabel_OnClick;
                noticeLabel.enableWordWrapping = true;

                scrollbox = ScrollBox.Create(container);
                panel = scrollbox.content;

                //Empty space
                UI.AddLabel(panel, "PaulMapper", "", Vector2.zero, null, null, 12, new Vector2(0, 10), TextAlignmentOptions.MidlineLeft);

                lengthLabel = UI.AddLabel(panel, "PaulMapper", "", Vector2.zero, null, null, 12, new Vector2(0, 10), TextAlignmentOptions.MidlineLeft);
                speedLabel = UI.AddLabel(panel, "PaulMapper", "", Vector2.zero, null, null, 12, new Vector2(0, 10), TextAlignmentOptions.MidlineLeft);

                var precisionCon = UI.AddField(panel, "Precision");
                var parsed = UI.AddParsed<int>(precisionCon, null, (val =>
                {
                    PaulMapperData.Instance.precision = val.GetValueOrDefault(16);
                    UpdateNPS();
                }));
                parsed.InputField.text = PaulMapperData.Instance.precision.ToString();

                GameObject gameObject6 = UI.AddField(panel, " ", null);
                parsed = UI.AddParsed<int>(gameObject6, null, (val =>
                {
                    PaulMapperData.Instance.endPrecision = val.GetValueOrDefault(16);
                    UpdateNPS();
                }));
                parsed.InputField.text = PaulMapperData.Instance.endPrecision.ToString();

                UI.AddCheckbox(gameObject6, PaulMapperData.Instance.useEndPrecision, delegate (bool val)
                {
                    PaulMapperData.Instance.useEndPrecision = val;
                    UpdateNPS();
                });


                npsLabel = UI.AddLabel(panel, "PaulMapper", "", Vector2.zero, null, null, 12, new Vector2(0, 10), TextAlignmentOptions.MidlineLeft);
                UpdateNPS();

                var rotateCon = UI.AddField(panel, "Rotate");
                UI.AddCheckbox(rotateCon, PaulMapperData.Instance.rotateNotes, (val =>
                {
                    PaulMapperData.Instance.rotateNotes = val;
                }));

                #region Note Settings

                var noteCollapsible = Collapsible.Create(panel, "Note Settings", "Note Settings", true);

                var vibroCon = UI.AddField(noteCollapsible.panel, "Vibro");
                UI.AddCheckbox(vibroCon, PaulMapperData.Instance.vibro, (val =>
                {
                    PaulMapperData.Instance.vibro = val;
                }));

                var forceCon = UI.AddField(noteCollapsible.panel, "Use Note Rotations");
                UI.AddCheckbox(forceCon, PaulMapperData.Instance.usePointRotations, (val =>
                {
                    PaulMapperData.Instance.usePointRotations = val;
                }));

                var scaleCon = UI.AddField(noteCollapsible.panel, "Scale Notes");
                UI.AddCheckbox(scaleCon, PaulMapperData.Instance.usePointRotations, (val =>
                {
                    PaulMapperData.Instance.useScale = val;
                }));

                if (PaulMapperData.IsV3())
                {
                    var u = UI.AddButton(noteCollapsible.panel, "Create Arc", () =>
                    {
                        Helper.SpawnPrecisionArc();
                    });

                    UI.AttachTransform(u.gameObject, new Vector2(-20, 30), Vector2.zero);
                }

                var worldRotCon = UI.AddField(noteCollapsible.panel, "Adjust To World Rotation");
                UI.AddCheckbox(worldRotCon, PaulMapperData.Instance.adjustToWorldRotation, val =>
                {
                    PaulMapperData.Instance.adjustToWorldRotation = val;
                });

                #endregion

                #region Transition Settings

                var transitionCollapsible = Collapsible.Create(panel, "Transition Settings", "Transition Settings", false);

                var transitionCon = UI.AddField(transitionCollapsible.panel, "Transition Time");
                parsed = UI.AddParsed<float>(transitionCon, null, (val =>
                {
                    PaulMapperData.Instance.transitionTime = val.GetValueOrDefault(0.3f);
                }));
                parsed.InputField.text = PaulMapperData.Instance.transitionTime.ToString();

                var keepRotationCon = UI.AddField(transitionCollapsible.panel, "Keep Rotation");
                UI.AddCheckbox(keepRotationCon, PaulMapperData.Instance.transitionRotation, (val =>
                {
                    PaulMapperData.Instance.transitionRotation = val;
                }));

                if (PaulMapperData.IsV3())
                {
                    var arcCon = UI.AddField(transitionCollapsible.panel, "Transition Arcs");
                    UI.AddCheckbox(arcCon, PaulMapperData.Instance.arcs, (val =>
                    {
                        PaulMapperData.Instance.arcs = val;
                    }));
                }

                #endregion

                #region Wall Settings

                var wallCollapsible = Collapsible.Create(panel, "Wall Settings", "Wall Settings", false);

                var fakeCon = UI.AddField(wallCollapsible.panel, "Fake Walls");
                UI.AddCheckbox(fakeCon, PaulMapperData.Instance.fakeWalls, (val =>
                {
                    PaulMapperData.Instance.fakeWalls = val;
                }));

                var wallrotaionCon = UI.AddField(wallCollapsible.panel, "Wall Rotation");
                parsed = UI.AddParsed<int>(wallrotaionCon, null, (val =>
                {
                    PaulMapperData.Instance.wallRotationAmount = val.GetValueOrDefault(5);
                }));
                parsed.InputField.text = PaulMapperData.Instance.wallRotationAmount.ToString();

                #endregion

                #region Bad Cuts

                var collapsible = Collapsible.Create(panel, "Disable Badcuts", "Disable Badcuts", false);

                var directionCon = UI.AddField(collapsible.panel, "Direction");
                UI.AddCheckbox(directionCon, PaulMapperData.Instance.disableBadCutDirection, (val =>
                {
                    PaulMapperData.Instance.disableBadCutDirection = val;
                }));

                var saberCon = UI.AddField(collapsible.panel, "Saber Type");
                UI.AddCheckbox(saberCon, PaulMapperData.Instance.disableBadCutSaberType, (val =>
                {
                    PaulMapperData.Instance.disableBadCutSaberType = val;
                }));

                var speedCon = UI.AddField(collapsible.panel, "Speed");
                UI.AddCheckbox(speedCon, PaulMapperData.Instance.disableBadCutSpeed, (val =>
                {
                    PaulMapperData.Instance.disableBadCutSpeed = val;
                }));

                #endregion

                #region Navigation

                Collapsible NavigationCollapsible = Collapsible.Create(this.panel, "Navigation", "Navigation", false);
                GameObject gameObject19 = new GameObject();
                RectTransform rectTransform = gameObject19.AddComponent<RectTransform>();
                gameObject19.transform.SetParent(NavigationCollapsible.panel.transform);
                UI.AttachTransform(gameObject19, new Vector2(1f, 30f), Vector2.zero, null, null, new Vector2?(new Vector2(0.5f, 0.5f)));
                UIButton uibutton3 = UI.AddButton(gameObject19, "Find All Pauls", delegate ()
                {
                    List<BaseGrid> allNotes = (from BaseGrid it in PaulMapper.notesContainer.LoadedObjects
                                               orderby it.SongBpmTime
                                               select it).ToList<BaseGrid>();
                    PaulFinder.pauls = (from p in PaulFinder.FindAllPauls(allNotes)
                                        orderby p.Beat
                                        select p).ToList<Paul>();
                    this.navigateButtons.SetActive(PaulFinder.pauls.Count > 0);
                    this.selectCurrent.gameObject.SetActive(PaulFinder.pauls.Count > 0);
                    this.gotoPaul.gameObject.SetActive(PaulFinder.pauls.Count > 0);
                    this.selectAll.gameObject.SetActive(PaulFinder.pauls.Count > 0);
                    NavigationCollapsible.SetExpanded(true);
                    this.UpdatePaulNumber();
                });
                UI.AttachTransform(uibutton3.gameObject, new Vector2(1f, 30f), Vector2.zero, null, null, new Vector2?(new Vector2(0f, 0.5f))).localScale = new Vector3(0.45f, 0.45f);

                this.gotoPaul = UI.AddButton(gameObject19, "GoTo Paul", delegate ()
                {
                    PersistentUI.Instance.ShowInputBox("Go to paul", delegate (string result)
                    {
                        int num = 0;
                        bool flag3 = int.TryParse(result, out num);
                        if (flag3)
                        {
                            PaulFinder.GoToPaul(PaulFinder.pauls[num - 1]);
                            this.UpdatePaulNumber();
                        }
                    }, "0");
                });
                UI.AttachTransform(this.gotoPaul.gameObject, new Vector2(1f, 30f), Vector2.zero, null, null, new Vector2?(new Vector2(1f, 0.5f))).localScale = new Vector3(0.45f, 0.45f);
                this.navigateButtons = new GameObject();
                RectTransform rectTransform2 = this.navigateButtons.AddComponent<RectTransform>();
                this.navigateButtons.transform.SetParent(NavigationCollapsible.panel.transform);
                UI.AttachTransform(this.navigateButtons, new Vector2(1f, 15f), Vector2.zero, null, null, new Vector2?(new Vector2(0.5f, 0.5f)));
                GameObject gameObject20 = UI.AddLabel(this.navigateButtons, "PaulMapper", "1/25", Vector2.zero, new Vector2?(Vector2.zero), new Vector2?(Vector2.one), 12, new Vector2?(new Vector2(0f, 10f)), TextAlignmentOptions.Center);
                gameObject20.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                this.paulNavNumber = gameObject20.GetComponent<TextMeshProUGUI>();
                UIButton uibutton4 = UI.AddButton(this.navigateButtons, "<", delegate ()
                {
                    try
                    {
                        Paul paul = PaulFinder.pauls.Last((Paul p) => p.Beat < PaulMapper.ats.CurrentSongBpmTime);
                        PaulFinder.GoToPaul(paul);
                    }
                    catch
                    {
                        PaulFinder.GoToPaul(PaulFinder.pauls.Last<Paul>());
                    }
                    this.UpdatePaulNumber();
                });
                uibutton4.Text.transform.localScale = new Vector3(2f, 2f, 2f);
                UI.AttachTransform(uibutton4.gameObject, new Vector2(1f, 15f), Vector2.zero, null, null, new Vector2?(new Vector2(0f, 0.5f))).localScale = new Vector3(0.2f, 0.2f);
                UIButton uibutton5 = UI.AddButton(this.navigateButtons, ">", delegate ()
                {
                    try
                    {
                        Paul paul = PaulFinder.pauls.First((Paul p) => p.Beat > PaulMapper.ats.CurrentSongBpmTime);
                        PaulFinder.GoToPaul(paul);
                    }
                    catch
                    {
                        PaulFinder.GoToPaul(PaulFinder.pauls.First<Paul>());
                    }
                    this.UpdatePaulNumber();
                });
                uibutton5.Text.transform.localScale = new Vector3(2f, 2f, 2f);
                UI.AttachTransform(uibutton5.gameObject, new Vector2(1f, 15f), Vector2.zero, null, null, new Vector2?(new Vector2(1f, 0.5f))).localScale = new Vector3(0.2f, 0.2f);
                this.selectCurrent = UI.AddButton(NavigationCollapsible.panel, "Select Current", delegate ()
                {
                    PaulFinder.SelectCurrentPaul();
                    this.UpdatePaulNumber();
                });
                UI.AttachTransform(this.selectCurrent.gameObject, new Vector2(-20f, 15f), Vector2.zero, null, null, null);
                this.selectAll = UI.AddButton(NavigationCollapsible.panel, "Select All", delegate ()
                {
                    PaulFinder.SelectAllPauls();
                });
                UI.AttachTransform(this.selectAll.gameObject, new Vector2(-20f, 15f), Vector2.zero, null, null, null);
                this.navigateButtons.SetActive(false);
                this.selectCurrent.gameObject.SetActive(false);
                this.gotoPaul.gameObject.SetActive(false);
                this.selectAll.gameObject.SetActive(false);

                #endregion

                UIButton uibutton6 = UI.AddButton(this.panel, "Refresh World Rotations", delegate ()
                {
                    List<BaseGrid> allNotes = (from BaseGrid it in PaulMapper.notesContainer.LoadedObjects
                                                orderby it.SongBpmTime
                                                select it).ToList<BaseGrid>();
                    List<Paul> list = PaulFinder.FindAllPauls(allNotes);
                    foreach (Paul paul in list)
                    {
                        BaseNote oldNote = null;
                        foreach (BaseNote baseNote in paul.notes)
                        {
                            if (oldNote != null)
                            {
                                Vector2 op = oldNote.GetPosition();

                                float? rotationValueAtTime = Helper.GetRotationValueAtTime(baseNote.SongBpmTime, paul.notes.Cast<BaseObject>().ToList<BaseObject>());
                                if (rotationValueAtTime.HasValue)
                                    baseNote.CustomWorldRotation = new Vector3(0f, rotationValueAtTime.Value, 0f);

                                if (!PaulMapperData.Instance.adjustToWorldRotation)
                                    continue;

                                float xPos = baseNote.GetPosition().x;
                                float yPos = baseNote.GetPosition().y;


                                if (rotationValueAtTime.HasValue)
                                {
                                    float oldWorldRot = Helper.GetRotationValueAtTime(oldNote.SongBpmTime, paul.notes.Cast<BaseObject>().ToList<BaseObject>()) ?? 0;
                                    float rotDif = (rotationValueAtTime.Value - oldWorldRot) * (Mathf.PI / 180f);

                                    xPos += Mathf.Cos(Mathf.PI / 2 - rotDif);// * (note.SongBpmTime - oldNote.SongBpmTime);
                                }

                                float ang = Mathf.Atan2(yPos - op.y, xPos - op.x) * 180 / Mathf.PI;
                                ang += 90;

                                oldNote.SetRotation(ang);
                            }                            

                            oldNote = baseNote;
                        }
                    }
                });
                UI.AttachTransform(uibutton6.gameObject, new Vector2(-20f, 30f), Vector2.zero, null, null, null);
                



                var quickCon = UI.AddField(panel, "Enable Quick Menu");
                UI.AddCheckbox(quickCon, PaulMapperData.Instance.enableQuickMenu, (val =>
                {
                    PaulMapperData.Instance.enableQuickMenu = val;
                    UpdateQuickMenu();
                }));

                #region Events

                SelectionController.SelectionChangedEvent = (Action)Delegate.Combine(SelectionController.SelectionChangedEvent, new Action(UpdateSelectionUI));
                SelectionController.SelectionChangedEvent = (Action)Delegate.Combine(SelectionController.SelectionChangedEvent, new Action(UpdateQuickMenu));
                float lastBpm = 0;
                PaulMapper.ats.TimeChanged = (Action)Delegate.Combine(PaulMapper.ats.TimeChanged, new Action(() => 
                {
                    float bpm = BeatSaberSongContainer.Instance.Map.BpmAtJsonTime(PaulMapper.ats.CurrentJsonTime) ?? 100;
                    
                    if (bpm == lastBpm)
                        return;

                    lastBpm = bpm;

                    UpdateNPS();
                }));

                #endregion

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false; 
            }
        }

        private void NoticeLabel_OnClick(object sender, UnityEngine.EventSystems.PointerEventData e)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(noticeLabel, Input.mousePosition, null);

            if (linkIndex == -1)
                return;

            string linkId = noticeLabel.textInfo.linkInfo[linkIndex].GetLinkID();

            if (linkId == "github")
            {
                System.Diagnostics.Process.Start("https://github.com/HypersonicSharkz/PaulMapper/releases");
            }
        }

        private void UpdatePaulNumber()
        {
            this.paulNavNumber.text = string.Format("{0}/{1}", PaulFinder.currentPaul + 1, PaulFinder.pauls.Count);
        }

        public void ToggleWindow()
        {
            paulWindow.Toggle();
        }
    }  
}