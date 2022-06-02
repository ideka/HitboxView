﻿using Blish_HUD;
using Blish_HUD.Settings;
using System;

namespace Ideka.HitboxView
{
    public class SliderEntry : IDisposable
    {
        private readonly SettingEntry<string> _string;
        public SettingEntry<int> Setting { get; }

        public int Value => Setting.Value;

        public Action<int> Changed { get; set; }

        private readonly int _minValue;
        private readonly int _maxValue;

        private bool _reflecting;

        public SliderEntry(SettingCollection settings,
            string key, int defaultValue, int minValue, int maxValue,
            Func<string> displayNameFunc, Func<string> descriptionFunc, Func<int, int, string> validationErrorFunc)
        {
            _minValue = minValue;
            _maxValue = maxValue;

            _string = settings.DefineSetting($"{key}Str", "", displayNameFunc, descriptionFunc);
            Setting = settings.DefineSetting($"{key}", defaultValue, () => " ", descriptionFunc);
            Setting.SetRange(_minValue, _maxValue);

            _string.Value = $"{Setting.Value}";

            _string.SetValidation(str => !Validate(str, out int _)
                ? new SettingValidationResult(false, validationErrorFunc?.Invoke(_minValue, _maxValue))
                : new SettingValidationResult(true));

            _string.SettingChanged += StringChanged;
            Setting.SettingChanged += IntChanged;
        }

        private bool Validate(string str, out int value)
            => int.TryParse(str, out value) && value > _minValue && value < _maxValue;

        private void StringChanged(object sender, ValueChangedEventArgs<string> e)
        {
            if (_reflecting)
                return;

            _reflecting = true;
            if (Validate(_string.Value, out int val))
                Setting.Value = val;
            else
                _string.Value = $"{Setting.Value}";
            _reflecting = false;
        }

        private void IntChanged(object sender, ValueChangedEventArgs<int> e)
        {
            Changed?.Invoke(Value);

            if (_reflecting)
                return;

            _reflecting = true;
            _string.Value = $"{Setting.Value}";
            _reflecting = false;
        }

        public void Dispose()
        {
            _string.SettingChanged -= StringChanged;
            Setting.SettingChanged -= IntChanged;
        }
    }
}