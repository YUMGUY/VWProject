using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The generated floor: all room nodes keyed by id, plus the start room's id.
/// </summary>
public class Floor
{
    public Dictionary<int, RoomData> nodes = new Dictionary<int, RoomData>();
    public int startId = -1;

    public RoomData Start => nodes.TryGetValue(startId, out var s) ? s : null;
}

/// <summary>
/// Procedurally builds a floor as a strict tree of rooms on a 2D grid, following a
/// FloorConfig. Two phases:
///   1. Layout  - grid BFS from the start cell; each room spawns children into free
///                cardinal cells. Branch isolation guarantees different branches never
///                touch, so the result is a tree (parent<->child links only, no loops).
///   2. Assign  - give each node a RoomType (guaranteed/dead-end types on leaves first,
///                then weighted-random for the rest), which fixes difficulty & enemyCount.
/// Pure C# (no MonoBehaviour) so it is trivial to unit-test and reuse.
/// </summary>
public static class FloorGenerator
{
    private static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, 1),   // North
        new Vector2Int(1, 0),   // East
        new Vector2Int(0, -1),  // South
        new Vector2Int(-1, 0)   // West
    };

    // Layout is retried up to this many times (with different randomness) if some room ended
    // up starved of children purely by bad luck in cell packing - see GenerateLayout's
    // starvedCount tracking below.
    private const int MaxLayoutAttempts = 20;

    public static Floor Generate(FloorConfig config, string startSceneName)
    {
        if (config == null)
        {
            Debug.LogError("FloorGenerator: FloorConfig is null.");
            return new Floor();
        }

        // --- Phase 1: layout (retried if branch isolation starves a room of children) -----
        Floor best = null;
        int bestStarved = int.MaxValue;

        for (int attempt = 0; attempt < MaxLayoutAttempts && bestStarved > 0; attempt++)
        {
            // seed == 0 means "fully random every run" - each attempt already gets a fresh
            // draw. A non-zero seed stays reproducible: attempts are salted by index so a
            // failed layout doesn't just repeat itself, but the same config+seed always
            // resolves to the same final floor.
            System.Random layoutRng = config.seed == 0
                ? new System.Random()
                : new System.Random(config.seed + attempt);

            Floor floor = GenerateLayout(config, startSceneName, layoutRng, out int starved);
            if (starved < bestStarved)
            {
                best = floor;
                bestStarved = starved;
            }
        }

        if (bestStarved > 0)
        {
            Debug.LogWarning($"FloorGenerator: could not give every eligible room at least 1 child after " +
                              $"{MaxLayoutAttempts} layout attempts ({bestStarved} room(s) ended up as unintended " +
                              "leaves - every cardinal cell around them was blocked by branch isolation). " +
                              "Consider raising maxRooms, lowering maxDepth, or narrowing branching.");
        }

        // --- Phase 2: assign room types ---------------------------------------
        System.Random assignRng = config.seed == 0
            ? new System.Random()
            : new System.Random(config.seed + MaxLayoutAttempts);
        AssignRoomTypes(best, config, assignRng);

        LogSummary(best, config);
        return best;
    }

    /// <summary>
    /// Builds one candidate tree layout. Returns the number of rooms that ended up with zero
    /// children despite being below maxDepth with room budget still available when their turn
    /// came - i.e. every one of their cardinal cells was blocked by branch isolation. A single
    /// node in that state has no further options to try (all 4 directions are already
    /// exhausted), so the only way to avoid it is to reroll the whole layout with different
    /// randomness, which Generate() does by calling this repeatedly.
    /// </summary>
    private static Floor GenerateLayout(FloorConfig config, string startSceneName, System.Random rng, out int starvedCount)
    {
        var floor = new Floor();
        var grid = new Dictionary<Vector2Int, RoomData>();
        int nextId = 0;
        starvedCount = 0;

        RoomData start = new RoomData
        {
            id = nextId++,
            parentId = -1,
            cell = Vector2Int.zero,
            depth = 0,
            sceneName = startSceneName,
            roomName = "Starting Room"
        };
        grid[start.cell] = start;
        floor.nodes[start.id] = start;
        floor.startId = start.id;

        var queue = new Queue<RoomData>();
        queue.Enqueue(start);

        int minChildren = Mathf.Clamp(config.minNeighborsPerRoom, 1, 4);
        int maxChildren = Mathf.Clamp(config.maxNeighborsPerRoom, minChildren, 4);

        while (queue.Count > 0 && floor.nodes.Count < config.maxRooms)
        {
            RoomData current = queue.Dequeue();
            if (current.depth >= config.maxDepth) continue;

            // The start room always fills its own cardinal ring (default 4) so the hub keeps
            // its classic N/E/S/W look, regardless of the per-room branching used deeper down.
            int target = current.parentId == -1
                ? Mathf.Clamp(config.startRoomExits, 1, 4)
                : rng.Next(minChildren, maxChildren + 1);
            int made = 0;
            bool hadBudgetToStart = floor.nodes.Count < config.maxRooms;

            foreach (Vector2Int dir in Shuffled(Dirs, rng))
            {
                if (made >= target || floor.nodes.Count >= config.maxRooms) break;

                Vector2Int cell = current.cell + dir;
                if (!IsValidChildCell(cell, current.cell, grid)) continue;

                RoomData child = new RoomData
                {
                    id = nextId++,
                    parentId = current.id,
                    cell = cell,
                    depth = current.depth + 1,
                    sceneName = PickScene(config, rng),
                    roomName = "Room " + nextId
                };

                // Tree link: parent <-> child ONLY. No sibling / cross-branch links.
                current.neighborIds.Add(child.id);
                child.neighborIds.Add(current.id);

                grid[cell] = child;
                floor.nodes[child.id] = child;
                queue.Enqueue(child);
                made++;
            }

            if (made == 0 && hadBudgetToStart)
                starvedCount++;
        }

        return floor;
    }

    /// <summary>
    /// A candidate cell is valid only if it is empty AND none of its four grid-neighbors
    /// is occupied except the parent. This keeps branches physically separated: a cell
    /// that would sit next to any room from another branch is rejected, so no two branches
    /// ever touch and no accidental doors form.
    /// </summary>
    private static bool IsValidChildCell(Vector2Int cell, Vector2Int parentCell,
        Dictionary<Vector2Int, RoomData> grid)
    {
        if (grid.ContainsKey(cell)) return false;

        foreach (Vector2Int dir in Dirs)
        {
            Vector2Int adj = cell + dir;
            if (adj == parentCell) continue;
            if (grid.ContainsKey(adj)) return false;
        }
        return true;
    }

    private static string PickScene(FloorConfig config, System.Random rng)
    {
        if (config.availableRoomScenes == null || config.availableRoomScenes.Count == 0)
            return null;
        return config.availableRoomScenes[rng.Next(config.availableRoomScenes.Count)];
    }

    // ----------------------------------------------------------------------------
    // Type assignment
    // ----------------------------------------------------------------------------
    private static void AssignRoomTypes(Floor floor, FloorConfig config, System.Random rng)
    {
        // Start room.
        RoomData start = floor.Start;
        if (start != null)
        {
            start.roomType = config.startType;
            start.difficulty = 0;
            start.enemyCount = 0;
        }

        // Pools of still-unassigned nodes.
        var leaves = new List<RoomData>();      // non-start, exactly one link (its parent)
        var internals = new List<RoomData>();   // non-start, has children
        foreach (var node in floor.nodes.Values)
        {
            if (node == start) continue;
            if (node.neighborIds.Count <= 1) leaves.Add(node);
            else internals.Add(node);
        }

        var assigned = new HashSet<int>();

        // Guaranteed types first (e.g. exactly one Boss at the farthest leaf).
        if (config.roomTypes != null)
        {
            foreach (RoomType type in config.roomTypes)
            {
                if (type == null || !type.isGuaranteed) continue;

                for (int n = 0; n < type.guaranteedCount; n++)
                {
                    RoomData target = PickGuaranteedTarget(type, leaves, internals, rng);
                    if (target == null)
                    {
                        Debug.LogWarning($"FloorGenerator: no free node available for guaranteed type '{type.typeName}'.");
                        break;
                    }
                    Apply(target, type, rng);
                    assigned.Add(target.id);
                    leaves.Remove(target);
                    internals.Remove(target);
                }
            }
        }

        // Weighted-random fill for everything else.
        // Leaves may receive dead-end types; internal rooms may not (they have children).
        List<RoomType> leafPool = BuildWeightedPool(config, allowDeadEnd: true);
        List<RoomType> internalPool = BuildWeightedPool(config, allowDeadEnd: false);

        foreach (RoomData node in leaves)
        {
            if (assigned.Contains(node.id)) continue;
            Apply(node, ChooseWeighted(leafPool, rng) ?? config.defaultCombatType, rng);
        }
        foreach (RoomData node in internals)
        {
            if (assigned.Contains(node.id)) continue;
            Apply(node, ChooseWeighted(internalPool, rng) ?? config.defaultCombatType, rng);
        }
    }

    /// <summary>
    /// Whether a room of this type must land on a leaf (dead-end) node. Shared by both the
    /// guaranteed-placement path and the ordinary weighted-random pool below, so `placement`
    /// isn't silently ignored just because a type isn't guaranteed.
    /// </summary>
    private static bool IsLeafOnly(RoomType type)
    {
        return type.isDeadEnd
               || type.placement == RoomType.Placement.AnyLeaf
               || type.placement == RoomType.Placement.FarthestLeaf;
    }

    private static RoomData PickGuaranteedTarget(RoomType type, List<RoomData> leaves,
        List<RoomData> internals, System.Random rng)
    {
        bool leafOnly = IsLeafOnly(type);

        if (leafOnly)
        {
            if (leaves.Count == 0) return null;
            if (type.placement == RoomType.Placement.FarthestLeaf)
            {
                RoomData best = leaves[0];
                foreach (RoomData leaf in leaves)
                    if (leaf.depth > best.depth) best = leaf;
                return best;
            }
            return leaves[rng.Next(leaves.Count)];
        }

        // Placement.Anywhere and not a dead-end: any unassigned non-start node.
        if (internals.Count + leaves.Count == 0) return null;
        var all = new List<RoomData>(internals);
        all.AddRange(leaves);
        return all[rng.Next(all.Count)];
    }

    /// <summary>Types eligible for weighted-random assignment (guaranteed types excluded so their count stays exact).</summary>
    private static List<RoomType> BuildWeightedPool(FloorConfig config, bool allowDeadEnd)
    {
        var pool = new List<RoomType>();
        if (config.roomTypes == null) return pool;
        foreach (RoomType type in config.roomTypes)
        {
            if (type == null || type.isGuaranteed) continue;
            if (IsLeafOnly(type) && !allowDeadEnd) continue;
            if (type.selectionWeight <= 0f) continue;
            pool.Add(type);
        }
        return pool;
    }

    private static RoomType ChooseWeighted(List<RoomType> pool, System.Random rng)
    {
        if (pool == null || pool.Count == 0) return null;
        float total = 0f;
        foreach (RoomType t in pool) total += t.selectionWeight;
        if (total <= 0f) return pool[rng.Next(pool.Count)];

        double roll = rng.NextDouble() * total;
        foreach (RoomType t in pool)
        {
            roll -= t.selectionWeight;
            if (roll <= 0d) return t;
        }
        return pool[pool.Count - 1];
    }

    private static void Apply(RoomData node, RoomType type, System.Random rng)
    {
        node.roomType = type;
        if (type == null)
        {
            node.difficulty = 1;
            node.enemyCount = 0;
            return;
        }
        node.difficulty = type.baseDifficulty;
        int min = Mathf.Min(type.enemyCountRange.x, type.enemyCountRange.y);
        int max = Mathf.Max(type.enemyCountRange.x, type.enemyCountRange.y);
        node.enemyCount = rng.Next(min, max + 1);
    }

    // ----------------------------------------------------------------------------
    // Utilities
    // ----------------------------------------------------------------------------
    private static IEnumerable<Vector2Int> Shuffled(Vector2Int[] source, System.Random rng)
    {
        var list = new List<Vector2Int>(source);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private static void LogSummary(Floor floor, FloorConfig config)
    {
        int maxDepth = 0;
        var histogram = new Dictionary<string, int>();
        RoomData boss = null;

        foreach (RoomData node in floor.nodes.Values)
        {
            if (node.depth > maxDepth) maxDepth = node.depth;
            string key = node.roomType != null ? node.roomType.typeName : "(none)";
            histogram[key] = histogram.TryGetValue(key, out int c) ? c + 1 : 1;
            if (node.roomType != null && node.roomType.placement == RoomType.Placement.FarthestLeaf)
                boss = node;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append($"[FloorGenerator] rooms={floor.nodes.Count}/{config.maxRooms}, depthReached={maxDepth}/{config.maxDepth}");
        foreach (var kv in histogram) sb.Append($", {kv.Key}={kv.Value}");
        if (boss != null) sb.Append($", farthestLeaf@{boss.cell}(depth {boss.depth})");
        Debug.Log(sb.ToString());
    }
}
