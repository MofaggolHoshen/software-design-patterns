# 📄 Template Method Pattern

The Template Method pattern defines the **skeleton of an algorithm** in a base class, deferring some steps to subclasses. It lets subclasses redefine certain steps without changing the algorithm's overall structure.

## Intent

> Define the skeleton of an algorithm in an operation, deferring some steps to subclasses. Template Method lets subclasses redefine certain steps of an algorithm without changing the algorithm's structure.

## Problem

When multiple classes share the same overall algorithm structure but differ only in specific steps, duplicating the skeleton across subclasses violates DRY. Any change to the shared structure must be replicated in every class.

### Bad Example

```csharp
class CsvReportGenerator
{
    public string Generate(IEnumerable<string> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("id,name,value");         // header — duplicated
        foreach (var row in rows) sb.AppendLine(row);
        sb.AppendLine("--- end ---");           // footer — duplicated
        return sb.ToString();
    }
}

class HtmlReportGenerator
{
    public string Generate(IEnumerable<string> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<table><tr><th>id</th><th>name</th></tr>");  // header — duplicated structure
        foreach (var row in rows) sb.AppendLine($"<tr><td>{row}</td></tr>");
        sb.AppendLine("</table>");  // footer — duplicated structure
        return sb.ToString();
    }
}
// Adding a "JSON" generator? Copy the skeleton a third time.
```

### Good Example

```csharp
// ── Abstract class with template method ───────────────────
abstract class ReportGenerator
{
    // Template method — defines the skeleton; final so subclasses can't reorder steps
    public string Generate(IEnumerable<string> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(WriteHeader());
        foreach (var row in rows)
            sb.AppendLine(WriteRow(row));
        sb.AppendLine(WriteFooter());
        return sb.ToString();
    }

    // Steps subclasses must implement
    protected abstract string WriteHeader();
    protected abstract string WriteRow(string row);
    protected abstract string WriteFooter();
}

// ── Concrete subclasses — implement only what differs ─────
class CsvReportGenerator : ReportGenerator
{
    protected override string WriteHeader() => "id,name,value";
    protected override string WriteRow(string row) => row;
    protected override string WriteFooter() => "--- end ---";
}

class HtmlReportGenerator : ReportGenerator
{
    protected override string WriteHeader() =>
        "<table><tr><th>id</th><th>name</th><th>value</th></tr>";
    protected override string WriteRow(string row) =>
        $"  <tr><td>{row}</td></tr>";
    protected override string WriteFooter() => "</table>";
}

class MarkdownReportGenerator : ReportGenerator
{
    protected override string WriteHeader() => "| id | name | value |";
    protected override string WriteRow(string row) => $"| {row.Replace(",", " | ")} |";
    protected override string WriteFooter() => "";   // no footer needed
}

// ── Demo ──────────────────────────────────────────────────
var rows = new[] { "1,Alice,100", "2,Bob,200" };

foreach (var generator in new ReportGenerator[]
    { new CsvReportGenerator(), new HtmlReportGenerator(), new MarkdownReportGenerator() })
{
    Console.WriteLine($"--- {generator.GetType().Name} ---");
    Console.WriteLine(generator.Generate(rows));
}
```

## Key Takeaways

- The invariant part of the algorithm (the skeleton) lives in one place — the base class.
- Subclasses only implement the variant steps — no duplication.
- "Hook" methods (optional overrides with default implementations) let subclasses opt in without being forced to.
- Template Method uses inheritance; Strategy achieves a similar goal with composition.

## When to Use

- Multiple classes share the same algorithm structure but differ in specific steps.
- You want to enforce a consistent process while allowing controlled variation.
- Framework designers use it to define extensible workflows (NUnit test lifecycle, ASP.NET middleware).

## When NOT to Use

- Subclasses need to reorder or skip steps — the template is too rigid; use Strategy instead.
- The class hierarchy is already deep — adding another layer worsens readability.
