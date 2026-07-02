using System.Text.Encodings.Web;
using System.Text.Json;

namespace CodeCheck.Cli.Services;

public sealed class CliProgressReporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public void Write(string type, object payload)
    {
        var json = JsonSerializer.Serialize(new { type, time = DateTime.Now, data = payload }, Options);
        Console.WriteLine(json);
    }
}
