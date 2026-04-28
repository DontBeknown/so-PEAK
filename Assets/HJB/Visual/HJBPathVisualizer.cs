using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(LineRenderer))]
public class HJBPathVisualizer : MonoBehaviour
{
    public HJBMeshDataProvider provider;
    public HJBBacktracker backtracker;

    LineRenderer line;

    [Header("Style")]
    public float lineWidth = 2f;
    public Color lineColor = Color.red;

    [Header("Spark VFX")]
    [SerializeField] GameObject sparkVFXPrefab;

    [Header("Footstep VFX")]
    [SerializeField] float stepSpacing = 1.5f;
    [SerializeField] float stepSpacingJitter = 0.25f;
    [SerializeField] float stepLateralOffset = 0.3f;
    [SerializeField] Vector2 stepStartSizeRange = new Vector2(0.8f, 1.2f);
    [SerializeField] bool alignToPathDirection = true;
    [SerializeField] float groundOffset = 0.05f;

    [Header("Pooling")]
    [SerializeField] int poolDefaultCapacity = 32;
    [SerializeField] int poolMaxSize = 256;

    ObjectPool<GameObject> stepPool;
    Transform stepVFXParent;
    List<GameObject> activeStepInstances = new List<GameObject>();
    List<ParticleSystem> activeSparkSystems = new List<ParticleSystem>();

    Sequence activeFadeSequence;
    float visibleAlpha = 1f;

    void Awake()
    {
        line = GetComponent<LineRenderer>();

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.material =
            new Material(
                Shader.Find("Sprites/Default"));

        line.startColor = lineColor;
        line.endColor = lineColor;

        visibleAlpha = Mathf.Clamp01(lineColor.a);
        if (visibleAlpha <= Mathf.Epsilon)
        {
            visibleAlpha = 1f;
        }

        line.positionCount = 0;
    }

    public void DrawPath(List<Vector2Int> gridPath)
    {
        if (gridPath == null ||
            gridPath.Count == 0)
            return;

        line.positionCount =
            gridPath.Count;

        for (int i = 0; i < gridPath.Count; i++)
        {
            Vector2Int g = gridPath[i];

            Vector3 world =
                provider.GridToWorld(
                    g.x, g.y);
            world.y += 1.0f;
            line.SetPosition(i, world);
        }
    }

    public void DrawPathWorld(List<Vector3> worldPath)
    {
        if (worldPath == null ||
            worldPath.Count == 0)
            return;

        line.positionCount =
            worldPath.Count;

        for (int i = 0; i < worldPath.Count; i++)
        {
            line.SetPosition(i, worldPath[i]);
        }

        SpawnSparksAlongPath(worldPath);
    }

    public Sequence AnimatePathFadeInOut(float fadeInDuration, float displayDuration, float fadeOutDuration)
    {
        if (line == null || line.positionCount == 0)
        {
            return null;
        }

        CancelFadeAnimation(false);

        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        displayDuration = Mathf.Max(0f, displayDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);

        SetLineNormalizedAlpha(0f);

        activeFadeSequence = DOTween.Sequence();

        if (fadeInDuration > 0f)
        {
            activeFadeSequence.Append(FadeLineAlpha(1f, fadeInDuration, Ease.OutSine));
        }
        else
        {
            SetLineNormalizedAlpha(1f);
        }

        if (displayDuration > 0f)
        {
            activeFadeSequence.AppendInterval(displayDuration);
        }

        if (fadeOutDuration > 0f)
        {
            activeFadeSequence.Append(FadeLineAlpha(0f, fadeOutDuration, Ease.InSine));
        }
        else
        {
            SetLineNormalizedAlpha(0f);
        }

        activeFadeSequence.OnKill(() => activeFadeSequence = null);
        return activeFadeSequence;
    }

    public Sequence ShowPathWithFade(float fadeInDuration)
    {
        if (line == null || line.positionCount == 0)
        {
            return null;
        }

        CancelFadeAnimation(false);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);

        SetLineNormalizedAlpha(0f);

        if (fadeInDuration <= 0f)
        {
            SetLineNormalizedAlpha(1f);
            return null;
        }

