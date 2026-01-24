using MusikMaskin;
using Spectre.Console;
using Spectre.Console.Cli;
// using Commands = MusikMaskin.Commands;

var app = new CommandApp<MusikMaskinCommand>();
app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
    config.UseStrictParsing();

    // config.AddCommand<MusikMaskinCommand>("track")
});
return app.Run(args);


public class MusikMaskinSettings : CommandSettings
{
    // [CommandArgument(0, "<name>")]
    // [Description("The name to greet")]
    // public required string Name { get; init; }
  
    // [CommandOption("-c|--count")]
    // [Description("Number of times to greet")]
    // [DefaultValue(1)]
    // public int Count { get; init; }
}
  
public class MusikMaskinCommand : Command<MusikMaskinSettings>
{
    public override int Execute(CommandContext? context, MusikMaskinSettings settings)
    {
        AnsiConsole.MarkupLine($"Hello, [green]dog[/]!");

        double master = 0.5;
        var scale = new WesternScale();
        var song = new Song();
        SynthGen.SynthMono(TimeSpan.FromSeconds(10), t =>
            {
                var freq = song.FrequencyAtSecond(scale, t);
                if (freq == null) return 0.0f;
                return master * 0.5 * Oscillator.SawHarsh(t, freq.Value);
            })
            .WriteToDisk("mono.wav");
        SynthGen.SynthStereo(TimeSpan.FromSeconds(3), t => new Sample(Left: master * Oscillator.Sine(t, 440), Right: master * 0.5 * Oscillator.Square(t, 400)))
            .WriteToDisk("stereo.wav");
        return 0;
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

