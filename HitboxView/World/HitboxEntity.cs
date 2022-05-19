using Blish_HUD;
using Blish_HUD.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ideka.HitboxView
{
    public class HitboxEntity : IEntity, IDisposable
    {
        private static readonly TimeSpan SmoothingCompensation = TimeSpan.FromMilliseconds(100 / 6);

        public float DrawOrder => 100;  // TODO: Figure out what to set this to.

        public bool Smoothing { get; set; } = true;

        private Color _color = Color.White;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                if (_effect != null)
                    _effect.DiffuseColor = _color.ToVector3();
            }
        }

        private TimeSpan _delay;
        public TimeSpan Delay
        {
            get => _delay + (Smoothing ? SmoothingCompensation : TimeSpan.Zero);
            set => _delay = value;
        }

        public int Ping
        {
            get => (int)Delay.TotalMilliseconds;
            set => Delay = TimeSpan.FromMilliseconds(value);
        }

        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; }
        public bool IsDisposed { get; private set; }

        private static Vector3 PlayerPosition => GameService.Gw2Mumble.RawClient.AvatarPosition.ToXnaVector3();
        private static Vector3 PlayerForward => GameService.Gw2Mumble.RawClient.AvatarFront.ToXnaVector3();

        private class TimePos
        {
            public readonly TimeSpan Time;
            public readonly Vector3 Position;
            public readonly Vector3 Forward;

            public TimePos(TimeSpan time, Vector3 position, Vector3 forward)
            {
                Time = time;
                Position = position;
                Forward = forward;
            }

            public TimePos(Vector3 position, Vector3 forward) : this(default, position, forward)
            {
            }

            public TimePos(TimeSpan time) : this(time, PlayerPosition, PlayerForward)
            {
            }

            public static bool AreEquivalent(TimePos a, TimePos b)
                => a.Position == b.Position && a.Forward == b.Forward;

            public static TimePos Lerp(TimePos a, TimePos b, TimeSpan delay, TimeSpan time)
            {
                float p;
                {
                    var start = (a.Time + delay).TotalMilliseconds;
                    var now = time.TotalMilliseconds;
                    var end = (b.Time + delay).TotalMilliseconds;
                    p = (float)((now - start) / (end - start));
                }

                return float.IsNaN(p) || float.IsInfinity(p)
                    ? a
                    : new TimePos(
                        Vector3.Lerp(a.Position, b.Position, p),
                        Vector3.Lerp(a.Forward, b.Forward, p));
            }
        }

        private readonly Quad _quad;
        private readonly Texture2D _texture;
        private BasicEffect _effect;

        private int _lastTick;
        private TimePos _lastPopped; 
        private TimePos _lastQueued; 
        private readonly Queue<TimePos> _timePosQueue = new Queue<TimePos>();

        public HitboxEntity()
        {
            _quad = new Quad(Vector3.Zero, Vector3.Backward, Vector3.Up, 1, 1);
            _texture = HitboxModule.ContentsManager.GetTexture("Hitbox.png");
            GameService.Graphics.QueueMainThreadRender(graphicsDevice =>
            {
                if (_effect != null || IsDisposed)
                    return;

                _effect = new BasicEffect(graphicsDevice)
                {
                    Texture = _texture,
                    TextureEnabled = true,
                    DiffuseColor = Color.ToVector3(),
                };
            });

            Reset();
        }

        public void Reset()
        {
            _lastQueued = null;
            _lastPopped = new TimePos(TimeSpan.Zero);
            _timePosQueue.Clear();
        }

        public void Update(GameTime gameTime)
        {
            if (GameService.Gw2Mumble.RawClient.Tick > _lastTick)
            {
                _lastTick = GameService.Gw2Mumble.RawClient.Tick;

                var newTimePos = new TimePos(gameTime.TotalGameTime);

                // Don't enqueue the same position multiple times in a row
                if (_lastQueued == null || !TimePos.AreEquivalent(_lastQueued, newTimePos))
                {
                    _lastQueued = new TimePos(gameTime.TotalGameTime);
                    _timePosQueue.Enqueue(_lastQueued);
                }
            }

            while (_timePosQueue.Any() && _timePosQueue.Peek().Time + Delay <= gameTime.TotalGameTime)
                _lastPopped = _timePosQueue.Dequeue();

            void apply(TimePos timePos)
            {
                Position = timePos.Position;
                Forward = timePos.Forward;
            }

            if (!Smoothing || !_timePosQueue.Any())
                apply(_lastPopped);
            else
                apply(TimePos.Lerp(_lastPopped, _timePosQueue.Peek(), Delay, gameTime.TotalGameTime));
        }

        public void Render(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
        {
            if (_effect == null)
                return;

            _effect.View = GameService.Gw2Mumble.PlayerCamera.View;
            _effect.Projection = GameService.Gw2Mumble.PlayerCamera.Projection;

            var worldMatrix = Matrix.CreateScale(1, 1, 1) * Matrix.CreateTranslation(Position);
            var t = worldMatrix.Translation;
            worldMatrix *= Matrix.CreateRotationZ(-(float)Math.Atan2(Forward.X, Forward.Y));
            worldMatrix.Translation = t;
            _effect.World = worldMatrix;

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _quad.Vertices, 0, 4, _quad.Indexes, 0, 2);
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
            _texture?.Dispose();
            _effect?.Dispose();
        }
    }
}
