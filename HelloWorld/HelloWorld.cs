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

        On.PlayerCharacter.RestartPlayer_Launch_Transform_float += (orig, self, spawnPoint, minVel) =>
        {
            // normally it's 100
            var launchSetting = GameHandler.Instance.SettingsHandler.GetSetting<HelloSetting>().Value;
            minVel = Mathf.Max(minVel, launchSetting);
            Debug.Log("Hooked launch! Velocity is now " + minVel);
            orig(self, spawnPoint, minVel);
        };
    }
}

// The HasteSetting attribute is equivalent to
// GameHandler.Instance.SettingsHandler.AddSetting(new HelloSetting());
[HasteSetting]
public class HelloSetting : FloatSetting, IExposedSetting
{
    public override void ApplyValue() => Debug.Log($"Mod apply value {Value}");
    protected override float GetDefaultValue() => 120;
    protected override float2 GetMinMaxValue() => new(100, 200);
    public LocalizedString GetDisplayName() => new UnlocalizedString("mod setting!!");
    public string GetCategory() => SettingCategory.General;
}
