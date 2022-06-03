using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Ideka.BHUDCommon;
using Microsoft.Xna.Framework;
using System.ComponentModel.Composition;
using static Blish_HUD.GameService;

namespace Ideka.HitboxView
{
    [Export(typeof(Module))]
    public class HitboxModule : Module
    {
        //private static HitboxModule Instance { get; set; }

        private HitboxDraw _hitbox;

        private GenericSetting<Color> _hitboxColor;
        private GenericSetting<Color> _hitboxOutlineColor;
        private KeyBindingSetting _toggleHitboxKey;
        private GenericSetting<bool> _hitboxVisible;
        private GenericSetting<bool> _hitboxSmoothing;
        private SliderSetting _gamePing;

        [ImportingConstructor]
        public HitboxModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            //Instance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            // This setting is currently not visible as BlishHUD doesn't have a default control for Color settings
            _hitboxColor = settings.Generic("HitboxColor", Color.White,
                () => Strings.SettingHitboxColor,
                () => Strings.SettingHitboxColorText);
            _hitboxOutlineColor = settings.Generic("HitboxOutlineColor", Color.Black,
                () => Strings.SettingHitboxOutlineColor,
                () => Strings.SettingHitboxOutlineColorText);
            _toggleHitboxKey = settings.KeyBinding("ToggleHitboxKey", new KeyBinding(),
                () => Strings.SettingToggleHitboxKey,
                () => Strings.SettingToggleHitboxKeyText);
            _hitboxVisible = settings.Generic("HitboxVisible", true,
                () => Strings.SettingHitboxVisible,
                () => Strings.SettingHitboxVisibleText);
            _hitboxSmoothing = settings.Generic("HitboxSmoothing", true,
                () => Strings.SettingHitboxSmoothing,
                () => Strings.SettingHitboxSmoothingText);
            _gamePing = settings.Slider("GamePing", 100, 0, 1000,
                () => Strings.SettingGamePing,
                () => Strings.SettingGamePingText,
                (min, max) => string.Format(Strings.SettingGamePingValidation, min, max));
        }

        protected override void Initialize()
        {
            _hitbox = new HitboxDraw()
            {
                Parent = Graphics.SpriteScreen,
                Color = _hitboxColor.Value,
                OutlineColor = _hitboxOutlineColor.Value,
                Visible = _hitboxVisible.Value,
                Smoothing = _hitboxSmoothing.Value,
                Ping = _gamePing.Value,
            };

            _hitboxColor.Changed = value => _hitbox.Color = value;

            _hitboxOutlineColor.Changed = value => _hitbox.OutlineColor = value;

            _toggleHitboxKey.OnTrigger(() => _hitboxVisible.Value = !_hitboxVisible.Value);

            _hitboxVisible.Changed = value => _hitbox.Visible = value;

            _hitboxSmoothing.Changed = value => _hitbox.Smoothing = value;

            _gamePing.Changed = value => _hitbox.Ping = value;
        }

        protected override void Unload()
        {
            _hitboxColor?.Dispose();
            _toggleHitboxKey?.Dispose();
            _hitboxVisible?.Dispose();
            _hitboxSmoothing?.Dispose();
            _gamePing?.Dispose();
            _hitbox?.Dispose();

            //Instance = null;
        }
    }
}
