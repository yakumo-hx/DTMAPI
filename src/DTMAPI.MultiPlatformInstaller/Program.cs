using System;
using System.Collections.Generic;
using System.IO;

namespace DTMAPI.MultiPlatformInstaller;

internal static class Program
{
    private static int Main(string[] args)
    {
        InstallerOptions options;
        try
        {
            options = ParseOptions(args);
        }
        catch (Exception ex)
        {
            PrintResult(InstallerResult.Failure(InstallerCodes.GamePath, 2,
                "命令行参数无效。", "Invalid command-line arguments.", ex.Message));
            PrintHelp();
            return 2;
        }

        if (options.Action == InstallerAction.Help)
        {
            PrintHelp();
            return 0;
        }

        if (options.Action == InstallerAction.Menu)
            return RunMenu(options);

        int exitCode = RunOnce(options);
        if (options.Pause && !Console.IsInputRedirected)
        {
            Console.WriteLine();
            Console.WriteLine("按 Enter 关闭 / Press Enter to close...");
            Console.ReadLine();
        }
        return exitCode;
    }

    private static int RunMenu(InstallerOptions baseOptions)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("DTMAPI-多平台 / DTMAPI - Multi-Platform");
            Console.WriteLine("Unsigned experimental host tool / 未签名实验性宿主工具");
            Console.WriteLine("1. Install DTMAPI / 安装");
            Console.WriteLine("2. Uninstall DTMAPI Runtime / 卸载 Runtime");
            Console.WriteLine("3. Check status / 检查状态");
            Console.WriteLine("4. Collect logs / 收集日志");
            Console.WriteLine("0. Exit / 退出");
            Console.Write("> ");
            string? choice = Console.ReadLine();
            InstallerAction action = choice?.Trim() switch
            {
                "1" => InstallerAction.Install,
                "2" => InstallerAction.Uninstall,
                "3" => InstallerAction.Status,
                "4" => InstallerAction.CollectLogs,
                "0" => InstallerAction.Menu,
                _ => InstallerAction.Help
            };
            if (choice?.Trim() == "0" || choice is null)
                return 0;
            if (action == InstallerAction.Help)
            {
                Console.WriteLine("请选择 0-4 / Choose 0-4.");
                continue;
            }

