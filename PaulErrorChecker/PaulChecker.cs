using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;
using PaulMapper;
using SmartXLS;
using SmartXLS.data;
using PaulMapper.PaulHelper;

namespace PaulErrorChecker
{
    public class PaulCheckerMono : MonoBehaviour
    {
        public int currentPaul = 0;

        public AudioTimeSyncController ats;
        private NotesContainer notesContainer;
        private BeatmapObjectContainerCollection beatmapObjectContainerCollection;

        private void Start()
        {
            ats = BeatmapObjectContainerCollection.GetCollectionForType(0).AudioTimeSyncController;
            notesContainer = UnityEngine.Object.FindObjectOfType<NotesContainer>();
            beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();
        }

        private void OnGUI()
        {
            if (PaulMapper.Plugin.momenter.showGUI)
            {
                if (GUI.Button(new Rect(PaulMomenter.guiX, 160, 120, 20), "CheckPauls"))
                {
                    PaulChecker.CheckAllPauls();
                }



                if (GUI.Button(new Rect(PaulMomenter.guiX + PaulMomenter.guiWidth / 2 + 5, 130, PaulMomenter.guiWidth / 2 - 10, 20), "All"))
                {
                    foreach (Paul paul in PaulFinder.pauls)
                    {
                        foreach (BeatmapNote beatmapNote in paul.notes)
                        {
                            SelectionController.Select(beatmapNote, true);
                        }
                    }
                }




                int left = 4;
                int top = 7;
                int right = 13;
                int bottom = 31;


                if (GUI.Button(new Rect(PaulMomenter.guiX, 280, 140, 40), "Print to Datasheet"))
                {

                    WorkBook workBook = new WorkBook();

                    workBook.NumSheets = PaulFinder.pauls.Count;

                    int paulnum = 0;

                    foreach (Paul paul in PaulFinder.pauls)
                    {
                        workBook.setSheetName(paulnum, $"Paul {paul.PaulNumber}");

                        workBook.setText(paulnum, 0, 0, "Time [s]");
                        workBook.setText(paulnum, 0, 1, "Angle [Deg/s]");

                        int i = 1;
                        foreach (KeyValuePair<float, float> TimeAnglePair in paul.AngleChangeOverTimeDict)
                        {
                            workBook.setNumber(paulnum, i, 0, Convert.ToDouble(TimeAnglePair.Key));
                            workBook.setNumber(paulnum, i, 1, Convert.ToDouble(TimeAnglePair.Value));
                            i++;
                        }


                        workBook.Sheet = paulnum;

                        ChartShape chart = workBook.addChart(left, top, right, bottom);

                        chart.ChartType = ChartShape.Scatter;

                        chart.addSeries();
                        chart.setSeriesName(0, "Angle Change");
                        chart.setSeriesXValueFormula(0, $"{workBook.getSheetName(paulnum)}!$A$2:$A${paul.AngleChangeOverTimeDict.Count + 1}");
                        chart.setSeriesYValueFormula(0, $"{workBook.getSheetName(paulnum)}!$B$2:$B${paul.AngleChangeOverTimeDict.Count + 1}");


                        chart.setAxisTitle(ChartShape.XAxis, 0, "Time [s]");
                        chart.setAxisTitle(ChartShape.YAxis, 0, "AngleChange [Deg/s]");

                        paulnum++;
                    }

                    workBook.write("result.xls");

                }
            }
        }
    }

    public static class PaulChecker
    {
        
        public static PaulErrorResults paulErrorResult;

