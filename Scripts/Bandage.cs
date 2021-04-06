using Godot;
using System;
using System.Collections.Generic;

public class Bandage : Particles
{
    ImmediateGeometry geometry;
    float elapsedTime = 0f;
    Vector3 cameraPos;

    [Export] public ShaderMaterial trailMaterial;
    [Export] public float desiredSpacing = 1f;
    [Export] public float stiffness = 4f;
    [Export] public float drag = 10f;
    [Export] public OpenSimplexNoise windNoise;
    [Export] public Vector3 windNoiseMotion = Vector3.Up * 4f;
    [Export] public Vector2 windSpeedRange = new Vector2(2f, 4f);
    [Export] public Vector2 horizontalWindSpeedRange = new Vector2(.5f, 1f);
    [Export] public float size = .01f;

    public override void _Ready()
    {
        base._Ready();
        geometry = new ImmediateGeometry();
        geometry.MaterialOverride = trailMaterial;
        AddChild(geometry);
    }

    public override void ResetParticles()
    {
        base.ResetParticles();
        for (int i = 0; i < numParticles; i++) {
            EmitParticle();
        }
    }

    protected override void InitParticle(Particle particle, Dictionary<string, object> overrideParams)
    {
        base.InitParticle(particle, overrideParams);
        particle.position += Vector3.Right * particle.idx * desiredSpacing;
        particle.size *= size;
        particle.persistent = true;
    }

    protected override void UpdateSystem(float delta)
    {
        base.UpdateSystem(delta);
        elapsedTime += delta;
        cameraPos = GetViewport().GetCamera().GlobalTransform.origin;
    }

    protected override void UpdateParticle(Particle particle, float delta)
    {
        base.UpdateParticle(particle, delta);
        if (particle.idx > 0) {
            Particle targetParticle = particles[particle.idx - 1];
            Vector3 deltaPos = targetParticle.position - particle.position;
            float deltaLen = deltaPos.Length();
            float force = (desiredSpacing - deltaLen) * -stiffness;
            particle.velocity += deltaPos.Normalized() * force * delta;
        }
        if (particle.idx < numParticles - 1) {
            Particle targetParticle = particles[particle.idx + 1];
            Vector3 deltaPos = targetParticle.position - particle.position;
            float deltaLen = deltaPos.Length();
            float force = (desiredSpacing - deltaLen) * -stiffness;
            particle.velocity += deltaPos.Normalized() * force * delta;
        }
        if (particle.idx == 0) {
            Vector3 deltaPos = Vector3.Zero - particle.position;
            float deltaLen = deltaPos.Length();
            float force = (desiredSpacing - deltaLen) * -stiffness;
            particle.velocity += deltaPos.Normalized() * force * delta;
            particle.position = Vector3.Zero;
        }
        float n = windNoise.GetNoise3dv(particle.position + windNoiseMotion * elapsedTime) * .5f + .5f;
        float hN = windNoise.GetNoise3dv(particle.position + windNoiseMotion * (elapsedTime + 10f));

        Vector2 h = Vector2.Right.Rotated(hN * Mathf.Pi * 2f) * (float)GD.RandRange(horizontalWindSpeedRange.x, horizontalWindSpeedRange.y);

        n = Mathf.Lerp(windSpeedRange.x, windSpeedRange.y, n);
        particle.velocity += new Vector3(h.x, n, h.y) * delta;
        particle.velocity -= particle.velocity * Mathf.Clamp(drag * delta, 0f, 1f);
    }

    public override void Draw() {
        base.Draw();

        geometry.Clear();
        geometry.Begin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < numParticles-1; i++) {
            Particle currParticle = particles[i];
            Particle nextParticle = particles[i+1];

            Transform curr = currParticle.transform;
            Transform next = nextParticle.transform;

            Vector3 currPointPos = curr.origin;
            Vector3 nextPointPos = next.origin;

            //(point[i] - point[i-1]).cross(cameraPosition - point[i]).normalized() * (thickness / 2);
            Vector3 right = (nextPointPos - currPointPos).Cross(cameraPos - nextPointPos).Normalized();
            Vector3 currPointRight = right;
            Vector3 nextPointRight = right;

            Vector3 currPointUp = (cameraPos - currPointPos).Normalized();
            Vector3 nextPointUp = (cameraPos - nextPointPos).Normalized();
            
            float currUVX = (i + 0) / (float)(numParticles - 1f);
            float nextUVX = (i + 1) / (float)(numParticles - 1f);

            float currWidth = curr.basis.x.Length();
            float nextWidth = next.basis.x.Length();

            Vector3[] normals = new Vector3[] {
                currPointUp,
                nextPointUp,
                currPointUp,

                nextPointUp,
                nextPointUp,
                currPointUp
            };
            Vector2[] uvs = new Vector2[] {
                new Vector2(currUVX, 0),
                new Vector2(nextUVX, 0),
                new Vector2(currUVX, 1),

                new Vector2(nextUVX, 0),
                new Vector2(nextUVX, 1),
                new Vector2(currUVX, 1),
            };
            Vector3[] vertices = new Vector3[] {
                currPointPos - currPointRight * currWidth,
                nextPointPos - nextPointRight * nextWidth,
                currPointPos + currPointRight * currWidth,

                nextPointPos - nextPointRight * nextWidth,
                nextPointPos + nextPointRight * nextWidth,
                currPointPos + currPointRight * currWidth
            };
            Color[] colors = new Color[] {
                currParticle.color,
                nextParticle.color,
                currParticle.color,

                nextParticle.color,
                nextParticle.color,
                currParticle.color
            };

            for (int j = 0; j < 6; j++) {
                geometry.SetColor(colors[j]);
                geometry.SetNormal(normals[j]);
                geometry.SetUv(uvs[j]);
                geometry.AddVertex(vertices[j]);
            }
        }

        geometry.End();
    }
}
