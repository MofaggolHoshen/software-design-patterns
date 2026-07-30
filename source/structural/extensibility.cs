// ============================================================
// Extensibility (Plug-in) Pattern — C# Example
// ============================================================
//
// Intent: Enable an application's behaviour to be extended
// without modifying its core code.
//
// Key roles:
//   IExportPlugin     — Extension point interface
//   ReportExporter    — Core application (never modified)
//   PdfExportPlugin, CsvExportPlugin, etc. — Plug-ins
// ============================================================

// ── Extension point ───────────────────────────────────────
interface IExportPlugin
{
    string FormatName { get; }
    void Export(string reportTitle, IEnumerable<string[]> rows);
}

// ── Core — open for extension, closed for modification ────
class ReportExporter
{
    private readonly Dictionary<string, IExportPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IExportPlugin plugin)
    {
        _plugins[plugin.FormatName] = plugin;
        Console.WriteLine($"  [Registry] Registered plugin: {plugin.FormatName}");
    }

    public IEnumerable<string> SupportedFormats => _plugins.Keys.OrderBy(k => k);

    public void Export(string format, string title, IEnumerable<string[]> rows)
    {
        if (_plugins.TryGetValue(format, out var plugin))
            plugin.Export(title, rows);
        else
            Console.WriteLine($"  [Error]    No plugin for '{format}'. Supported: {string.Join(", ", SupportedFormats)}");
    }
}

// ── Built-in plug-ins ─────────────────────────────────────
class CsvExportPlugin : IExportPlugin
{
    public string FormatName => "csv";
    public void Export(string title, IEnumerable<string[]> rows)
    {
        Console.WriteLine($"  [CSV]    {title}");
        foreach (var row in rows) Console.WriteLine($"  {string.Join(",", row)}");
    }
}

class HtmlExportPlugin : IExportPlugin
{
    public string FormatName => "html";
    public void Export(string title, IEnumerable<string[]> rows)
    {
        Console.WriteLine($"  [HTML]   <h1>{title}</h1><table>");
        foreach (var row in rows)
            Console.WriteLine($"    <tr>{string.Join("", row.Select(c => $"<td>{c}</td>"))}</tr>");
        Console.WriteLine("[HTML]  </table>");
    }
}

// ── New plug-ins — no changes to ReportExporter ──────────
class MarkdownExportPlugin : IExportPlugin
{
    public string FormatName => "markdown";
    public void Export(string title, IEnumerable<string[]> rows)
    {
        Console.WriteLine($"  [MD]     ## {title}");
        foreach (var row in rows)
            Console.WriteLine($"  | {string.Join(" | ", row)} |");
    }
}

class JsonExportPlugin : IExportPlugin
{
    public string FormatName => "json";
    public void Export(string title, IEnumerable<string[]> rows)
    {
        Console.WriteLine($"  [JSON]   {{ \"title\": \"{title}\", \"rows\": [");
        foreach (var row in rows)
            Console.WriteLine($"    [{string.Join(", ", row.Select(c => $"\"{c}\""))}]");
        Console.WriteLine("  ] }");
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Extensibility (Plug-in) Pattern ===\n");

var exporter = new ReportExporter();

// Register built-in plug-ins
exporter.Register(new CsvExportPlugin());
exporter.Register(new HtmlExportPlugin());

// Register new plug-ins at runtime — ReportExporter core never changes
exporter.Register(new MarkdownExportPlugin());
exporter.Register(new JsonExportPlugin());

Console.WriteLine($"\nSupported formats: {string.Join(", ", exporter.SupportedFormats)}\n");

var rows = new[] { new[] { "1", "Alice", "100" }, new[] { "2", "Bob", "200" } };
string title = "Q1 Sales Report";

foreach (var format in exporter.SupportedFormats)
{
    Console.WriteLine($"--- {format.ToUpper()} ---");
    exporter.Export(format, title, rows);
    Console.WriteLine();
}

// Unknown format handled gracefully
exporter.Export("xml", title, rows);
