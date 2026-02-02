namespace MusikMaskin;

internal class WesternScale(double tuningFrequency = 440.0)
{
    // a2
    public double BaseFrequency => tuningFrequency / 4.0;

    private static readonly double SemitoneBaseStep = Math.Pow(2.0, 1.0 / 12.0);

    public double FrequencyFromSemitone(double semiToneFromA2) => BaseFrequency * Math.Pow(SemitoneBaseStep, semiToneFromA2);
}


internal enum Oscillator
{
    Sine, Square, Triangle, SawWarm, SawHarsh, Noise
}

internal class OscillatorSettings
{
    public double LfoHz {get; set;} = 0;
    public double LfoAmplitude { get; set; } = 0;
    public int SawWarmSteps { get; set; } = 50;
}

internal static class OscillatorFunctions
{
    public static double Generate(this Oscillator osc, double time, double hz, OscillatorSettings? settings = null)
    {
        var set = settings ?? new OscillatorSettings();
        var freq = time * W(hz) + set.LfoAmplitude * Math.Sin(W(set.LfoHz) * time);
        return osc switch
        {
            Oscillator.Sine => Math.Sin(freq),
            Oscillator.Square => Math.Sin(freq) > 0.0 ? 1.0 : -1.0,
            Oscillator.Triangle => Math.Asin(Math.Sin(freq)) * (2.0 / Math.PI),
            Oscillator.SawWarm => GenerateSawWarm(freq, set.SawWarmSteps),
            Oscillator.SawHarsh => GenerateSawHarsh(time, hz),
            Oscillator.Noise => GenerateNoise(),
            _ => throw new ArgumentOutOfRangeException(nameof(osc), osc, null)
        };
    }
    // from hertz to angular velocity
    private static double W(double hertz) => hertz * Math.PI * 2;

    private static double GenerateSawWarm(double freq, int steps)
    {
        var r = 0.0;
        for (var stepIndex = 1; stepIndex <= steps; stepIndex += 1)
        {
            r += Math.Sin(stepIndex * freq) / stepIndex;
        }
        return r;
    }

    private static double GenerateSawHarsh(double time, double hz) => (2 / Math.PI) * (hz * Math.PI * (time % (1.0 / hz)) - Math.PI / 2);

    private static readonly Random Rng = new();
    private static double GenerateNoise() => Rng.NextDouble() * 2 - 1;
}

internal abstract class Instrument(Envelope envelope)
{
    public abstract string Name
    {
        get;
    }
    public Envelope Envelope => envelope;
    public abstract double GetTone(double time, WesternScale scale, ActiveNote note);
}

internal class ActiveNote(Instrument instrument, int semitone, double pressedTime, double? releasedTime)
{
    public Instrument Instrument => instrument;
    private double? _releasedTime = releasedTime;

    public int Semitone => semitone;

    public double PressedTime => pressedTime;
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

    public void RemoveInactiveTones(double time)
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

    // if note ends at time, when does it truly end?
    public abstract double GetFade(double time);
}

public record EnvelopeADSR(double AttackTime, double DecayTime, double ReleaseTime, double SustainAmplitude) : Envelope
{
    public double AmplitudeAt(double time, double notePressedTime, double? noteReleaseTime)
    {
        if (noteReleaseTime == null) return CalculateAds(time, notePressedTime);
        
        var life = time - noteReleaseTime.Value;
        
        if (life < 0.0f) return CalculateAds(time, notePressedTime);

        if (life > ReleaseTime) return 0.0f;

        var from = CalculateAds(noteReleaseTime.Value, notePressedTime);
        return from * (life/ReleaseTime);
    }

    public bool IsAlive(double time, double noteReleasedTime)
    {
        var life = time - noteReleasedTime;
        return life < ReleaseTime;
    }

    public double GetFade(double time)
    {
        return time + ReleaseTime;
    }

    private double CalculateAds(double time, double notePressedTime)
    {
        var life = time - notePressedTime;
        if (life <= 0.0) return 0;

        // ads
        var attackLife = life - AttackTime;
        if (attackLife < 0) return ApplyAmplitudeLimit(life / AttackTime);

        if (attackLife < DecayTime)
        {
            var sustainChange = 1 - SustainAmplitude;
            var timeFromAttack = attackLife / DecayTime;
            var attackVolume = 1;
            return ApplyAmplitudeLimit(attackVolume - timeFromAttack * sustainChange);
        }

        return ApplyAmplitudeLimit(SustainAmplitude);

        static double ApplyAmplitudeLimit(double d)
        {
            if (d < 0.01) return 0;
            return d;
        }
    }
}