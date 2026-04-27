using System;
using System.Collections.Generic;
using Game.Core.DI;
using Game.Core.Events;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class HazardLevelProfile
{
    public WorldLevel targetLevel;

    [Header("Hazard Weights")]
    public float landslideWeight = 0f;
    public float tornadoWeight = 0f;

    [Header("Level Specific Time & Risk Settings")]
    public float gracePeriodSeconds = 180f;
    public float timeToTargetRisk = 300f;
    public float targetTimeRiskValue = 0.5f;

    [Header("Procedural Search Settings")]
    [Tooltip("How much higher than the player must a cliff be to spawn rocks?")]
    public float minCliffHeightOffset = 10f;
    [Tooltip("Initial radius to search for a cliff (meters).")]
    public float initialSearchRadius = 30f;
    [Tooltip("Expanded radius if the initial search fails (meters).")]
    public float expandedSearchRadius = 60f;
}

public class NaturalEventDirector : MonoBehaviour
{
    [Header("Core Links")]
    public WorldDataManager dataManager;

    [Header("Level Profiles")]
    public List<HazardLevelProfile> levelProfiles = new List<HazardLevelProfile>();

    public event Action<Transform, WorldLevel> OnLandslideTriggered;
    public event Action<Transform, WorldLevel> OnTornadoTriggered;

    [Header("Grid & Search Setup")]
    public float gridScale = 40f;
    [Tooltip("If no cliff is found, how long to wait before trying again?")]
    public float failedSearchWaitTime = 60f;

    [Header("Current State (Read Only)")]
    [SerializeField] private float currentTimeRisk = 0f;
    [SerializeField] private float mapRisk = 0f;
    [SerializeField] private float totalRisk = 0f;
    [SerializeField] private float totalRunTime = 0f;

    [Header("Wait State (Read Only)")]
    [SerializeField] private bool isWaitingForSearch = false;
    [SerializeField] private float currentWaitTimer = 0f;

    public Transform playerTransform;

    private float[,] worldRiskMap;
    private bool isInitialized = false;

    private float[,] worldHeightMap;
    private float heightMultiplier;
    private IEventBus _eventBus;

    public void InitializeMap(float[,] generatedRiskMap, float[,] generatedHeightMap, float multiplier)
    {
        worldRiskMap = generatedRiskMap;
        worldHeightMap = generatedHeightMap;
        heightMultiplier = multiplier;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || playerTransform == null || dataManager == null) return;

        HazardLevelProfile currentProfile = levelProfiles.Find(p => p.targetLevel == dataManager.currentLevel);
        if (currentProfile == null) return;

        float dt = Time.deltaTime;
        totalRunTime += dt;

        if (totalRunTime < currentProfile.gracePeriodSeconds) return;

        // 1. Handle the 1-Minute Wait Cooldown
        if (isWaitingForSearch)
        {
            currentWaitTimer -= dt;

            // Keep updating the visual totalRisk so you can still see it in the Inspector
            mapRisk = GetRiskAtPlayerPosition();
            totalRisk = currentTimeRisk + mapRisk;

            if (currentWaitTimer <= 0f)
            {
                Debug.Log("[NaturalEventDirector] Cooldown finished! Resuming hazard search...");
                isWaitingForSearch = false;
            }
            return;
        }

        // 2. Normal Risk Calculation
        float riskIncreaseRate = currentProfile.targetTimeRiskValue / currentProfile.timeToTargetRisk;
        currentTimeRisk += riskIncreaseRate * dt;

        mapRisk = GetRiskAtPlayerPosition();
        totalRisk = currentTimeRisk + mapRisk;

