using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace PaulErrorChecker
{
    [Plugin("PaulChecker")]
    public class Plugin
    {
        public static PaulCheckerMono PCM;

        [Init]
        private void Init()
        {
            Debug.LogError("PaulError V0.1 - Loaded");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.buildIndex == 3) //Mapper scene 
            {
                if (PCM != null && PCM.isActiveAndEnabled)
                    return;

                PCM = new GameObject("PaulMomenter").AddComponent<PaulCheckerMono>();
            }

        }

    }
}
