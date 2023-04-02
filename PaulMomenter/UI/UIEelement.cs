using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace PaulMapper.UI
{
    public class UIEelement
    {

		public GameObject AddField(string title)
		{
			GameObject gameObject = UIHelpers.AddChild(new GameObject(), title + " Container", Array.Empty<Type>());
			UIHelpers.AttachTransform(gameObject, new Vector2(0f, 20f), new Vector2(0f, 0f), null, null, null);
			GameObject gameObject2 = UIHelpers.AddChild(gameObject, title + " Label", new Type[]
			{
				typeof(TextMeshProUGUI)
			});
			UIHelpers.AttachTransform(gameObject2, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2?(new Vector2(0f, 0f)), new Vector2?(new Vector2(0.5f, 1f)), null);
			TextMeshProUGUI component = gameObject2.GetComponent<TextMeshProUGUI>();
			component.font = PersistentUI.Instance.ButtonPrefab.Text.font;
			component.alignment = (TextAlignmentOptions)513;
			component.enableAutoSizing = true;
			component.fontSizeMin = 8f;
			component.fontSizeMax = 16f;
			component.text = title;
			return gameObject;
		}

		public UITextInput AddParsed<T>(string title, object value) where T : struct
		{
			GameObject gameObject = AddField(title);

			UITextInput uitextInput = UnityEngine.Object.Instantiate<UITextInput>(PersistentUI.Instance.TextInputPrefab, gameObject.transform);

			UIHelpers.MoveTransform((RectTransform)uitextInput.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2?(new Vector2(0.5f, 0f)), new Vector2?(new Vector2(1f, 1f)), null);
			uitextInput.InputField.text = (string)Convert.ChangeType(value, typeof(string));
			uitextInput.InputField.onEndEdit.AddListener(delegate (string s)
			{

			});
			uitextInput.InputField.onSelect.AddListener(delegate (string s)
			{

			});
			return uitextInput;
		}

	}
}
