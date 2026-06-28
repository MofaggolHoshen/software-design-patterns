// ============================================================
// Template Method Pattern — C# Example
// ============================================================
//
// Intent: Define the skeleton of an algorithm in a base class and
// defer specific steps to subclasses.
//
// Key roles:
//   ReportGenerator    — Abstract class with template method
//   CsvReportGenerator — Concrete subclass
//   HtmlReportGenerator — Concrete subclass
//   MarkdownReportGenerator — Concrete subclass
// ============================================================

// ── Abstract class with template method ─────────────────
abstract class ReportGenerator
{
    // Template method — the invariant skeleton; sealed so subclasses cannot reorder steps
    public sealed string Generate(IEnumerable<string[]> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(WriteHeader());
        foreach (var row in rows)
            sb.AppendLine(WriteRow(row));
        var footer = WriteFooter();
        if (!string.IsNullOrEmpty(footer))
            sb.AppendLine(footer);
        return sb.ToString();
    }

    // Steps that subclasses must implement
    protected abstract string WriteHeader();
    protected abstract string WriteRow(string[] columns);
    protected abstract string WriteFooter();

    // Hook — optional override with default no-op
    protected virtual string WritePrelude() => "";
}

// ── Concrete Subclasses ──────────────────────────────────
class CsvReportGenerator : ReportGenerator
{
    protected override string WriteHeader() => "id,name,value";
    protected override string WriteRow(string[] cols) => string.Join(",", cols);
    protected override string WriteFooter() => "--- end of report ---";
}

class HtmlReportGenerator : ReportGenerator
{
    protected override string WriteHeader() =>
        "<table>\n  <tr><th>id</th><th>name</th><th>value</th></tr>";
    protected override string WriteRow(string[] cols) =>
        $"  <tr>{string.Join("", cols.Select(c => $"<td>{c}</td>"))}</tr>";
    protected override string WriteFooter() => "</table>";
}

class MarkdownReportGenerator : ReportGenerator
{
    protected override string WriteHeader()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("| id | name | value |");
        sb.Append("|-----|------|-------|");
        return sb.ToString();
    }
    protected override string WriteRow(string[] cols) => $"| {string.Join(" | ", cols)} |";
    protected override string WriteFooter() => "";
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Template Method Pattern ===\n");

var rows = new[]
{
    new[] { "1", "Alice", "1000" },
    new[] { "2", "Bob",   "2500" },
    new[] { "3", "Carol", "750"  },
};

var generators = new ReportGenerator[]
{
    new CsvReportGenerator(),
    new HtmlReportGenerator(),
    new MarkdownReportGenerator(),
};

foreach (var gen in generators)
{
    Console.WriteLine($"--- {gen.GetType().Name} ---");
    Console.WriteLine(gen.Generate(rows));
}
