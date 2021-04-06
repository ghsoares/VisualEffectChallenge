using Godot;
using System;
using System.Collections.Generic;

public class Trail : Particles
{
    ImmediateGeometry geometry;
    float currentRate = 0f;
    Vector3 cameraPos;

    [Export] public float emitRate = 16f;
    [Export] public int trailSize = 4;
    [Export] public float trailLength = 2f;
    [Export] public ShaderMaterial trailMaterial;
    [Export] public float sphereRadius = 1f;
    [Export] public Vector2 initialPosMarginRange = new Vector2(.5f, 1f);
    [Export] public float verticalVelocity = 1f;
    [Export] public Vector2 initialOuterVelocityRange = Vector2.Zero;
    [Export] public float centerVelocity = 2f;
    [Export] public float orbitalVelocity = 8f;
    [Export] public Vector2 sizeRange = new Vector2(.01f, .1f);

    public override void _Ready()
    {
        base._Ready();
        geometry = new ImmediateGeometry();
        geometry.MaterialOverride = trailMaterial;
        AddChild(geometry);
    }

    protected override void UpdateSystem(float delta)
    {
        base.UpdateSystem(delta);
        cameraPos = GetViewport().GetCamera().GlobalTransform.origin;
        currentRate += emitRate * delta;
        while (currentRate >= 1f) {
            EmitParticle();
            currentRate -= 1f;
        }
    }

    protected override void InitParticle(Particle particle, Dictionary<string, object> overrideParams)
    {
        base.InitParticle(particle, overrideParams);

        Vector2 c = Vector2.Right.Rotated(GD.Randf() * Mathf.Pi * 2f) * sphereRadius;
        c += c.Normalized() * (float)GD.RandRange(initialPosMarginRange.x, initialPosMarginRange.y);
        //c *= (float)GD.RandRange(1f - circleThickness, 1f) * sphereRadius;

        particle.velocity += new Vector3(c.x, 0f, c.y).Normalized() * (float)GD.RandRange(initialOuterVelocityRange.x, initialOuterVelocityRange.y);

        particle.position += new Vector3(c.x, 0f, c.y);
        particle.size *= (float)GD.RandRange(sizeRange.x, sizeRange.y);

        Transform[] trail = new Transform[trailSize];
        for (int i = 0; i < trailSize; i++) {
            trail[i] = particle.transform;
        }
        particle.customData["trail"] = trail;
    }

    protected override void UpdateParticle(Particle particle, float delta)
    {
        base.UpdateParticle(particle, delta);
        Vector3 pos = particle.position;
        Vector2 pos2D = new Vector2(pos.x, pos.z);

        pos2D -= pos2D.Normalized() * centerVelocity * delta;

        if (pos2D.Length() <= sphereRadius) {
            pos2D = pos2D.Rotated(Mathf.Deg2Rad(orbitalVelocity) * delta);
            float s = Mathf.Sin((particle.position.y / sphereRadius + .5f) * Mathf.Pi * .5f);
            pos2D = pos2D.Normalized() * sphereRadius * s;
            pos.y += verticalVelocity * delta;
        }

        pos.x = pos2D.x;
        pos.z = pos2D.y;

        particle.position = pos;

        Transform[] trail = particle.customData["trail"] as Transform[];
        float particleDeltaLen = (particle.position - particle.prevPosition).Length();

        float segmentSpacing = trailLength / trailSize;
        float deltaT = Mathf.Clamp(particleDeltaLen / segmentSpacing, 0f, 1f);

        for (int i = trailSize - 1; i > 0; i--) {
            Transform curr = trail[i];
            Transform nxt = trail[i-1];

            curr = curr.InterpolateWith(nxt, deltaT);

            trail[i] = curr;
        }

        trail[0] = particle.transform;
    }

    public override void Draw() {
        base.Draw();

        geometry.Clear();
        geometry.Begin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < numParticles; i++) {
            Particle particle = particles[i];
            if (!particle.alive) continue;

            geometry.SetColor(particle.color);
            DrawTrail(particle.customData["trail"] as Transform[]);
        }

        geometry.End();
    }

    private void DrawTrail(Transform[] trail) {
        for (int i = 0; i < trailSize-1; i++) {
            Transform curr = trail[i];
            Transform next = trail[i+1];

            Vector3 currPointPos = curr.origin;
            Vector3 nextPointPos = next.origin;

            //(point[i] - point[i-1]).cross(cameraPosition - point[i]).normalized() * (thickness / 2);
            Vector3 right = (nextPointPos - currPointPos).Cross(cameraPos - nextPointPos).Normalized();
            Vector3 currPointRight = right;
            Vector3 nextPointRight = right;

            Vector3 currPointUp = (cameraPos - currPointPos).Normalized();
            Vector3 nextPointUp = (cameraPos - nextPointPos).Normalized();
            
            float currUVX = (i + 0) / (float)(trailSize - 1f);
            float nextUVX = (i + 1) / (float)(trailSize - 1f);

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

            for (int j = 0; j < 6; j++) {
                geometry.SetNormal(normals[j]);
                geometry.SetUv(uvs[j]);
                geometry.AddVertex(vertices[j]);
            }
        }
    }
}
