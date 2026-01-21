using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeatherEffect : MonoBehaviour
{
    public string effectId = "Fog"; // e.g. "Fog", "Snow", "Ash"

    [Min(0.01f)]
    public float fadeDuration = 1f;

    public List<Renderer> renderers = new List<Renderer>();

    public List<ParticleSystem> particleSystems = new List<ParticleSystem>();

    private float currentAlpha = 0f;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        // Auto-fill renderers if none assigned
        if (renderers.Count == 0)
        {
            renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        }

        if (particleSystems.Count == 0)
        {
            particleSystems.AddRange(GetComponentsInChildren<ParticleSystem>(true));
        }

        // Start fully invisible
        SetAlpha(0f);
    }

    public void FadeIn()
    {
        StartFade(1f);
    }

    public void FadeOut()
    {
        StartFade(0f);
    }

    public void SetVisibleInstant(bool visible)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        currentAlpha = visible ? 1f : 0f;
        SetAlpha(currentAlpha);
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = currentAlpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(currentAlpha);
            yield return null;
        }

        currentAlpha = targetAlpha;
        SetAlpha(currentAlpha);
        fadeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        // === RENDERERS (including SpriteRenderer) ===
        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            // 1) Special case: SpriteRenderer in 2D
            SpriteRenderer spriteRend = rend as SpriteRenderer;
            if (spriteRend != null)
            {
                Color c = spriteRend.color;
                c.a = alpha;
                spriteRend.color = c;
                continue; // done with this renderer
            }

            // 2) Generic Renderer: use MaterialPropertyBlock
            var block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);

            var mat = rend.sharedMaterial;
            if (mat != null)
            {
                // Standard/Built-in shaders usually use "_Color"
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.GetColor("_Color");
                    c.a = alpha;
                    block.SetColor("_Color", c);
                }
                // URP Lit / Sprite Lit often use "_BaseColor"
                else if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    block.SetColor("_BaseColor", c);
                }
            }

            rend.SetPropertyBlock(block);
        }

        // === PARTICLE SYSTEMS (optional) ===
        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;

            var main = ps.main;
            var startColor = main.startColor;

            if (startColor.mode == ParticleSystemGradientMode.Color)
            {
                Color c = startColor.color;
                c.a = alpha;
                main.startColor = c;
            }
        }
    }

    //if (Mathf.Approximately(alpha, 0f))
    //gameObject.SetActive(false);
    //else if (!gameObject.activeSelf)
    //gameObject.SetActive(true);
}
