using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Inventory.HeldItems
{
    /// <summary>
    /// Manages per-instance state for held items (charges, durability).
    /// Allows items to maintain state across equip/unequip cycles and saves.
    /// Follows Single Responsibility Principle.
    /// </summary>
    public class HeldItemStateManager : MonoBehaviour
    {
        private static HeldItemStateManager instance;
        private Dictionary<string, HeldItemState> itemStates = new Dictionary<string, HeldItemState>();

        public static HeldItemStateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("HeldItemStateManager");
                    instance = go.AddComponent<HeldItemStateManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Gets or creates state for a specific item instance.
        /// </summary>
        public HeldItemState GetOrCreateState(string itemID)
        {
            if (!itemStates.ContainsKey(itemID))
            {
                itemStates[itemID] = new HeldItemState();
            }
            return itemStates[itemID];
        }

        /// <summary>
        /// Removes state for a specific item (when destroyed/consumed).
        /// </summary>
        public void RemoveState(string itemID)
        {
            itemStates.Remove(itemID);
        }

        /// <summary>
        /// Checks if state exists for an item.
        /// </summary>
        public bool HasState(string itemID)
        {
            return itemStates.ContainsKey(itemID);
        }
    }

    /// <summary>
    /// Represents the runtime state of a held item.
    /// </summary>
    [System.Serializable]
    public class HeldItemState
    {
        // For charge-based items (canteen)
        public int currentCharges;
        public int maxCharges;
        public float lastUsedTime;

        // For durability-based items (torch)
        public float currentDurability; // In seconds
        public float maxDurability; // In seconds
    }
}
