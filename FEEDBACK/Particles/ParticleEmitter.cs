// Bounded reusable slots. The owning canvas explicitly advances/draws this helper only while active.
using Godot;
using System;
public sealed class ParticleEmitter
{
    private struct Particle
    {
        public Vector2 Position,Velocity;
        public float Age,Life,Phase;
        public Color Colour;
        public ParticleValues Values;
    }
    private Particle[] _particles;
    private int _next;
    public int Capacity { get; }
    public int ActiveCount { get; private set; }
    public bool Active => ActiveCount>0;
    // Storage stays lazy, including for never-hit and foliage-free trees; hard cap bounds every update/draw.
    // =========================================================
    public ParticleEmitter(int capacity=24) { Capacity=Math.Clamp(capacity,1,256); }
    // One bounded burst. Positions/velocities are in canvas coordinates; only the spawn region is transformed.
    // Round-robin replacement retains the existing tree policy; per-spawn appearance never mutates the profile.
    // =========================================================
    public void EmitBurst(ParticleValues values,int count,Rect2 region,Transform2D spawnTransform,Vector2 direction,Color colour,RandomNumberGenerator random)
    {
        count=Math.Clamp(count,0,Capacity);
        if (count==0 || !values.IsValid || random==null || !region.Position.IsFinite() || !region.Size.IsFinite()
            || !spawnTransform.Origin.IsFinite() || !spawnTransform.X.IsFinite() || !spawnTransform.Y.IsFinite()) return;
        _particles ??= new Particle[Capacity];
        direction=direction.IsFinite() ? direction.Clamp(-Vector2.One,Vector2.One) : Vector2.Zero;
        if (!float.IsFinite(colour.R) || !float.IsFinite(colour.G) || !float.IsFinite(colour.B) || !float.IsFinite(colour.A)) colour=Colors.White;
        for(int i=0;i<count;i++)
        {
            Vector2 pixel=region.Position+region.Size*new Vector2(random.Randf(),random.Randf());
            Vector2 launch=values.SpreadDegrees>0 ? direction.Rotated(Mathf.DegToRad(random.RandfRange(-values.SpreadDegrees,values.SpreadDegrees))) : direction;
            ref Particle particle=ref _particles[_next];
            if (particle.Life<=0 || particle.Age>=particle.Life) ActiveCount++;
            particle=new Particle {
                Position=spawnTransform*pixel,
                Velocity=new Vector2(random.RandfRange(values.VelocityMin.X,values.VelocityMax.X),random.RandfRange(values.VelocityMin.Y,values.VelocityMax.Y))
                    +launch*values.DirectionSpeed,
                Life=random.RandfRange(values.LifetimeMin,values.LifetimeMax),Phase=random.RandfRange(0,Mathf.Tau),Colour=colour,Values=values
            };
            _next=(_next+1)%Capacity;
        }
    }
    // Wind is injected by the adapter once per frame, never found through scene searches here.
    // =========================================================
    public void Advance(float delta,Vector2 wind)
    {
        if (!Active || !float.IsFinite(delta) || delta<=0) return;
        if (!wind.IsFinite()) wind=Vector2.Zero;
        for(int i=0;i<Capacity;i++)
        {
            ref Particle p=ref _particles[i];
            if (p.Life<=0 || p.Age>=p.Life) continue;
            p.Age+=delta;
            p.Position+=(p.Velocity+wind*p.Values.WindMultiplier+new Vector2(Mathf.Sin(p.Age*p.Values.FlutterFrequency+p.Phase)*p.Values.FlutterAmplitude,0))*delta
                +p.Values.Acceleration*(0.5f*delta*delta);
            p.Velocity+=p.Values.Acceleration*delta;
            if (p.Age>=p.Life) ActiveCount--;
        }
    }
    // The owner calls this from _Draw only; all particle fields, including colour, are burst snapshots.
    // =========================================================
    public void Draw(Node2D canvas,bool worldSpace=false)
    {
        if (_particles==null) return;
        Transform2D worldToLocal=worldSpace ? canvas.GlobalTransform.AffineInverse() : Transform2D.Identity;
        for(int i=0;i<Capacity;i++)
        {
            ref Particle p=ref _particles[i];
            if (p.Life<=0 || p.Age>=p.Life) continue;
            float fade=Mathf.Clamp((p.Life-p.Age)/p.Values.FadeSeconds,0,1);
            float stretch=p.Values.StretchMin+Mathf.Abs(Mathf.Sin(p.Age*p.Values.StretchFrequency))*(1-p.Values.StretchMin);
            float angle=p.Phase+p.Age*p.Values.Spin;
            Vector2 scale=new Vector2(1,stretch)*p.Values.Size;
            if (worldSpace)
            {
                var pose=new Transform2D(angle,p.Position); pose.X*=scale.X; pose.Y*=scale.Y;
                canvas.DrawSetTransformMatrix(worldToLocal*pose);
            }
            else canvas.DrawSetTransform(p.Position,angle,scale);
            ParticleShapes.Draw(canvas,p.Values.Shape,new Color(p.Colour,p.Colour.A*fade));
        }
        canvas.DrawSetTransform(Vector2.Zero);
    }
    // Clear without allocating; owner queues one redraw to remove the last canvas commands, then sleeps.
    // =========================================================
    public void Clear() { if (_particles!=null) Array.Clear(_particles); ActiveCount=0; _next=0; }
    // Optional diagnostics/test snapshot; no exposed mutable slots or extra processing.
    // =========================================================
    public bool TryGetParticle(int index,out Vector2 position,out Color colour)
    {
        position=Vector2.Zero; colour=default;
        if (_particles==null || index<0 || index>=Capacity) return false;
        ref Particle p=ref _particles[index];
        if (p.Life<=0 || p.Age>=p.Life) return false;
        position=p.Position; colour=p.Colour; return true;
    }
}
