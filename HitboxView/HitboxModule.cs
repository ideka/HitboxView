using Blish_HUD;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
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
        //private static readonly Logger Logger = Logger.GetLogger<HitboxModule>();

        private static readonly (int min, int max) PingRange = (0, 1000);

        internal static SettingsManager SettingsManager => Instance.ModuleParameters.SettingsManager;
        internal static ContentsManager ContentsManager => Instance.ModuleParameters.ContentsManager;
        internal static DirectoriesManager DirectoriesManager => Instance.ModuleParameters.DirectoriesManager;
        internal static Gw2ApiManager Gw2ApiManager => Instance.ModuleParameters.Gw2ApiManager;

        private static HitboxModule Instance { get; set; }

        private HitboxDraw _hitbox;

        private SettingEntry<Color> _hitboxColor;
        private SettingEntry<KeyBinding> _toggleHitboxKey;
        private SettingEntry<bool> _hitboxVisible;
        private SettingEntry<bool> _hitboxSmoothing;
        private SettingEntry<string> _gamePingString;
        private SettingEntry<int> _gamePing;

        [ImportingConstructor]
        public HitboxModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            Instance = this;
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
            _gamePingString = settings.DefineSetting("GamePingString", "100",
                () => Strings.SettingGamePing,
                () => Strings.SettingGamePingText);
            _gamePing = settings.DefineSetting("GamePing", 100,
                () => " ",
                () => Strings.SettingGamePingText);
            _gamePing.SetRange(PingRange.min, PingRange.max);

            _gamePingString.SetValidation(value
                => new SettingValidationResult(
                    ParsePingString(value, out var ping) && $"{ping}" == value.Trim(),
                    string.Format(Strings.SettingGamePingValidation, PingRange.min, PingRange.max)));
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

            _hitboxColor.SettingChanged += HitboxColorChanged;
            _toggleHitboxKey.Value.Enabled = true;
            _toggleHitboxKey.Value.Activated += HitboxToggled;
            _hitboxVisible.SettingChanged += HitboxVisibleChanged;
            _hitboxSmoothing.SettingChanged += HitboxSmoothingChanged;
            _gamePingString.SettingChanged += GamePingStringChanged;
            _gamePing.SettingChanged += GamePingChanged;
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

        private bool _reflecting = false;
        private void GamePingStringChanged(object sender, ValueChangedEventArgs<string> e)
        {
            if (_reflecting)
                return;

            _reflecting = true;
            _gamePing.Value = ParsePingString(_gamePingString.Value, out int ping) ? ping : _gamePing.Value;
            _reflecting = false;

            _hitbox.Ping = _gamePing.Value;
        }

        private void GamePingChanged(object sender, ValueChangedEventArgs<int> e)
        {
            if (_reflecting)
                return;

            _reflecting = true;
            _gamePingString.Value = $"{_gamePing.Value}";
            _reflecting = false;

            _hitbox.Ping = _gamePing.Value;
        }

        protected override void Unload()
        {
            _hitboxColor.SettingChanged -= HitboxColorChanged;
            _toggleHitboxKey.Value.Activated -= HitboxToggled;
            _hitboxVisible.SettingChanged -= HitboxVisibleChanged;
            _hitboxSmoothing.SettingChanged -= HitboxSmoothingChanged;
            _gamePingString.SettingChanged -= GamePingStringChanged;
            _gamePing.SettingChanged -= GamePingChanged;

            _hitbox?.Dispose();

            Instance = null;
        }

        private static bool ParsePingString(string pingString, out int ping)
        {
            var parsed = int.TryParse(pingString, out ping);
            ping = Math.Min(Math.Max(ping, PingRange.min), PingRange.max);
            return parsed;
        }
    }
}
