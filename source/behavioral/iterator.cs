// ============================================================
// Iterator Pattern — C# Example
// ============================================================
//
// Intent: Provide a way to access elements of a collection
// sequentially without exposing its underlying representation.
//
// Two approaches shown:
//   1. Custom iterator interface (explicit IMusicIterator)
//   2. C# idiomatic IEnumerable<T> + yield return
// ============================================================

// ══════════════════════════════════════════════════════════
// Approach 1: Custom iterator
// ══════════════════════════════════════════════════════════

interface IMusicIterator
{
    bool HasNext();
    string Next();
    void Reset();
}

class Playlist
{
    private readonly List<string> _songs = new();

    public void Add(string song) => _songs.Add(song);
    public int Count => _songs.Count;

    public IMusicIterator GetForwardIterator() => new ForwardIterator(_songs);
    public IMusicIterator GetReverseIterator() => new ReverseIterator(_songs);

    private class ForwardIterator(List<string> songs) : IMusicIterator
    {
        private int _index;
        public bool HasNext() => _index < songs.Count;
        public string Next() => songs[_index++];
        public void Reset() => _index = 0;
    }

    private class ReverseIterator(List<string> songs) : IMusicIterator
    {
        private int _index = songs.Count - 1;
        public bool HasNext() => _index >= 0;
        public string Next() => songs[_index--];
        public void Reset() => _index = songs.Count - 1;
    }
}

// ══════════════════════════════════════════════════════════
// Approach 2: C# IEnumerable<T> + yield return (idiomatic)
// ══════════════════════════════════════════════════════════

class SmartPlaylist : IEnumerable<string>
{
    private readonly List<string> _songs = new();

    public void Add(string song) => _songs.Add(song);

    // Default forward iterator — compatible with foreach and LINQ
    public IEnumerator<string> GetEnumerator()
    {
        foreach (var song in _songs)
            yield return song;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        => GetEnumerator();

    // Alternative traversals as IEnumerable<string>
    public IEnumerable<string> Reversed()
    {
        for (int i = _songs.Count - 1; i >= 0; i--)
            yield return _songs[i];
    }

    public IEnumerable<string> Shuffled()
    {
        var rng = new Random(42); // seeded for reproducible demo
        var indices = Enumerable.Range(0, _songs.Count).OrderBy(_ => rng.Next()).ToList();
        foreach (var i in indices) yield return _songs[i];
    }
}

// ── Demo ─────────────────────────────────────────────────
Console.WriteLine("=== Iterator Pattern ===\n");

var playlist = new Playlist();
playlist.Add("Song A"); playlist.Add("Song B"); playlist.Add("Song C");

Console.WriteLine("Forward:");
var fwd = playlist.GetForwardIterator();
while (fwd.HasNext()) Console.WriteLine($"  {fwd.Next()}");

Console.WriteLine("Reverse:");
var rev = playlist.GetReverseIterator();
while (rev.HasNext()) Console.WriteLine($"  {rev.Next()}");

Console.WriteLine("\nC# IEnumerable<T> (foreach):");
var smart = new SmartPlaylist();
smart.Add("Alpha"); smart.Add("Beta"); smart.Add("Gamma");

Console.WriteLine("  Default:");
foreach (var s in smart) Console.WriteLine($"    {s}");

Console.WriteLine("  Reversed:");
foreach (var s in smart.Reversed()) Console.WriteLine($"    {s}");

Console.WriteLine("  Shuffled:");
foreach (var s in smart.Shuffled()) Console.WriteLine($"    {s}");
