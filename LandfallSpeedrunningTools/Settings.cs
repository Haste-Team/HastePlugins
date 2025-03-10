using Landfall.Haste;
using UnityEngine.Localization;
using Zorro.Settings;
using Zorro.UI.Modal;

namespace LandfallSpeedrunningTools;

[HasteSetting]
public class FixedSeed : IntSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        if (Value == 0)
        {
            RunHandler.SeedGenerator -= GetSeed;
        }
        else
        {
            RunHandler.SeedGenerator -= GetSeed;
            RunHandler.SeedGenerator += GetSeed;
        }
    }

    private int GetSeed() => Value;

    protected override int GetDefaultValue() => 0;

    public LocalizedString GetDisplayName() => new UnlocalizedString("Fixed seed (0 to disable)");

    public string GetCategory() => SpeedrunningPlugin.SpeedrunningSetting;
}

[HasteSetting]
public class LivesplitterEnabled : BoolSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        if (Value)
        {
            if (LiveSplit.Instance == null)
            {
                try
                {
                    LiveSplit.Instance = new LiveSplit();
                }
                catch (Exception e)
                {
                    Modal.OpenModal(new DefaultHeaderModalOption("Failed to start live split client", e.ToString()), new CloseModalOnKeypress());
                }
            }
        }
        else
        {
            if (LiveSplit.Instance != null)
            {
                var inst = LiveSplit.Instance;
                LiveSplit.Instance = null;
                inst.Dispose();
            }
        }
    }

    protected override bool GetDefaultValue() => false;
    public override LocalizedString OffString => new UnlocalizedString("Disabled");
    public override LocalizedString OnString => new UnlocalizedString("Enabled");
    public LocalizedString GetDisplayName() => new UnlocalizedString("Autosplitter for LiveSplit");
    public string GetCategory() => SpeedrunningPlugin.SpeedrunningSetting;
}

public abstract class SplitOnSetting : BoolSetting, IExposedSetting
{
    private readonly LocalizedString _displayName;

    public SplitOnSetting()
    {
        _displayName = new UnlocalizedString(new string(GetType().Name.SelectMany((ch, i) => char.IsUpper(ch) ? i == 0 ? ch.ToString() : " " + char.ToLower(ch) : ch.ToString()).ToArray()));
    }

    protected static void Trigger() => LiveSplit.Instance?.Split();

    public override LocalizedString OffString => new UnlocalizedString("Disabled");
    public override LocalizedString OnString => new UnlocalizedString("Enabled");
    protected override bool GetDefaultValue() => false;

    public LocalizedString GetDisplayName() => _displayName;
    public string GetCategory() => SpeedrunningPlugin.SpeedrunningSetting;
}

[HasteSetting]
public class SplitOnDeath : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.Died += Trigger;
        else
            GM_API.Died -= Trigger;
    }
}

[HasteSetting]
public class SplitOnBossDeath : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.BossDeath += Trigger;
        else
            GM_API.BossDeath -= Trigger;
    }
}

[HasteSetting]
public class SplitOnPlayNode : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.PlayNode += Trigger;
        else
            GM_API.PlayNode -= Trigger;
    }

    private static void Trigger(LevelSelectionNode.Data obj)
    {
        if (obj.Type != LevelSelectionNode.NodeType.Boss)
            Trigger();
    }
}

[HasteSetting]
public class SplitOnPlayBossNode : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.PlayNode += Trigger;
        else
            GM_API.PlayNode -= Trigger;
    }

    private static void Trigger(LevelSelectionNode.Data obj)
    {
        if (obj.Type == LevelSelectionNode.NodeType.Boss)
            Trigger();
    }
}

[HasteSetting]
public class SplitOnRunEnd : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.RunEnd += Trigger;
        else
            GM_API.RunEnd -= Trigger;
    }

    private static void Trigger(RunHandler.LastRunState lastRunState) => Trigger();
}

[HasteSetting]
public class SplitOnTutorial1Start : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.TutorialStart += Trigger;
        else
            GM_API.TutorialStart -= Trigger;
    }

    private static void Trigger(int id)
    {
        if (id == 1)
            Trigger();
    }
}

[HasteSetting]
public class SplitOnTutorial2Start : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.TutorialStart += Trigger;
        else
            GM_API.TutorialStart -= Trigger;
    }

    private static void Trigger(int id)
    {
        if (id == 2)
            Trigger();
    }
}

[HasteSetting]
public class SplitOnSpawnedInHub : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.SpawnedInHub += Trigger;
        else
            GM_API.SpawnedInHub -= Trigger;
    }
}

[HasteSetting]
public class SplitOnStartNewRun : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.StartNewRun += Trigger;
        else
            GM_API.StartNewRun -= Trigger;
    }
}

[HasteSetting]
public class SplitOnMainMenuPlayButton : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.MainMenuPlayButton += Trigger;
        else
            GM_API.MainMenuPlayButton -= Trigger;
    }
}
