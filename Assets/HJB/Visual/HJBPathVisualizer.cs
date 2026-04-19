using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HJBPathVisualizer : MonoBehaviour
{
    public HJBMeshDataProvider provider;
    public HJBBacktracker backtracker;

    LineRenderer line;

    [Header("Style")]
    public float lineWidth = 2f;
    public Color lineColor = Color.red;

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
    }

    void OnDisable()
    {
        CancelFadeAnimation(false);
    }
}
