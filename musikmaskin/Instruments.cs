using System.Diagnostics;
using Chords;
using KdlSharp;
using System.IO;

namespace MusikMaskin;

internal class SimpleInstrument(Oscillator oscillator = Oscillator.SawHarsh) : Instrument(new EnvelopeADSR(0.1, 0.1, 0.1, 0.8))
{
    public override string Name => $"Simple oscillator {oscillator}";

    public override double GetTone(double time, WesternScale scale, ActiveNote note)
    {
        return oscillator.Generate(time, scale.FrequencyFromSemitone(note.Semitone));
    }
}

enum Time
{
    Time, PressedMinusTime, TimeMinusPressed
}
internal record InstrumentTask(Oscillator Oscillator, double Volume, int Steps, Time Time, OscillatorSettings Settings);

internal class ComplexInstrument(string name, double volume, Envelope env, InstrumentTask[] tasks) : Instrument(env)
{
    public static ComplexInstrument? Load(string path)
    {
        var content = File.ReadAllText(path);
        var doc = KdlDocument.Parse(content);

        Envelope envelope = new EnvelopeADSR(0.1, 0.1, 0.1, 0.8);
        var volume = 1.0;
        var name = "untitled";
        List<InstrumentTask> tasks = [];
        bool status = true;

        foreach (var node in doc.Nodes)
        {
            void OnError(string err)
            {
                Console.WriteLine($"{path}({node.SourcePosition?.Line ?? -1}, {node.SourcePosition?.Offset ?? -1}): error for {node.Name}: {err}");
                status = false;
            }

            switch (node.Name)
            {
                case "name":
                {
                    var parser = new KdlEval.Parser();
                    string v = "";
                    parser.AddString("name", (x) => v = x);
                    if (parser.Parse(node, OnError) == false) continue;
                    name = v;
                    break;
                }
                case "envelope-adsr":
                {
                    var parser = new KdlEval.Parser();
                    double a = 0; double d = 0; double s = 0; double r = 0;
                    parser.AddDouble("attack", (x) => a = x);
                    parser.AddDouble("decay", (x) => d = x);
                    parser.AddDouble("sustain", (x) => s = x);
                    parser.AddDouble("release", (x) => r = x);
                    if (parser.Parse(node, OnError) == false) continue;
                    envelope = new EnvelopeADSR(a, d, r, s);
                    break;
                }
                case "volume":
                {
                    var parser = new KdlEval.Parser();
                    double v = 0;
                    parser.AddDouble("volume", x => v = x);
                    if (parser.Parse(node, OnError) == false) continue;
                    volume = v;
                    break;
                }
                case "sine":
                case "square":
                case "triangle":
                case "saw":
                case "saw-dig":
                case "noise":
                {
                    var parser = new KdlEval.Parser();
                    double v = 0; int steps = 0; var tt = Time.Time;
                        var settings = new OscillatorSettings();
                    parser.AddDouble("volume", x => v = x);
                    parser.AddInt("steps", x => steps = x);
                    parser.AddStringWithError("-time", time =>
                    {
                        switch (time)
                        {
                            case "time":
                                tt = Time.Time;
                                return "";
                            case "pressed-time":
                                tt = Time.PressedMinusTime;
                                return "";
                            case "time-pressed":
                                tt = Time.TimeMinusPressed;
                                return "";
                            default:
                                return "not a valid time property";
                        }
                    });
                    parser.AddDouble("-lfo-hertz", x => settings.LfoHz = x);
                    parser.AddDouble("-lfo-amplitude", x => settings.LfoAmplitude = x);
                    parser.AddInt("-steps", x => settings.SawWarmSteps = x);

                    if (parser.Parse(node, OnError) == false) continue;

                    var osc = node.Name switch
                    {
                        "sine" => Oscillator.Sine,
                        "square" => Oscillator.Square,
                        "triangle" => Oscillator.Triangle,
                        "saw" => Oscillator.SawWarm,
                        "saw-dig" => Oscillator.SawHarsh,
                        "noise" => Oscillator.Noise,
                        _ => throw new Exception($"buggy code handling {node.Name}"),
                    };

                    tasks.Add(new InstrumentTask(osc, Volume: v, Steps: steps, Time: tt, Settings: settings));
                    break;
                }
                default:
                    OnError($"Found invalid node {node.Name}");
                    break;
            }
        }

        if (status == false) return null;
        return new(name: name, volume: volume, env: envelope, tasks.ToArray());
    }

    public override string Name => name;

    public override double GetTone(double time, WesternScale scale, ActiveNote n)
    {
        double ret = 0;

        foreach (var t in tasks)
        {
            var tt = t.Time switch
            {
                Time.Time => time,
                Time.PressedMinusTime => n.PressedTime - time,
                Time.TimeMinusPressed => time - n.PressedTime,
                _ => throw new ArgumentOutOfRangeException()
            };

            ret += t.Volume * t.Oscillator.Generate(tt, scale.FrequencyFromSemitone(n.Semitone + t.Steps), t.Settings);
        }

        return ret * volume;
    }

    public override string ToString()
    {
        return name;
    }
}