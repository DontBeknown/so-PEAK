using UnityEngine;

namespace Game.Environment.DayNight
{
    /// <summary>
    /// Helper component to smoothly blend between two skybox materials using a custom shader.
    /// Attach this to the DayNightCycleManager or use it standalone.
    /// </summary>
    public class SkyboxBlender : MonoBehaviour
    {
        [Header("Skybox Blend Material")]
        [Tooltip("Material using the Custom/BlendedSkybox shader")]
        [SerializeField] private Material blendMaterial;
        
        [Header("Runtime State (Read Only)")]
        [SerializeField] private Material currentSkybox1;
        [SerializeField] private Material currentSkybox2;
        [SerializeField, Range(0f, 1f)] private float currentBlend = 0f;
        
        // Shader property IDs (cached for performance)
        private static readonly int Skybox1Property = Shader.PropertyToID("_Skybox1");
        private static readonly int Skybox2Property = Shader.PropertyToID("_Skybox2");
        private static readonly int BlendProperty = Shader.PropertyToID("_Blend");
        private static readonly int ExposureProperty = Shader.PropertyToID("_Exposure");
        
        private void Awake()
        {
            // Create a copy of the blend material to avoid modifying the asset
            if (blendMaterial != null)
            {
                blendMaterial = new Material(blendMaterial);
            }
            else
            {
                Debug.LogError("[SkyboxBlender] No blend material assigned! Please assign a material using Custom/BlendedSkybox shader.");
            }
        }
        
        /// <summary>
        /// Start a blend transition between two skybox cubemaps.
        /// </summary>
        /// <param name="fromSkybox">Starting skybox material (must have a cubemap texture)</param>
        /// <param name="toSkybox">Target skybox material (must have a cubemap texture)</param>
        public void StartBlend(Material fromSkybox, Material toSkybox)
        {
            if (blendMaterial == null)
            {
                Debug.LogError("[SkyboxBlender] Blend material is null!");
                return;
            }
            
            currentSkybox1 = fromSkybox;
            currentSkybox2 = toSkybox;
            currentBlend = 0f;
            
            // Extract cubemap textures from the skybox materials
            Cubemap cubemap1 = ExtractCubemap(fromSkybox);
            Cubemap cubemap2 = ExtractCubemap(toSkybox);
            
            if (cubemap1 == null || cubemap2 == null)
            {
                Debug.LogError("[SkyboxBlender] Could not extract cubemaps from skybox materials!");
                return;
            }
            
            // Set shader properties
            blendMaterial.SetTexture(Skybox1Property, cubemap1);
            blendMaterial.SetTexture(Skybox2Property, cubemap2);
            blendMaterial.SetFloat(BlendProperty, 0f);
            
            // Apply blend material to scene
            RenderSettings.skybox = blendMaterial;
            DynamicGI.UpdateEnvironment();
        }
        
        /// <summary>
        /// Update the blend progress (0 = fully skybox1, 1 = fully skybox2).
        /// </summary>
        /// <param name="blend">Blend value between 0 and 1</param>
        public void SetBlend(float blend)
        {
            if (blendMaterial == null) return;
            
            currentBlend = Mathf.Clamp01(blend);
            blendMaterial.SetFloat(BlendProperty, currentBlend);
        }
        
        /// <summary>
        /// Get current blend progress.
        /// </summary>
        public float GetBlend()
        {
            return currentBlend;
        }
        
        /// <summary>
        /// Finish blend and set final skybox.
        /// </summary>
        /// <param name="finalSkybox">The final skybox material to use</param>
        public void FinishBlend(Material finalSkybox)
        {
            if (finalSkybox != null)
            {
                RenderSettings.skybox = finalSkybox;
                DynamicGI.UpdateEnvironment();
            }
            
            currentSkybox1 = null;
            currentSkybox2 = null;
            currentBlend = 0f;
        }
        
        /// <summary>
        /// Set exposure for the blended skybox (useful for HDR skyboxes).
        /// </summary>
        public void SetExposure(float exposure)
        {
            if (blendMaterial == null) return;
            blendMaterial.SetFloat(ExposureProperty, exposure);
        }
        
        /// <summary>
        /// Extract cubemap texture from a skybox material.
        /// Supports common skybox shader types.
        /// </summary>
        private Cubemap ExtractCubemap(Material skyboxMaterial)
        {
            if (skyboxMaterial == null)
            {
                Debug.LogError("[SkyboxBlender] Skybox material is null!");
                return null;
            }
            
            // Try common skybox shader texture names
            string[] possibleNames = { "_Tex", "_MainTex", "_Cubemap", "_FrontTex", "_LeftTex", "_RightTex" };
            
            foreach (string texName in possibleNames)
            {
                if (skyboxMaterial.HasProperty(texName))
                {
                    Texture tex = skyboxMaterial.GetTexture(texName);
                    if (tex is Cubemap cubemap)
                    {
                        return cubemap;
                    }
                }
            }
            
            // Log detailed error with shader information
            Debug.LogError($"[SkyboxBlender] Could not find cubemap texture in material: {skyboxMaterial.name}\n" +
                          $"Shader: {skyboxMaterial.shader.name}\n" +
                          $"Make sure you're using a cubemap-based skybox shader like:\n" +
                          $"- Skybox/Cubemap\n" +
                          $"- Skybox/6 Sided\n" +
                          $"Procedural skyboxes (Skybox/Procedural) are not supported for blending.");
            return null;
        }
        
        /// <summary>
        /// Check if currently blending.
        /// </summary>
        public bool IsBlending()
        {
            return currentSkybox1 != null && currentSkybox2 != null && currentBlend < 1f;
        }
    }
}
