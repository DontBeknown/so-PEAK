using UnityEngine;

public class ResourceCollector : MonoBehaviour
{
    [Header("Resource Settings")]
    [SerializeField] private InventoryItem resourceItem;
    [SerializeField] private int minQuantity = 1;
    [SerializeField] private int maxQuantity = 1;
    [SerializeField] private bool destroyAfterCollect = true;
    [SerializeField] private int maxHarvests = 1;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private GameObject highlightEffect; // Optional glow/outline when near player

    private int harvestCount = 0;
    private bool isHighlighted = false;

    public bool CanBeCollected => harvestCount < maxHarvests && resourceItem != null;

    public bool CollectResource(InventoryManager inventory)
    {
        if (!CanBeCollected) return false;

        // Calculate quantity to give
        int quantityToGive = Random.Range(minQuantity, maxQuantity + 1);

        // Try to add to inventory
        if (inventory.AddItem(resourceItem, quantityToGive))
        {
            harvestCount++;

            // Visual/Audio feedback
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            Debug.Log($"Collected {quantityToGive} {resourceItem.itemName}");

            // Check if should be destroyed or disabled
            if (destroyAfterCollect || harvestCount >= maxHarvests)
            {
                if (destroyAfterCollect)
                {
                    Destroy(gameObject, 0.1f); // Small delay for effects
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }

            return true;
        }
        else
        {
            Debug.Log("Inventory full - cannot collect item!");
            return false;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted) return;

        isHighlighted = highlighted;

        if (highlightEffect != null)
        {
            highlightEffect.SetActive(highlighted);
        }

        // You can add other visual feedback here (outline shader, etc.)
    }

    public string GetDisplayName()
    {
        if (resourceItem == null) return "Unknown Item";

        int quantityRange = maxQuantity == minQuantity ? minQuantity : Random.Range(minQuantity, maxQuantity + 1);
        return $"{resourceItem.itemName} x{quantityRange}";
    }

    private void OnDrawGizmosSelected()
    {
        // Visual indicator in scene view
        Gizmos.color = CanBeCollected ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}