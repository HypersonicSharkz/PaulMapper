using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using UnityEngine;

namespace PaulMapper
{
    public class PaulMapperData : INotifyPropertyChanged
    {
        private static PaulMapperData? instance;

        private const string FileName = "paulMapper.json";

        public static PaulMapperData INSTANCE
        {
            get
            {
                return instance ??= GetSaveData();
            }
        }

        private int precision = 32;
        public int Precision
        {
            get => precision;
            set => SetField(ref precision, value);
        }

        private bool vibro = false;
        public bool Vibro
        {
            get => vibro;
            set => SetField(ref vibro, value);
        }

        private bool rotateNotes = true;
        public bool RotateNotes
        {
            get => rotateNotes;
            set => SetField(ref rotateNotes, value);
        }

        private bool arcs = true;
        public bool Arcs
        {
            get => arcs;
            set => SetField(ref arcs, value);
        }

        private bool autoDot = true;
        public bool AutoDot
        {
            get => autoDot;
            set => SetField(ref autoDot, value);
        }

        private float transitionTime = 0.3f;
        public float TransitionTime
        {
            get => transitionTime;
            set => SetField(ref transitionTime, value);
        }

        private bool transitionRotation = true;
        public bool TransitionRotation
        {
            get => transitionRotation;
            set => SetField(ref transitionRotation, value);
        }

        private bool usePointRotations = false;
        public bool UsePointRotations
        {
            get => usePointRotations;
            set => SetField(ref usePointRotations, value);
        }

        private bool fakeWalls;
        public bool FakeWalls
        {
            get => fakeWalls;
            set => SetField(ref fakeWalls, value);
        }

        private bool useScale = false;
        public bool UseScale
        {
            get => useScale;
            set => SetField(ref useScale, value);
        }

        private bool disableBadCutDirection = false;
        public bool DisableBadCutDirection
        {
            get => disableBadCutDirection;
            set => SetField(ref disableBadCutDirection, value);
        }

        private bool disableBadCutSpeed = false;
        public bool DisableBadCutSpeed
        {
            get => disableBadCutSpeed;
            set => SetField(ref disableBadCutSpeed, value);
        }

        private bool disableBadCutSaberType = false;
        public bool DisableBadCutSaberType
        {
            get => disableBadCutSaberType;
            set => SetField(ref disableBadCutSaberType, value);
        }

        private int wallRotationAmount = 5;
        public int WallRotationAmount
        {
            get => wallRotationAmount;
            set => SetField(ref wallRotationAmount, value);
        }

        private bool enableQuickMenu = true;
        public bool EnableQuickMenu
        {
            get => enableQuickMenu;
            set => SetField(ref enableQuickMenu, value);
        }

        private bool useEndPrecision;
        public bool UseEndPrecision
        {
            get => useEndPrecision;
            set => SetField(ref useEndPrecision, value);
        }

        private int endPrecision;
        public int EndPrecision
        {
            get => endPrecision;
            set => SetField(ref endPrecision, value);
        }

        private bool adjustToWorldRotation = true;
        public bool AdjustToWorldRotation
        {
            get => adjustToWorldRotation;
            set => SetField(ref adjustToWorldRotation, value);
        }

        private bool SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private static string SavePath =>
            Path.Combine(
                Application.persistentDataPath,
                FileName);

        public static PaulMapperData GetSaveData()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);

                    var data = JsonConvert.DeserializeObject<PaulMapperData>(json);

                    if (data != null)
                    {
                        instance = data;
                        return data;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load PaulMapper data: {e}");
            }

            instance = new PaulMapperData();

            instance.SaveData();

            return instance;
        }

        public void SaveData()
        {
            try
            {
                string json = JsonConvert.SerializeObject(
                    this,
                    Formatting.Indented);

                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save PaulMapper data: {e}");
            }
        }

        public static bool IsV3()
        {
            return int.Parse(
                BeatSaberSongContainer.Instance.Map.Version
                    .Split('.')[0]) >= 3;
        }
    }
}