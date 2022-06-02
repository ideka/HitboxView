using Blish_HUD;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using System;
using System.ComponentModel.Composition;
using static Blish_HUD.GameService;

namespace Ideka.HitboxView
{
    [Export(typeof(Module))]
    public class HitboxModule : Module
    {
        private HitboxDraw _hitbox;

        private SettingEntry<Color> _hitboxColor;
        private SettingEntry<KeyBinding> _toggleHitboxKey;
        private SettingEntry<bool> _hitboxVisible;
        private SettingEntry<bool> _hitboxSmoothing;
        private SliderEntry _gamePing;

        [ImportingConstructor]
        public HitboxModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            // This setting is currently not visible as BlishHUD doesn't have a default control for Color settings
            _hitboxColor = settings.DefineSetting("HitboxColor", Color.White,
                () => Strings.SettingHitboxColor,
                () => Strings.SettingHitboxColorText);
            _toggleHitboxKey = settings.DefineSetting("ToggleHitboxKey", new KeyBinding(),
                () => Strings.SettingToggleHitboxKey,
                () => Strings.SettingToggleHitboxKeyText);
            _hitboxVisible = settings.DefineSetting("HitboxVisible", true,
                () => Strings.SettingHitboxVisible,
                () => Strings.SettingHitboxVisibleText);
            _hitboxSmoothing = settings.DefineSetting("HitboxSmoothing", true,
                () => Strings.SettingHitboxSmoothing,
                () => Strings.SettingHitboxSmoothingText);

            _gamePing = new SliderEntry(settings, "GamePing", 100, 0, 1000,
                () => Strings.SettingGamePing,
                () => Strings.SettingGamePingText,
                (min, max) => string.Format(Strings.SettingGamePingValidation, min, max));
        }

        protected override void Initialize()
        {
            _hitbox = new HitboxDraw()
            {
                Parent = Graphics.SpriteScreen,
                Smoothing = _hitboxSmoothing.Value,
                Ping = _gamePing.Value,
            };

            if (_hitboxVisible.Value)
                _hitbox.Show();
            else
                _hitbox.Hide();

            _hitboxColor.SettingChanged += HitboxColorChanged;
            _toggleHitboxKey.Value.Enabled = true;
            _toggleHitboxKey.Value.Activated += HitboxToggled;
            _hitboxVisible.SettingChanged += HitboxVisibleChanged;
            _hitboxSmoothing.SettingChanged += HitboxSmoothingChanged;
            _gamePing.Changed = value => _hitbox.Ping = value;
        }

        private void HitboxColorChanged(object sender, ValueChangedEventArgs<Color> e)
            => _hitbox.Color = _hitboxColor.Value;

        private void HitboxToggled(object sender, EventArgs e)
            => _hitboxVisible.Value = !_hitboxVisible.Value;

        private void HitboxVisibleChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            _hitbox.Reset();
            if (_hitboxVisible.Value)
                _hitbox.Show();
            else
                _hitbox.Hide();
        }

        private void HitboxSmoothingChanged(object sender, ValueChangedEventArgs<bool> e)
            => _hitbox.Smoothing = _hitboxSmoothing.Value;

        protected override void Unload()
        {
            _hitboxColor.SettingChanged -= HitboxColorChanged;
            _toggleHitboxKey.Value.Activated -= HitboxToggled;
            _hitboxVisible.SettingChanged -= HitboxVisibleChanged;
            _hitboxSmoothing.SettingChanged -= HitboxSmoothingChanged;

            _gamePing?.Dispose();

            _hitbox?.Dispose();
        }
    }
}
