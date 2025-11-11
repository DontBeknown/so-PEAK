using UnityEngine;

namespace Game.Player.Animation
{
    /// <summary>
    /// Configuration for Foot IK system.
    /// ScriptableObject to allow easy tweaking without code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Player/Animation/Foot IK Config", fileName = "FootIKConfig")]
    public class FootIKConfig : ScriptableObject
    {
        [Header("IK Weights")]
        [Range(0f, 1f)] public float ikWeight = 1f;
        [Range(0f, 1f)] public float rotationWeight = 1f;

        [Header("Ground Detection")]
        public LayerMask groundLayer = -1;
        public float raycastDistance = 1.5f;
        public float raycastUpOffset = 1f;
        public float footOffset = 0.05f;
        public float minFootDistance = 0.05f;

        [Header("Climbing")]
        public LayerMask climbableLayer = -1;
        public float climbingFootReachDistance = 1f;
        public float climbingFootOffset = 0.08f;

        [Header("Pelvis Adjustment")]
        public bool enablePelvisAdjustment = true;
        public float pelvisUpDownSpeed = 5f;
        public float pelvisOffset = 0f;
        public float maxPelvisAdjustment = 0.3f;

        [Header("Smoothing")]
        public float smoothSpeed = 10f;
    }
}
