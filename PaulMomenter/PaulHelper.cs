using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PaulMapper.PaulHelper
{
    public static class PaulFinder
    {
        public static List<Paul> pauls = new List<Paul>();
        public static int currentPaul = 0;

        public static List<Paul> FindAllPauls(List<BeatmapNote> allNotes)
        {
            List<BeatmapNote> notesLeft = allNotes.Where(n => n._type == 0).ToList();
            List<BeatmapNote> notesRight = allNotes.Where(n => n._type == 1).ToList();

            List<Paul> pauls = new List<Paul>();
            pauls.AddRange(FindPauls(notesLeft));
            pauls.AddRange(FindPauls(notesRight));

            //Debug.LogError("Found: " + pauls.Count + " Pauls");

            return pauls;
        }

        public static List<Paul> FindPauls(List<BeatmapNote> notesOneSide)
        {
            //Find closest notes

            BeatmapNote oldNote = notesOneSide[0];

            List<Paul> foundPauls = new List<Paul>();

            bool paul = false;

            List<BeatmapNote> groupedNotes = new List<BeatmapNote>();
            float lastPrecision = 0;

            foreach (BeatmapNote note in notesOneSide)
            {
                if (note._time != oldNote._time)
                {
                    float dist = note._time - oldNote._time;

                    if (lastPrecision != 0)
                    {

                        if (dist > lastPrecision - 0.01 && dist < lastPrecision + 0.01 && notesOneSide.IndexOf(note) != notesOneSide.Count - 1)
                        {
                            //Is still part of a paul
                            if (!paul)
                            {
                                paul = true;
                                groupedNotes = new List<BeatmapNote>();
                                groupedNotes.Add(notesOneSide[notesOneSide.IndexOf(note) - 2]);
                                groupedNotes.Add(oldNote);
                                groupedNotes.Add(note);
                            }
                            else
                            {
                                groupedNotes.Add(note);
                            }

                        }
                        else
                        {
                            if (paul)
                            {
                                if (notesOneSide.IndexOf(note) == notesOneSide.Count - 1)
                                    groupedNotes.Add(note);

                                paul = false;

                                //For a paul to be a paul it must be longer than 4 notes and not too long

                                if (groupedNotes.Count > 4 && (int)(1 / lastPrecision) > 8)
                                {
                                    foundPauls.Add(new Paul() { notes = groupedNotes, PaulPrecision = (int)(1 / lastPrecision) }); //Create paul
                                }

                            }
                        }

                        lastPrecision = dist;
                    }
                    else
                    {
                        //First note
                        lastPrecision = dist;
                    }
                }

                oldNote = note;

            }

            return foundPauls.OrderBy(p => p.Beat).ToList();
        }

        public static void GoToPaul(Paul paul)
        {
            SelectionController.DeselectAll();

            currentPaul = pauls.IndexOf(paul);

            PaulMomenter.ats.MoveToTimeInBeats(paul.notes[0]._time);


        }

        static Paul lastPaul;

        public static void SelectCurrentPaul()
        {
            Paul paul = null;

            Paul closest = pauls.OrderBy(p => Math.Abs(p.Beat - PaulMomenter.ats.CurrentBeat) ).First();
            if (lastPaul != null && closest == lastPaul && pauls.Any(p => p.Beat == lastPaul.Beat && p != lastPaul)) 
            {
                paul = pauls.First(p => p.Beat == lastPaul.Beat && p != lastPaul);
            } else
            {
                paul = closest;
            }


            SelectionController.DeselectAll();


            foreach (BeatmapNote note in paul.notes)
                SelectionController.Select(note, true, true, true);

            currentPaul = pauls.IndexOf(paul);

            lastPaul = paul;
        }
    }
}
