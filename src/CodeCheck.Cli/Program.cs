using CodeCheck.Cli.Services;

const string Version = "1.0.0";

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "--help";
var orchestrator = new ScanOrchestrator();
var exportService = new ExportService();

return command switch
{
    "--version" or "version" => PrintVersion(),
    "validate" => await orchestrator.ValidateAsync(GetOption(args, "--config") ?? "configs/default-codecheck.json", CancellationToken.None),
    "scan" => await orchestrator.ScanAsync(GetOption(args, "--config") ?? "configs/default-codecheck.json", CancellationToken.None),
    "export" when IsExportFormat(args, "csv") => await exportService.ExportCsvAsync(GetOption(args, "--report") ?? string.Empty, GetOption(args, "--output"), CancellationToken.None),
    "export" when IsExportFormat(args, "html") => await exportService.ExportHtmlAsync(GetOption(args, "--report") ?? string.Empty, GetOption(args, "--output"), CancellationToken.None),
    "export" when IsExportFormat(args, "sarif") => await exportService.ExportSarifAsync(GetOption(args, "--report") ?? string.Empty, GetOption(args, "--output"), CancellationToken.None),
    _ => PrintHelp()
};

static int PrintVersion()
{
    Console.WriteLine($"CodeCheck.Cli {Version}");
    return 0;
}

static int PrintHelp()
{
    Console.WriteLine("CodeCheck.Cli");
    Console.WriteLine("用法：");
    Console.WriteLine("  CodeCheck.Cli --version");
    Console.WriteLine("  CodeCheck.Cli validate --config configs/default-codecheck.json");
    Console.WriteLine("  CodeCheck.Cli scan --config configs/default-codecheck.json");
    Console.WriteLine("  CodeCheck.Cli export csv --report reports/demo/report.json [--output reports/demo/report.csv]");
    Console.WriteLine("  CodeCheck.Cli export html --report reports/demo/report.json [--output reports/demo/report.html]");
    Console.WriteLine("  CodeCheck.Cli export sarif --report reports/demo/report.json [--output reports/demo/report.sarif]");
    return 0;
}

static bool IsExportFormat(string[] args, string format)
{
    if (args.Length > 1 && string.Equals(args[1], format, StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    return string.Equals(GetOption(args, "--format"), format, StringComparison.OrdinalIgnoreCase);
}

static string? GetOption(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}
