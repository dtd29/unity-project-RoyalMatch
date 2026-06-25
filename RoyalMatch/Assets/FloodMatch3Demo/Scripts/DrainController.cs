using System.Collections.Generic;
using UnityEngine;

public class DrainController : MonoBehaviour
{
    [Header("V21 Legacy Effects")]
    public bool useLegacyCyanEffects = false;
    [Header("Top Inflow")]
    public ParticleSystem leftInflowParticles;
    public ParticleSystem rightInflowParticles;
    public Renderer leftPipeRenderer;
    public Renderer rightPipeRenderer;
    public Material pipeOnMaterial;
    public Material pipeOffMaterial;

    [Header("Puzzle Hole Drain")]
    public ParticleSystem matchedDrainParticles;
    public GravityWaterStreamEffect gravityWaterStreamEffect;

    [Header("Final Drain")]
    public ParticleSystem finalDrainParticles;
    public Renderer finalDrainRenderer;
    public Material drainOpenMaterial;
    public Material drainClosedMaterial;

    private void Awake()
    {
        EnsureGravityEffect();
    }

    public void ResetDrain()
    {
        EnsureGravityEffect();
        SetOpen(false);
        SetInflowActive(true);
    }

    public void SetInflowActive(bool active)
    {
        SetParticle(leftInflowParticles, active, false);
        SetParticle(rightInflowParticles, active, false);

        if (leftPipeRenderer != null)
            leftPipeRenderer.sharedMaterial = active ? pipeOnMaterial : pipeOffMaterial;

        if (rightPipeRenderer != null)
            rightPipeRenderer.sharedMaterial = active ? pipeOnMaterial : pipeOffMaterial;
    }

    public void SetOpen(bool open)
    {
        if (finalDrainRenderer != null)
            finalDrainRenderer.sharedMaterial = open ? drainOpenMaterial : drainClosedMaterial;

        SetParticle(finalDrainParticles, open, true);
    }

    public void PlayMatchedFlow(List<Vector3> clearedPositions)
    {
        if (!useLegacyCyanEffects)
            return;

        PlayMatchedFlow(clearedPositions, 0.42f);
    }

    public void PlayMatchedFlow(List<Vector3> clearedPositions, float waterTopY)
    {
        if (!useLegacyCyanEffects)
            return;

        EnsureGravityEffect();

        if (clearedPositions == null)
            return;

        foreach (Vector3 pos in clearedPositions)
        {
            if (gravityWaterStreamEffect != null)
                gravityWaterStreamEffect.PlayFlowToHole(pos, waterTopY);

            if (matchedDrainParticles != null)
            {
                matchedDrainParticles.transform.position = pos + Vector3.back * 0.5f;
                matchedDrainParticles.Emit(10);
            }
        }
    }

    public void PlayFinalDrainPathFlow(List<Vector3> pathPositions)
    {
        if (!useLegacyCyanEffects)
        {
            SetOpen(true);
            return;
        }

        EnsureGravityEffect();

        if (pathPositions != null)
        {
            float topY = 0.42f;

            WaterController water = FindFirstObjectByType<WaterController>();
            if (water != null)
                topY = water.CurrentTopY;

            if (gravityWaterStreamEffect != null)
                gravityWaterStreamEffect.PlayFlowPath(pathPositions, topY);

            if (matchedDrainParticles != null)
            {
                foreach (Vector3 pos in pathPositions)
                {
                    matchedDrainParticles.transform.position = pos + Vector3.back * 0.5f;
                    matchedDrainParticles.Emit(18);
                }
            }
        }

        SetOpen(true);
    }

    private void SetParticle(ParticleSystem ps, bool active, bool clearWhenStop)
    {
        if (ps == null)
            return;

        if (active)
        {
            if (!ps.isPlaying)
                ps.Play();
        }
        else
        {
            ps.Stop(true, clearWhenStop ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void EnsureGravityEffect()
    {
        if (gravityWaterStreamEffect != null)
            return;

        gravityWaterStreamEffect = GetComponent<GravityWaterStreamEffect>();
        if (gravityWaterStreamEffect == null)
            gravityWaterStreamEffect = gameObject.AddComponent<GravityWaterStreamEffect>();
    }
}
