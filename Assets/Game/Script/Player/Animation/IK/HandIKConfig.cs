using UnityEngine;

namespace Game.Player.Animation
{
    /// <summary>
    /// Configuration for Hand IK system.
    /// ScriptableObject for easy tweaking.
    /// </summary>
    [CreateAssetMenu(menuName = "Player/Animation/Hand IK Config", fileName = "HandIKConfig")]
    public class HandIKConfig : ScriptableObject
    {
        [Header("IK Settings")]
        public bool enableHandIK = true;
        [Range(0f, 1f)] public float handIKWeight = 1f;
        public float handRigBlendSpeed = 10f;

        [Header("Hand Positioning")]
        public bool autoPositionHands = false;
        public LayerMask climbableLayer = -1;
        public float handReachDistance = 1.5f;
        public float handOffsetFromWall = 0.05f;
        public float handHorizontalSpread = 0.4f;
        public float handHeightOffset = 0.2f;
    }
}
