using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PaulMapper.UI
{
    public class PaulWindow : UIEelement
    {
		public GameObject window;
		public GameObject title;
		public GameObject panel;

        public void Init(MapEditorUI mapEditorUI)
        {
			CanvasGroup canvasGroup = mapEditorUI.MainUIGroup[5];
			window = new GameObject("ErrorChecker Popup");
			window.transform.parent = canvasGroup.transform;
			window.AddComponent<DragWindowController>();
			window.GetComponent<DragWindowController>().canvas = canvasGroup.GetComponent<Canvas>();
			window.GetComponent<DragWindowController>().OnDragWindow += this.AnchoredPosSave;
			GameObject obj = this.window;
			Vector2 size = new Vector2(140f, 256f);
			Vector2 pos = new Vector2(0f, 0f);
			Vector2? anchor_min = new Vector2?(new Vector2(0.5f, 0.5f));
			Vector2? anchor_max = null;
			Vector2? vector = null;
			UIHelpers.AttachTransform(obj, size, pos, anchor_min, anchor_max, vector);
			Image image = this.window.AddComponent<Image>();
			image.sprite = PersistentUI.Instance.Sprites.Background;
			image.type = Image.Type.Sliced;
			image.color = new Color(0.24f, 0.24f, 0.24f, 1f);
			window.SetActive(false);
			Transform transform = this.window.transform;
			string text = "Title";
			string text2 = "Paul Mapper";
			Vector2 pos2 = new Vector2(10f, -20f);
			vector = new Vector2?(new Vector2(-10f, 30f));
			title = UIHelpers.AddLabel(transform, text, text2, pos2, new Vector2?(new Vector2(0f, 1f)), new Vector2?(new Vector2(1f, 1f)), 28, vector, (TMPro.TextAlignmentOptions)513);
			panel = UIHelpers.AddChild(window, "PanelContainer", Array.Empty<Type>());
			UIHelpers.AttachTransform(panel, new Vector2(-10f, -40f), new Vector2(0f, -15f), new Vector2?(new Vector2(0f, 0f)), new Vector2?(new Vector2(1f, 1f)), null);
			Image image2 = panel.AddComponent<Image>();
			image2.sprite = PersistentUI.Instance.Sprites.Background;
			image2.type = Image.Type.Sliced;
			image2.color = new Color(0.1f, 0.1f, 0.1f, 1f);
			VerticalLayoutGroup verticalLayoutGroup = panel.AddComponent<VerticalLayoutGroup>();
			verticalLayoutGroup.padding = new RectOffset(10, 15, 0, 0);
			verticalLayoutGroup.spacing = 0f;
			verticalLayoutGroup.childControlHeight = false;
			verticalLayoutGroup.childForceExpandHeight = false;
			verticalLayoutGroup.childForceExpandWidth = true;
			verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
			LayoutElement layoutElement = this.panel.AddComponent<LayoutElement>();
			ContentSizeFitter contentSizeFitter = this.panel.AddComponent<ContentSizeFitter>();
			contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		}

		public void ToggleWindow()
		{
			LoadSettings();
			window.SetActive(!this.window.activeSelf);
		}

		private void AnchoredPosSave()
		{
			Vector2 anchoredPosition = this.window.GetComponent<RectTransform>().anchoredPosition;
			JSONObject jsonobject = new JSONObject();
			jsonobject.Add("x", anchoredPosition.x);
			jsonobject.Add("y", anchoredPosition.y);
			jsonobject.Add("w", this.window.GetComponent<RectTransform>().sizeDelta.x);
			jsonobject.Add("h", this.window.GetComponent<RectTransform>().sizeDelta.y);
			//File.WriteAllText(this.SETTINGS_FILE, jsonobject.ToString(4));
		}

		private void LoadSettings()
		{
			LayoutElement component = this.panel.GetComponent<LayoutElement>();
			component.minHeight = this.window.GetComponent<RectTransform>().sizeDelta.y - 40f - 15f;
		}
	}

	public class DragWindowController : MonoBehaviour, IDragHandler, IEventSystemHandler
	{
		public Canvas canvas { get; set; }

		public event Action OnDragWindow;

		public void OnDrag(PointerEventData eventData)
		{
			base.gameObject.GetComponent<RectTransform>().anchoredPosition += eventData.delta / this.canvas.scaleFactor;
			Action onDragWindow = this.OnDragWindow;
			if (onDragWindow != null)
			{
				onDragWindow();
			}
		}
	}
}
