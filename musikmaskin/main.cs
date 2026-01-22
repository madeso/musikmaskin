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
        Synth.SynthMono(TimeSpan.FromSeconds(3), t => master * SineGen(t, 440))
            .WriteToDisk("mono.wav");
        Synth.SynthStereo(TimeSpan.FromSeconds(3), t => new Sample(Left: master * SineGen(t, 440), Right: master * 0.5 * SquareGen(t, 400)))
            .WriteToDisk("stereo.wav");
        return 0;
    }

    private static double SineGen(double t, double freq)
    {
        return Math.Sin(t * (freq * Math.PI * 2));
    }
    private static double SquareGen(double t, double freq)
    {
        return SineGen(t, freq) > 0.0 ? 1.0 : -1.0;
    }
}

