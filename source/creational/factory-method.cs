// ============================================================
// Factory Method Pattern — C# Example
// ============================================================
//
// Intent: Define an interface for creating a single object, but
// let subclasses decide which class to instantiate.
//
// Key roles:
//   IDocument         — Product interface
//   WordDocument      — Concrete Product
//   PdfDocument       — Concrete Product
//   DocumentEditor    — Abstract Creator (defines factory method)
//   WordEditor        — Concrete Creator
//   PdfEditor         — Concrete Creator
// ============================================================

// ── Product interface ──────────────────────────────────────
interface IDocument
{
    string Name { get; }
    void Open();
    void Save();
}

// ── Concrete products ──────────────────────────────────────
class WordDocument : IDocument
{
    public string Name => "Word Document (.docx)";
    public void Open() => Console.WriteLine($"  Opening {Name} in Word editor");
    public void Save() => Console.WriteLine($"  Saving  {Name} with full formatting");
}

class PdfDocument : IDocument
{
    public string Name => "PDF Document (.pdf)";
    public void Open() => Console.WriteLine($"  Opening {Name} in PDF viewer");
    public void Save() => Console.WriteLine($"  Saving  {Name} — flattening layers");
}

class HtmlDocument : IDocument
{
    public string Name => "HTML Document (.html)";
    public void Open() => Console.WriteLine($"  Opening {Name} in browser");
    public void Save() => Console.WriteLine($"  Saving  {Name} as static file");
}

// ── Abstract Creator ──────────────────────────────────────
// The creator defines the factory method but never references concrete products.
abstract class DocumentEditor
{
    // Factory Method — the hook subclasses must override
    protected abstract IDocument CreateDocument();

    // Template method that uses the product via its interface
    public void OpenAndSave()
    {
        var doc = CreateDocument();   // polymorphic creation
        Console.WriteLine($"Editor selected: {doc.Name}");
        doc.Open();
        doc.Save();
    }
}

// ── Concrete Creators ───────────────────────────────────────
class WordEditor : DocumentEditor
{
    protected override IDocument CreateDocument() => new WordDocument();
}

class PdfEditor : DocumentEditor
{
    protected override IDocument CreateDocument() => new PdfDocument();
}

class HtmlEditor : DocumentEditor
{
    protected override IDocument CreateDocument() => new HtmlDocument();
}

// ── Demo ───────────────────────────────────────────────────
Console.WriteLine("=== Factory Method Pattern ===\n");

var editors = new DocumentEditor[] { new WordEditor(), new PdfEditor(), new HtmlEditor() };

foreach (var editor in editors)
{
    editor.OpenAndSave();
    Console.WriteLine();
}

// Adding a new document type = new product class + new creator class.
// DocumentEditor and all existing editors are untouched.
Console.WriteLine("Adding 'SpreadsheetEditor' requires no changes to existing code.");
