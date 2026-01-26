namespace MusikMaskin;

internal class SimpleInstrument() : Instrument(new EnvelopeADSR(0.1, 0.1, 0.2, 0.8))
{
    public override double GetTone(double time, WesternScale scale, ActiveNote note)
    {
        return Oscillator.SawHarsh(time, scale.FrequencyFromSemitone(note.Semitone));
    }
}