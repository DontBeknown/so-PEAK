using System;
using Game.Collectable;
using Game.Dialog;

namespace Game.Core.Events
{
    // Equipment Events
    public class ItemEquippedEvent
    {
        public IEquippable Item { get; }
        
        public ItemEquippedEvent(IEquippable item)
        {
            Item = item;
        }
    }
    
    public class ItemUnequippedEvent
    {
        public IEquippable Item { get; }
        
        public ItemUnequippedEvent(IEquippable item)
        {
            Item = item;
        }
    }
    
    // Crafting Events
    public class CraftingStartedEvent
    {
        public CraftingRecipe Recipe { get; }
        
        public CraftingStartedEvent(CraftingRecipe recipe)
        {
            Recipe = recipe;
        }
    }
    
    public class CraftingCompletedEvent
    {
        public CraftingRecipe Recipe { get; }
        
        public CraftingCompletedEvent(CraftingRecipe recipe)
        {
            Recipe = recipe;
        }
    }
    
    public class CraftingFailedEvent
    {
        public CraftingRecipe Recipe { get; }
        public string Reason { get; }
        
        public CraftingFailedEvent(CraftingRecipe recipe, string reason = "")
        {
            Recipe = recipe;
            Reason = reason;
        }
    }
    
    // Interaction/Detection Events
    public class NearestItemChangedEvent
    {
        public ResourceCollector NewNearest { get; }
        
        public NearestItemChangedEvent(ResourceCollector newNearest)
        {
            NewNearest = newNearest;
        }
    }
    
    public class ItemInRangeChangedEvent
    {
        public bool IsInRange { get; }
        
        public ItemInRangeChangedEvent(bool isInRange)
        {
            IsInRange = isInRange;
        }
    }
    
    // UI Panel Events
    public class PanelOpenedEvent
    {
        public string PanelName { get; }
        
        public PanelOpenedEvent(string panelName)
        {
            PanelName = panelName;
        }
    }
    
    public class PanelClosedEvent
    {
        public string PanelName { get; }
        
        public PanelClosedEvent(string panelName)
        {
            PanelName = panelName;
        }
    }

    // Player Events
    public class PlayerDeathEvent
    {
        public DeathCause Cause { get; }

        public PlayerDeathEvent(DeathCause cause)
        {
            Cause = cause;
        }
    }

    // Collectable/Dialog Events
    public class CollectableUnlockedEvent
    {
        public CollectableItem Collectable { get; }

        public CollectableUnlockedEvent(CollectableItem collectable)
        {
            Collectable = collectable;
        }
    }

    public class CollectableOpenRequestedEvent
    {
        public CollectableItem Collectable { get; }
        public bool ReplayDialog { get; }

        public CollectableOpenRequestedEvent(CollectableItem collectable, bool replayDialog = false)
        {
            Collectable = collectable;
            ReplayDialog = replayDialog;
        }
    }

    public class CollectableHubFocusRequestedEvent
    {
        public string CollectableId { get; }

        public CollectableHubFocusRequestedEvent(string collectableId)
        {
            CollectableId = collectableId;
        }
    }

    public class DialogStartedEvent
    {
        public DialogData Dialog { get; }
        public bool IsReplay { get; }

        public DialogStartedEvent(DialogData dialog, bool isReplay)
        {
            Dialog = dialog;
            IsReplay = isReplay;
        }
    }

    public class DialogLineChangedEvent
    {
        public DialogData Dialog { get; }
        public int LineIndex { get; }
        public DialogLine Line { get; }

        public DialogLineChangedEvent(DialogData dialog, int lineIndex, DialogLine line)
        {
            Dialog = dialog;
            LineIndex = lineIndex;
            Line = line;
        }
    }

    public class DialogPausedEvent
    {
        public bool IsPaused { get; }

        public DialogPausedEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }

    public class DialogEndedEvent
    {
        public string DialogId { get; }

        public DialogEndedEvent(string dialogId)
        {
            DialogId = dialogId;
        }
    }
}
