using MusikMaskin;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Net.NetworkInformation;
// using Commands = MusikMaskin.Commands;

var app = new CommandApp<AutoMusicCommand>();
app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
    config.UseStrictParsing();

    config.AddCommand<SongCommand>("song");
    config.AddCommand<AutoMusicCommand>("auto");
});
return app.Run(args);


public class SongCommand : Command<SongCommand.Arg>
{
    public class Arg : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("The music file to generate")]
        public required string MusicFile { get; init; }

        [CommandOption("-t|--bpm")]
        [Description("The beats/minute")]
        [DefaultValue(120)]
        public int Bpm { get; init; }

        [CommandOption("-i|--instrument")]
        [Description("The instrument to load")]
        [DefaultValue(null)]
        public string? Instrument { get; init; }
    }

    private static void Print(string status, string what = "Synth") => AnsiConsole.MarkupLine($"{what}: [green]{status}[/]!");

    public override int Execute(CommandContext? context, Arg arg)
    {
        var lines = File.ReadAllLines(arg.MusicFile);
        var pattern = string.Join('\n', lines.Where(l => l.StartsWith('#') == false)); // strip headers/comments
        var song = new Song
        {
            BeatsPerMinute = arg.Bpm
        };

        Instrument? instrument = null;

        if (arg.Instrument != null)
        {
            instrument = ComplexInstrument.Load(arg.Instrument);
            if (instrument == null)
            {
                Console.WriteLine("Failed to load instrument, aborting");
                return -1;
            }
        }
        else
        {
            instrument = new SimpleInstrument();
        }

        Print($"Using {instrument.Name}");
        var errors = song.NewTrack("song", instrument).NotesFromPattern(pattern);
        if (errors.Length > 0)
        {
            foreach (var err in errors)
            {
                AnsiConsole.MarkupLine($"[red]ERROR[/]: {err}");
            }
            return -1;
        }

        Print("Synthing");
        Print($"{song.Length} seconds long", "...");

        var player = new Player(song, new WesternScale());
        SynthGen.SynthMono(TimeSpan.FromSeconds(song.Length), t => player.Synth(t))
            .WriteToDisk("music.wav");

        Print("Done");
        return 0;
    }
}


public class AutoMusicCommand : Command<AutoMusicCommand.Arg>
{
    public class Arg : CommandSettings
    {
        // [CommandArgument(0, "<name>")]
        // [Description("The name to greet")]
        // public required string Name { get; init; }

        // [CommandOption("-c|--count")]
        // [Description("Number of times to greet")]
        // [DefaultValue(1)]
        // public int Count { get; init; }
    }

    private static void Print(string status, string what="Synth") => AnsiConsole.MarkupLine($"{what}: [green]{status}[/]!");

    public override int Execute(CommandContext? context, Arg arg)
    {
        var song = new Song();
        song.NewTrack("song", new SimpleInstrument()).NotesFromPattern("E:1/2 D C D E E E -\r\nD D D -\r\n- E G G\r\nE D C D E E E -\r\nD D E D C:2");
        // Mary Had A Little Lamb
        Print("Mary had a little lamb", "Synthing");
        Print($"{song.Length} seconds long", "...");

        var player = new Player(song, new WesternScale());
        SynthGen.SynthMono(TimeSpan.FromSeconds(song.Length), t => player.Synth(t))
            .WriteToDisk("mono.wav");

        Print("Stereo sample", "Synthing");
        double master = 0.5;
        SynthGen.SynthStereo(TimeSpan.FromSeconds(3), t => new Sample(Left: master * Oscillator.Sine.Generate(t, 440), Right: master * 0.5 * Oscillator.Square.Generate(t, 400)))
            .WriteToDisk("stereo.wav");

        Print("Done");
        return 0;
    }
}

