using System.Reflection;
using Landfall.Modding;
using Steamworks;
using UnityEngine;
using UnityEngine.UIElements;
using Zorro.Core.CLI;

namespace LeaderboardRuns;

[LandfallPlugin]
public static class Program
{
    static Program()
    {
        DebugUIHandler.Instance.RegisterPage("Leaderboard Runs", () => new LeaderboardRunsPage());
    }
}

public class LeaderboardRunsPage : DebugPage
{
    private static readonly Callback<PersonaStateChange_t> PersonaStateChange = new(OnPersonaStateChange);
    private static readonly List<LeaderboardRunsPage> OpenPages = [];
    private readonly List<LeaderboardEntry_t> _leaderboardEntries = [];
    private readonly ScrollView _leaderboardList;
    private static SteamLeaderboard_t _leaderboard;
    private static double _newLevelStartTime;
    private static bool _runActive;

    static LeaderboardRunsPage()
    {
        GM_API.newLevelAction += NewLevelAction;
        GM_API.restartLevelAction += NewLevelAction;
        GM_API.playerEnteredPortalAction += PlayerEnteredPortalAction;
    }

    public LeaderboardRunsPage()
    {
        Add(new Button(StartNewRun) { text = "Start new run" });
        _leaderboardList = new ScrollView();
        Add(_leaderboardList);
        RegisterCallback<AttachToPanelEvent>(_ => OpenPages.Add(this));
        RegisterCallback<AttachToPanelEvent>(_ => OpenPages.Remove(this));
        PopulateLeaderboard();
    }

    private async void PopulateLeaderboard()
    {
        try
        {
            await DoPopulateLeaderboard();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task DoPopulateLeaderboard()
    {
        var result = await SteamUserStats.FindOrCreateLeaderboard("asdf",
            ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending,
            ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds).ToAsync<LeaderboardFindResult_t>();
        Debug.Log($"FindOrCreateLeaderboard: {result.m_hSteamLeaderboard} (found={result.m_bLeaderboardFound})");
        if (result.m_hSteamLeaderboard == new SteamLeaderboard_t(0))
        {
            Debug.LogError("No leaderboard found!");
            return;
        }

        _leaderboard = result.m_hSteamLeaderboard;
        var entries = await SteamUserStats.DownloadLeaderboardEntries(_leaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 0, 50).ToAsync<LeaderboardScoresDownloaded_t>();
        _leaderboardEntries.Clear();
        for (var i = 0; i < entries.m_cEntryCount; i++)
        {
            if (SteamUserStats.GetDownloadedLeaderboardEntry(entries.m_hSteamLeaderboardEntries, i, out var entry, null, 0))
            {
                // boot request (returns false if it's already downloaded)
                SteamFriends.RequestUserInformation(entry.m_steamIDUser, true);
                _leaderboardEntries.Add(entry);
            }
        }

        RefreshNames();
    }

    private void RefreshNames()
    {
        _leaderboardList.Clear();
        foreach (var item in _leaderboardEntries)
        {
            var rank = item.m_nGlobalRank;
            var steamName = SteamFriends.GetFriendPersonaName(item.m_steamIDUser);
            var score = TimeSpan.FromMilliseconds(item.m_nScore);
            _leaderboardList.Add(new Label($"#{rank}: {steamName} - {score}") { style = { color = Color.white } });
        }
    }

    private static void OnPersonaStateChange(PersonaStateChange_t personaStateChange)
    {
        foreach (var page in OpenPages)
            page.RefreshNames();
    }

    private static void StartNewRun()
    {
        DebugUIHandler.Instance.Hide();

        var shards = UnityEngine.Object.FindObjectsOfType<WorldShard>();
        LevelGenConfig? forest = null;
        foreach (var shard in shards)
        {
            foreach (var levelGenConfig in shard.runConfig.categories)
            {
                if (levelGenConfig.name == "Forest")
                {
                    forest = levelGenConfig;
                    break;
                }
            }

            if (forest != null)
                break;
        }

        if (forest == null)
        {
            Debug.LogError("Forest LevelGenConfig not found (are you in the full hub scene?)");
            return;
        }

        var runConfig = ScriptableObject.CreateInstance<RunConfig>();

        runConfig.title = "LeaderboardRun";
        runConfig.nrOfLevels = 0;
        runConfig.startDifficulty = 5;
        runConfig.endDifficulty = 5;

        runConfig.maxSlope = 1f;
        runConfig.minSlope = 0.5f;

        runConfig.minLength = 1;
        runConfig.maxLegnth = 1;

        runConfig.minSpeed = 80;
        runConfig.maxSpeed = 85;

        runConfig.bossScene = "";

        runConfig.noiseAdditions = [];
        runConfig.propAdditions = [];
        runConfig.genObjects = [];
        runConfig.keyProps = [];

        runConfig.categories = [forest];
        runConfig.bossTeir = 0;

        _runActive = true;
        SimpleRunHandler.StartAndPlayNewRun(runConfig, 1, 978027761);
    }

    private static void NewLevelAction()
    {
        _newLevelStartTime = Time.timeAsDouble;
    }

    private static void PlayerEnteredPortalAction()
    {
        if (!_runActive)
            return;

        var now = Time.timeAsDouble;

        _runActive = false;

        // cancel the rest of the shard
        var gmRun = UnityEngine.Object.FindObjectOfType<GM_Run>();
        if (gmRun)
        {
            var done = gmRun.GetType().GetField("done", BindingFlags.Instance | BindingFlags.NonPublic);
            if (done != null)
                done.SetValue(gmRun, true);
            else
                Debug.LogError("Could not find Done field on GM_Run");
        }
        else
            Debug.LogError("Could not find GM_Run object");

        var duration = TimeSpan.FromSeconds(now - _newLevelStartTime);
        MessageScreen.instance.DisplayText("Leaderboard Run: " + duration, 2f);

        UploadScore(duration);

        SimpleRunHandler.EndRun();
        MonoFunctions.DelayCall(SimpleRunHandler.TransitionToHub, 2f);
    }

    private static async void UploadScore(TimeSpan duration)
    {
        try
        {
            var res = await SteamUserStats.UploadLeaderboardScore(_leaderboard, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, (int)duration.TotalMilliseconds, [], 0).ToAsync<LeaderboardScoreUploaded_t>();
            Debug.Log($"Leaderboard upload result: success={res.m_bSuccess} score={res.m_nScore} scoreChanged={res.m_bScoreChanged} prevRank={res.m_nGlobalRankPrevious} newRank={res.m_nGlobalRankNew}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
