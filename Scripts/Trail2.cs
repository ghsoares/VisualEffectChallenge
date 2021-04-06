using Godot;
using System;
using System.Collections.Generic;

public class Trail2 : Particles
{
    float currentRate = 0f;
    [Export] public float emitRate = 16f;
    [Export] public float circleRadius = 4f;
    [Export] public float circleThickness = .1f;
    [Export] public float initialVerticalVelocity = 4f;
    [Export] public Vector2 initialOuterVelocityRange = Vector2.Zero;
    [Export] public float orbitalVelocity = 8f;
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
        particle.velocity = Vector3.Up * initialVerticalVelocity;

        Vector2 c = Vector2.Right.Rotated(GD.Randf() * Mathf.Pi * 2f);
        c *= (float)GD.RandRange(1f - circleThickness, 1f) * circleRadius;

        particle.velocity += new Vector3(c.x, 0f, c.y).Normalized() * (float)GD.RandRange(initialOuterVelocityRange.x, initialOuterVelocityRange.y);

        particle.position += new Vector3(c.x, 0f, c.y);
        particle.size *= (float)GD.RandRange(sizeRange.x, sizeRange.y);
    }

    protected override void UpdateParticle(Particle particle, float delta)
    {
        base.UpdateParticle(particle, delta);
        Vector3 pos = particle.position;
        Vector2 pos2D = new Vector2(pos.x, pos.z);

        pos2D = pos2D.Rotated(Mathf.Deg2Rad(orbitalVelocity) * delta);

        pos.x = pos2D.x;
        pos.z = pos2D.y;
        particle.position = pos;
    }
}
