# 🏗️ Factory Method Pattern

The Factory Method pattern defines an interface for **creating objects**, but lets **subclasses** (or implementations) decide which class to instantiate. It promotes loose coupling by eliminating the need for client code to know about concrete types.

## Intent

> Define an interface for creating a single object, but let subclasses decide which class to instantiate. Factory Method lets a class defer instantiation to subclasses.

## Problem

When a base class controls creation logic with a `switch` or `if/else`, it must be modified every time a new product type is introduced. This tightly couples the creator to every concrete product, violates OCP, and makes unit testing difficult because you cannot inject a mock product.

### Bad Example

```csharp
// Base class knows about every concrete document type — must change every time a new type is added
class DocumentEditor
{
    public IDocument CreateDocument(string type) => type switch
    {
        "Word" => new WordDocument(),
        "PDF"  => new PdfDocument(),
        "HTML" => new HtmlDocument(),
        _      => throw new ArgumentException($"Unknown type: {type}")
    };

    public void OpenDocument(string type)
    {
        var doc = CreateDocument(type);
        doc.Open();
    }
}
```

### Good Example

```csharp
// ── Product interface ──────────────────────────────────────
interface IDocument
{
    void Open();
    void Save();
}

// ── Concrete products ──────────────────────────────────────
class WordDocument : IDocument
{
    public void Open() => Console.WriteLine("Opening Word document");
    public void Save() => Console.WriteLine("Saving Word document");
}

class PdfDocument : IDocument
{
    public void Open() => Console.WriteLine("Opening PDF document");
    public void Save() => Console.WriteLine("Saving PDF document — read-only check skipped");
}

// ── Creator (abstract) ────────────────────────────────────
abstract class DocumentEditor
{
    // Factory Method — subclasses override this
    protected abstract IDocument CreateDocument();

    // Template uses the factory method without knowing the concrete type
    public void OpenDocument()
    {
        var doc = CreateDocument();
        doc.Open();
        Console.WriteLine("Document opened successfully.");
    }
}

// ── Concrete creators ─────────────────────────────────────
class WordEditor : DocumentEditor
{
    protected override IDocument CreateDocument() => new WordDocument();
}

class PdfEditor : DocumentEditor
{
    protected override IDocument CreateDocument() => new PdfDocument();
}

// ── Usage ─────────────────────────────────────────────────
DocumentEditor editor = new WordEditor();
editor.OpenDocument();
// Opening Word document
// Document opened successfully.

editor = new PdfEditor();
editor.OpenDocument();
// Opening PDF document
// Document opened successfully.

// Adding a new document type = add new product + new creator, no existing code changes
```

## Key Takeaways

- The creator depends only on the product interface, never on concrete products.
- Adding a new product type requires only a new product class and a new creator subclass — existing code is untouched.
- The factory method can provide a default product in the base class, making override optional.
- Integrates naturally with Dependency Injection — register each creator against the `DocumentEditor` interface.

## When to Use

- A class cannot anticipate the kind of objects it needs to create.
- You want subclasses to have full control over which objects get created.
- You want to localise product-creation knowledge to the concrete creator.

## When NOT to Use

- There is truly only one product type and this will never change — a simple `new` or a static factory method is cleaner.
- The hierarchy of creators would become unwieldy; consider Abstract Factory when products come in families.
