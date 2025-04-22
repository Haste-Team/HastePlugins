using Landfall.Haste;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Localization;
using Zorro.Settings;
using Zorro.UI.Modal;

namespace LandfallSpeedrunningTools;

[HasteSetting]
public class FixedSeed : IntSetting, IExposedSetting
{
    static FixedSeed()
    {
        On.WorldShard.PlayLevel_int += (orig, self, seed) =>
        {
            if (!GameHandler.Instance)
                return;
            var setting = GameHandler.Instance.SettingsHandler.GetSetting<FixedSeed>();
            if (setting == null)
                return;
            var value = setting.Value;
            if (value == 0)
                orig(self, seed);
            else
            {
                // ignore the provided seed and use our own
                // offset the seed by shardID to make every shard have different base seeds
                // (and multiply by 100 so that it's not just shifting level ID seeds by 1)
                orig(self, value + self.shardID * 100);
            }
        };
    }

    public override void ApplyValue()
    {
    }

    protected override int GetDefaultValue() => 0;

    public LocalizedString GetDisplayName() => new UnlocalizedString("Fixed seed (0 to disable)");

    public string GetCategory() => SpeedrunningPlugin.SpeedrunningSetting;
}

public enum LivesplitterKind
{
    None,
    LiveSplitClassic,
}

[HasteSetting]
public class LivesplitterEnabled : EnumSetting<LivesplitterKind>, IExposedSetting
{
    public override async void ApplyValue()
    {
        switch (Value)
        {
            case LivesplitterKind.None:
                if (LiveSplit.Instance is not null)
                {
                    var inst = LiveSplit.Instance;
                    LiveSplit.Instance = null;
                    inst?.Dispose();
                }
                break;
            case LivesplitterKind.LiveSplitClassic:
                bool useNamedPipe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                if (LiveSplit.Instance?.GetType() != (useNamedPipe ? typeof(LiveSplitNamedPipe) : typeof(LiveSplitTCP)))
                {
                    var inst = LiveSplit.Instance;
                    LiveSplit.Instance = null;
                    inst?.Dispose();
                }
                try
                {
                    LiveSplit client = useNamedPipe ? new LiveSplitNamedPipe() : new LiveSplitTCP();
                    await client.ConnectAsync();
                    LiveSplit.Instance = client;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    var message = e.Message;
                    if (!useNamedPipe && e.GetType() == typeof(SocketException))
                    {
                        message = "Make sure LiveSplit is running and the TCP Server is enabled!\nRight Click -> Control -> Start TCP Server";
                    }
                    else if (useNamedPipe && e.GetType() == typeof(TimeoutException))
                    {
                        message = "Make sure LiveSplit is running. If the problem persists, restart LiveSplit, then try again.";
                    }
                    message += "\nTo retry, go to the settings and disable, then re-enable the autosplitter.";
                    Modal.OpenModal(new DefaultHeaderModalOption("Failed to connect to LiveSplit", message), new CloseModalOnKeypress());
                }
                break;
        }
    }

    protected override LivesplitterKind GetDefaultValue() => LivesplitterKind.None;

    public override List<LocalizedString> GetLocalizedChoices() =>
    [
        new UnlocalizedString("Disabled"),
        new UnlocalizedString("Enabled")
    ];

    public LocalizedString GetDisplayName() => new UnlocalizedString("Autosplitter for LiveSplit");
    public string GetCategory() => SpeedrunningPlugin.SpeedrunningSetting;
}

/*
[HasteSetting]
public class LivesplitterWebsocketPrefix : StringSetting, IExposedSetting
{
    public override void ApplyValue()
    {
    }

    protected override string GetDefaultValue() => "http://*:8080/";
    public LocalizedString GetDisplayName() => new UnlocalizedString("Websocket server listen prefix (LiveSplitCore only)");
    public string GetCategory() => SpeedrunningPlugin.SpeedrunningSetting;
}
*/

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
public class SplitOnEndBossWin : SplitOnSetting
{
    public override void ApplyValue()
    {
        if (Value)
            GM_API.EndBossWin += Trigger;
        else
            GM_API.EndBossWin -= Trigger;
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
