using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTMAPI.AudioContent.Preparation;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new PrepareForm());
    }
}
internal sealed class PrepareForm : Form
{
    private readonly TextBox source = new() { Dock = DockStyle.Fill };
    private readonly TextBox destination = new() { Dock = DockStyle.Fill, PlaceholderText = "选一个空文件夹" };
    private readonly ComboBox purpose = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox preset = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 95 };
    private readonly TextBox volume = new() { Text = "-23", Width = 75, AccessibleName = "音量 dB" };
    private bool fillingVolume;
    private readonly Button prepare = new() { Text = "生成", AutoSize = true };
    private readonly Button cancel = new() { Text = "取消", AutoSize = true, Enabled = false };
    private readonly ProgressBar progress = new() { Width = 170, Height = 22, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 0, Visible = false };
    private readonly TextBox status = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private CancellationTokenSource? operation;
    internal PrepareForm()
    {
        Text = "DTMAPI 音频准备工具"; ClientSize = new Size(750, 430); MinimumSize = new Size(680, 430);
        Font = new Font("Microsoft YaHei UI", 10); StartPosition = FormStartPosition.CenterScreen;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 3, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Controls.Add(layout);
        var selectAudio = new Button { Text = "选择…", AutoSize = true };
        selectAudio.Click += (_, _) => { using var dialog = new OpenFileDialog { Filter = "音频文件|*.wav;*.mp3;*.flac;*.aac;*.m4a|所有文件|*.*" }; if (dialog.ShowDialog(this) == DialogResult.OK) source.Text = dialog.FileName; };
        var selectDirectory = new Button { Text = "选择…", AutoSize = true };
        selectDirectory.Click += (_, _) => { using var dialog = new FolderBrowserDialog(); if (dialog.ShowDialog(this) == DialogResult.OK) destination.Text = dialog.SelectedPath; };
        layout.Controls.Add(new Label { Text = "音频文件", AutoSize = true }, 0, 0); layout.Controls.Add(source, 1, 0); layout.Controls.Add(selectAudio, 2, 0);
        purpose.Items.AddRange(new object[] { "操作音效（Actor）", "界面音效（UI）", "环境音（Ambience）", "音乐（Music）" }); purpose.SelectedIndex = 0;
        layout.Controls.Add(new Label { Text = "用途", AutoSize = true }, 0, 1); layout.Controls.Add(purpose, 1, 1);
        layout.Controls.Add(new Label { Text = "输出到", AutoSize = true }, 0, 2); layout.Controls.Add(destination, 1, 2); layout.Controls.Add(selectDirectory, 2, 2);
        preset.Items.AddRange(new object[] { "安静", "日常", "热闹", "自定义" }); preset.SelectedIndex = 1;
        var volumeRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        var down = new Button { Text = "−1 dB", AutoSize = true }; var up = new Button { Text = "+1 dB", AutoSize = true };
        var mute = new Button { Text = "静音", AutoSize = true };
        volumeRow.Controls.AddRange(new Control[] { preset, volume, down, up, mute });
        layout.Controls.Add(new Label { Text = "音量（dB）", AutoSize = true }, 0, 3); layout.Controls.Add(volumeRow, 1, 3); layout.SetColumnSpan(volumeRow, 2);
        void FillSuggestion()
        {
            if (preset.SelectedIndex > 2) return;
            fillingVolume = true;
            volume.Text = AudioPreparer.SuggestedVolumeDb(purpose.SelectedIndex == 3, Channel(), preset.SelectedIndex).ToString(System.Globalization.CultureInfo.InvariantCulture);
            fillingVolume = false;
        }
        preset.SelectedIndexChanged += (_, _) => FillSuggestion(); purpose.SelectedIndexChanged += (_, _) => FillSuggestion();
        volume.TextChanged += (_, _) => { if (!fillingVolume) preset.SelectedIndex = 3; };
        void Step(float amount)
        {
            try { float db = ReadVolume(); volume.Text = (float.IsNegativeInfinity(db) ? 0 : Math.Round(db + amount, 1)).ToString(System.Globalization.CultureInfo.InvariantCulture); }
            catch (ArgumentException error) { status.Text = error.Message; }
        }
        down.Click += (_, _) => Step(-1); up.Click += (_, _) => Step(1); mute.Click += (_, _) => volume.Text = "静音";
        var hint = new Label { Text = "生成后，输出文件夹里有 PackData/content.json 和声音文件，把它们合进你的包。\n三档音量只是试听起点：0 dB 是原音量，负数更小。进游戏和原版声音对比着听，\n再直接改 content.json 里的 volumeDb，不用重新生成。", AutoSize = true, Margin = new Padding(0, 12, 0, 12) };
        layout.Controls.Add(hint, 0, 4); layout.SetColumnSpan(hint, 3);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; actions.Controls.Add(prepare); actions.Controls.Add(cancel); actions.Controls.Add(progress);
        layout.Controls.Add(actions, 0, 5); layout.SetColumnSpan(actions, 3);
        layout.Controls.Add(status, 0, 6); layout.SetColumnSpan(status, 3); for (int row = 0; row < 6; row++) layout.RowStyles.Add(new RowStyle()); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        prepare.Click += async (_, _) => await Prepare(); cancel.Click += (_, _) => operation?.Cancel();
        FormClosing += (_, e) => { if (operation != null) { e.Cancel = true; operation.Cancel(); status.Text = "正在取消，取消完就能关窗口。"; } };
    }
    private AudioPreparationChannel Channel() => purpose.SelectedIndex == 1 ? AudioPreparationChannel.Ui : purpose.SelectedIndex == 2 ? AudioPreparationChannel.Ambience : AudioPreparationChannel.Actor;
    private float ReadVolume() => AudioPreparer.ParseVolumeDb(volume.Text == "静音" ? "-inf" : volume.Text);
    private async Task Prepare()
    {
        if (string.IsNullOrWhiteSpace(source.Text) || string.IsNullOrWhiteSpace(destination.Text)) { status.Text = "先选音频文件和输出文件夹。"; return; }
        float volumeDb;
        try { volumeDb = ReadVolume(); }
        catch (ArgumentException) { status.Text = "音量填数字（单位 dB），静音填 -inf。"; return; }
        string input = source.Text, output = destination.Text; int selected = purpose.SelectedIndex;
        if (File.Exists(Path.Combine(output, "PackData", "content.json"))) { status.Text = "这个文件夹里已经有 PackData/content.json，换一个空文件夹。"; return; }
        using var cancellation = new CancellationTokenSource(); operation = cancellation;
        prepare.Enabled = false; cancel.Enabled = true; source.Enabled = destination.Enabled = purpose.Enabled = volume.Enabled = preset.Enabled = false;
        status.Text = "正在生成…";
        progress.Visible = true; progress.MarqueeAnimationSpeed = 30;
        var stages = new Progress<string>(message => status.Text = message + "…");
        try
        {
            var result = await Task.Run(() => AudioPreparer.Prepare(input, output, selected == 3,
                selected == 1 ? AudioPreparationChannel.Ui : selected == 2 ? AudioPreparationChannel.Ambience : AudioPreparationChannel.Actor, cancellation.Token, ((IProgress<string>)stages).Report, volumeDb));
            status.Text = "生成好了：" + result.DeclarationPath + "\r\n下一步：把 PackData 里的文件复制进你的包，content.json 里的 resources 和 events 合进包里的 content.json，"
                + "再给事件加上 replaceNativeEvent（替换原版音效）或 nativeEvent（接入新音乐）。";
        }
        catch (OperationCanceledException) { status.Text = "已取消，生成到一半的文件已删掉。"; }
        catch (Exception error) { status.Text = "生成失败：" + error.Message; }
        finally { operation = null; progress.MarqueeAnimationSpeed = 0; progress.Visible = false; prepare.Enabled = true; cancel.Enabled = false; source.Enabled = destination.Enabled = purpose.Enabled = volume.Enabled = preset.Enabled = true; }
    }
}
