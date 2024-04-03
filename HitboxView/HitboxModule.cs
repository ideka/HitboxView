using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Ideka.BHUDCommon;
using Ideka.NetCommon;
using Microsoft.Xna.Framework;
using System.ComponentModel.Composition;
using static Blish_HUD.GameService;

namespace Ideka.HitboxView;

[Export(typeof(Module))]
[method: ImportingConstructor]
public class HitboxModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : Module(moduleParameters)
{
    private HitboxDraw _hitbox = null!;

    private GenericSetting<Color> _hitboxColor = null!;
    private GenericSetting<Color> _hitboxOutlineColor = null!;
    private KeyBindingSetting _toggleHitboxKey = null!;
    private GenericSetting<bool> _hitboxVisible = null!;
    private GenericSetting<bool> _hitboxSmoothing = null!;
    private SliderSetting _gamePing = null!;

    private readonly DisposableCollection _dc = new();

    protected override void DefineSettings(SettingCollection settings)
    {
        // This setting is currently not visible as BlishHUD doesn't have a default control for Color settings
        _hitboxColor = _dc.Add(settings.Generic("HitboxColor", Color.White,
            () => Strings.SettingHitboxColor,
            () => Strings.SettingHitboxColorText));
        _hitboxOutlineColor = _dc.Add(settings.Generic("HitboxOutlineColor", Color.Black,
            () => Strings.SettingHitboxOutlineColor,
            () => Strings.SettingHitboxOutlineColorText));
        _toggleHitboxKey = _dc.Add(settings.KeyBinding("ToggleHitboxKey", new KeyBinding(),
            () => Strings.SettingToggleHitboxKey,
            () => Strings.SettingToggleHitboxKeyText));
        _hitboxVisible = _dc.Add(settings.Generic("HitboxVisible", true,
            () => Strings.SettingHitboxVisible,
            () => Strings.SettingHitboxVisibleText));
        _hitboxSmoothing = _dc.Add(settings.Generic("HitboxSmoothing", true,
            () => Strings.SettingHitboxSmoothing,
            () => Strings.SettingHitboxSmoothingText));
        _gamePing = _dc.Add(settings.Slider("GamePing", 100, 0, 1000,
            () => Strings.SettingGamePing,
            () => Strings.SettingGamePingText,
            (min, max) => string.Format(Strings.SettingGamePingValidation, min, max)));
    }

    protected override void Initialize()
    {
        _hitbox = new HitboxDraw()
        {
            Parent = Graphics.SpriteScreen,
        };

        _dc.Add(_hitboxColor.OnChangedAndNow(value => _hitbox.Color = value));
        _dc.Add(_hitboxOutlineColor.OnChangedAndNow(value => _hitbox.OutlineColor = value));
        _dc.Add(_toggleHitboxKey.OnActivated(() => _hitboxVisible.Value = !_hitboxVisible.Value));
        _dc.Add(_hitboxVisible.OnChangedAndNow(value => _hitbox.Visible = value));
        _dc.Add(_hitboxSmoothing.OnChangedAndNow(value => _hitbox.Smoothing = value));
        _dc.Add(_gamePing.OnChangedAndNow(value => _hitbox.Ping = value));
    }

    protected override void Unload()
    {
        _dc.Dispose();
    }
}
