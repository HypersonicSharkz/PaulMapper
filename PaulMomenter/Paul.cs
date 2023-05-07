using Beatmap.Base;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaulMapper
{
    public class Paul
    {
        [JsonIgnore]
        public List<BaseNote> notes;

        [JsonProperty(Order = 1)]
        public int PaulNumber { get => PaulHelper.PaulFinder.pauls.IndexOf(this) + 1; }

        [JsonProperty(Order = 2)]
        public float Beat { get => notes[0].SongBpmTime; }

        [JsonProperty(Order = 3)]
        public int PaulPrecision;

        [JsonProperty(Order = 4)]
        public float PaulLength { get => PaulMomenter.ats.GetSecondsFromBeat(notes[notes.Count - 1].SongBpmTime - notes[0].SongBpmTime); }

        [JsonIgnore]
        public Dictionary<float, float> AngleChangeOverTimeDict = new Dictionary<float, float>();

        [JsonProperty(Order = 5)]
        public float MaxAngleChange { get => AngleChangeOverTimeDict.Count > 0 ? AngleChangeOverTimeDict.Values.Max() : 0; }

        [JsonProperty(Order = 6)]
        public float AvgAngleChange { get => AngleChangeOverTimeDict.Count > 0 ? AngleChangeOverTimeDict.Values.Average() : 0; }
    }
}
