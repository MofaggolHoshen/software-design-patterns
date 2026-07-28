// ============================================================
// Composite Pattern — C# Example
// ============================================================
//
// Intent: Compose objects into tree structures for part-whole
// hierarchies. Clients treat leaves and composites uniformly.
//
// Key roles:
//   IFileSystemItem — Component interface
//   FileItem        — Leaf
//   FolderItem      — Composite
// ============================================================

// ── Component interface ───────────────────────────────────
interface IFileSystemItem
{
    string Name { get; }
    long Size { get; }
    void Print(string indent = "");
}

// ── Leaf ─────────────────────────────────────────────────
class FileItem(string name, long sizeInBytes) : IFileSystemItem
{
    public string Name => name;
    public long Size => sizeInBytes;

    public void Print(string indent = "") =>
        Console.WriteLine($"{indent}📄 {Name} ({Size:N0} bytes)");
}

// ── Composite ─────────────────────────────────────────────
class FolderItem(string name) : IFileSystemItem
{
    private readonly List<IFileSystemItem> _children = new();

    public string Name => name;
    public long Size => _children.Sum(c => c.Size);   // recursive — works at any depth

    public FolderItem Add(IFileSystemItem item) { _children.Add(item); return this; }
    public void Remove(IFileSystemItem item) => _children.Remove(item);

    public void Print(string indent = "")
    {
        Console.WriteLine($"{indent}📁 {Name}/ ({Size:N0} bytes)");
        foreach (var child in _children)
            child.Print(indent + "  ");
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Composite Pattern ===\n");

var root = new FolderItem("project");

var src = new FolderItem("src");
src.Add(new FileItem("Program.cs", 2_048))
   .Add(new FileItem("AppConfig.cs", 1_024));

var tests = new FolderItem("tests");
tests.Add(new FileItem("UnitTests.cs", 4_096))
     .Add(new FileItem("IntegrationTests.cs", 8_192));

var assets = new FolderItem("assets");
assets.Add(new FileItem("logo.png", 102_400))
      .Add(new FileItem("banner.jpg", 204_800));

root.Add(src)
    .Add(tests)
    .Add(assets)
    .Add(new FileItem("README.md", 512));

root.Print();

Console.WriteLine($"\nTotal project size: {root.Size:N0} bytes");

// Client works with IFileSystemItem — no type checks needed
IFileSystemItem any = root;
Console.WriteLine($"Via interface: {any.Name} = {any.Size:N0} bytes");

// Searching recursively works uniformly
void FindLargeFiles(IFileSystemItem item, long threshold)
{
    if (item is FileItem f && f.Size > threshold)
        Console.WriteLine($"  Large file: {f.Name} ({f.Size:N0} bytes)");
    if (item is FolderItem folder)
        foreach (var child in folder.Print == null ? Array.Empty<IFileSystemItem>()
                                                     : GetChildren(folder))
            FindLargeFiles(child, threshold);
}

// Helper to access children for demo (not part of the pattern interface)
IEnumerable<IFileSystemItem> GetChildren(FolderItem folder)
{
    // In production, expose IReadOnlyList<IFileSystemItem> Children on FolderItem
    yield break; // simplified for demo — children not directly accessible via IFileSystemItem
}

Console.WriteLine("\nNote: IFileSystemItem is enough to call Size and Print on any node.");
Console.WriteLine($"  Tests folder size: {tests.Size:N0} bytes");