        public static void CheckAllPauls()
        {
            TimeKeeper TK = new TimeKeeper();
            TK.Start();
            NotesContainer notesContainer = UnityEngine.Object.FindObjectOfType<NotesContainer>();
            List<BeatmapNote> allNotes = (from BeatmapNote it in notesContainer.LoadedObjects
                                          orderby it.Time
                                          select it).ToList();

            AudioTimeSyncController ats = Plugin.PCM.ats;


            List<BeatmapNote> notesLeft = allNotes.Where(n => n.Type == 0).ToList();
            List<BeatmapNote> notesRight = allNotes.Where(n => n.Type == 1).ToList();

            paulErrorResult = new PaulErrorResults();
            List<PaulError> paulErrorsList = new List<PaulError>();


            TimeKeeper TC = new TimeKeeper();
            TC.Start();

            //now check the pauls
            foreach (Paul paul in PaulFinder.pauls)
            {
                //Poodle stuff
                if (paul.notes.All(p => p.CustomData != null && p.CustomData.HasKey("_cutDirection")))
                {
                    float angChange = 0;
                    float[] timePoints = new float[paul.notes.Count - 1];
                    float[] angleChangeOverTime = new float[paul.notes.Count - 1];

                    for (int i = 1; i < paul.notes.Count; i++)
                    {
                        float angOne = paul.notes[i].CustomData["_cutDirection"].AsFloat;
                        float angTwo = paul.notes[i - 1].CustomData["_cutDirection"].AsFloat;

                        float ang = angOne - angTwo;
                        ang = Mathf.Abs(ang);

                        if (ang > 180)
                            ang = 360 - ang;

                        timePoints[i-1] = ats.GetSecondsFromBeat(paul.notes[i].Time);
                        angleChangeOverTime[i-1] = ang / (ats.GetSecondsFromBeat(paul.notes[i].Time) - ats.GetSecondsFromBeat(paul.notes[i - 1].Time));

                        angChange += ang;


                        //Vision block
                        if (!paulErrorsList.Any(e => e is PoodleErrorVisionBlock) )
                        {
                            //First check if it is vision block area
                            if (paul.notes[i].GetRealPosition().x > -1.5 && paul.notes[i].GetRealPosition().x < 0.5 &&
                                paul.notes[i].GetRealPosition().y > 0.5 && paul.notes[i].GetRealPosition().y < 1.5
                                )
                            {
                                //Need to look at which direction the note is moving
                                bool movingX = Math.Abs(Math.Round(paul.notes[i].GetRealPosition().x - paul.notes[i - 1].GetRealPosition().x, 2)) > 0;
                                bool down = paul.notes[i].GetRealPosition().y - paul.notes[i - 1].GetRealPosition().y < 0;

                                if (!down || !movingX)
                                {
                                    paulErrorsList.Add(new PoodleErrorVisionBlock() { Paul = paul });
                                }
                            }
                        }


                    }
                    if (angChange > 90) //Overrotation
                    {
                        paulErrorsList.Add(new PoodleErrorSmallOverRotation() { Paul = paul });
                    }
                    else if (angChange > 120) //Overrotation
                    {
                        paulErrorsList.Add(new PoodleErrorBigOverRotation() { Paul = paul });
                    }


                    //To fast angle change
                    float maxChange = angleChangeOverTime.Max();
                    float avgChange = angleChangeOverTime.Average();


                    paul.AngleChangeOverTimeDict = timePoints.Zip(angleChangeOverTime, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

                } else
                {
                    //Normal paul stuff
                    //If paul is toprow up
                    if (paul.notes.All(p => p.LineLayer == 2 && (p.CutDirection == 4 || p.CutDirection == 0 || p.CutDirection == 5)))
                    {
                        paulErrorsList.Add(new PaulErrorTopRowUp() { Paul = paul });
                    }

                    //If one note has weird rotation
                    if (!paul.notes.All(p => p.CutDirection == paul.notes[0].CutDirection || p.CutDirection == 8))
                        paulErrorsList.Add(new PaulErrorBadRotation() { Paul = paul });

                    //If paul too long
                    float paulLenght = ats.GetSecondsFromBeat(paul.notes.Last().Time - paul.notes[0].Time);
                    if (paulLenght > 0.700f) //Way too long
                        paulErrorsList.Add(new PaulErrorTooLong() { Paul = paul });
                    else if (paulLenght > 0.620f) //A bit too long
                        paulErrorsList.Add(new PaulErrorBitLong() { Paul = paul });

                    //Inline paul
                    Paul lastPaul = PaulFinder.pauls.IndexOf(paul) > 0 ? PaulFinder.pauls[PaulFinder.pauls.IndexOf(paul) - 1] : null;
                    if (lastPaul != null &&
                        paul.notes[0].LineIndex == lastPaul.notes.Last().LineIndex && // on same line
                        ats.GetSecondsFromBeat(paul.notes[0].Time - lastPaul.notes.Last().Time) < 0.350f // Is too close for inline
                        )
                    {
                        //Also check if it might be a missing note in paul
                        if (ats.GetSecondsFromBeat(paul.notes[0].Time - lastPaul.notes.Last().Time) < paul.PaulPrecision * 3 + 0.01)
                        {
                            paulErrorsList.Add(new PaulErrorMissingBlock() { Paul = paul });
                        }
                        else
                        {
                            paulErrorsList.Add(new PaulErrorInline() { Paul = paul });
                        }


                    }
                }

                //Common stuff

                //If paul is flick
                //Look at end note, 
                BeatmapNote lastInPaul = paul.notes.Last();
                //If last is not last in map
                if (notesLeft.Last() != lastInPaul && notesRight.Last() != lastInPaul)
                {
                    //if next is within 350ms it might play bad
                    BeatmapNote nextNote = allNotes.Where(n => n.Type == lastInPaul.Type && n.Time > lastInPaul.Time).First();
                    if (ats.GetSecondsFromBeat(nextNote.Time - lastInPaul.Time) < 0.350f)
                    {
                        //Check if dots have already been placed
                        if (!paul.notes.Skip(paul.notes.Count - 5).All(p => p.CutDirection == 8))
                        {
                            paulErrorsList.Add(new PaulErrorFlick() { Paul = paul });
                        }

                    }
                }

            }


            paulErrorResult.paulErrorsList = paulErrorsList.OrderBy(p => p.ErrorType).ToList();
            paulErrorResult.allPauls = PaulFinder.pauls.OrderBy(p => p.PaulNumber).ToList();

            TC.Complete("ErrorCheck");

            /*
            //Print all the errors into a file
            string json = JsonConvert.SerializeObject(paulErrorsList.OrderBy(p => p.ErrorType), Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "PaulErrorLog.json"), json);
            */


            //Print checker results
            string json = JsonConvert.SerializeObject(paulErrorResult, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "PaulCheckerResults.json"), json);


            string allPauls = JsonConvert.SerializeObject(PaulFinder.pauls.OrderBy(p => p.PaulNumber), Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "AllFoundPauls.json"), allPauls);

