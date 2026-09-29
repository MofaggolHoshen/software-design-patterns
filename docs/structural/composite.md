# 🌲 Composite Pattern

The Composite pattern composes objects into **tree structures** to represent part-whole hierarchies. Clients treat individual objects (leaves) and groups of objects (composites) uniformly through a common interface.

## Intent

> Compose objects into tree structures to represent part-whole hierarchies. Composite lets clients treat individual objects and compositions of objects uniformly.

## Problem

Without this pattern, client code must distinguish between leaf nodes and container nodes, leading to type checks and duplicated traversal logic. Adding a leaf to a composite requires updating all client-side `if` checks.

### Bad Example

```csharp
class File  { public long Size { get; init; } public string Name { get; init; } = ""; }
class Folder{ public string Name { get; init; } = "";
              public List<object> Items { get; } = new(); }  // mixes File and Folder

long TotalSize(object item) => item switch
{
    File f    => f.Size,
    Folder fo => fo.Items.Sum(i => TotalSize(i)), // recursive type check — fragile
    _         => throw new ArgumentException("Unknown type")
};
// Adding a new node type (Symlink) requires finding and updating every switch/if
```

### Good Example

```csharp
// ── Component interface — uniform for leaf and composite ──
interface IFileSystemItem
{
    string  Name    { get; }
    long    Size    { get; }
    void    Print(string indent = "");
}

// ── Leaf ─────────────────────────────────────────────────
class FileItem(string name, long size) : IFileSystemItem
{
    public string Name => name;
    public long   Size => size;
    public void   Print(string indent = "") =>
        Console.WriteLine($"{indent}📄 {Name} ({Size:N0} bytes)");
}

// ── Composite ────────────────────────────────────────────
class FolderItem(string name) : IFileSystemItem
{
    private readonly List<IFileSystemItem> _children = new();

    public string Name => name;
    public long   Size => _children.Sum(c => c.Size);  // recursive, works for any depth

    public void Add(IFileSystemItem item)    => _children.Add(item);
    public void Remove(IFileSystemItem item) => _children.Remove(item);

    public void Print(string indent = "")
    {
        Console.WriteLine($"{indent}📁 {Name}/ ({Size:N0} bytes)");
        foreach (var child in _children)
            child.Print(indent + "  ");
    }
}

// ── Demo ──────────────────────────────────────────────────
Console.WriteLine("=== Composite Pattern ===\n");

var root = new FolderItem("project");

var src = new FolderItem("src");
src.Add(new FileItem("Program.cs",   2_048));
src.Add(new FileItem("AppConfig.cs", 1_024));

var tests = new FolderItem("tests");
tests.Add(new FileItem("UnitTests.cs",        4_096));
tests.Add(new FileItem("IntegrationTests.cs", 8_192));

var assets = new FolderItem("assets");
assets.Add(new FileItem("logo.png",   102_400));
assets.Add(new FileItem("banner.jpg", 204_800));

root.Add(src);
root.Add(tests);
root.Add(assets);
root.Add(new FileItem("README.md", 512));

root.Print();
Console.WriteLine($"\nTotal project size: {root.Size:N0} bytes");

// Client never asks "is this a file or a folder?" — same interface for both
IFileSystemItem any = root;
Console.WriteLine($"\nSize via interface: {any.Size:N0} bytes");
```

## Key Takeaways

- Client code is identical for leaves and composites — no type checks needed.
- Adding a new leaf type (e.g., `SymlinkItem`) means adding one class that implements `IFileSystemItem`; no client code changes.
- `Size` is computed recursively and correctly at any depth without special-casing.
- Common in file systems, UI component trees, organisation charts, and expression trees.

## When to Use

- You need to represent part-whole hierarchies (trees).
- Clients should be able to ignore the difference between individual objects and groups of objects.

## When NOT to Use

- The hierarchy is always flat (no nesting) — a simple list is enough.
- Leaf and composite operations are so different that a shared interface forces meaningless no-ops (e.g., `Add()` on a leaf).
