using ChroMapper_PropEdit.Components;
using ChroMapper_PropEdit.UserInterface;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaulMapper
{
    public static class CurvePointEditor
    {
        private static CurveParameter editing;

        public static Window paulWindow = null;
        public static GameObject panel;
        public static ScrollBox scrollbox;

        private static TMP_InputField xpostmp;
        private static TMP_InputField ypostmp;
        private static UIButton colorbtn;
        private static TMP_InputField widthtmp;
        private static TMP_InputField heighttmp;
        private static TMP_InputField lengthtmp;
        private static TMP_InputField cuttmp;
        private static Toggle dotcheck;
        private static TMP_InputField dottimetmp;

        public delegate void ParameterChange();
        public static event ParameterChange ParameterChanged;

        public static void UpdatePoint(CurveParameter curveParameter)
        {
            editing = curveParameter;

            if (editing == null)
            {
                if (paulWindow != null && paulWindow.gameObject.activeSelf)
                    paulWindow.Toggle();

                return;
            }
            else if (paulWindow != null && !paulWindow.gameObject.activeSelf)
            {
                paulWindow.Toggle();
            }

            if (paulWindow == null)
            {
                var mapEditorUI = UnityEngine.Object.FindObjectOfType<MapEditorUI>();
                Init(mapEditorUI);
            }

            xpostmp.text = editing.xPos.ToString("0.00");
            ypostmp.text = editing.yPos.ToString("0.00");
            widthtmp.text = editing.scale.x.ToString("0.00");
            heighttmp.text = editing.scale.y.ToString("0.00");
            lengthtmp.text = editing.scale.z.ToString("0.00");

            ColorBlock block = colorbtn.Button.colors;
            block.normalColor = editing.color;
            if (editing.color == Color.clear)
                block.highlightedColor = new Color(0.2f, 0.2f, 0.2f);
            else
                block.highlightedColor = editing.color / 1.2f;

            colorbtn.Button.colors = block;

            if (editing.type != Beatmap.Enums.ObjectType.Note)
            {
                cuttmp.transform.parent.gameObject.SetActive(false);
                dotcheck.transform.parent.gameObject.SetActive(false);
                dottimetmp.transform.parent.gameObject.SetActive(false);
                return;
            }
            else
            {
                cuttmp.transform.parent.gameObject.SetActive(true);
                dotcheck.transform.parent.gameObject.SetActive(true);
                dottimetmp.transform.parent.gameObject.SetActive(true);
            }

            if (PaulmapperData.Instance.usePointRotations && editing.cutDirection.HasValue)
            {
                cuttmp.text = editing.cutDirection.Value.ToString("0.00");
                cuttmp.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                cuttmp.transform.parent.gameObject.SetActive(false);
            }

            dotcheck.isOn = editing.dotPoint;
            dottimetmp.text = editing.dotTime.ToString("0.00");
        }

        private static void OnParameterChanged()
        {
            ParameterChanged?.Invoke();
        }

        private static bool Init(MapEditorUI mapEditorUI)
        {
            try
            {
                var parent = mapEditorUI.MainUIGroup[5].gameObject;

                paulWindow = Window.Create("CurvePointEditor", "Curve Point", parent, new Vector2(300, 150));

                var container = UI.AddChild(paulWindow.gameObject, "Settings Scroll Container");
                UI.AttachTransform(container, new Vector2(-10, -40), new Vector2(0, -15), new Vector2(0, 0), new Vector2(1, 1));
                {
                    var image = container.AddComponent<Image>();
                    image.sprite = PersistentUI.Instance.Sprites.Background;
                    image.type = Image.Type.Sliced;
                    image.color = new Color(0.1f, 0.1f, 0.1f, 1);
                }

                scrollbox = ScrollBox.Create(container);
                panel = scrollbox.content;

                var xpos = UI.AddField(panel, "X Position");
                xpostmp = UI.AddParsed<float>(xpos, 0, (val =>
                {
                    float x = val.GetValueOrDefault(0);
                    editing.anchorPoint.transform.position = new Vector3(x + editing.anchorPoint.parameterOffset.x, editing.anchorPoint.transform.position.y, editing.anchorPoint.transform.position.z);
                    OnParameterChanged();
                })).GetComponent<TMP_InputField>();

                var ypos = UI.AddField(panel, "Y Position");
                ypostmp = UI.AddParsed<float>(ypos, 0, (val =>
                {
                    float y = val.GetValueOrDefault(0);
                    editing.anchorPoint.transform.position = new Vector3(editing.anchorPoint.transform.position.x, y + editing.anchorPoint.parameterOffset.y, editing.anchorPoint.transform.position.z);
                    OnParameterChanged();
                })).GetComponent<TMP_InputField>();


                colorbtn = UI.AddButton(panel, "Color", () =>
                {
                    PersistentUI.Instance.ShowColorInputBox("Mapper", "bookmark.update.color", new Action<Color?>(delegate (Color? color)
                    {
                        editing.color = color.HasValue ? color.Value : Color.clear;
                        OnParameterChanged();
                    }), editing.color);
                });

                UI.AttachTransform(colorbtn.gameObject, new Vector2(-20, 30), Vector2.zero);

                var width = UI.AddField(panel, "Width");
                widthtmp = UI.AddParsed<float>(width, 0, (val =>
                {
                    float w = val.GetValueOrDefault(0);
                    editing.scale.x = w;
                    OnParameterChanged();
                })).GetComponent<TMP_InputField>();

                var height = UI.AddField(panel, "Height");
                heighttmp = UI.AddParsed<float>(height, 0, (val =>
                {
                    float h = val.GetValueOrDefault(0);
                    editing.scale.y = h;
                    OnParameterChanged();
                })).GetComponent<TMP_InputField>();

                var length = UI.AddField(panel, "Length");
                lengthtmp = UI.AddParsed<float>(length, 0, (val =>
                {
                    float l = val.GetValueOrDefault(0);
                    editing.scale.z = l;
                    OnParameterChanged();
                })).GetComponent<TMP_InputField>();

                var cutDir = UI.AddField(panel, "Cut Direction");
                cuttmp = UI.AddParsed<float>(cutDir, 0, (val =>
                {
                    float c = val.GetValueOrDefault(0);
                    editing.cutDirection = c;
                    OnParameterChanged();
                })).GetComponent<TMP_InputField>();

                var dotPoint = UI.AddField(panel, "Dot Point");
                dotcheck = UI.AddCheckbox(dotPoint, editing.dotPoint, (val =>
                {
                    editing.dotPoint = val;
                    OnParameterChanged();
                }));

                var dotTime = UI.AddField(panel, "Dot Length");
                dottimetmp = UI.AddParsed<float>(dotTime, 0, (val =>
                {
                    float d = val.GetValueOrDefault(0);
                    editing.dotTime = d;
                    OnParameterChanged();
                })).GetComponent<TMP_InputField>();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
    }
}
