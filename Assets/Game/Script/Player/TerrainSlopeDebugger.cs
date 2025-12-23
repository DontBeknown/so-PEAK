using UnityEngine;

/// <summary>
/// Debug helper to visualize terrain slope and stamina drain calculations
/// Attach to player GameObject to see real-time slope information
/// </summary>
public class TerrainSlopeDebugger : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool drawGroundNormal = true;
    [SerializeField] private float normalLineLength = 2f;

    private float currentSlopeAngle;
    private bool isMovingUphill;
    private float currentSpeedMultiplier = 1f;
    private float currentSlopeGradient;
    private Vector3 groundNormal = Vector3.up;
    private Vector3 lastPosition;
    private float actualMovementSpeed;
    private bool raycastHit;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<PlayerStats>();
        
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (stats == null || stats.Config == null) return;

        CalculateSlopeInfo();
    }

    private void CalculateSlopeInfo()
    {
        var config = stats.Config;
        
        // Calculate actual movement speed
        Vector3 horizontalMovement = transform.position - lastPosition;
        horizontalMovement.y = 0f;
        actualMovementSpeed = horizontalMovement.magnitude / Time.fixedDeltaTime;
        
        // Get ground normal via raycast
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        raycastHit = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f, config.groundLayer);
        
        if (raycastHit)
        {
            groundNormal = hit.normal;
            currentSlopeAngle = Vector3.Angle(Vector3.up, groundNormal);
            
            if (horizontalMovement.sqrMagnitude > 0.001f)
            {
                float movementDot = Vector3.Dot(horizontalMovement.normalized, groundNormal);
                isMovingUphill = movementDot < 0f;
                
                
                // Calculate Tobler's hiking function speed multiplier
                float slopeAngleRad = currentSlopeAngle * Mathf.Deg2Rad;
                float slopeGradient = Mathf.Tan(slopeAngleRad);
                
                // Apply sign based on direction
                if (!isMovingUphill)
                {
                    slopeGradient = -slopeGradient;
                }
                
                currentSlopeGradient = slopeGradient;
                
                // Tobler's hiking function: speed = exp(-3.5 * abs(slope + 0.05))
                float toblerRaw = Mathf.Exp(-3.5f * Mathf.Abs(slopeGradient + 0.05f));
                
                // Calculate flat ground reference (gradient = 0)
                float flatGroundValue = Mathf.Exp(-3.5f * 0.05f); // ≈ 0.839
                
                // Remap so flat ground = max speed, steep slopes = min speed
                float normalizedValue = toblerRaw / flatGroundValue;
                currentSpeedMultiplier = Mathf.Lerp(config.minSlopeSpeedMultiplier, config.maxSlopeSpeedMultiplier, Mathf.Clamp01(normalizedValue));
            }
            else
            {
                currentSpeedMultiplier = 1f;
                currentSlopeGradient = 0f;
                actualMovementSpeed = 0f;
            }
        }
        
        lastPosition = transform.position;
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 11;
        style.normal.textColor = Color.white;
        style.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.5f));
        style.padding = new RectOffset(4, 4, 4, 4);

        GUILayout.BeginArea(new Rect(10, 10, 260, 380), style);
        
        GUILayout.Label($"<b>Terrain Slope Debug</b>", style);
        GUILayout.Label($"Slope Angle: {currentSlopeAngle:F1}°");
        GUILayout.Label($"Direction: {(isMovingUphill ? "UPHILL ↗" : "DOWNHILL ↘")}");
        GUILayout.Label($"Tobler Multiplier: {currentSpeedMultiplier:F2}x");
        
        if (stats != null && stats.Config != null && stats.StaminaStat != null)
        {
            float drainPerSec = stats.Config.baseMovementStaminaDrain;
            float fatigue = stats.StaminaStat.Fatigue;
            float fatiguePercent = stats.StaminaStat.FatiguePercent;
            
            // Calculate speeds
            float baseSpeed = stats.Config.walkSpeed;
            float theoreticalSpeed = baseSpeed * currentSpeedMultiplier;
            
            // Calculate fatigue components using same values as WalkingState
            float fatigueTimeRate = stats.Config.fatigueRateTime;
            float fatigueElevRate = stats.Config.fatigueRateElev * Mathf.Abs(currentSlopeGradient);
            float fatigueSpeedRate = stats.Config.fatigueRateSpeed * actualMovementSpeed;
            float totalFatigueRate = fatigueTimeRate + fatigueElevRate + fatigueSpeedRate;
            
            GUILayout.Label($"Base Speed: {baseSpeed:F2} m/s");
            GUILayout.Label($"After Tobler: {theoreticalSpeed:F2} m/s");
            GUILayout.Label($"Actual Speed: {actualMovementSpeed:F2} m/s");
            GUILayout.Label($"Slope Gradient: {currentSlopeGradient:F3}");
            GUILayout.Label($"Stamina: {stats.Stamina:F0}/{stats.MaxStamina:F0}");
            
            GUILayout.Space(3);
            GUILayout.Label($"<b>Fatigue Breakdown:</b>", style);
            GUILayout.Label($"  Time: {fatigueTimeRate:F3}/sec");
            GUILayout.Label($"  Elevation: {fatigueElevRate:F3}/sec");
            GUILayout.Label($"  Speed: {fatigueSpeedRate:F3}/sec");
            GUILayout.Label($"  Total: {totalFatigueRate:F3}/sec");
            
            GUILayout.Space(3);
            
            // Display fatigue with color coding
            Color originalColor = GUI.color;
            if (fatiguePercent > 70f)
                GUI.color = Color.red;
            else if (fatiguePercent > 50f)
                GUI.color = Color.yellow;
            else
                GUI.color = Color.green;
                
            GUILayout.Label($"Fatigue: {fatigue:F1}/{stats.Config.maxFatigue:F0} ({fatiguePercent:F0}%)");
            GUI.color = originalColor;
        }
        
        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        if (!drawGroundNormal) return;

        Vector3 origin = transform.position;
        
        // Draw ground normal
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + groundNormal * normalLineLength);
        
        // Draw upward reference
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + Vector3.up * normalLineLength);
        
        // Color code based on slope angle
        if (currentSlopeAngle > 45f)
            Gizmos.color = Color.red;
        else if (currentSlopeAngle > 25f)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.green;
            
        Gizmos.DrawWireSphere(origin, 0.5f);
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
