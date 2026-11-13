using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Text.Json;
using System.Windows.Forms;

internal static class ToolUiChecks
{
    [STAThread]
    private static void Main(string[] args)
    {
        string input = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        Application.SetHighDpiMode(HighDpiMode.SystemAware); Application.EnableVisualStyles();
        using var form = new PrepareForm();
        form.ShowInTaskbar = false; form.Opacity = 0; form.Show(); Application.DoEvents();
        T Field<T>(string name) => (T)form.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
        var source = Field<TextBox>("source"); var destination = Field<TextBox>("destination"); var use = Field<ComboBox>("purpose");
        var prepare = Field<Button>("prepare"); var cancel = Field<Button>("cancel"); var status = Field<TextBox>("status");
        var volume = Field<TextBox>("volume"); var preset = Field<ComboBox>("preset");
        void Wait()
        {
            var clock = Stopwatch.StartNew();
            do { Application.DoEvents(); Thread.Sleep(5); if (clock.ElapsedMilliseconds > 30000) throw new Exception("UI timeout: " + status.Text); } while (!prepare.Enabled);
            Application.DoEvents();
        }
        void Check(bool value, string description) { if (!value) throw new Exception(description + ": " + status.Text); Console.WriteLine("PASS " + description); }
        Check(volume.Text == "-23", "Actor starts with the editable daily suggestion");
        use.SelectedIndex = 3; Check(volume.Text == "-29", "Unedited suggestion follows the selected route");
        volume.Text = "-17.3"; use.SelectedIndex = 0;
        Check(volume.Text == "-17.3" && preset.SelectedIndex == 3, "Switching route preserves a custom author value");
        foreach (int purpose in new[] { 0, 3 })
        {
            source.Text = input; use.SelectedIndex = purpose; destination.Text = Path.Combine(output, purpose == 0 ? "short" : "stream");
            prepare.PerformClick(); Check(!prepare.Enabled && cancel.Enabled, "UI owns the active operation and offers cancellation"); Wait();
            Check(Directory.GetFiles(destination.Text, "*.bnk", SearchOption.AllDirectories).Length == 1 &&
                Directory.GetFiles(destination.Text, "*.wem", SearchOption.AllDirectories).Length == (purpose == 3 ? 1 : 0), "UI wrote the selected kind of media");
            string declarationPath = Path.Combine(destination.Text, "PackData", "content.json");
            string declarationText = File.ReadAllText(declarationPath);
            using var declaration = JsonDocument.Parse(declarationText);
            var audio = declaration.RootElement.GetProperty("audio");
            var resource = audio.GetProperty("resources")[0];
            var audioEvent = audio.GetProperty("events")[0];
            Check(declaration.RootElement.GetProperty("schemaVersion").GetInt32() == 1 &&
                Math.Abs(audioEvent.GetProperty("volumeDb").GetDouble() + 17.3) < .001,
                "Both output kinds preserve the one author dB field");
            Check(File.Exists(Path.Combine(destination.Text, resource.GetProperty("path").GetString()!)) &&
                resource.GetProperty("stream").GetBoolean() == (purpose == 3) &&
                audioEvent.GetProperty("resource").GetString() == resource.GetProperty("key").GetString() &&
                audioEvent.GetProperty("use").GetString() == (purpose == 3 ? "music" : "effect"),
                "Declaration points to generated media with the selected use");
            int fileCount = Directory.GetFiles(destination.Text, "*", SearchOption.AllDirectories).Length;
            prepare.PerformClick();
            Check(prepare.Enabled && !cancel.Enabled &&
                File.ReadAllText(declarationPath) == declarationText &&
                Directory.GetFiles(destination.Text, "*", SearchOption.AllDirectories).Length == fileCount,
                "An existing declaration is preserved without starting another operation");
        }
        destination.Text = Path.Combine(output, "invalid-volume");
        volume.Text = "not-a-volume"; prepare.PerformClick();
        Check(prepare.Enabled && !cancel.Enabled && !Directory.Exists(destination.Text), "Invalid numeric input is rejected before creating output");
        volume.Text = "静音";
        source.Text = input; destination.Text = Path.Combine(output, "mute"); prepare.PerformClick(); Wait();
        using (var mute = JsonDocument.Parse(File.ReadAllText(Path.Combine(destination.Text, "PackData", "content.json"))))
            Check(mute.RootElement.GetProperty("audio").GetProperty("events")[0].GetProperty("volumeDb").GetString() == "-inf", "Readable mute produces exact JSON silence");
        string broken = Path.Combine(output, "broken.mp3"); File.WriteAllText(broken, "invalid audio");
        source.Text = broken; destination.Text = Path.Combine(output, "error"); prepare.PerformClick(); Wait();
        Check(!cancel.Enabled && status.Text.Length > 0 &&
            !Directory.GetFiles(destination.Text, "*", SearchOption.AllDirectories).Any(), "Decoder failure releases the UI and removes unfinished output");
        source.Text = input; destination.Text = Path.Combine(output, "cancel"); prepare.PerformClick(); cancel.PerformClick(); Wait();
        Check(!cancel.Enabled && !File.Exists(Path.Combine(destination.Text, "PackData", "content.json")) &&
            !Directory.GetFiles(destination.Text, "*", SearchOption.AllDirectories).Any(), "UI cancellation removes unfinished output");
        form.Close(); Console.WriteLine("Windows Forms UI checks passed: short, stream, error, cancellation; no game dependency.");
    }
}