            InstallerOptions run = new()
            {
                Action = action,
                GamePath = baseOptions.GamePath,
                PackageRoot = baseOptions.PackageRoot,
                OutputPath = baseOptions.OutputPath,
                NonInteractive = false,
                Pause = false
            };
            _ = RunOnce(run);
            Console.WriteLine();
            Console.WriteLine("按 Enter 返回菜单 / Press Enter to return to the menu...");
            Console.ReadLine();
        }
    }

    private static int RunOnce(InstallerOptions options)
    {
        try
        {
            PlatformEnvironment platform = new();
            string packageRoot = PackageLayout.FindPackageRoot(options.PackageRoot);
            PackageLayout package = PackageLayout.LoadAndValidate(packageRoot);
            string? gameDir;
            try
            {
                gameDir = platform.DiscoverGameDirectory(packageRoot, options.GamePath);
                if (gameDir is null && !options.NonInteractive && !Console.IsInputRedirected)
                {
                    Console.WriteLine("未自动找到游戏。请输入包含 DolocTown.exe 的游戏目录：");
                    Console.WriteLine("Game not found automatically. Enter the folder containing DolocTown.exe:");
                    Console.Write("> ");
                    string? manual = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(manual))
                        gameDir = platform.ValidateGameDirectory(manual.Trim().Trim('"'), "manual path");
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                throw new InstallerException(InstallerCodes.GamePath, 2,
                    "指定的游戏目录无效；不会回退到机器上的其他安装位置。",
                    "The explicit game directory is invalid; the installer will not fall through to another installation on this machine.",
                    ex.Message, ex);
            }
            if (gameDir is null)
                throw new InstallerException(InstallerCodes.GamePath, 2,
                    "找不到《多洛可小镇》目录。请使用 --game-path 指定包含 DolocTown.exe 的目录。",
                    "The Doloc Town directory was not found. Use --game-path with the folder containing DolocTown.exe.");

            InstallerResult result = options.Action switch
            {
                InstallerAction.Install => InstallAction.Run(package, gameDir, platform),
                InstallerAction.Uninstall => UninstallAction.Run(package, gameDir, platform),
                InstallerAction.Status => RunStatus(package, gameDir, platform),
                InstallerAction.CollectLogs => CollectLogsAction.Run(package, gameDir, platform, options.OutputPath),
                _ => InstallerResult.Failure(InstallerCodes.Unexpected, 2,
                    "未选择有效操作。", "No valid action was selected.")
            };
            PrintResult(result);
            return result.ExitCode;
        }
        catch (InstallerException ex)
        {
            InstallerResult result = InstallerResult.Failure(ex.Code, ex.ExitCode, ex.Chinese, ex.English,
                string.IsNullOrWhiteSpace(ex.Detail) ? ex.Message : ex.Detail);
            PrintResult(result);
            return result.ExitCode;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            InstallerResult result = InstallerResult.Failure(InstallerCodes.LocalAccess, 3,
                "本地文件访问失败。通常这不是 Windows 防火墙问题。",
                "A local file operation failed. This is usually not a Windows Firewall problem.", ex.ToString());
            PrintResult(result);
            return result.ExitCode;
        }
        catch (Exception ex)
        {
            InstallerResult result = InstallerResult.Failure(InstallerCodes.Unexpected, 10,
                "安装器发生未预期错误；没有证据时不会宣称安装成功。",
                "The installer hit an unexpected error and will not claim success without evidence.", ex.ToString());
            PrintResult(result);
            return result.ExitCode;
        }
    }

    private static InstallerResult RunStatus(PackageLayout package, string gameDir, PlatformEnvironment platform)
    {
        StatusReport report = StatusAction.Evaluate(package, gameDir, platform);
        string formatted = StatusAction.Format(report);
        if (report.Files == FileHealth.Healthy)
            return InstallerResult.Success(InstallerCodes.Healthy,
                "Runtime 文件健康；启动接入与最近运行记录请看下方独立状态。",
                "Runtime files are healthy; see the separate launch-integration and runtime-observation states below.", formatted);
        return InstallerResult.Failure(InstallerCodes.Unhealthy, 1,
            "Runtime 文件尚未达到健康状态。状态检查没有修改任何文件。",
            "Runtime files are not healthy. Status did not modify any files.", formatted);
    }

    private static InstallerOptions ParseOptions(string[] args)
    {
        InstallerOptions options = new();
        int index = 0;
        if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            options.Action = ParseAction(args[0]);
            index = 1;
        }

        while (index < args.Length)
        {
            string argument = args[index++];
            switch (argument)
            {
                case "--game-path":
                    options.GamePath = RequireValue(args, ref index, argument);
                    break;
                case "--package-root":
                    options.PackageRoot = RequireValue(args, ref index, argument);
                    break;
                case "--output":
                    options.OutputPath = RequireValue(args, ref index, argument);
                    break;
                case "--non-interactive":
                    options.NonInteractive = true;
                    break;
                case "--pause":
                    options.Pause = true;
                    break;
                case "--help":
                case "-h":
                    options.Action = InstallerAction.Help;
                    break;
                default:
                    throw new ArgumentException("Unknown argument: " + argument);
            }
        }
        return options;
    }

    private static string RequireValue(string[] args, ref int index, string argument)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            throw new ArgumentException("Missing value for " + argument);
        return args[index++];
    }

    private static InstallerAction ParseAction(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "install" => InstallerAction.Install,
            "uninstall" => InstallerAction.Uninstall,
            "status" or "check" => InstallerAction.Status,
            "collect-logs" or "collect" or "logs" => InstallerAction.CollectLogs,
            "help" => InstallerAction.Help,
            _ => throw new ArgumentException("Unknown action: " + value)
        };
    }

    private static void PrintResult(InstallerResult result)
    {
        Console.WriteLine();
        Console.WriteLine("[" + result.Code + "] " + result.Chinese);
        Console.WriteLine(result.English);
        foreach (string detail in result.Details)
        {
            if (!string.IsNullOrWhiteSpace(detail))
                Console.WriteLine(detail);
        }
        Console.WriteLine("ExitCode=" + result.ExitCode);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("DTMAPI-MultiPlatform-Installer [install|uninstall|status|collect-logs] [options]");
        Console.WriteLine("  --game-path <path>       Folder containing DolocTown.exe");
        Console.WriteLine("  --package-root <path>    Complete Workshop package root");
        Console.WriteLine("  --output <directory>     Parent directory for collected logs");
        Console.WriteLine("  --non-interactive        Never prompt for a path");
        Console.WriteLine("  --pause                  Wait for Enter before closing");
        Console.WriteLine();
        Console.WriteLine("Steam Deck / Linux required launch option:");
        Console.WriteLine(PlatformEnvironment.RequiredLinuxLaunchOption);
        Console.WriteLine("CrossOver: use the existing Steam/game Bottle and set winhttp to Native, then Builtin.");
    }
}
