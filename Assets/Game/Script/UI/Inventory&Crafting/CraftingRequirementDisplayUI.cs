using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingRequirementDisplayUI : MonoBehaviour
{
    [Header("Requirement Display References")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI requirementText;

    public Image Icon => icon;
    public TextMeshProUGUI RequirementText => requirementText;
}
