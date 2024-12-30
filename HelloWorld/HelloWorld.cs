using Landfall.Modding;
using Unity.Mathematics;
using UnityEngine;
using Zorro.Settings;

namespace HelloWorld;

[LandfallPlugin]
public class Program
{
    static Program()
    {
        Debug.Log("Hello, World!");
        GameHandler.Instance.SettingsHandler.AddSetting(new HelloSetting());
    }
}

public class HelloSetting : FloatSetting, IExposedSetting
{
    public override void ApplyValue() => Debug.Log($"Mod apply value {Value}");
    protected override float GetDefaultValue() => 5;
    protected override float2 GetMinMaxValue() => new(0, 10);
    public string GetDisplayName() => "mod setting!!";
    public SettingCategory GetCategory() => SettingCategory.Graphics;
}
