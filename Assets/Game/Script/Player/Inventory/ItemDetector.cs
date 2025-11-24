using UnityEngine;
using System.Collections.Generic;
using System;

public class ItemDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask itemLayerMask = -1; // Which layers to detect items on
    [SerializeField] private Transform detectionCenter; // Optional custom center point

    [Header("References")]
    [SerializeField] private InventoryManager inventoryManager;

    // Events
    public static event Action<ResourceCollector> OnNearestItemChanged;
    public static event Action<bool> OnItemInRange; // True when item is in range

    private List<ResourceCollector> itemsInRange = new List<ResourceCollector>();
    private ResourceCollector nearestItem = null;
    private ResourceCollector previousNearestItem = null;

    // Properties
    public ResourceCollector NearestItem => nearestItem;
    public bool HasItemInRange => nearestItem != null;

    private void Awake()
    {
        if (inventoryManager == null)
            inventoryManager = GetComponent<InventoryManager>();

        if (detectionCenter == null)
            detectionCenter = transform;
    }

    private void Update()
    {
        UpdateNearestItem();
    }

    private void UpdateNearestItem()
    {
        // Clear the list and find all items in range
        itemsInRange.Clear();

        Collider[] colliders = Physics.OverlapSphere(detectionCenter.position, detectionRadius, itemLayerMask);

        foreach (var collider in colliders)
        {
            ResourceCollector collector = collider.GetComponent<ResourceCollector>();
            if (collector != null && collector.CanBeCollected)
            {
                itemsInRange.Add(collector);
            }
        }

        // Find the nearest item
        ResourceCollector newNearestItem = null;
        float nearestDistance = float.MaxValue;

        foreach (var item in itemsInRange)
        {
            float distance = Vector3.Distance(detectionCenter.position, item.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                newNearestItem = item;
            }
        }

        // Update nearest item if it changed
        if (newNearestItem != nearestItem)
        {
            // Remove highlight from previous item
            if (nearestItem != null)
            {
                nearestItem.SetHighlighted(false);
            }

            previousNearestItem = nearestItem;
            nearestItem = newNearestItem;

            // Highlight new nearest item
            if (nearestItem != null)
            {
                nearestItem.SetHighlighted(true);
            }

            // Trigger events
            OnNearestItemChanged?.Invoke(nearestItem);
            OnItemInRange?.Invoke(nearestItem != null);
        }
    }

    public bool TryCollectNearestItem()
    {
        if (nearestItem == null || inventoryManager == null)
        {
            return false;
        }

        bool collected = nearestItem.CollectResource(inventoryManager);

        if (collected)
        {
            // Force update to find next nearest item
            UpdateNearestItem();
        }

        return collected;
    }

    public List<ResourceCollector> GetItemsInRange()
    {
        return new List<ResourceCollector>(itemsInRange);
    }

    // Method to be called by input system if you want to use the new Input System
    public void OnPickupInput()
    {
        TryCollectNearestItem();
    }

    private void OnDrawGizmos()
    {
        if (detectionCenter == null) return;

        // Draw detection radius
        Gizmos.color = HasItemInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(detectionCenter.position, detectionRadius);

        // Draw line to nearest item
        if (nearestItem != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(detectionCenter.position, nearestItem.transform.position);
        }
    }
}