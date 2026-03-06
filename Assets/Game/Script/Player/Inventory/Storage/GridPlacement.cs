using UnityEngine;

namespace Game.Player.Inventory.Storage
{
    /// <summary>
    /// Tracks one item placed on the 2D inventory grid.
    /// Immutable except for position (changed via GridInventoryStorage.MoveItem).
    /// </summary>
    public class GridPlacement
    {
        public InventoryItem Item { get; }
        public Vector2Int Position { get; internal set; }
        public Vector2Int Size { get; internal set; }
        public bool Rotated { get; internal set; }

        public RectInt Bounds => new RectInt(Position, Size);

        public GridPlacement(InventoryItem item, Vector2Int position, Vector2Int size)
        {
            Item = item;
            Position = position;
            Size = size;
            Rotated = false;
        }

        /// <summary>
        /// Returns true if the given cell coordinate falls within this placement's area.
        /// </summary>
        public bool OccupiesCell(Vector2Int cell)
        {
            return cell.x >= Position.x && cell.x < Position.x + Size.x &&
                   cell.y >= Position.y && cell.y < Position.y + Size.y;
        }
    }
}
