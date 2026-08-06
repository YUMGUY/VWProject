using UnityEngine;

/// <summary>
/// Data-driven definition of a room "kind" (Combat, Treasure, Shop, Boss, Start...).
/// A RoomType drives a node's difficulty and enemy count during floor generation,
/// controls where it can be placed (dead-end / guaranteed), and how it looks on the
/// minimap. It is also the future hook for the chest/loot system (different chest
/// tables per room type).
/// Create assets via Assets > Create > Roguelike > Room Type.
/// </summary>
[CreateAssetMenu(fileName = "RoomType", menuName = "Roguelike/Room Type")]
public class RoomType : ScriptableObject
{
    /// <summary>How this type is placed on the floor tree.</summary>
    public enum Placement
    {
        Anywhere,     // any non-start node
        AnyLeaf,      // any dead-end (leaf) node
        FarthestLeaf  // the leaf with the greatest depth from the start (e.g. Boss)
    }

    [Header("Identity")]
    public string typeName = "Combat";

    [Header("Difficulty / Enemies")]
    [Tooltip("Base difficulty assigned to a node of this type. Higher = harder.")]
    public int baseDifficulty = 1;

    [Tooltip("Inclusive min/max number of enemies rolled per node of this type. x = min, y = max.")]
    public Vector2Int enemyCountRange = new Vector2Int(3, 6);

    [Header("Placement")]
    [Tooltip("Relative weight when a normal (non-guaranteed) node rolls its type. Ignored for guaranteed types.")]
    public float selectionWeight = 1f;

    [Tooltip("Forces this node to be a leaf: it will never be given children during layout.")]
    public bool isDeadEnd = false;

    [Tooltip("If set, the generator places exactly guaranteedCount of this type per floor before weighted-random assignment.")]
    public bool isGuaranteed = false;

    [Tooltip("How many of this type to force per floor when guaranteed (e.g. 1 Boss).")]
    public int guaranteedCount = 1;

    [Tooltip("Where guaranteed instances of this type are placed.")]
    public Placement placement = Placement.Anywhere;

    [Header("Minimap")]
    public Color minimapColor = Color.white;

    // TODO (future): reference to a LootTable ScriptableObject so chests/loot vary by room type.
    // public LootTable lootTable;
}
