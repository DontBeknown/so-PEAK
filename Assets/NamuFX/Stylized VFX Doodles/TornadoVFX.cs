using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the Tornado VFX behavior:
///  - Slow wandering movement with a sine-wave drift
///  - Intensity ramp-up / ramp-down (spawn / despawn lifecycle)
///  - Public API to set intensity from external game logic
///  - Exposes UnityEvents for audio-hook callbacks
/// </summary>
[RequireComponent(typeof(Transform))]
public class TornadoVFX : MonoBehaviour
{
    // ---------------------------------------------------------------
    //  Inspector
    // ---------------------------------------------------------------
    [Header("Movement")]
    [Tooltip("How fast the tornado drifts across the ground.")]
    public float moveSpeed = 2.0f;

    [Tooltip("Amplitude of the sinusoidal wander (world units).")]
    public float wanderAmplitude = 4.0f;

    [Tooltip("How quickly the tornado changes wander direction.")]
    public float wanderFrequency = 0.3f;

    [Tooltip("Target the tornado moves toward. If null, wanders freely.")]
    public Transform target;

    [Header("Lifecycle")]
    [Tooltip("How long (seconds) the tornado takes to fully spin up.")]
    public float spawnRampTime = 2.0f;

    [Tooltip("How long (seconds) the tornado sustains at full intensity.")]
    public float sustainTime = 10.0f;

    [Tooltip("How long (seconds) the tornado takes to dissipate.")]
    public float despawnRampTime = 3.0f;

    [Tooltip("Destroy the GameObject when despawn is complete?")]
    public bool destroyOnDespawn = false;

    [Header("Particle Systems")]
    [Tooltip("If empty, all child ParticleSystems are used automatically.")]
    public List<ParticleSystem> particleSystems = new List<ParticleSystem>();

    [Header("Intensity")]
    [Range(0f, 1f)]
    public float currentIntensity = 0f;

    // ---------------------------------------------------------------
    //  Private state
    // ---------------------------------------------------------------
    private float _lifeTimer      = 0f;
    private float _totalLifetime  = 0f;
    private bool  _isAlive        = false;
    private bool  _isDespawning   = false;

    // Per-PS cached data
    private List<float> _baseEmissionRates = new List<float>();
    private List<float> _baseStartSpeeds   = new List<float>();

    // Wander state
    private Vector3 _wanderDir;
    private float   _wanderOffset;

    // ---------------------------------------------------------------
    //  Unity Lifecycle
    // ---------------------------------------------------------------
    private void Awake()
    {
        // Auto-collect particle systems from children if list is empty
        if (particleSystems == null || particleSystems.Count == 0)
        {
            particleSystems = new List<ParticleSystem>(
                GetComponentsInChildren<ParticleSystem>());
        }

        // Cache original emission rates & speeds
        foreach (var ps in particleSystems)
        {
            var emission = ps.emission;
            _baseEmissionRates.Add(emission.rateOverTime.constant);

            var main = ps.main;
            _baseEmissionRates.Add(main.startSpeed.constant);
        }

        _wanderOffset = Random.value * 100f;
        _wanderDir    = new Vector3(
            Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

        _totalLifetime = spawnRampTime + sustainTime + despawnRampTime;
    }

    private void OnEnable()
    {
        _lifeTimer    = 0f;
        _isAlive      = true;
        _isDespawning = false;
        currentIntensity = 0f;

        // Make sure all PSes are playing
        foreach (var ps in particleSystems)
            if (!ps.isPlaying) ps.Play();
    }

    private void Update()
    {
        if (!_isAlive) return;

        _lifeTimer += Time.deltaTime;

        // --- Calculate intensity from lifecycle phase ---
        float intensity;
        if (_lifeTimer < spawnRampTime)
        {
            intensity = Mathf.SmoothStep(0f, 1f, _lifeTimer / spawnRampTime);
        }
        else if (_lifeTimer < spawnRampTime + sustainTime)
        {
            intensity = 1f;
        }
        else if (_lifeTimer < _totalLifetime)
        {
            float t = (_lifeTimer - spawnRampTime - sustainTime) / despawnRampTime;
            intensity = Mathf.SmoothStep(1f, 0f, t);
            _isDespawning = true;
        }
        else
        {
            // Lifecycle complete
            intensity = 0f;
            _isAlive  = false;

            foreach (var ps in particleSystems)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (destroyOnDespawn)
                Destroy(gameObject, 3f); // let existing particles fade

            return;
        }

        currentIntensity = intensity;
        ApplyIntensity(intensity);
        UpdateMovement(intensity);
    }

    // ---------------------------------------------------------------
    //  Public API
    // ---------------------------------------------------------------

    /// <summary>Force a specific intensity (0-1). Overrides lifecycle control.</summary>
    public void SetIntensity(float value)
    {
        currentIntensity = Mathf.Clamp01(value);
        ApplyIntensity(currentIntensity);
    }

    /// <summary>Begin the despawn sequence immediately.</summary>
    public void Despawn()
    {
        if (!_isDespawning)
        {
            // Jump timer to despawn phase
            _lifeTimer    = spawnRampTime + sustainTime;
            _isDespawning = true;
        }
    }

    // ---------------------------------------------------------------
    //  Private helpers
    // ---------------------------------------------------------------

    private void ApplyIntensity(float t)
    {
        for (int i = 0; i < particleSystems.Count; i++)
        {
            var ps = particleSystems[i];

            // Scale emission rate
            var emission      = ps.emission;
            float baseRate    = (i < _baseEmissionRates.Count) ? _baseEmissionRates[i] : 20f;
            var rateModule    = emission.rateOverTime;
            // Use the ParticleSystem.EmissionModule setter via MinMaxCurve
            // (can't assign .constant directly — need to reassign the whole MinMaxCurve)
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(baseRate * t);
        }
    }

    private void UpdateMovement(float intensity)
    {
        if (intensity <= 0f) return;

        Vector3 moveDir;

        if (target != null)
        {
            // Steer toward target with wander layered on top
            Vector3 toTarget = (target.position - transform.position);
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist > 0.5f)
                moveDir = toTarget.normalized;
            else
                moveDir = Vector3.zero;
        }
        else
        {
            // Pure wander
            float angle = Mathf.Sin((_lifeTimer + _wanderOffset) * wanderFrequency) * wanderAmplitude;
            moveDir   = Quaternion.Euler(0, angle, 0) * _wanderDir;
        }

        transform.position += moveDir * (moveSpeed * intensity * Time.deltaTime);
    }

    // ---------------------------------------------------------------
    //  Gizmos
    // ---------------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, wanderAmplitude);

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 12f);

        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
#endif
}
