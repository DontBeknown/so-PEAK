using UnityEngine;
using UnityEditor;
using System.IO;

namespace Game.Environment.DayNight.Editor
{
    /// <summary>
    /// Unity Editor utility for setting up the day/night cycle skybox system.
    /// Provides menu items to create required materials and assets.
    /// </summary>
    public static class SkyboxSetupUtility
    {
        private const string SHADER_NAME = "Custom/BlendedSkybox";
        private const string MATERIAL_FOLDER = "Assets/Game/Materials/Skybox";
        private const string MATERIAL_NAME = "BlendedSkyboxMaterial.mat";
        
        [MenuItem("Tools/Day Night Cycle/Create Blended Skybox Material")]
        public static void CreateBlendedSkyboxMaterial()
        {
            // Find the shader
            Shader shader = Shader.Find(SHADER_NAME);
            if (shader == null)
            {
                EditorUtility.DisplayDialog(
                    "Shader Not Found",
                    $"Could not find shader '{SHADER_NAME}'.\n\nMake sure BlendedSkybox.shader exists in your project.",
                    "OK"
                );
                return;
            }
            
            // Create folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(MATERIAL_FOLDER))
            {
                string[] folders = MATERIAL_FOLDER.Split('/');
                string currentPath = folders[0];
                
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }
            
            // Create material
            Material material = new Material(shader);
            material.name = "BlendedSkyboxMaterial";
            
            // Set default properties
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_Exposure", 1f);
            material.SetFloat("_Rotation1", 0f);
            material.SetFloat("_Rotation2", 0f);
            
            // Save material
            string fullPath = Path.Combine(MATERIAL_FOLDER, MATERIAL_NAME);
            
            // Check if material already exists
            if (AssetDatabase.LoadAssetAtPath<Material>(fullPath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Material Already Exists",
                    $"A material already exists at:\n{fullPath}\n\nDo you want to overwrite it?",
                    "Overwrite",
                    "Cancel"
                );
                
                if (!overwrite)
                {
                    return;
                }
                
                AssetDatabase.DeleteAsset(fullPath);
            }
            
            AssetDatabase.CreateAsset(material, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the newly created material
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = material;
            
            EditorUtility.DisplayDialog(
                "Success!",
                $"Blended Skybox Material created successfully!\n\nLocation: {fullPath}\n\n" +
                "Next steps:\n" +
                "1. Assign this material to a SkyboxBlender component\n" +
                "2. Add SkyboxBlender to your DayNightCycleManager GameObject\n" +
                "3. Assign the SkyboxBlender reference in DayNightCycleManager",
                "OK"
            );
        }
        
        [MenuItem("Tools/Day Night Cycle/Setup Day Night Manager")]
        public static void SetupDayNightManager()
        {
            // Find or create the manager
            DayNightCycleManager manager = Object.FindFirstObjectByType<DayNightCycleManager>();
            
            if (manager == null)
            {
                // Create new GameObject with manager
                GameObject go = new GameObject("DayNightCycleManager");
                manager = go.AddComponent<DayNightCycleManager>();
                
                // Try to find directional light
                Light directionalLight = Object.FindFirstObjectByType<Light>();
                if (directionalLight != null && directionalLight.type == LightType.Directional)
                {
                    SerializedObject so = new SerializedObject(manager);
                    so.FindProperty("directionalLight").objectReferenceValue = directionalLight;
                    so.ApplyModifiedProperties();
                }
                
                EditorUtility.DisplayDialog(
                    "Manager Created",
                    "DayNightCycleManager GameObject created!\n\n" +
                    "Please assign:\n" +
                    "- DayNightConfig asset\n" +
                    "- Directional Light (if not auto-assigned)\n" +
                    "- SkyboxBlender component",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Manager Already Exists",
                    "A DayNightCycleManager already exists in the scene.\n\n" +
                    "GameObject: " + manager.gameObject.name,
                    "OK"
                );
            }
            
            // Select the manager
            Selection.activeGameObject = manager.gameObject;
            EditorGUIUtility.PingObject(manager.gameObject);
        }
        
        [MenuItem("Tools/Day Night Cycle/Add SkyboxBlender Component")]
        public static void AddSkyboxBlenderComponent()
        {
            DayNightCycleManager manager = Object.FindFirstObjectByType<DayNightCycleManager>();
            
            if (manager == null)
            {
                EditorUtility.DisplayDialog(
                    "Manager Not Found",
                    "No DayNightCycleManager found in the scene.\n\n" +
                    "Use 'Tools/Day Night Cycle/Setup Day Night Manager' first.",
                    "OK"
                );
                return;
            }
            
            // Check if SkyboxBlender already exists
            SkyboxBlender blender = manager.GetComponent<SkyboxBlender>();
            if (blender != null)
            {
                EditorUtility.DisplayDialog(
                    "Component Already Exists",
                    "SkyboxBlender component already exists on the manager GameObject.",
                    "OK"
                );
                Selection.activeGameObject = manager.gameObject;
                return;
            }
            
            // Add component
            blender = manager.gameObject.AddComponent<SkyboxBlender>();
            
            // Try to find and assign the blend material
            string materialPath = Path.Combine(MATERIAL_FOLDER, MATERIAL_NAME);
            Material blendMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (blendMaterial != null)
            {
                SerializedObject so = new SerializedObject(blender);
                so.FindProperty("blendMaterial").objectReferenceValue = blendMaterial;
                so.ApplyModifiedProperties();
            }
            
            // Assign SkyboxBlender to manager
            SerializedObject managerSO = new SerializedObject(manager);
            managerSO.FindProperty("skyboxBlender").objectReferenceValue = blender;
            managerSO.ApplyModifiedProperties();
            
            EditorUtility.DisplayDialog(
                "Success!",
                "SkyboxBlender component added and linked to DayNightCycleManager!\n\n" +
                (blendMaterial != null ? 
                    "BlendedSkyboxMaterial has been automatically assigned." :
                    "Please assign the BlendedSkyboxMaterial manually."),
                "OK"
            );
            
            Selection.activeGameObject = manager.gameObject;
            EditorGUIUtility.PingObject(manager.gameObject);
        }
        
        [MenuItem("Tools/Day Night Cycle/Complete Setup (All Steps)")]
        public static void CompleteSetup()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Complete Day/Night Cycle Setup",
                "This will perform all setup steps:\n\n" +
                "1. Create BlendedSkybox material\n" +
                "2. Setup DayNightCycleManager (if needed)\n" +
                "3. Add SkyboxBlender component\n" +
                "4. Link all components together\n\n" +
                "Continue?",
                "Yes",
                "Cancel"
            );
            
