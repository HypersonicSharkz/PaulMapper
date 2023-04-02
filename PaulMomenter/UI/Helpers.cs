using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace PaulMapper.UI
{
    public class UIHelpers
    {
		public static RectTransform AttachTransform(GameObject obj, Vector2 size, Vector2 pos, Vector2? anchor_min = null, Vector2? anchor_max = null, Vector2? pivot = null)
		{
			RectTransform rectTransform = obj.GetComponent<RectTransform>();
			bool flag = rectTransform == null;
			if (flag)
			{
				rectTransform = obj.AddComponent<RectTransform>();
			}
			return MoveTransform(rectTransform, size, pos, anchor_min, anchor_max, pivot);
		}

		public static RectTransform MoveTransform(RectTransform trans, Vector2 size, Vector2 pos, Vector2? anchor_min = null, Vector2? anchor_max = null, Vector2? pivot = null)
		{
			trans.localScale = new Vector3(1f, 1f, 1f);
			trans.sizeDelta = size;
			trans.pivot = (pivot ?? new Vector2(0.5f, 0.5f));
			trans.anchorMin = (anchor_min ?? new Vector2(0f, 0f));
			trans.anchorMax = (anchor_max ?? (anchor_min ?? new Vector2(1f, 1f)));
			trans.anchoredPosition = new Vector3(pos.x, pos.y, 0f);
			return trans;
		}

		public static GameObject AddLabel(Transform parent, string title, string text, Vector2 pos, Vector2? anchor_min = null, Vector2? anchor_max = null, int font_size = 14, Vector2? size = null, TextAlignmentOptions align = (TextAlignmentOptions)514)
		{
			GameObject gameObject = new GameObject(title + " Label", new Type[]
			{
				typeof(TextMeshProUGUI)
			});
			RectTransform rectTransform = (RectTransform)gameObject.transform;
			rectTransform.SetParent(parent);
			MoveTransform(rectTransform, size ?? new Vector2(110f, 24f), pos, new Vector2?(anchor_min ?? new Vector2(0.5f, 1f)), new Vector2?(anchor_max ?? new Vector2(0.5f, 1f)), null);
			TextMeshProUGUI component = gameObject.GetComponent<TextMeshProUGUI>();
			component.name = title;
			component.font = PersistentUI.Instance.ButtonPrefab.Text.font;
			component.alignment = align;
			component.fontSize = (float)font_size;
			component.text = text;
			return gameObject;
		}

		public static GameObject AddChild(GameObject parent, string name, params Type[] components)
		{
			GameObject gameObject = new GameObject(name, components);
			gameObject.transform.SetParent(parent.transform);
			return gameObject;
		}

		public static Sprite LoadSprite(string asset)
		{
			Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(asset);
			byte[] array = new byte[manifestResourceStream.Length];
			manifestResourceStream.Read(array, 0, (int)manifestResourceStream.Length);
			Texture2D texture2D = new Texture2D(256, 256);
			texture2D.LoadImage(array);
			return Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0f, 0f), 100f);
		}
	}
}
