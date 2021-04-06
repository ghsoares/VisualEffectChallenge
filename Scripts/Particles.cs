using System;
using System.Collections.Generic;
using ExtensionMethods.DictionaryExtensions;
using Godot;

public class Particle
{
    public int idx { get; set; }

    public Vector3 prevPosition {get; set;}
    public Vector3 position { get; set; }
    public Vector3 size { get; set; }
    public Vector3 rotation { get; set; }
    public float gravityScale { get; set; }

    public Vector3 velocity { get; set; }
    public float lifetime { get; set; }
    public float life { get; set; }
    public Color color { get; set; }
    public bool persistent { get; set; }

    public Vector3 startSize { get; set; }
    public Color startColor { get; set; }

    public Dictionary<String, object> customData { get; set; }

    public bool alive { get; set; }

    public Transform transform
    {
        get
        {
            Transform t = Transform.Identity;

            t.basis = new Basis(rotation);
            t = t.Scaled(size);
            t.origin = position;

            return t;
        }
    }
}
public class Particles : MultiMeshInstance
{
    public enum UpdateMode
    {
        Process,
        PhysicsProcess
    }

    protected RigidBody rigidbody { get; private set; }

    public Particle[] particles { get; private set; }
    public int aliveParticles { get; private set; }
    public Vector3 prevPos { get; private set; }
    public Vector3 currentVelocity { get; private set; }
    public float currentVelocityLen { get; private set; }
    public Vector3 externalForces { get; private set; }

    public float velocityMultiply = 1f;

    [Export] public bool emitting = true;
    [Export] public bool local = false;
    [Export] public int numParticles = 1024;
    [Export] public float lifetime = 1f;
    [Export] public Color color = Colors.White;
    [Export] public Vector3 gravity = Vector3.Down * 98f;
    [Export] public float timeScale = 1f;
    [Export] public UpdateMode updateMode = UpdateMode.PhysicsProcess;
    [Export] public Mesh mesh;
    [Export] public Texture texture;
    [Export] public Curve sizeOverLife;
    [Export] public Gradient colorOverLife;

    public virtual void ResetMultimesh()
    {
        if (mesh == null)
        {
            Multimesh = null;
            return;
        }

        Multimesh = new MultiMesh();
        Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3d;
        Multimesh.ColorFormat = MultiMesh.ColorFormatEnum.Float;
        Multimesh.Mesh = mesh;
        Multimesh.InstanceCount = numParticles;
    }

    public virtual void ResetParticles()
    {
        aliveParticles = 0;
        particles = new Particle[numParticles];

        for (int i = 0; i < numParticles; i++)
        {
            Particle part = new Particle();
            part.customData = new Dictionary<string, object>();
            part.idx = i;
            particles[i] = part;
        }
    }

    public virtual void ResetIfNeeded()
    {
        if (particles == null || particles.Length != numParticles)
        {
            ResetParticles();
        }

        if (Multimesh == null || Multimesh.InstanceCount != numParticles || Multimesh.Mesh != mesh)
        {
            ResetMultimesh();
        }
    }

    public override void _Ready()
    {
        Node parent = GetParent();
        while (parent != null && !(parent is RigidBody))
        {
            parent = parent.GetParent();
        }
        if (parent != null)
        {
            rigidbody = parent as RigidBody;
        }

        ResetParticles();
        ResetMultimesh();
    }

    public void AddForce(Vector3 force)
    {
        externalForces += force;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (updateMode == UpdateMode.PhysicsProcess)
        {
            ResetIfNeeded();
            UpdateSystem(delta * Mathf.Max(timeScale, 0f));
        }
    }

    public override void _Process(float delta)
    {
        if (updateMode == UpdateMode.PhysicsProcess)
        {
            ResetIfNeeded();
            UpdateSystem(delta * Mathf.Max(timeScale, 0f));
        }
        Draw();
    }

    public virtual void EmitParticle(Dictionary<String, object> overrideParams = null, bool update = true)
    {
        if (!emitting) return;
        if (overrideParams == null) overrideParams = new Dictionary<string, object>();
        for (int i = 0; i < numParticles; i++)
        {
            Particle particle = particles[i];
            if (!particle.alive)
            {
                aliveParticles += 1;
                InitParticle(particle, overrideParams);
                particle.lifetime = particle.life;
                particle.startSize = particle.size;
                particle.startColor = particle.color;
                particle.prevPosition = particle.position;
                if (update)
                {
                    UpdateParticle(particle, 0f);
                }
                break;
            }
        }
    }

    protected virtual void UpdateSystem(float delta)
    {
        if (rigidbody != null)
        {
            currentVelocity = rigidbody.LinearVelocity;
        }
        else
        {
            currentVelocity = (GlobalTransform.origin - prevPos) / delta;
        }
        currentVelocityLen = currentVelocity.Length();

        for (int i = 0; i < numParticles; i++)
        {
            Particle particle = particles[i];
            if (particle.alive)
            {
                UpdateParticle(particle, delta);
                particle.prevPosition = particle.position;
                if (particle.life <= 0f || !particle.alive)
                {
                    DestroyParticle(particle);
                    aliveParticles -= 1;
                    particle.alive = false;
                    particle.life = 0f;
                }
            }
        }

        prevPos = GlobalTransform.origin;
        externalForces = Vector3.Zero;
    }

    protected virtual void InitParticle(Particle particle, Dictionary<String, object> overrideParams)
    {
        particle.customData.Clear();

        particle.alive = true;
        particle.life = (float)overrideParams.Get("lifetime", lifetime);
        particle.persistent = false;

        particle.gravityScale = (float)overrideParams.Get("gravityScale", lifetime);

        if (local)
        {
            particle.position = (Vector3)overrideParams.Get("position", Vector3.Zero);
        }
        else
        {
            particle.position = (Vector3)overrideParams.Get("position", GlobalTransform.origin);
        }

        particle.size = (Vector3)overrideParams.Get("size", Vector3.One);
        particle.rotation = (Vector3)overrideParams.Get("rotation", Vector3.Zero);

        particle.velocity = (Vector3)overrideParams.Get("velocity", Vector3.Zero);

        particle.color = (Color)overrideParams.Get("color", color);
    }

    protected virtual void UpdateParticle(Particle particle, float delta)
    {
        particle.position += particle.velocity * delta;
        particle.velocity += externalForces * delta;
        particle.velocity += gravity * delta * particle.gravityScale;
        if (!particle.persistent)
        {
            particle.life -= delta;
            particle.life = Mathf.Clamp(particle.life, 0f, particle.lifetime);
        }
        float lifeT = particle.life / particle.lifetime;
        if (sizeOverLife != null)
        {
            particle.size = particle.startSize * sizeOverLife.Interpolate(lifeT);
        }
        if (colorOverLife != null)
        {
            particle.color = particle.startColor * colorOverLife.Interpolate(lifeT);
        }
    }

    protected virtual void DestroyParticle(Particle particle) { }

    public virtual void Draw()
    {
        if (Multimesh != null)
        {
            int visibleParticles = 0;
            foreach (Particle particle in particles)
            {
                if (particle.alive)
                {
                    Transform t = particle.transform;

                    if (!local) {
                        t = GlobalTransform.AffineInverse() * t;
                    }

                    Multimesh.SetInstanceTransform(visibleParticles, t);
                    Multimesh.SetInstanceColor(visibleParticles, particle.color);
                    visibleParticles++;
                }
            }

            Multimesh.VisibleInstanceCount = visibleParticles;
        }
    }
}

