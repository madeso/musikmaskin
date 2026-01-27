namespace MusikMaskin;


internal class Song(WesternScale scale, Instrument instrument)
{
    public double BeatsPerMinute = 120 * 3;

    // Mary Had A Little Lamb
    public string Pattern = "E D C D E E E  D D D  E G G  E D C D E E E  D D E D CC";

    private int? SemitoneFromTime(double s)
    {
        var beatIndex = IndexFromTime(s);
        if (beatIndex >= Pattern.Length) return null;
        var key = Pattern[beatIndex];

        var semitone = "AaBCcDdEFfGg".IndexOf(key);
        if (semitone == -1) return null;
        return semitone;
    }

    private int IndexFromTime(double s)
    {
        var beatsPerSecond = BeatsPerMinute / 60.0;
        var beatIndex = (int)Math.Floor(s * beatsPerSecond);
        return beatIndex;
    }

    private ActiveNote? _activeNote = null;
    private Synth _synth = new Synth();
    public double Synth(double time)
    {
        var tone = SemitoneFromTime(time);
        if (tone != null)
        {
            if (_activeNote != null && _activeNote.Semitone != tone.Value)
            {
                _activeNote.NoteOff(time);
                _activeNote = null;
            }

            if (_activeNote == null)
            {
                _activeNote = _synth.NoteOn(time, tone.Value, instrument);
            }
        }

        if (tone == null && _activeNote != null)
        {
            _activeNote.NoteOff(time);
            _activeNote = null;
        }

        _synth.Clean(time);

        return _synth.Evaluate(scale, time);
    }
}