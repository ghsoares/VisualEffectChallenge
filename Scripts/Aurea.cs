using Godot;
using System;
using System.Collections.Generic;

public class Aurea : Particles
{
    float currentRate = 0f;
    [Export] public float emitRate = 16f;
    [Export] public float sphereRadius = 4f;
    [Export] public float sphereThickness = .1f;
    [Export] public float centerVelocity = 2f;
    [Export] public Vector2 sizeRange = new Vector2(.01f, .1f);

    protected override void UpdateSystem(float delta)
    {
        base.UpdateSystem(delta);
        currentRate += emitRate * delta;
        while (currentRate >= 1f) {
            EmitParticle();
            currentRate -= 1f;
        }
    }

    protected override void InitParticle(Particle particle, Dictionary<string, object> overrideParams)
    {
        base.InitParticle(particle, overrideParams);

        Vector3 v = new Vector3(
            (float)GD.Randf() * 2f - 1f,
            (float)GD.Randf() * 2f - 1f,
            (float)GD.Randf() * 2f - 1f
        ).Normalized();

        particle.position += v * sphereRadius * (float)GD.RandRange(1f - sphereThickness, 1f);

        particle.size *= (float)GD.RandRange(sizeRange.x, sizeRange.y);
    }

    protected override void UpdateParticle(Particle particle, float delta)
    {
        base.UpdateParticle(particle, delta);

        float deltaT = particle.position.Length();
        deltaT = Mathf.Min(deltaT, centerVelocity * delta);

        particle.position -= particle.position.Normalized() * deltaT;
    }
}
