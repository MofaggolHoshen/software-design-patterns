# 🔧 Extensibility Pattern

The Extensibility pattern (also known as the **Plug-in** or **Hook** model) enables an application to be **extended with new behaviour without modifying existing source code**. New capabilities are registered as plug-ins, hooks, or handlers that conform to a known interface.

## Intent

> Allow an application's behaviour to be extended at runtime or compile time by registering new implementations — without modifying the application's core code.

## Problem

When an application hard-codes its list of supported operations, adding a new one requires changing core code (violating OCP). In plugin systems, report generators, export formats, or validation rules, the set of operations grows over time and must remain open for extension.

### Bad Example

```csharp
class ReportExporter
{
    public void Export(string format, string data)
    {
        if (format == "pdf")       Console.WriteLine("[PDF]  Exporting: " + data);
        else if (format == "excel") Console.WriteLine("[Excel] Exporting: " + data);
        else if (format == "csv")   Console.WriteLine("[CSV]  Exporting: " + data);
        else throw new NotSupportedException(format);
        // Adding "html" requires editing this method
    }
}
```

### Good Example

```csharp
// ── Extension point (plug-in interface) ───────────────────
interface IExportPlugin
{
    string FormatName { get; }
    void   Export(string data);
}

// ── Core — open for extension, closed for modification ────
class ReportExporter
{
    private readonly Dictionary<string, IExportPlugin> _plugins = new();

    public void Register(IExportPlugin plugin) =>
        _plugins[plugin.FormatName.ToLower()] = plugin;

    public void Export(string format, string data)
    {
        if (_plugins.TryGetValue(format.ToLower(), out var plugin))
            plugin.Export(data);
        else
            Console.WriteLine($"  [Exporter] No plugin registered for '{format}'.");
    }

    public IEnumerable<string> SupportedFormats => _plugins.Keys;
}

// ── Built-in plug-ins ─────────────────────────────────────
class PdfExportPlugin : IExportPlugin
{
    public string FormatName => "pdf";
    public void Export(string data) => Console.WriteLine($"  [PDF]   Exporting: {data}");
}

class CsvExportPlugin : IExportPlugin
{
    public string FormatName => "csv";
    public void Export(string data) => Console.WriteLine($"  [CSV]   Exporting: {data}");
}

// ── New plug-in — no changes to ReportExporter ────────────
class ExcelExportPlugin : IExportPlugin
{
    public string FormatName => "excel";
    public void Export(string data) => Console.WriteLine($"  [Excel] Exporting: {data}");
}

class HtmlExportPlugin : IExportPlugin
{
    public string FormatName => "html";
    public void Export(string data) => Console.WriteLine($"  [HTML]  <pre>{data}</pre>");
}

// ── Demo ──────────────────────────────────────────────────
Console.WriteLine("=== Extensibility (Plug-in) Pattern ===\n");

var exporter = new ReportExporter();
exporter.Register(new PdfExportPlugin());
exporter.Register(new CsvExportPlugin());
exporter.Register(new ExcelExportPlugin());
exporter.Register(new HtmlExportPlugin());

Console.WriteLine($"Registered formats: {string.Join(", ", exporter.SupportedFormats)}\n");

exporter.Export("pdf",   "Q1 Sales Report");
exporter.Export("csv",   "Q1 Sales Report");
exporter.Export("excel", "Q1 Sales Report");
exporter.Export("html",  "Q1 Sales Report");
exporter.Export("json",  "Q1 Sales Report");  // no plugin — handled gracefully
```

## Key Takeaways

- Adding a new format = implement `IExportPlugin` and register it — core code never changes.
- Plug-ins are discovered and registered at startup (from DI container, config file, or assembly scan).
- The registry pattern (dictionary keyed by name) is the most common extensibility mechanism.
- ASP.NET Core middleware, EF Core providers, and .NET source generators all use this pattern.

## When to Use

- The set of supported operations or formats grows over time.
- Third parties need to extend your application without accessing or modifying your source code.
- You want a true Open/Closed design at the application level.

## When NOT to Use

- You have a fixed, small set of operations that will never grow — YAGNI applies.
- Loading plug-ins dynamically increases attack surface — verify and sandbox plug-in assemblies.
