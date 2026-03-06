using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Inventory.Storage
{
    /// <summary>
    /// 2D grid backend for the inventory.
    /// Each cell references the GridPlacement that occupies it (or null if empty).
    /// </summary>
    public class GridInventoryStorage
    {
        private readonly GridPlacement[,] _grid; // [col, row]
        private readonly List<GridPlacement> _placements = new List<GridPlacement>();

        public int Width { get; }
        public int Height { get; }

        public GridInventoryStorage(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new GridPlacement[width, height];
        }

        #region Query

        /// <summary>
        /// Returns true if every cell in the rectangle is empty (or occupied by <paramref name="ignore"/>).
        /// </summary>
        public bool CanPlaceAt(Vector2Int topLeft, Vector2Int size, GridPlacement ignore = null)
        {
            if (topLeft.x < 0 || topLeft.y < 0 ||
                topLeft.x + size.x > Width ||
                topLeft.y + size.y > Height)
                return false;

            for (int x = topLeft.x; x < topLeft.x + size.x; x++)
            {
                for (int y = topLeft.y; y < topLeft.y + size.y; y++)
                {
                    var occupant = _grid[x, y];
                    if (occupant != null && occupant != ignore)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the placement occupying the given cell, or null.
        /// </summary>
        public GridPlacement GetPlacementAt(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
                return null;
            return _grid[cell.x, cell.y];
        }

        /// <summary>
        /// Returns a snapshot of all current placements.
        /// </summary>
        public List<GridPlacement> GetAllPlacements()
        {
            return new List<GridPlacement>(_placements);
        }

        public bool HasItem(InventoryItem item)
        {
            foreach (var p in _placements)
                if (p.Item == item)
                    return true;
            return false;
        }

        /// <summary>
        /// Returns true if this specific placement instance is still on the grid.
        /// </summary>
        public bool HasPlacement(GridPlacement placement)
        {
            return _placements.Contains(placement);
        }

        public GridPlacement FindPlacement(InventoryItem item)
        {
            foreach (var p in _placements)
                if (p.Item == item)
                    return p;
            return null;
        }

        /// <summary>
        /// Returns the number of times this item appears on the grid.
        /// </summary>
        public int GetItemCount(InventoryItem item)
        {
            int count = 0;
            foreach (var p in _placements)
                if (p.Item == item)
                    count++;
            return count;
        }

        #endregion

        #region Mutate

        /// <summary>
        /// Places an item at the given top-left cell. Throws if cells are occupied.
        /// </summary>
        public GridPlacement PlaceItem(InventoryItem item, Vector2Int topLeft)
        {
            var size = item.gridSize;

            if (!CanPlaceAt(topLeft, size))
            {
                Debug.LogWarning($"[GridInventoryStorage] Cannot place {item.itemName} at {topLeft}");
                return null;
            }

            var placement = new GridPlacement(item, topLeft, size);
            _placements.Add(placement);
            Stamp(placement);
            return placement;
        }

        /// <summary>
        /// Scans row-by-row for the first position that fits the item. Returns null if full.
        /// </summary>
        public GridPlacement AutoPlace(InventoryItem item)
        {
            var size = item.gridSize;

            for (int y = 0; y <= Height - size.y; y++)
            {
                for (int x = 0; x <= Width - size.x; x++)
                {
                    var pos = new Vector2Int(x, y);
                    if (CanPlaceAt(pos, size))
                    {
                        return PlaceItem(item, pos);
                    }
                }
            }

            Debug.LogWarning($"[GridInventoryStorage] No space for {item.itemName} ({size.x}x{size.y})");
            return null;
        }

        /// <summary>
        /// Removes a placement from the grid, clearing all its cells.
        /// </summary>
        public void RemoveItem(GridPlacement placement)
        {
            if (placement == null) return;
            Clear(placement);
            _placements.Remove(placement);
        }

        /// <summary>
        /// Atomic move: clears old cells, checks destination, stamps new cells.
        /// Rolls back on failure.
        /// </summary>
        public bool MoveItem(GridPlacement placement, Vector2Int newPos)
        {
            if (placement == null) return false;

            var oldPos = placement.Position;

            // Clear current cells so they don't block the overlap check
            Clear(placement);

            if (!CanPlaceAt(newPos, placement.Size))
            {
                // Rollback — re-stamp at old position
                Stamp(placement);
                return false;
            }

            placement.Position = newPos;
            Stamp(placement);
            return true;
        }

        /// <summary>
        /// Rotates an item 90°: swaps width/height in-place.
        /// Rolls back if the rotated shape doesn't fit at the current position.
        /// </summary>
        public bool RotateItem(GridPlacement placement)
        {
            if (placement == null) return false;

            var rotatedSize = new Vector2Int(placement.Size.y, placement.Size.x);

            // If already square, rotation is a no-op
            if (rotatedSize == placement.Size) return true;

            Clear(placement);

            if (!CanPlaceAt(placement.Position, rotatedSize))
            {
                // Can't fit rotated — rollback
                Stamp(placement);
                return false;
            }

            placement.Size = rotatedSize;
            placement.Rotated = !placement.Rotated;
            Stamp(placement);
            return true;
        }

        #endregion

        #region Internal Helpers

        private void Stamp(GridPlacement placement)
        {
            for (int x = placement.Position.x; x < placement.Position.x + placement.Size.x; x++)
                for (int y = placement.Position.y; y < placement.Position.y + placement.Size.y; y++)
                    _grid[x, y] = placement;
        }

        private void Clear(GridPlacement placement)
        {
            for (int x = placement.Position.x; x < placement.Position.x + placement.Size.x; x++)
                for (int y = placement.Position.y; y < placement.Position.y + placement.Size.y; y++)
                    if (_grid[x, y] == placement)
                        _grid[x, y] = null;
        }

        #endregion
    }
}
