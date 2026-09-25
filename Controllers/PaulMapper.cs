using Beatmap.Base;
using Beatmap.Enums;
using ChroMapper_PropEdit.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PaulMapper
{
    public class PaulMapper : MonoBehaviour
    {
        public static AudioTimeSyncController ats;
        public static BeatmapObjectContainerCollection notesContainer;
        public static BeatmapObjectContainerCollection obstacleContainer;
        public static BeatmapObjectContainerCollection bpmChangesContainer;

        internal static UIHandler uiHandler = new UIHandler();

        public void Awake()
        {
            ats = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note).BeatmapContext.Atsc;
            notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Note);
            bpmChangesContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.BpmChange);
            obstacleContainer = BeatmapObjectContainerCollection.GetCollectionForType(Beatmap.Enums.ObjectType.Obstacle);

            if (Plugin.createPoodle != null)
                Plugin.createPoodle.performed += StartPoodle;

            if (Plugin.openMenu != null)
                Plugin.openMenu.performed += OpenMenu;

            if (Plugin.wallRight != null)
                Plugin.wallRight.performed += WallRight_performed;

            if (Plugin.wallLeft != null)
                Plugin.wallLeft.performed += WallLeft_performed;

            if (Plugin.wallForward != null)
                Plugin.wallForward.performed += WallForward_performed;

            if (Plugin.wallBack != null)
                Plugin.wallBack.performed += WallBack_performed;

            uiHandler.Init();
        }

        private void WallBack_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            NoteHelper.RotateWalls(true, false);
        }

        private void WallForward_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            NoteHelper.RotateWalls(false, false);
        }

        private void WallLeft_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            NoteHelper.RotateWalls(false, true);
        }

        private void WallRight_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            NoteHelper.RotateWalls(true, true);
        }

        private void OpenMenu(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            ToggleUI();
        }

        private void OnDisable()
        {
            PaulMapperData.INSTANCE.SaveData();

            if (Plugin.createPoodle != null)
                Plugin.createPoodle.performed -= StartPoodle;

            if (Plugin.openMenu != null)
                Plugin.openMenu.performed -= OpenMenu;

            if (Plugin.wallRight != null)
                Plugin.wallRight.performed -= WallRight_performed;

            if (Plugin.wallLeft != null)
                Plugin.wallLeft.performed -= WallLeft_performed;

            if (Plugin.wallForward != null)
                Plugin.wallForward.performed -= WallForward_performed;

            if (Plugin.wallBack != null)
                Plugin.wallBack.performed -= WallBack_performed;
        }

        public void ToggleUI()
        {
            uiHandler.ToggleWindow();
        }

        private void StartPoodle(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            BaseGrid[] beatmapObjects = SelectionController.SelectedObjects.OrderBy(o => o.SongBpmTime).Cast<BaseGrid>().ToArray();

            if (beatmapObjects.Length < 2)
            {
                SetNotice("Select at least 2 points", noticeType.Error);
                return;
            }
            if (beatmapObjects.Length == 2)
            {
                if (beatmapObjects[1].SongBpmTime - beatmapObjects[0].SongBpmTime < 1 / PaulMapperData.INSTANCE.Precision)
                {
                    SetNotice("Points are closer than precision", noticeType.Error);
                    return;
                }
            }

            if (beatmapObjects.All(b => b.ObjectType == Beatmap.Enums.ObjectType.Note))
            {
                GameObject gameObject = new GameObject("Curve");
                RealtimeCurve curve = gameObject.AddComponent<RealtimeNoteCurve>();
                curve.InstantiateCurve(beatmapObjects.ToList());
            }
            else if (beatmapObjects.All(b => b.ObjectType == Beatmap.Enums.ObjectType.Obstacle))
            {
                GameObject gameObject = new GameObject("Curve");
                RealtimeCurve curve = gameObject.AddComponent<RealtimeWallCurve>();
                curve.InstantiateCurve(beatmapObjects.ToList());
            }
            else
            {
                SetNotice("Only select objects of same type", noticeType.Error);
            }
        }

        public void SetNotice(string p_notice, noticeType noticeType)
        {
            uiHandler.SetNotice(p_notice, noticeType);
        }
    }
}