            if (!proceed) return;
            
            // Step 1: Create material
            CreateBlendedSkyboxMaterial();
            
            // Wait for asset database to refresh
            AssetDatabase.Refresh();
            
            // Step 2: Setup manager (if needed)
            DayNightCycleManager manager = Object.FindFirstObjectByType<DayNightCycleManager>();
            if (manager == null)
            {
                GameObject go = new GameObject("DayNightCycleManager");
                manager = go.AddComponent<DayNightCycleManager>();
                
                Light directionalLight = Object.FindFirstObjectByType<Light>();
                if (directionalLight != null && directionalLight.type == LightType.Directional)
                {
                    SerializedObject so = new SerializedObject(manager);
                    so.FindProperty("directionalLight").objectReferenceValue = directionalLight;
                    so.ApplyModifiedProperties();
                }
            }
            
            // Step 3: Add SkyboxBlender if needed
            SkyboxBlender blender = manager.GetComponent<SkyboxBlender>();
            if (blender == null)
            {
                blender = manager.gameObject.AddComponent<SkyboxBlender>();
            }
            
            // Step 4: Link everything
            string materialPath = Path.Combine(MATERIAL_FOLDER, MATERIAL_NAME);
            Material blendMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (blendMaterial != null)
            {
                SerializedObject blenderSO = new SerializedObject(blender);
                blenderSO.FindProperty("blendMaterial").objectReferenceValue = blendMaterial;
                blenderSO.ApplyModifiedProperties();
            }
            
            SerializedObject managerSO = new SerializedObject(manager);
            managerSO.FindProperty("skyboxBlender").objectReferenceValue = blender;
            managerSO.ApplyModifiedProperties();
            
            Selection.activeGameObject = manager.gameObject;
            EditorGUIUtility.PingObject(manager.gameObject);
            
            EditorUtility.DisplayDialog(
                "Setup Complete!",
                "Day/Night Cycle setup is complete!\n\n" +
                "Remaining steps:\n" +
                "1. Create a DayNightConfig asset (Right-click → Create → Game → Environment → Day Night Config)\n" +
                "2. Assign 4 skybox materials to the config\n" +
                "3. Assign the config to DayNightCycleManager\n" +
                "4. Enter Play Mode to test!",
                "OK"
            );
        }
    }
}
