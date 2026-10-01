using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PaulMapper
{
    [Plugin("PaulMapper")]
    public class Plugin
    {
        public static PaulMapper? paulMapper;
        public static InputAction? openMenu;
        public static InputAction? createPoodle;
        public static InputAction? addAnchor;
        public static InputAction? wallLeft;
        public static InputAction? wallRight;
        public static InputAction? wallForward;
        public static InputAction? wallBack;

        public static bool UpToDate = true;


        [Init]
        private void Init()
        {
            var cm_ver = new System.Version(Application.version);

            if (cm_ver.Minor != 14)
            {
                var dialog = PersistentUI.Instance.CreateNewDialogBox().WithNoTitle();
                dialog.AddComponent<TextComponent>().WithInitialValue($"This build of PaulMapper is made for ChroMapper v0.14.x! Please update ChroMapper to the Dev branch");
                dialog.AddFooterButton(() => { }, "Okay");
                dialog.Open();
                return;
            }

            Assembly propEditAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "ChroMapper-PropEdit");
            if (propEditAssembly == null)
            {
                var dialog = PersistentUI.Instance.CreateNewDialogBox().WithNoTitle();
                dialog.AddComponent<TextComponent>().WithInitialValue($"PaulMapper requires PropEdit v0.14.0.0 or above. Please download it here 'https://github.com/FallenCharlotte/ChroMapper-PropEdit/releases'");
                dialog.AddFooterButton(() => { }, "Okay");
                dialog.Open();
                return;
            }

            if (propEditAssembly.GetName().Version < new Version("0.14.0.0"))
            {
                var dialog = PersistentUI.Instance.CreateNewDialogBox().WithNoTitle();
                dialog.AddComponent<TextComponent>().WithInitialValue($"PaulMapper requires PropEdit v0.14.0.0 or above. Please update it! 'https://github.com/FallenCharlotte/ChroMapper-PropEdit/releases'");
                dialog.AddFooterButton(() => { }, "Okay");
                dialog.Open();
                return;
            }

            SceneManager.sceneLoaded += SceneLoaded;

            ExtensionButtons.AddButton(LoadSprite("PaulMapper.Resources.Icon.png"), "Paul Mapper", () => { paulMapper?.ToggleUI(); });

            InitKeybinds();
        }

        [Exit]
        private void Exit()
        {
            PaulMapperData.INSTANCE.SaveData();
        }

        private void InitKeybinds()
        {
            CMInputCallbackInstaller.InputInstance.Disable();

            var actionMap = CMInputCallbackInstaller.InputInstance.asset.AddActionMap("Paul Mapper");
            openMenu = actionMap.AddAction("Paul Mapper Menu", type: InputActionType.Button);
            openMenu.AddBinding("<Keyboard>/f10");

            createPoodle = actionMap.AddAction("Create Poodle", type: InputActionType.Button);
            createPoodle.AddBinding("<Keyboard>/f12");

            addAnchor = actionMap.AddAction("Add Anchor Point", type: InputActionType.Button);
            addAnchor.AddBinding("<Keyboard>/c");

            wallLeft = actionMap.AddAction("Rotate Left", type: InputActionType.Button);
            wallLeft.AddCompositeBinding("OneModifier")
                .With("Modifier", "<keyboard>/alt")
                .With("Binding", "<Keyboard>/leftArrow");

            wallRight = actionMap.AddAction("Rotate Right", type: InputActionType.Button);
            wallRight.AddCompositeBinding("OneModifier")
                .With("Modifier", "<keyboard>/alt")
                .With("Binding", "<Keyboard>/rightArrow");

            wallForward = actionMap.AddAction("Rotate Forwards", type: InputActionType.Button);
            wallForward.AddCompositeBinding("OneModifier")
                .With("Modifier", "<keyboard>/alt")
                .With("Binding", "<Keyboard>/upArrow");

            wallBack = actionMap.AddAction("Rotate Backwards", type: InputActionType.Button);
            wallBack.AddCompositeBinding("OneModifier")
                .With("Modifier", "<keyboard>/alt")
                .With("Binding", "<Keyboard>/downArrow");

            GameObject.FindFirstObjectByType<LoadKeybindsController>().InputObjectCreated(null);

            CMInputCallbackInstaller.InputInstance.Enable();
        }

        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.buildIndex == 3) //Mapper scene 
            {
                if (paulMapper == null || !paulMapper.isActiveAndEnabled)
                {
                    paulMapper = new GameObject("PaulMapper").AddComponent<PaulMapper>();
                }
            }
        }

        private static Sprite LoadSprite(string asset)
        {
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(asset);
            byte[] array = new byte[manifestResourceStream.Length];
            manifestResourceStream.Read(array, 0, (int)manifestResourceStream.Length);
            Texture2D texture2D = new Texture2D(256, 256);
            texture2D.LoadImage(array);
            return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0f, 0f), 100f);
        }
    }
}
