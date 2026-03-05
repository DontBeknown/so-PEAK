using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component attached to notification prefab that holds UI element references
/// </summary>
public class NotificationUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI actionText;
}
