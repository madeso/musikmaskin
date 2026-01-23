using System.Diagnostics;
using System.Text;

namespace MusikMaskin;

internal record Sample(double Left, double Right);

internal static class SynthGen
{
    internal static Wav SynthMono(TimeSpan length, Func<double, double> generator, uint sampleRate=44100)
    {
        const int channels = 1;
        var sampleLength = (int)Math.Floor(length.TotalSeconds * sampleRate);
        var samples = new double[sampleLength * channels];

        for (int i = 0; i < sampleLength; i += 1)
        {
            var t = i / (double)sampleRate;
            var samp = generator(t);
            samples[i * channels + 0] = samp;
        }

        return new Wav(samples, sampleRate, channels);
    }

    internal static Wav SynthStereo(TimeSpan length, Func<double, Sample> generator, uint sampleRate=44100)
    {
        const int channels = 2;
        var sampleLength = (int)Math.Floor(length.TotalSeconds * sampleRate);
        var samples = new double[sampleLength * channels];

        for (int i = 0; i < sampleLength; i += 1)
        {
            var t = i / (double)sampleRate;
            var samp = generator(t);
            samples[i * channels + 0] = samp.Left;
            samples[i * channels + 1] = samp.Right;
        }

        return new Wav(samples, sampleRate, channels);
    }
}

internal record Wav(double[] Samples, uint SampleRate, ushort Channels)
{
    public void WriteToDisk(string fileName)
    {
        using var wavFile = File.OpenWrite(fileName);
        WriteToStream(wavFile);
    }

    public void WriteToStream(Stream stream)
    {
        WavWriter.WriteWavFile(Samples, SampleRate, Channels, stream);
    }
}

internal static class WavWriter
{
    /// https://stackoverflow.com/a/57225774/180307
    internal static void WriteWavFile(double[] samples, uint sampleRate, ushort channels, Stream stream)
    {
        const int chunkHeaderSize = 8,
            waveHeaderSize = 4,
            fmtChunkSize = 16;
        var samplesByteLength = (uint)samples.Length * 3u;

        // RIFF header
        WriteAscii(stream, "RIFF");
        WriteLittleEndianUInt32(stream, waveHeaderSize
                                         + chunkHeaderSize + fmtChunkSize
                                         + chunkHeaderSize + samplesByteLength);
        WriteAscii(stream, "WAVE");

        // fmt header
        WriteAscii(stream, "fmt ");
        WriteLittleEndianUInt32(stream, fmtChunkSize);
        WriteLittleEndianUInt16(stream, 1);               // AudioFormat = PCM
        WriteLittleEndianUInt16(stream, channels);
        WriteLittleEndianUInt32(stream, sampleRate);
        WriteLittleEndianUInt32(stream, sampleRate * channels);
        WriteLittleEndianUInt16(stream, (ushort)(3 * channels));    // Block Align (stride)
        WriteLittleEndianUInt16(stream, 24);              // Bits per sample

        // samples data
        WriteAscii(stream, "data");
        WriteLittleEndianUInt32(stream, samplesByteLength);
        foreach (var samp in samples)
        {
            var scaledValue = DoubleToInt24(samp);
            WriteLittleEndianInt24(stream, scaledValue);
        }
    }

    private static int DoubleToInt24(double value)
    {
        Debug.Assert(-1 <= value && value <= 1, "Clipping occured");
        var clipped = Math.Max(-1, Math.Min(1, value));

        const int int24Max = 0x7f_ffff;
        return (int)(clipped * int24Max);
    }

    private static void WriteAscii(Stream s, string str) => s.Write(Encoding.ASCII.GetBytes(str));

    private static void WriteLittleEndianUInt32(Stream s, uint i)
    {
        var b = new byte[4];
        b[0] = (byte)((i >> 0) & 0xff);
        b[1] = (byte)((i >> 8) & 0xff);
        b[2] = (byte)((i >> 16) & 0xff);
        b[3] = (byte)((i >> 24) & 0xff);
        s.Write(b);
    }

    private static void WriteLittleEndianInt24(Stream s, int i)
    {
        var b = new byte[3];
        b[0] = (byte)((i >> 0) & 0xff);
        b[1] = (byte)((i >> 8) & 0xff);
        b[2] = (byte)((i >> 16) & 0xff);
        s.Write(b);
    }

    private static void WriteLittleEndianUInt16(Stream s, ushort i)
    {
        var b = new byte[2];
        b[0] = (byte)((i >> 0) & 0xff);
        b[1] = (byte)((i >> 8) & 0xff);
        s.Write(b);
    }
}