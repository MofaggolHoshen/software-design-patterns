// ============================================================
// Fixed Partitions — C# Simulation
// ============================================================
//
// Intent: Divide the key space into a fixed number of logical
// partitions and map them to physical nodes. When nodes are
// added or removed only partitions relocate, not individual
// records — keeping rebalancing cost bounded.
//
// Key roles:
//   PartitionMap — owns partition→node assignment
//   DataStore    — simulates per-partition storage
//   Client       — routes requests using the partition map
// ============================================================

class PartitionMap(int totalPartitions = 256)
{
    private readonly string?[] _assignment = new string[totalPartitions];

    public int TotalPartitions => totalPartitions;

    // Evenly distribute partitions across nodes
    public void Assign(IReadOnlyList<string> nodes)
    {
        for (int p = 0; p < totalPartitions; p++)
            _assignment[p] = nodes[p % nodes.Count];

        Console.WriteLine($"[PartitionMap] {totalPartitions} partitions → {nodes.Count} nodes assigned.");
    }

    // Rebalance: reassign partitions, report how many moved
    public void Rebalance(IReadOnlyList<string> nodes)
    {
        int moved = 0;
        for (int p = 0; p < totalPartitions; p++)
        {
            string newNode = nodes[p % nodes.Count];
            if (_assignment[p] != newNode) { _assignment[p] = newNode; moved++; }
        }
        Console.WriteLine($"[PartitionMap] Rebalanced → {nodes.Count} nodes, {moved}/{totalPartitions} partitions moved.");
    }

    public string GetNode(string key)
    {
        int partition = Math.Abs(key.GetHashCode()) % totalPartitions;
        return _assignment[partition] ?? throw new InvalidOperationException("Not assigned.");
    }

    public int GetPartition(string key) =>
        Math.Abs(key.GetHashCode()) % totalPartitions;
}

class DataStore
{
    private readonly Dictionary<string, string> _data = new();
    public void Put(string key, string value) => _data[key] = value;
    public string Get(string key) => _data.TryGetValue(key, out var v) ? v : "<not found>";
}

class DistributedKVStore(PartitionMap map)
{
    private readonly Dictionary<string, DataStore> _stores = new();

    private DataStore StoreFor(string key)
    {
        var node = map.GetNode(key);
        if (!_stores.TryGetValue(node, out var s)) { s = new DataStore(); _stores[node] = s; }
        return s;
    }

    public void Put(string key, string value)
    {
        StoreFor(key).Put(key, value);
        Console.WriteLine($"  PUT '{key}' → partition {map.GetPartition(key)} @ {map.GetNode(key)}");
    }

    public string Get(string key)
    {
        var v = StoreFor(key).Get(key);
        Console.WriteLine($"  GET '{key}' → partition {map.GetPartition(key)} @ {map.GetNode(key)} = '{v}'");
        return v;
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Fixed Partitions ===\n");

var map = new PartitionMap(totalPartitions: 64);
var nodes3 = new[] { "node-1", "node-2", "node-3" };

map.Assign(nodes3);
var store = new DistributedKVStore(map);

Console.WriteLine();
store.Put("user:alice", "Alice Smith");
store.Put("user:bob", "Bob Jones");
store.Put("order:100", "order data");

Console.WriteLine();
store.Get("user:alice");
store.Get("order:100");

Console.WriteLine("\n--- Add node-4 (rebalance) ---");
var nodes4 = new[] { "node-1", "node-2", "node-3", "node-4" };
map.Rebalance(nodes4);
// ~25% of partitions move; existing data still readable via new map
store.Get("user:alice");
