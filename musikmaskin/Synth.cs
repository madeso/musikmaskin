namespace MusikMaskin;

internal class WesternScale(double tuningFrequency = 440.0)
{
    // a2
    public double BaseFrequency => tuningFrequency / 4.0;

    private static readonly double SemitoneBaseStep = Math.Pow(2.0, 1.0 / 12.0);

    public double FrequencyFromSemitone(double semiToneFromA2) => BaseFrequency * Math.Pow(SemitoneBaseStep, semiToneFromA2);
}

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
        if(semitone == -1) return null;
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

internal static class Oscillator
{
    // from hertz to angular velocity
    private static double W(double hertz) => hertz * Math.PI * 2;

    public static double Sine(double t, double freq) => Math.Sin(t * W(freq));
    public static double Square(double t, double freq) => Math.Sin(t * W(freq)) > 0.0 ? 1.0 : -1.0;
    public static double Triangle(double t, double freq) => Math.Asin(Math.Sin(t * W(freq))) * (2.0 / Math.PI);

    public static double SawWarm(double t, double freq, int steps)
    {
        var r = 0.0;
        for (var stepIndex = 1; stepIndex <= steps; stepIndex += 1)
        {
            r += Math.Sin(stepIndex * t * W(freq)) / stepIndex;
        }
        return r;
    }

    public static double SawHarsh(double t, double freq) => (2 / Math.PI) * (freq * Math.PI * (t % (1.0 / freq)) - Math.PI / 2);

    private static readonly Random Rng = new Random();
    public static double Noise() => Rng.NextDouble() * 2 - 1;
}

internal abstract class Instrument(Envelope envelope)
{
    public Envelope Envelope => envelope;
    public abstract double GetTone(double time, WesternScale scale, ActiveNote note);
}

internal class ActiveNote(Instrument instrument, int semitone, double pressedTime, double? releasedTime)
{
    public Instrument Instrument => instrument;
    private double? _releasedTime = releasedTime;

    public int Semitone => semitone;

    public double? ReleasedTime => _releasedTime;

    public void NoteOff(double time)
    {
        _releasedTime = time;
    }

    public bool IsAlive(double time)
    {
        if(_releasedTime == null) return true;
        else return Instrument.Envelope.IsAlive(time, _releasedTime.Value);
    }

    public double Evaluate(double time, WesternScale scale, ActiveNote note)
    {
        var tone = Instrument.GetTone(time, scale, note);
        var env = Instrument.Envelope.AmplitudeAt(time, pressedTime, _releasedTime);
        return tone * env;
    }
}

internal class Synth
{
    public List<ActiveNote> ActiveNotes { get; } = new();

    public ActiveNote NoteOn(double time, int semiTone, Instrument instrument)
    {
        var note = new ActiveNote(instrument, semiTone, time, null);
        ActiveNotes.Add(note);
        return note;
    }

    public void Clean(double time)
    {
        ActiveNotes.RemoveAll(note => note.IsAlive(time) == false);
    }

    public double Evaluate(WesternScale scale, double time)
    {
        double amp = 0;

        foreach (var note in ActiveNotes)
        {
            amp += note.Evaluate(time, scale, note);
        }

        return amp;
    }
}

interface Envelope
{
    double AmplitudeAt(double time, double notePressedTime, double? noteReleaseTime);
    bool IsAlive(double time, double releaseTime);
}

public record EnvelopeADSR(double AttackTime, double DecayTime, double ReleaseTime, double SustainAmplitude) : Envelope
{
    public double AmplitudeAt(double time, double notePressedTime, double? noteReleaseTime)
    {
        if (noteReleaseTime == null) return GetAds(time, notePressedTime);
        
        var life = time - noteReleaseTime.Value;
        
        if (life < 0.0f) return GetAds(time, notePressedTime);

        if (life > ReleaseTime) return 0.0f;

        var from = GetAds(noteReleaseTime.Value, notePressedTime);
        return from * (life/ReleaseTime);
    }

    public bool IsAlive(double time, double noteReleasedTime)
    {
        var life = time - noteReleasedTime;
        return life < ReleaseTime;
    }

    private double GetAds(double time, double notePressedTime)
    {
        var life = time - notePressedTime;
        if (life <= 0.0) return 0;

        // ads
        var attackLife = life - AttackTime;
        if (attackLife < 0) return life/AttackTime;

        if (attackLife < DecayTime)
        {
            var sustainChange = 1 - SustainAmplitude;
            var timeFromAttack = attackLife / DecayTime;
            var attackVolume = 1;
            return attackVolume - timeFromAttack * sustainChange;
        }

        return SustainAmplitude;
    }
}