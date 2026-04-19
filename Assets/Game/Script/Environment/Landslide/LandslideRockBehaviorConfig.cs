using UnityEngine;

namespace Game.Environment.Landslide
{
    public readonly struct LandslideRockBehaviorConfig
    {
        public readonly LayerMask DamageLayers;
        public readonly LayerMask DecalSpawnLayers;
        public readonly float MinImpactDamage;
        public readonly float MaxImpactDamage;
        public readonly float MinDamageVelocity;
        public readonly float MaxDamageVelocity;
        public readonly float DamageMultiplier;
        public readonly float DecalScaleMultiplier;
        public readonly GameObject DecalProjectorPrefab;
        public readonly GameObject ImpactFxPrefab;
        public readonly Material[] DecalMaterials;
        public readonly float ImpactDecalRevealDuration;
        public readonly float ImpactDecalHoldDuration;
        public readonly float ImpactDecalFadeDuration;
        public readonly float ImpactDecalSpawnDelay;
        public readonly string ImpactDecalSoundId;
        public readonly float ImpactDecalSoundVolumeScale;
        public readonly float DecalSurfaceOffset;
        public readonly float PushImpulse;
        public readonly float HitCooldownSeconds;
        public readonly float RecycleAfterSeconds;
        public readonly float SleepRecycleDelaySeconds;

        public LandslideRockBehaviorConfig(
            LayerMask damageLayers,
            LayerMask decalSpawnLayers,
            float minImpactDamage,
            float maxImpactDamage,
            float minDamageVelocity,
            float maxDamageVelocity,
            float damageMultiplier,
            float decalScaleMultiplier,
            GameObject decalProjectorPrefab,
            GameObject impactFxPrefab,
            Material[] decalMaterials,
            float impactDecalRevealDuration,
            float impactDecalHoldDuration,
            float impactDecalFadeDuration,
            float impactDecalSpawnDelay,
            string impactDecalSoundId,
            float impactDecalSoundVolumeScale,
            float decalSurfaceOffset,
            float pushImpulse,
            float hitCooldownSeconds,
            float recycleAfterSeconds,
            float sleepRecycleDelaySeconds)
        {
            DamageLayers = damageLayers;
            DecalSpawnLayers = decalSpawnLayers;
            MinImpactDamage = minImpactDamage;
            MaxImpactDamage = maxImpactDamage;
            MinDamageVelocity = minDamageVelocity;
            MaxDamageVelocity = maxDamageVelocity;
            DamageMultiplier = damageMultiplier;
            DecalScaleMultiplier = decalScaleMultiplier;
            DecalProjectorPrefab = decalProjectorPrefab;
            ImpactFxPrefab = impactFxPrefab;
            DecalMaterials = decalMaterials;
            ImpactDecalRevealDuration = impactDecalRevealDuration;
            ImpactDecalHoldDuration = impactDecalHoldDuration;
            ImpactDecalFadeDuration = impactDecalFadeDuration;
            ImpactDecalSpawnDelay = impactDecalSpawnDelay;
            ImpactDecalSoundId = impactDecalSoundId;
            ImpactDecalSoundVolumeScale = impactDecalSoundVolumeScale;
            DecalSurfaceOffset = decalSurfaceOffset;
            PushImpulse = pushImpulse;
            HitCooldownSeconds = hitCooldownSeconds;
            RecycleAfterSeconds = recycleAfterSeconds;
            SleepRecycleDelaySeconds = sleepRecycleDelaySeconds;
        }
    }
}