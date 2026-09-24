using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaulMapper
{
    [Plugin("PaulMapper")]
    public class Plugin
    {
        public static PaulMapper? paulMapper;
        public static InputAction? openMenu;
        public static InputAction? createPoodle;
        public static InputAction? addAnchor;

        public static bool UpToDate = true;

        [Init]
        private void Init()
        {
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

            CMInputCallbackInstaller.InputInstance.Enable();
        }

        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.buildIndex == 3) //Mapper scene 
            {
                PaulMapper pm = GameObject.FindFirstObjectByType<PaulMapper>();
                if (pm == null)
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