        if (totalRisk >= 1.0f)
        {
            PickAndTriggerHazard(currentProfile);
        }
    }

    private float GetRiskAtPlayerPosition()
    {
        if (worldRiskMap == null) return 0f;

        int gridX = Mathf.FloorToInt(playerTransform.position.x / gridScale);
        int gridZ = Mathf.FloorToInt(playerTransform.position.z / gridScale);

        if (gridX >= 0 && gridX < worldRiskMap.GetLength(0) &&
            gridZ >= 0 && gridZ < worldRiskMap.GetLength(1))
        {
            return worldRiskMap[gridX, gridZ];
        }

        return 0f;
    }

    private void PickAndTriggerHazard(HazardLevelProfile currentProfile)
    {
        float totalWeight = currentProfile.landslideWeight + currentProfile.tornadoWeight;
        if (totalWeight <= 0) return;

        float roll = Random.Range(0f, totalWeight);

        if (roll <= currentProfile.landslideWeight)
        {
            Debug.Log($"[NaturalEventDirector] Searching for a Landslide Cliff...");

            // Pass the current profile down to the search algorithm!
            Transform cliffAnchor = FindProceduralCliffAnchor(currentProfile);

            if (cliffAnchor != null)
            {
                Debug.Log($"[NaturalEventDirector] Spawning Landslide!");
                currentTimeRisk = 0f;
                OnLandslideTriggered?.Invoke(cliffAnchor, currentProfile.targetLevel);
                _eventBus ??= ServiceContainer.Instance.TryGet<IEventBus>();
                _eventBus?.Publish(new NaturalDisasterEvent(NaturalDisasterEvent.DisasterType.Landslide));
            }
            else
            {
                Debug.Log($"[NaturalEventDirector] Search failed. Waiting {failedSearchWaitTime} seconds. Risk is preserved.");
                isWaitingForSearch = true;
                currentWaitTimer = failedSearchWaitTime;
            }
        }
        else
        {
            Debug.Log($"[NaturalEventDirector] Spawning Tornado!");
            currentTimeRisk = 0f;
            OnTornadoTriggered?.Invoke(playerTransform, currentProfile.targetLevel);
            _eventBus ??= ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Publish(new NaturalDisasterEvent(NaturalDisasterEvent.DisasterType.Tornado));
        }
    }

    // --- UPDATED ALGORITHM: Uses Profile Settings ---
    private Transform FindProceduralCliffAnchor(HazardLevelProfile profile)
    {
        // 1. Try the Initial Radius
        Transform foundCliff = SearchAreaForCliff(profile.initialSearchRadius, profile.minCliffHeightOffset);

        if (foundCliff != null)
        {
            return foundCliff;
        }

        // 2. If it fails, expand to the Expanded Radius
        Debug.Log($"[NaturalEventDirector] No cliff at {profile.initialSearchRadius}m. Expanding to {profile.expandedSearchRadius}m...");
        foundCliff = SearchAreaForCliff(profile.expandedSearchRadius, profile.minCliffHeightOffset);

        if (foundCliff != null)
        {
            return foundCliff;
        }

        // 3. Return null to trigger the cooldown timer
        Debug.Log($"[NaturalEventDirector] No cliffs found within {profile.expandedSearchRadius}m.");
        return null;
    }

    // Notice this now accepts the radius AND the height requirement as parameters
    private Transform SearchAreaForCliff(float searchRadiusInMeters, float requiredHeightOffset)
    {
        int searchRadius = Mathf.CeilToInt(searchRadiusInMeters);
        int playerX = Mathf.FloorToInt(playerTransform.position.x);
        int playerZ = Mathf.FloorToInt(playerTransform.position.z);

        // --- NEW: Track only the absolute highest valid cliff ---
        Vector3 highestCliffPosition = Vector3.zero;
        float highestValidY = float.MinValue;

        int step = 2;

        for (int x = -searchRadius; x <= searchRadius; x += step)
        {
            for (int z = -searchRadius; z <= searchRadius; z += step)
            {
                int checkX = playerX + x;
                int checkZ = playerZ + z;

                if (checkX >= 0 && checkX < worldHeightMap.GetLength(0) &&
                    checkZ >= 0 && checkZ < worldHeightMap.GetLength(1))
                {
                    float rawHeight = worldHeightMap[checkX, checkZ];
                    float actualHeight = rawHeight * heightMultiplier;

                    // 1. Is it tall enough?
                    if (actualHeight > playerTransform.position.y + requiredHeightOffset)
                    {
                        // 2. Is it the tallest one we have seen so far?
                        if (actualHeight > highestValidY)
                        {
                            highestValidY = actualHeight; // Crown the new king
                            highestCliffPosition = new Vector3(checkX, actualHeight, checkZ);
                        }
                    }
                }
            }
        }

        // Did we find a winner?
        if (highestValidY != float.MinValue)
        {
            Debug.Log($"[NaturalEventDirector] Selected the ABSOLUTE HIGHEST cliff! Height: {highestValidY}");

            GameObject tempAnchor = new GameObject("ProceduralLandslideAnchor");

            // Set the position to the highest peak
            tempAnchor.transform.position = highestCliffPosition;

            // --- NEW: Rotate the invisible anchor to aim directly at the player! ---
            tempAnchor.transform.LookAt(playerTransform.position);

            Destroy(tempAnchor, 15f);

            return tempAnchor.transform;
        }

        return null;
    }
}