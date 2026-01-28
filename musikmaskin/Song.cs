using Chords;
using System.Text.RegularExpressions;

namespace MusikMaskin;

internal record NoteToPlay(double StartTimeBeat, double LengthBeat, int Semitone)
{
    public double EndTimeBeat => StartTimeBeat + LengthBeat;
}

public record ParsedNote(int? SemiTone, double? Length, bool ChangeAll)
{
    private static Regex reg = new Regex(@"^(?:(?<note>[ABCDEFG][#b.]?)(?<octave>[0-9]+)?|(?:-))(?:(?<change>:|!)(?<first>[0-9]+)(?:\/(?<under>[0-9]+))?)?$");

    private static int? SemitoneFromKey(string c) => c switch
    {
        "C" => 0,
        "C#" => 1,
        "D" => 2,
        "D#" => 3,
        "E" => 4,
        "F" => 5,
        "F#" => 6,
        "G" => 7,
        "G#" => 8,
        "A" => 9,
        "A#" or "Bb" => 10,
        "B" => 11,
        _ => null
    };

    public static (ParsedNote?, string) ParseNote(string token)
    {
        var match = reg.Match(token);
        if (!match.Success)
        {
            return (null, $"Invalid token: {token}");
        }

        string? GetGroup(string grp)
        {
            var g = match.Groups[grp];
            return g.Success ? g.Value : null;
        }

        var note = GetGroup("note");
        var octave = GetGroup("octave");
        var first = GetGroup("first");
        var under = GetGroup("under");
        var tempo = GetGroup("temp");

        var changeAll = tempo == null || tempo == "!";

        double? stepLength = null;

        // change step length
        if (first != null)
        {
            var f = double.Parse(first);
            var u = under != null ? double.Parse(under) : 1;

            stepLength = f / u;
        }

        if (note == null) return (new ParsedNote(null, stepLength, changeAll), "");
        
        int? foundSemitone = SemitoneFromKey(note);
        if (foundSemitone == null)
        {
            return (null, $"Invalid name of note: {note}");
        }

        var oct = 4;
        if (octave != null)
        {
            oct = int.Parse(octave);
        }

        // transform from C to A
        var semitoneC2 = foundSemitone.Value - 9;

        // transform from requested to a2
        var semitone = semitoneC2 + (oct - 2) * 12;

        return (new ParsedNote(semitone, stepLength, changeAll), "");
    }
}

internal class Track(string name, Instrument instrument)
{
    public string Name => name;
    public Instrument Instrument => instrument;
    public readonly List<NoteToPlay> Notes = new();

    public string[] NotesFromPattern(string pattern, double notePercentage = 0.8)
    {
        var errors = new List<string>();

        Notes.Clear();
        var tokens = pattern.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        double beat = 0;
        
        var stepLength = 1.0;

        foreach (var token in tokens)
        {
            if (token.Length == 0) continue;

            var (note, err) = ParsedNote.ParseNote(token);
            if (note == null)
            {
                errors.Add(err);
                continue;
            }

            // change step length
            if (note.Length != null)
            {
                stepLength = note.Length.Value;
            }

            if(note.SemiTone != null)
            {
                Notes.Add(new NoteToPlay(beat, stepLength * notePercentage, note.SemiTone.Value));
            }
            beat += stepLength;
        }

        return errors.ToArray();
    }
}

public class Bpm(double bpm=120)
{
    public double BeatsPerMinute = bpm;

    public double SecondsFromBeats(double beats)
    {
        return beats * 60.0 / BeatsPerMinute;
    }

    public double BeatsFromSeconds(double seconds)
    {
        return seconds * BeatsPerMinute / 60.0;
    }
}

internal class Song : Bpm
{
    // https://en.wikipedia.org/wiki/Tempo#Approximately_from_the_slowest_to_the_fastest

    public readonly List<Track> Tracks = new ();

    public Track NewTrack(string name, Instrument inst)
    {
        var t = new Track(name, inst);
        Tracks.Add(t);
        return t;
    }

    public double Length => Tracks
        .SelectMany(track => track.Notes.Select(note => new { track.Instrument, EndTIme = note.EndTimeBeat}))
        .Select(x => x.Instrument.Envelope.GetFade(SecondsFromBeats(x.EndTIme)))
        .DefaultIfEmpty(0)
        .Max();
}

internal class Player(Song song, WesternScale scale)
{
    private readonly List<(ActiveNote, NoteToPlay)> _activeNotes = new();
    private readonly Synth _synth = new Synth();
    private double _lastTimeSec = -1;
    public double Synth(double currentTimeSec)
    {
        bool IsTriggered(double tBeat)
        {
            return song.BeatsFromSeconds(_lastTimeSec) < tBeat && tBeat <= song.BeatsFromSeconds(currentTimeSec);
        }
        foreach (var track in song.Tracks)
        {
            // todo(Gustav): this should be ordered better
            foreach (var note in track.Notes)
            {
                if (!IsTriggered(note.StartTimeBeat)) continue;
                var playing = _synth.NoteOn(song.SecondsFromBeats(note.StartTimeBeat), note.Semitone, track.Instrument);
                _activeNotes.Add((playing, note));
            }
        }

        for (int noteIndex = 0; noteIndex < _activeNotes.Count;)
        {
            var (playing, note) = _activeNotes[noteIndex];
            if (!IsTriggered(note.EndTimeBeat))
            {
                // not triggered, ignore
                noteIndex += 1;
            }
            else
            {
                // triggered, stop playing, and remove from tracking notes
                playing.NoteOff(song.SecondsFromBeats(note.EndTimeBeat));
                _activeNotes.RemoveAt(noteIndex);
            }
        }

        _synth.RemoveInactiveTones(currentTimeSec);

        _lastTimeSec = currentTimeSec;
        return _synth.Evaluate(scale, currentTimeSec);
    }
}