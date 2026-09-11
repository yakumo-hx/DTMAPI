using DTMAPI.Authoring.Contracts;

namespace DTMAPI.AuthorSdk;

internal static class CodeModBuilder
{
    public static CommandReport BuildCommand(ParsedCommand command)
    {
        command.RequireOnlyOptions("compatibility-root", "output", "game-root", "configuration", "offline");
        if (command.Positionals.Count != 1) throw new CommandLineException("build requires one standard Mod project directory.");
        return StandardBuildIntegration.Build(PathSafety.FullPath(command.Positionals[0]), command.Option("compatibility-root"),
            command.Option("output"), command.Option("game-root"), command.Option("configuration", "Release"),
            offline: StandardBuildIntegration.BooleanOption(command, "offline"));
    }
}
