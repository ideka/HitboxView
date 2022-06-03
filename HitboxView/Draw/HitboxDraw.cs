using Blish_HUD;
using Blish_HUD.Controls;
using Gw2Sharp.Models;
using Ideka.BHUDCommon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using static Blish_HUD.GameService;

namespace Ideka.HitboxView
{
    public class HitboxDraw : Container
    {
        private static readonly TimeSpan SmoothingCompensation = TimeSpan.FromMilliseconds(100 / 6);

        public float DrawOrder => 100;  // TODO: Figure out what to set this to.

        public bool Smoothing { get; set; } = true;
        public Color Color { get; set; }
        public Color OutlineColor { get; set; }

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

        private static Vector3 PlayerPosition => Gw2Mumble.RawClient.AvatarPosition.ToXnaVector3();
        private static Vector3 PlayerForward => Gw2Mumble.RawClient.AvatarFront.ToXnaVector3();

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

        private readonly Primitive _circle;
        private readonly Primitive _slice;

        private int _lastTick;
        private TimePos _lastPopped; 
        private TimePos _lastQueued; 
        private readonly Queue<TimePos> _timePosQueue = new Queue<TimePos>();

        public HitboxDraw()
        {
            ClipsBounds = false;

            _circle = Primitive.HorizontalCircle(0.5f, 100);
            _slice = new Primitive(
                new Vector3(-.5f, 0, 0),
                new Vector3(0, 0, 0),
                new Vector3(0, .5f, 0)
            ).Transformed(Matrix.CreateRotationZ(MathHelper.ToRadians(-45)));

            Reset();
        }

        public void Reset()
        {
            _lastQueued = null;
            _lastPopped = new TimePos(TimeSpan.Zero);
            _timePosQueue.Clear();
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            if (Gw2Mumble.RawClient.Tick > _lastTick)
            {
                _lastTick = Gw2Mumble.RawClient.Tick;

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

        // TODO: Skimmer and Skiff need better testing to confirm their sizes
        private static readonly Dictionary<MountType, Vector2> Sizes = new Dictionary<MountType, Vector2>
        {
            [MountType.None] = Vector2.One * 1,
            [MountType.Raptor] = Vector2.One * 2.8f,
            [MountType.Springer] = Vector2.One * 2.2f,
            [MountType.Skimmer] = Vector2.One * 3.1f,
            [MountType.Jackal] = Vector2.One * 2.2f,
            [MountType.Griffon] = Vector2.One * 2.7f,
            [MountType.RollerBeetle] = Vector2.One * 2.8f,
            [MountType.Warclaw] = Vector2.One * 1.7f,
            [MountType.Skyscale] = Vector2.One * 2.7f,
            [MountType.Skiff] = new Vector2(3.1f, 11.1f),
            [MountType.SiegeTurtle] = Vector2.One * 3.9f,
        };

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            var scale = Sizes.TryGetValue(Gw2Mumble.PlayerCharacter.CurrentMount, out var s) ? s : Vector2.One;

            var trs =
                Matrix.CreateScale(scale.X, scale.Y, 1) *
                Matrix.CreateRotationZ(-(float)Math.Atan2(Forward.X, Forward.Y)) *
                Matrix.CreateTranslation(Position);

            {
                var circle = _circle.Transformed(trs).ToScreen();
                spriteBatch.DrawPolygon(Vector2.Zero, circle, Color.Black, 3);
                spriteBatch.DrawPolygon(Vector2.Zero, circle, Color.White, 2);
            }

            if (scale.X == scale.Y)
            {
                var slice = _slice.Transformed(trs).ToScreen();
                spriteBatch.DrawPolygon(Vector2.Zero, slice, Color.Black, 3, open: true);
                spriteBatch.DrawPolygon(Vector2.Zero, slice, Color.White, 2, open: true);
            }
        }
    }
}
