// using MusikMaskin;
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
        return 0;
    }
}

