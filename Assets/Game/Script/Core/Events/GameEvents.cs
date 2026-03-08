using System;

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
}