            List<PaulError> paulErrors = paulErrorsList.Where(p => p.ErrorType == ErrorType.Erorr).ToList();
            List<PaulError> paulWarnings = paulErrorsList.Where(p => p.ErrorType == ErrorType.Warning).ToList();
            List<PaulError> paulSuggestions = paulErrorsList.Where(p => p.ErrorType == ErrorType.Suggestion).ToList();

            Debug.LogError("-------------------------------");
            Debug.LogError("--------- Paul Mapper ---------");
            Debug.LogError("-------------------------------");
            Debug.LogError($"Found {paulErrors.Count} Errors");
            Debug.LogError($"Found {paulWarnings.Count} Warnings");
            Debug.LogError($"Found {paulSuggestions.Count} Suggestions");
            Debug.LogError("-------------------------------");

            TK.Complete("AllPaulCheck");
        }


    }


    public enum ErrorType
    {
        [EnumMember(Value = "Error")]
        Erorr,
        [EnumMember(Value = "Warning")]
        Warning,
        [EnumMember(Value = "Suggestion")]
        Suggestion
    }

    public class PaulErrorResults
    {
        [JsonProperty(Order = 1)]
        public float AveragePaulLength { get => allPauls.Select(p => p.PaulLength).Sum() / allPauls.Count; }
        [JsonProperty(Order = 2)]
        public Paul LongestPaul { get => allPauls.OrderBy(p => p.PaulLength).Last(); }

        [JsonProperty(Order = 3)]
        public List<PaulError> Errors { get => paulErrorsList.Where(p => p.ErrorType == ErrorType.Erorr).OrderBy(e => e.ErrorText).ToList(); }
        [JsonProperty(Order = 4)]
        public List<PaulError> Warnings { get => paulErrorsList.Where(p => p.ErrorType == ErrorType.Warning).OrderBy(e => e.ErrorText).ToList(); }
        [JsonProperty(Order = 5)]
        public List<PaulError> Suggestions { get => paulErrorsList.Where(p => p.ErrorType == ErrorType.Suggestion).OrderBy(e => e.ErrorText).ToList(); }

        [JsonIgnore]
        public List<PaulError> paulErrorsList;
        
        [JsonIgnore]
        public List<Paul> allPauls;
    }


    public class TimeKeeper
    {
        public DateTime StartTime;
        public void Start() => StartTime = DateTime.Now;
        public void Complete(string action) => Debug.LogError($"{action} Completed in {(DateTime.Now - StartTime).TotalSeconds} seconds");
    }

    public interface PaulError
    {
        [JsonConverter(typeof(StringEnumConverter))]
        ErrorType ErrorType { get; }

        string ErrorText { get; }

        Paul Paul { get; set; }
    }

    public class PaulErrorTopRowUp : PaulError
    {
        public string ErrorText => "Top row upwards paul, will rarely work out";

        public ErrorType ErrorType => ErrorType.Warning;

        public Paul Paul { get; set; }
    }

    public class PaulErrorTooLong : PaulError
    {
        public ErrorType ErrorType => ErrorType.Erorr;

        public string ErrorText => $"Paul is too long and will be very hard to play";

        public Paul Paul { get; set; }
    }

    public class PaulErrorBitLong : PaulError
    {
        public ErrorType ErrorType => ErrorType.Warning;

        public string ErrorText => $"Paul might be a bit too long to play smoothly";

        public Paul Paul { get; set; }
    }

    public class PaulErrorFlick : PaulError
    {
        public ErrorType ErrorType => ErrorType.Suggestion;

        public string ErrorText => "Paul is ending in a flick, might want to make the paul end in a few dot notes";

        public Paul Paul { get; set; }
    }

    public class PaulErrorBadRotation : PaulError
    {
        public ErrorType ErrorType => ErrorType.Erorr;

        public string ErrorText => "Paul contains a note with wrong rotation";

        public Paul Paul { get; set; }
    }

    public class PaulErrorInline : PaulError
    {
        public ErrorType ErrorType => ErrorType.Erorr;

        public string ErrorText => "Paul is inline, hard to read and hit";

        public Paul Paul { get; set; }
    }

    public class PaulErrorMissingBlock : PaulError
    {
        public ErrorType ErrorType => ErrorType.Erorr;

        public string ErrorText => "Either paul is way too close or the paul is missing a note somewhere";

        public Paul Paul { get; set; }
    }

    public class PoodleErrorSmallOverRotation : PaulError
    {
        public ErrorType ErrorType => ErrorType.Warning;

        public string ErrorText => "Poodle is rotating over 90 degrees";

        public Paul Paul { get; set; }
    }

    public class PoodleErrorBigOverRotation : PaulError
    {
        public ErrorType ErrorType => ErrorType.Erorr;

        public string ErrorText => "Poodle is rotating over 120 degrees";

        public Paul Paul { get; set; }
    }

    public class PoodleErrorVisionBlock : PaulError
    {
        public ErrorType ErrorType => ErrorType.Erorr;

        public string ErrorText => "Poodle is visionblocking itself";

        public Paul Paul { get; set; }
    }
}
