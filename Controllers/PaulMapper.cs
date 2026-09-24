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

        internal static UIHandler uiHandler;

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

            uiHandler = new UIHandler();
        }

        public void Update()
        {
            if (!RealtimeCurve.Editing)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        NoteHelper.RotateWalls(false, true);
                    }
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        NoteHelper.RotateWalls(true, true);
                    }
                }

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        NoteHelper.RotateWalls(false, false);
                    }
                }

                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        NoteHelper.RotateWalls(true, false);
                    }
                }
            }
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
