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
