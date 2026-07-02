using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace CodeCheck.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Process? _scanProcess;
    private List<IssueListItem> _allIssueItems = [];
    private readonly DesktopSettingsService _settingsService = new();

    private readonly DispatcherTimer _statusTimer = new()
    {
        Interval = TimeSpan.FromSeconds(1)
    };

    public MainWindow()
    {
        InitializeComponent();
        WorkspaceDirectoryTextBox.Text = FindWorkspaceDirectory();
        CliPathTextBox.Text = FindDefaultCliPath(WorkspaceDirectoryTextBox.Text);
        Loaded += async (_, _) => await LoadSettingsAsync();
        Closing += async (_, _) => await SaveSettingsAsync();
        _statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
        _statusTimer.Start();
    }

    private async void StartScanButton_Click(object sender, RoutedEventArgs e)
    {
        if (_scanProcess is { HasExited: false })
        {
            AppendMessage("Scan process is already running.");
            return;
        }

        var workspaceDirectory = GetWorkspaceDirectory();
        var cliPath = ResolvePath(CliPathTextBox.Text);
        if (!File.Exists(cliPath))
        {
            cliPath = FindDefaultCliPath(workspaceDirectory);
            CliPathTextBox.Text = cliPath;
        }

        var configPath = ResolvePath(ConfigFileTextBox.Text);
        if (!File.Exists(cliPath))
        {
            AppendMessage($"CLI not found: {cliPath}");
            return;
        }

        if (!File.Exists(configPath))
        {
            AppendMessage($"Config not found: {configPath}");
            return;
        }

        ClearControlButton_Click(sender, e);
        await SaveSettingsAsync();
        MessageTextBox.Clear();
        StartScanButton.IsEnabled = false;
        AppendMessage($"Starting scan: {cliPath} scan --config {configPath}");

        try
        {
            var exitCode = await RunCliAsync(cliPath, workspaceDirectory, "scan", configPath);
            AppendMessage($"Scan process exited: {exitCode}");
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            AppendMessage($"Failed to start scan: {ex.Message}");
        }
        finally
        {
            StartScanButton.IsEnabled = true;
        }
    }

    private async void ValidateConfigButton_Click(object sender, RoutedEventArgs e)
    {
        if (_scanProcess is { HasExited: false })
        {
            AppendMessage("Scan process is running. Stop or wait for it before validating.");
            return;
        }

        var workspaceDirectory = GetWorkspaceDirectory();
        var cliPath = ResolvePath(CliPathTextBox.Text);
        if (!File.Exists(cliPath))
        {
            cliPath = FindDefaultCliPath(workspaceDirectory);
            CliPathTextBox.Text = cliPath;
        }

        var configPath = ResolvePath(ConfigFileTextBox.Text);
        if (!File.Exists(cliPath))
        {
            AppendMessage($"CLI not found: {cliPath}");
            return;
        }

        if (!File.Exists(configPath))
        {
            AppendMessage($"Config not found: {configPath}");
            return;
        }

        MessageTextBox.Clear();
        await SaveSettingsAsync();
        ValidateConfigButton.IsEnabled = false;
        AppendMessage($"Validating config: {cliPath} validate --config {configPath}");

        try
        {
            var exitCode = await RunCliAsync(cliPath, workspaceDirectory, "validate", configPath);
            AppendMessage($"Validate process exited: {exitCode}");
        }
        catch (Exception ex)
        {
            AppendMessage($"Failed to validate config: {ex.Message}");
        }
        finally
        {
            ValidateConfigButton.IsEnabled = true;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshStatusAsync();
    }

    private void StopProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (_scanProcess is null)
        {
            AppendMessage("No CLI process has been started.");
            return;
        }

        if (_scanProcess.HasExited)
        {
            AppendMessage($"CLI process already exited: {_scanProcess.ExitCode}");
            return;
        }

        try
        {
            _scanProcess.Kill(entireProcessTree: true);
            AppendMessage("CLI process was stopped.");
        }
        catch (Exception ex)
        {
            AppendMessage($"Failed to stop CLI process: {ex.Message}");
        }
    }

    private void BrowseWorkspaceButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select CodeCheck workspace directory",
            SelectedPath = Directory.Exists(WorkspaceDirectoryTextBox.Text) ? WorkspaceDirectoryTextBox.Text : FindWorkspaceDirectory(),
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        WorkspaceDirectoryTextBox.Text = dialog.SelectedPath;
        var cliPath = FindDefaultCliPath(dialog.SelectedPath);
        if (File.Exists(cliPath))
        {
            CliPathTextBox.Text = cliPath;
        }

        _ = SaveSettingsAsync();
    }

    private void BrowseCliButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select CodeCheck.Cli.exe",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(GetWorkspaceDirectory()) ? GetWorkspaceDirectory() : Environment.CurrentDirectory,
            FileName = "CodeCheck.Cli.exe"
        };

        if (dialog.ShowDialog(this) == true)
        {
            CliPathTextBox.Text = dialog.FileName;
            _ = SaveSettingsAsync();
        }
    }

    private void BrowseConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var workspaceDirectory = GetWorkspaceDirectory();
        var configDirectory = Path.Combine(workspaceDirectory, "configs");
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select CodeCheck config file",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(configDirectory) ? configDirectory : workspaceDirectory,
            FileName = "default-codecheck.json"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ConfigFileTextBox.Text = dialog.FileName;
            _ = SaveSettingsAsync();
        }
    }

    private async void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        await WriteControlCommandAsync("pause");
    }

    private async void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        await WriteControlCommandAsync("resume");
    }

    private async void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        await WriteControlCommandAsync("cancel");
    }

    private void OpenReportButton_Click(object sender, RoutedEventArgs e)
    {
        var reportPath = ResolveReportPath();
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            AppendMessage("Report path is empty.");
            return;
        }

        var htmlPath = Path.ChangeExtension(reportPath, ".html");
        var pathToOpen = File.Exists(htmlPath) ? htmlPath : reportPath;
        OpenPath(pathToOpen);
    }

    private void OpenReportFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var reportPath = ResolveReportPath();
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            AppendMessage("Report path is empty.");
            return;
        }

        var directory = Directory.Exists(reportPath) ? reportPath : Path.GetDirectoryName(reportPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            AppendMessage($"Report folder not found: {reportPath}");
            return;
        }

        OpenPath(directory);
    }

    private void OpenStatusFileButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileOrDirectoryFallback(ResolvePath(StatusFileTextBox.Text), "Status file");
    }

    private void OpenControlFileButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileOrDirectoryFallback(ResolvePath(ControlFileTextBox.Text), "Control file");
    }

    private void IssuesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (IssuesDataGrid.SelectedItem is not IssueListItem issue)
        {
            return;
        }

        var filePath = ResolvePath(issue.File);
        if (!File.Exists(filePath))
        {
            AppendMessage($"Issue file not found: {filePath}");
            return;
        }

        OpenPath(filePath);
        AppendMessage($"Opened issue file: {filePath}:{issue.Line}");
    }

    private void IssueFilter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyIssueFilter();
    }

    private void ClearIssueFilterButton_Click(object sender, RoutedEventArgs e)
    {
        SeverityFilterComboBox.SelectedIndex = 0;
        IssueKeywordTextBox.Clear();
        ApplyIssueFilter();
    }

    private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
    {
        MessageTextBox.Clear();
    }

    private void ClearControlButton_Click(object sender, RoutedEventArgs e)
    {
        var controlFile = ResolvePath(ControlFileTextBox.Text);
        if (File.Exists(controlFile))
        {
            File.Delete(controlFile);
            AppendMessage($"Control file deleted: {controlFile}");
        }
    }

    private async Task RefreshStatusAsync()
    {
        var statusFile = ResolvePath(StatusFileTextBox.Text);
        if (!File.Exists(statusFile))
        {
            StatusTextBlock.Text = "NotStarted";
            PhaseTextBlock.Text = string.Empty;
            EngineTextBlock.Text = string.Empty;
            UpdatedAtTextBlock.Text = string.Empty;
            TotalFilesTextBlock.Text = "0";
            IssueStatsTextBlock.Text = "0 / 0";
            ReportPathTextBlock.Text = string.Empty;
            ClearReportSummary();
            return;
        }

        try
        {
            await using var stream = File.OpenRead(statusFile);
            var status = await JsonSerializer.DeserializeAsync<ScanStatusViewModel>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (status is null)
            {
                return;
            }

            StatusTextBlock.Text = status.Status;
            PhaseTextBlock.Text = status.Phase;
            EngineTextBlock.Text = status.Engine;
            UpdatedAtTextBlock.Text = status.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            TotalFilesTextBlock.Text = status.TotalFiles.ToString();
            IssueStatsTextBlock.Text = $"{status.TotalIssues} / {status.FailedFiles}";
            ReportPathTextBlock.Text = status.ReportPath;
            MessageTextBox.Text = status.Message;
            await RefreshReportSummaryAsync(status.ReportPath);
        }
        catch (IOException)
        {
        }
        catch (JsonException ex)
        {
            AppendMessage($"Failed to parse status file: {ex.Message}");
        }
    }

    private async Task WriteControlCommandAsync(string command)
    {
        await SaveSettingsAsync();
        var controlFile = ResolvePath(ControlFileTextBox.Text);
        var directory = Path.GetDirectoryName(controlFile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(controlFile, JsonSerializer.Serialize(new { command }, new JsonSerializerOptions { WriteIndented = true }));
        AppendMessage($"Control command written: {command}");
    }

    private string ResolvePath(string path)
    {
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(GetWorkspaceDirectory(), path));
    }

    private string GetWorkspaceDirectory()
    {
        return string.IsNullOrWhiteSpace(WorkspaceDirectoryTextBox.Text)
            ? FindWorkspaceDirectory()
            : Path.GetFullPath(WorkspaceDirectoryTextBox.Text);
    }

    private static string FindWorkspaceDirectory()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CodeCheck.sln")) || Directory.Exists(Path.Combine(directory.FullName, "configs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private static string FindDefaultCliPath(string workspaceDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(workspaceDirectory, "CodeCheck.Cli.exe"),
            Path.Combine(workspaceDirectory, "release", "CodeCheck.Cli.exe"),
            Path.Combine(workspaceDirectory, "src", "CodeCheck.Cli", "bin", "Debug", "net8.0", "CodeCheck.Cli.exe"),
            Path.Combine(workspaceDirectory, "src", "CodeCheck.Cli", "bin", "Release", "net8.0", "CodeCheck.Cli.exe")
        };

        return candidates.FirstOrDefault(File.Exists) ?? "CodeCheck.Cli.exe";
    }

    private string ResolveReportPath()
    {
        return string.IsNullOrWhiteSpace(ReportPathTextBlock.Text)
            ? string.Empty
            : ResolvePath(ReportPathTextBlock.Text);
    }

    private void OpenPath(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            AppendMessage($"Path not found: {path}");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AppendMessage($"Failed to open path: {ex.Message}");
        }
    }

    private void OpenFileOrDirectoryFallback(string filePath, string label)
    {
        if (File.Exists(filePath))
        {
            OpenPath(filePath);
            return;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            AppendMessage($"{label} not found, opening folder: {directory}");
            OpenPath(directory);
            return;
        }

        AppendMessage($"{label} not found: {filePath}");
    }

    private async Task RefreshReportSummaryAsync(string reportPath)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            ClearReportSummary();
            return;
        }

        var resolvedReportPath = ResolvePath(reportPath);
        if (!File.Exists(resolvedReportPath))
        {
            ClearReportSummary();
            return;
        }

        try
        {
            await using var stream = File.OpenRead(resolvedReportPath);
            using var document = await JsonDocument.ParseAsync(stream);
            var root = document.RootElement;
            var qualityScore = root.GetProperty("qualityScore");
            var summary = root.GetProperty("summary");

            ScoreTextBlock.Text = qualityScore.GetProperty("score").ToString();
            LevelTextBlock.Text = qualityScore.GetProperty("level").GetString() ?? string.Empty;
            ActiveIssuesTextBlock.Text = summary.GetProperty("activeIssues").ToString();
            NewIssuesTextBlock.Text = summary.GetProperty("newIssueCount").ToString();
            ExistingIssuesTextBlock.Text = summary.GetProperty("existingIssueCount").ToString();
            FixedIssuesTextBlock.Text = summary.GetProperty("fixedIssueCount").ToString();
            _allIssueItems = root.GetProperty("issues")
                .EnumerateArray()
                .Take(100)
                .Select(issue => new IssueListItem
                {
                    Severity = GetString(issue, "severity"),
                    RuleId = GetString(issue, "ruleId"),
                    File = GetString(issue, "file"),
                    Line = issue.TryGetProperty("line", out var line) ? line.GetInt32() : 0,
                    Message = GetString(issue, "message")
                })
                .ToList();
            ApplyIssueFilter();
        }
        catch (IOException)
        {
        }
        catch (JsonException ex)
        {
            AppendMessage($"Failed to parse report summary: {ex.Message}");
        }
        catch (KeyNotFoundException)
        {
            ClearReportSummary();
        }
    }

    private void ClearReportSummary()
    {
        ScoreTextBlock.Text = string.Empty;
        LevelTextBlock.Text = string.Empty;
        ActiveIssuesTextBlock.Text = string.Empty;
        NewIssuesTextBlock.Text = string.Empty;
        ExistingIssuesTextBlock.Text = string.Empty;
        FixedIssuesTextBlock.Text = string.Empty;
        _allIssueItems = [];
        IssuesDataGrid.ItemsSource = null;
    }

    private void ApplyIssueFilter()
    {
        var severity = (SeverityFilterComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "All";
        var keyword = IssueKeywordTextBox.Text.Trim();
        IEnumerable<IssueListItem> issues = _allIssueItems;

        if (!string.Equals(severity, "All", StringComparison.OrdinalIgnoreCase))
        {
            issues = issues.Where(issue => string.Equals(issue.Severity, severity, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            issues = issues.Where(issue =>
                issue.RuleId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                issue.File.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                issue.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        IssuesDataGrid.ItemsSource = issues.ToList();
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? property.GetString() ?? string.Empty : string.Empty;
    }

    private void AppendMessage(string message)
    {
        MessageTextBox.Text = string.IsNullOrWhiteSpace(MessageTextBox.Text)
            ? message
            : MessageTextBox.Text + Environment.NewLine + message;
        MessageTextBox.ScrollToEnd();
    }

    private void AppendProcessOutput(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Dispatcher.Invoke(() => AppendMessage(message));
    }

    private async Task<int> RunCliAsync(string cliPath, string workspaceDirectory, string command, string configPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = cliPath,
            WorkingDirectory = workspaceDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(command);
        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add(configPath);

        _scanProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _scanProcess.OutputDataReceived += (_, args) => AppendProcessOutput(args.Data);
        _scanProcess.ErrorDataReceived += (_, args) => AppendProcessOutput(args.Data);

        _scanProcess.Start();
        _scanProcess.BeginOutputReadLine();
        _scanProcess.BeginErrorReadLine();
        await _scanProcess.WaitForExitAsync();
        return _scanProcess.ExitCode;
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync();
        if (!string.IsNullOrWhiteSpace(settings.WorkspaceDirectory))
        {
            WorkspaceDirectoryTextBox.Text = settings.WorkspaceDirectory;
        }

        if (!string.IsNullOrWhiteSpace(settings.CliPath))
        {
            CliPathTextBox.Text = settings.CliPath;
        }
        else
        {
            CliPathTextBox.Text = FindDefaultCliPath(GetWorkspaceDirectory());
        }

        if (!string.IsNullOrWhiteSpace(settings.ConfigFile))
        {
            ConfigFileTextBox.Text = settings.ConfigFile;
        }

        if (!string.IsNullOrWhiteSpace(settings.StatusFile))
        {
            StatusFileTextBox.Text = settings.StatusFile;
        }

        if (!string.IsNullOrWhiteSpace(settings.ControlFile))
        {
            ControlFileTextBox.Text = settings.ControlFile;
        }
    }

    private async Task SaveSettingsAsync()
    {
        await _settingsService.SaveAsync(new DesktopSettings
        {
            WorkspaceDirectory = WorkspaceDirectoryTextBox.Text,
            CliPath = CliPathTextBox.Text,
            ConfigFile = ConfigFileTextBox.Text,
            StatusFile = StatusFileTextBox.Text,
            ControlFile = ControlFileTextBox.Text
        });
    }

    private sealed class ScanStatusViewModel
    {
        public string Status { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public string Engine { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int TotalIssues { get; set; }
        public int FailedFiles { get; set; }
        public string ReportPath { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class IssueListItem
    {
        public string Severity { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public int Line { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}