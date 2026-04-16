using UnityEngine;

namespace Game.Environment.Tornado
{
    /// <summary>
    /// Configuration for tornado weather effects during the warning phase.
    /// Defines lighting, fog, and ambient light changes that override day/night settings.
    /// </summary>
    [CreateAssetMenu(fileName = "TornadoConfig", menuName = "Game/Environment/Tornado Config")]
    public class TornadoConfig : ScriptableObject
    {
        [Header("Warning Phase Weather Settings")]
        [Tooltip("Duration in seconds for transitioning to/from tornado weather")]
        [Range(0.5f, 5f)]
        public float transitionDuration = 1.5f;

        [Header("Tornado Lighting")]
        [Tooltip("Light color during tornado warning (dark storm color)")]
        public Color warningPhaseLightColor = new Color(0.6f, 0.6f, 0.65f);

        [Tooltip("Light intensity during tornado warning (reduced from normal)")]
        [Range(0f, 2f)]
        public float warningPhaseLightIntensity = 0.5f;

        [Header("Tornado Ambient")]
        [Tooltip("Ambient light color during tornado warning (stormy tint)")]
        public Color warningPhaseAmbientColor = new Color(0.4f, 0.45f, 0.5f);

        [Tooltip("Ambient light intensity during tornado warning")]
        [Range(0f, 2f)]
        public float warningPhaseAmbientIntensity = 0.6f;

        [Header("Tornado Fog")]
        [Tooltip("Whether to use fog override during tornado")]
        public bool useFogOverride = true;

        [Tooltip("Fog color during tornado warning (greenish-gray storm color)")]
        public Color warningPhaseFogColor = new Color(0.5f, 0.52f, 0.48f);

        [Tooltip("Fog density during tornado warning (affects visibility)")]
        [Range(0f, 0.3f)]
        public float warningPhaseFogDensity = 0.08f;
    }
}
