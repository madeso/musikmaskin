using System.Runtime.InteropServices;
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
    private static void Print(string status, string what="Synth") => AnsiConsole.MarkupLine($"{what}: [green]{status}[/]!");

    public override int Execute(CommandContext? context, MusikMaskinSettings settings)
    {
        
        var scale = new WesternScale();
        var song = new Song(scale);
        song.NewTrack("song", new SimpleInstrument()).NotesFromPattern("E D C D E E E  D D D  E G G  E D C D E E E  D D E D CC");
        // Mary Had A Little Lamb
        Print("Mary had a little lamb", "Synthing");
        Print($"{song.Length} seconds long", "...");
        SynthGen.SynthMono(TimeSpan.FromSeconds(song.Length), t => song.Synth(t))
            .WriteToDisk("mono.wav");

        Print("Stereo sample", "Synthing");
        double master = 0.5;
        SynthGen.SynthStereo(TimeSpan.FromSeconds(3), t => new Sample(Left: master * Oscillator.Sine(t, 440), Right: master * 0.5 * Oscillator.Square(t, 400)))
            .WriteToDisk("stereo.wav");

        Print("Done");
        return 0;
    }
}