        activeFadeSequence = DOTween.Sequence();
        activeFadeSequence.Append(FadeLineAlpha(1f, fadeInDuration, Ease.OutSine));
        activeFadeSequence.OnKill(() => activeFadeSequence = null);
        return activeFadeSequence;
    }

    public Sequence HidePathWithFade(float fadeOutDuration, bool clearAfterFade = true)
    {
        StopSparkEmission();

        if (line == null || line.positionCount == 0)
        {
            return null;
        }

        CancelFadeAnimation(false);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);

        if (fadeOutDuration <= 0f)
        {
            SetLineNormalizedAlpha(0f);
            if (clearAfterFade)
            {
                Clear();
            }
            return null;
        }

        activeFadeSequence = DOTween.Sequence();
        activeFadeSequence.Append(FadeLineAlpha(0f, fadeOutDuration, Ease.InSine));
        if (clearAfterFade)
        {
            activeFadeSequence.AppendCallback(Clear);
        }
        activeFadeSequence.OnKill(() => activeFadeSequence = null);
        return activeFadeSequence;
    }

    public bool HasVisiblePath()
    {
        return line != null && line.positionCount > 0 && line.startColor.a > 0.01f;
    }

    public void CancelFadeAnimation(bool resetToInvisible = true)
    {
        if (activeFadeSequence != null && activeFadeSequence.IsActive())
        {
            activeFadeSequence.Kill();
        }

        if (resetToInvisible)
        {
            SetLineNormalizedAlpha(0f);
        }
    }

    float GetCurrentNormalizedAlpha()
    {
        if (line == null || visibleAlpha <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.Clamp01(line.startColor.a / visibleAlpha);
    }

    Tween FadeLineAlpha(float targetAlpha, float duration, Ease easeType = Ease.Linear)
    {
        return DOTween
            .To(GetCurrentNormalizedAlpha, SetLineNormalizedAlpha, Mathf.Clamp01(targetAlpha), duration)
            .SetEase(easeType);
    }

    void SetLineNormalizedAlpha(float normalizedAlpha)
    {
        if (line == null)
        {
            return;
        }

        Color colorWithAlpha = lineColor;
        colorWithAlpha.a = visibleAlpha * Mathf.Clamp01(normalizedAlpha);

        line.startColor = colorWithAlpha;
        line.endColor = colorWithAlpha;
    }

    public void Clear()
    {
        line.positionCount = 0;
        ReleaseActiveSteps();
    }

    void SpawnSparksAlongPath(List<Vector3> worldPath)
    {
        ClearSparkInstances();
        if (sparkVFXPrefab == null || worldPath == null || worldPath.Count < 2) return;

        int sideFlag = 1;
        float distToNext = SampleNextStepDistance();

        for (int i = 1; i < worldPath.Count; i++)
        {
            Vector3 a = worldPath[i - 1];
            Vector3 b = worldPath[i];
            Vector3 delta = b - a;
            float segLen = delta.magnitude;
            if (segLen <= Mathf.Epsilon) continue;

            Vector3 segDir = delta / segLen;
            Vector3 right = Vector3.Cross(Vector3.up, segDir).normalized;
            Quaternion rot = alignToPathDirection
                ? Quaternion.LookRotation(segDir, Vector3.up)
                : Quaternion.Euler(-90f, 0f, 0f);

            float cursor = 0f;
            while (cursor + distToNext <= segLen)
            {
                cursor += distToNext;
                Vector3 spawnPos = a + segDir * cursor + right * (stepLateralOffset * sideFlag);
                SpawnStepAt(spawnPos, rot);
                sideFlag = -sideFlag;
                distToNext = SampleNextStepDistance();
            }
            distToNext -= (segLen - cursor);
        }
    }

    float SampleNextStepDistance()
    {
        float jitter = Random.Range(-stepSpacingJitter, stepSpacingJitter);
        return Mathf.Max(0.05f, stepSpacing + jitter);
    }

    void SpawnStepAt(Vector3 pos, Quaternion rot)
    {
        EnsurePool();
        pos.y += groundOffset;

        var go = stepPool.Get();
        go.transform.SetPositionAndRotation(pos, rot);
        activeStepInstances.Add(go);

        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(stepStartSizeRange.x, stepStartSizeRange.y);
            ps.Clear(true);
            ps.time = 0f;
            ps.Play(true);
            activeSparkSystems.Add(ps);
        }
    }

    void EnsurePool()
    {
        if (stepPool != null) return;

        if (stepVFXParent == null)
        {
            var parentGo = new GameObject("StepVFX_Pool");
            stepVFXParent = parentGo.transform;
            stepVFXParent.SetParent(transform, false);
        }

        Vector3 parkPos = new Vector3(0f, -100000f, 0f);

        stepPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(sparkVFXPrefab, stepVFXParent);
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                go.transform.position = parkPos;
                return go;
            },
            actionOnGet: go =>
            {
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) ps.Clear(true);
            },
            actionOnRelease: go =>
            {
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                go.transform.position = parkPos;
            },
            actionOnDestroy: go => { if (go != null) Destroy(go); },
            collectionCheck: false,
            defaultCapacity: poolDefaultCapacity,
            maxSize: poolMaxSize);
    }

    void StopSparkEmission()
    {
        foreach (var ps in activeSparkSystems)
            if (ps != null) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    void ClearSparkInstances()
    {
        ReleaseActiveSteps();
    }

    void ReleaseActiveSteps()
    {
        if (stepPool == null)
        {
            activeStepInstances.Clear();
            activeSparkSystems.Clear();
            return;
        }

        for (int i = 0; i < activeStepInstances.Count; i++)
        {
            var go = activeStepInstances[i];
            if (go != null) stepPool.Release(go);
        }
        activeStepInstances.Clear();
        activeSparkSystems.Clear();
    }

    void OnDisable()
    {
        CancelFadeAnimation(false);
        ClearSparkInstances();
    }

    void OnDestroy()
    {
        if (stepPool != null)
        {
            stepPool.Dispose();
            stepPool = null;
        }
    }
}
