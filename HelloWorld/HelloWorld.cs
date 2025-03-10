using Landfall.Haste;
using Landfall.Modding;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Localization;
using Zorro.Settings;

namespace HelloWorld;

[LandfallPlugin]
public class Program
{
    static Program()
    {
        Debug.Log("Hello, World!");
    }
}

// The HasteSetting attribute is equivalent to
// GameHandler.Instance.SettingsHandler.AddSetting(new HelloSetting());
[HasteSetting]
public class HelloSetting : FloatSetting, IExposedSetting
{
    public override void ApplyValue() => Debug.Log($"Mod apply value {Value}");
    protected override float GetDefaultValue() => 5;
    protected override float2 GetMinMaxValue() => new(0, 10);
    public LocalizedString GetDisplayName() => new UnlocalizedString("mod setting!!");
    public string GetCategory() => SettingCategory.General;
}
