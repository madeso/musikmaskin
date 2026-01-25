namespace MusikMaskin;

internal class WesternScale(double tuningFrequency = 440.0)
{
    // a2
    public double BaseFrequency => tuningFrequency / 4.0;

    private static readonly double SemitoneBaseStep = Math.Pow(2.0, 1.0 / 12.0);

    public double FrequencyFromSemitone(double semiToneFromA2) => BaseFrequency * Math.Pow(SemitoneBaseStep, semiToneFromA2);
}

internal class Song()
{
    public double BeatsPerMinute = 120 * 3;

    // Mary Had A Little Lamb
    public string Pattern = "E D C D E E E  D D D  E G G  E D C D E E E  D D E D CC";

    internal double? FrequencyAtSecond(WesternScale scale, double s)
    {
        var beatsPerSecond = BeatsPerMinute / 60.0;
        var beatIndex = (int)Math.Floor(s * beatsPerSecond);
        if (beatIndex >= Pattern.Length) return null;
        var key = Pattern[beatIndex];

        var semitone = "AaBCcDdEFfGg".IndexOf(key);
        if(semitone == -1) return null;

        return scale.FrequencyFromSemitone(24 + semitone);
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

internal record EnvelopeADSR(double AttackTime, double DecayTime, double ReleaseTime, double SustainAmplitude)
{
    double AmplitudeAt(double time, double notePressedTime, double? noteReleaseTime)
    {
        if (noteReleaseTime == null) return GetAds(time, notePressedTime);
        
        var life = time - noteReleaseTime.Value;
        
        if (life < 0.0f) return GetAds(time, notePressedTime);

        if (life > ReleaseTime) return 0.0f;

        var from = GetAds(noteReleaseTime.Value, notePressedTime);
        return from * (life/ReleaseTime);
    }

    private double GetAds(double time, double triggeredTime)
    {
        var life = time - triggeredTime;
        if (life <= 0.0) return 0;

        // ads
        var attackLife = life - AttackTime;
        if (attackLife < 0) return life/AttackTime;

        var sustainChange = 1 - SustainAmplitude;
        if (attackLife < DecayTime) return 1 - (1-(attackLife / DecayTime))*sustainChange;

        return SustainAmplitude;
    }
}