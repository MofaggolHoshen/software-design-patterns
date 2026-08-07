# 🔁 Iterator Pattern

The Iterator pattern provides a way to **sequentially access elements** of a collection without exposing its underlying representation. It decouples traversal logic from the collection itself, allowing different traversal strategies without changing the collection.

## Intent

> Provide a way to access elements of an aggregate object sequentially without exposing its underlying representation.

## Problem

When collection internals are exposed to allow traversal, clients become coupled to implementation details (array indices, node pointers, etc.). Changing the data structure breaks all clients. Multiple simultaneous traversals are also difficult.

### Bad Example

```csharp
class PlaylistBad
{
    // Internal list is exposed — clients depend on the index-based API
    public readonly List<string> Songs = new();

    public void Add(string song) => Songs.Add(song);
}

var playlist = new PlaylistBad();
playlist.Add("Song A"); playlist.Add("Song B"); playlist.Add("Song C");

// Client knows it's a List<string> — coupling to internal representation
for (int i = 0; i < playlist.Songs.Count; i++)
    Console.WriteLine(playlist.Songs[i]);
// Changing Songs to LinkedList<string> breaks this loop
```

### Good Example

```csharp
// ── Iterator interface ────────────────────────────────────
interface IMusicIterator
{
    bool HasNext();
    string Next();
    void Reset();
}

// ── Collection interface ──────────────────────────────────
interface IPlaylist
{
    void Add(string song);
    IMusicIterator GetIterator();
    IMusicIterator GetReverseIterator();
}

// ── Concrete Collection ───────────────────────────────────
class Playlist : IPlaylist
{
    private readonly List<string> _songs = new();

    public void Add(string song) => _songs.Add(song);

    public IMusicIterator GetIterator()        => new ForwardIterator(_songs);
    public IMusicIterator GetReverseIterator() => new ReverseIterator(_songs);

    // ── Forward Iterator ──────────────────────────────────
    private class ForwardIterator(List<string> songs) : IMusicIterator
    {
        private int _index;
        public bool   HasNext() => _index < songs.Count;
        public string Next()    => songs[_index++];
        public void   Reset()   => _index = 0;
    }

    // ── Reverse Iterator ──────────────────────────────────
    private class ReverseIterator(List<string> songs) : IMusicIterator
    {
        private int _index = songs.Count - 1;
        public bool   HasNext() => _index >= 0;
        public string Next()    => songs[_index--];
        public void   Reset()   => _index = songs.Count - 1;
    }
}

// ── C# idiomatic: IEnumerable<T> + yield return ───────────
class SmartPlaylist : System.Collections.Generic.IEnumerable<string>
{
    private readonly List<string> _songs = new();

    public void Add(string song) => _songs.Add(song);

    // Forward iterator built into the language
    public System.Collections.Generic.IEnumerator<string> GetEnumerator()
    {
        foreach (var song in _songs) yield return song;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        => GetEnumerator();

    // Shuffle iterator — different traversal, same collection
    public System.Collections.Generic.IEnumerable<string> Shuffled()
    {
        var rng     = new Random();
        var indices = Enumerable.Range(0, _songs.Count).OrderBy(_ => rng.Next());
        foreach (var i in indices) yield return _songs[i];
    }
}

// ── Demo ──────────────────────────────────────────────────
var playlist = new Playlist();
playlist.Add("Song A"); playlist.Add("Song B"); playlist.Add("Song C");

Console.WriteLine("Forward:");
var it = playlist.GetIterator();
while (it.HasNext()) Console.WriteLine($"  {it.Next()}");

Console.WriteLine("Reverse:");
var rev = playlist.GetReverseIterator();
while (rev.HasNext()) Console.WriteLine($"  {rev.Next()}");

Console.WriteLine("Smart playlist (foreach, IEnumerable<T>):");
var smart = new SmartPlaylist();
smart.Add("Alpha"); smart.Add("Beta"); smart.Add("Gamma");
foreach (var s in smart) Console.WriteLine($"  {s}");
```

## Key Takeaways

- The collection is protected from direct access — internals can change without breaking clients.
- Multiple iterators can traverse the same collection independently and simultaneously.
- C#'s `IEnumerable<T>` and `yield return` are the idiomatic implementation of this pattern.
- Different traversal strategies (forward, reverse, shuffle, filtered) can co-exist.

## When to Use

- You want to traverse a collection without exposing its implementation.
- You need multiple simultaneous traversals.
- You want a uniform API for different collection types.

## When NOT to Use

- You are using built-in .NET collections — they already implement `IEnumerable<T>`.
- The traversal is trivial and using LINQ is more expressive.
