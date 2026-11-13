using DTMAPI.AudioContent.Preparation;
using DTMAPI.Authoring.Contracts;

namespace DTMAPI.AuthorSdk;

internal static class AudioPrepareCommand
{
    internal static CommandReport Execute(ParsedCommand command)
    {
        command.RequireOnlyOptions("output", "use", "route", "volume-db");
        if (command.Positionals.Count != 1 || string.IsNullOrWhiteSpace(command.Option("output")))
            throw new CommandLineException("audio-prepare <audio-file> --use effect|music --output <directory> [--route actor|ui|ambience] [--volume-db <dB|-inf>]");
        string use = command.Option("use", "effect"), channel = command.Option("route", "actor");
        if (use != "effect" && use != "music") throw new CommandLineException("--use must be effect or music (streamed).");
        AudioPreparationChannel route = channel switch { "actor" => AudioPreparationChannel.Actor, "ui" => AudioPreparationChannel.Ui, "ambience" => AudioPreparationChannel.Ambience, _ => throw new CommandLineException("Unknown audio channel.") };
        if (use == "music" && route != AudioPreparationChannel.Actor) throw new CommandLineException("Music uses the game's MUSIC bus; --route applies to effect.");
        float? volumeDb = null;
        if (command.Option("volume-db") is string requested && requested.Length != 0)
        {
            try { volumeDb = AudioPreparer.ParseVolumeDb(requested); }
            catch (ArgumentException error) { throw new CommandLineException(error.Message); }
        }
        var result = AudioPreparer.Prepare(command.Positionals[0], command.Option("output"), use == "music", route, volumeDb: volumeDb);
        return new CommandReport { Command = command.Name, Success = true, RootPath = Path.GetFullPath(command.Positionals[0]), OutputPath = result.DeclarationPath,
            FileCount = result.MediaPath == null ? 2 : 3, Values = new(StringComparer.Ordinal) { ["bank"] = result.BankPath, ["media"] = result.MediaPath ?? "", ["durationSeconds"] = result.DurationSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) } };
    }
}
