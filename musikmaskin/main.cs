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

