namespace MusikMaskin;

internal record NoteToPlay(double StartTimeBeat, double LengthBeat, int Semitone)
{
    public double EndTimeBeat => StartTimeBeat + LengthBeat;
}

internal class Track(string name, Instrument instrument)
{
    public string Name => name;
    public Instrument Instrument => instrument;
    public readonly List<NoteToPlay> Notes = new();

    public void NotesFromPattern(string pattern, double stepLength = 1.0, double notePercentage = 0.8)
    {
        Notes.Clear();
        var noteMap = "AaBCcDdEFfGg";
        var tokens = pattern.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        double beat = 0;
        foreach (var token in tokens)
        {
            if (token.Length == 0) continue;
            var key = token[0];
            int semitone = noteMap.IndexOf(key);
            Notes.Add(new NoteToPlay(beat, stepLength * notePercentage, semitone));
            beat += stepLength;
        }
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

internal class Song(WesternScale scale) : Bpm
{
    // https://en.wikipedia.org/wiki/Tempo#Approximately_from_the_slowest_to_the_fastest

    private readonly List<Track> _tracks = new ();

    public Track NewTrack(string name, Instrument inst)
    {
        var t = new Track(name, inst);
        _tracks.Add(t);
        return t;
    }

    public double Length => _tracks
        .SelectMany(track => track.Notes.Select(note => new { track.Instrument, EndTIme = note.EndTimeBeat}))
        .Max(x => x.Instrument.Envelope.GetFade(SecondsFromBeats(x.EndTIme)));

    // todo(Gustav): move player out of song
    private readonly List<(ActiveNote, NoteToPlay)> _activeNotes = new();
    private readonly Synth _synth = new Synth();
    private double _lastTimeSec = -1;
    public double Synth(double currentTimeSec)
    {
        bool IsTriggered(double tBeat)
        {
            return BeatsFromSeconds(_lastTimeSec) < tBeat && tBeat <= BeatsFromSeconds(currentTimeSec);
        }
        foreach (var track in _tracks)
        {
            // todo(Gustav): this should be ordered better
            foreach (var note in track.Notes)
            {
                if (!IsTriggered(note.StartTimeBeat)) continue;
                var playing = _synth.NoteOn(SecondsFromBeats(note.StartTimeBeat), note.Semitone, track.Instrument);
                _activeNotes.Add((playing, note));
            }
        }

        for(int noteIndex = 0; noteIndex<_activeNotes.Count;)
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
                playing.NoteOff(SecondsFromBeats(note.EndTimeBeat));
                _activeNotes.RemoveAt(noteIndex);
            }
        }

        _synth.RemoveInactiveTones(currentTimeSec);

        _lastTimeSec = currentTimeSec;
        return _synth.Evaluate(scale, currentTimeSec);
    }
}