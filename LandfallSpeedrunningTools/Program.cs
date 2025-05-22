using Landfall.Modding;
using QRCoder.Core;
using UnityEngine;
using UnityEngine.UI;
using Zorro.Core;
using Object = UnityEngine.Object;

namespace LandfallSpeedrunningTools;

[LandfallPlugin]
public class SpeedrunningPlugin
{
    public const string SpeedrunningSetting = "Speedrunning";

    static SpeedrunningPlugin()
    {
        var go = new GameObject(nameof(SpeedrunningPlugin));
        Object.DontDestroyOnLoad(go);
        go.AddComponent<LiveSplitUpdater>();
        SeedQRCode.Init();
    }
}

public class LiveSplitUpdater : MonoBehaviour
{
    private bool isTransitioning;

    private void Update()
    {
        var newTransitioning = UI_TransitionHandler.IsTransitioning;
        if (isTransitioning != newTransitioning)
        {
            isTransitioning = newTransitioning;
            if (isTransitioning)
                LiveSplit.Instance?.PauseGameTime();
            else
                LiveSplit.Instance?.UnpauseGameTime();
        }
    }
}

internal static class SeedQRCode
{
    public static void Init()
    {
        On.LevelSelectionHandler.Generate += (orig, self) =>
        {
            var ui = GameObject.Find("UI_Gameplay_Minimal");
            GameObject imgObject = new GameObject("SeedQRCode");
            Image image = imgObject.AddComponent<Image>();
            var tex = GetTexture();
            image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            imgObject.transform.SetParent(ui.transform, false);

            RectTransform rectTransform = imgObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.86f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);

            var fit = imgObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fit.aspectRatio = (float)tex.width / tex.height;

            orig(self);
        };
    }

    private static Texture2D GetTexture()
    {
        var seed = RunHandler.RunData.currentSeed;
        var shardID = RunHandler.RunData.shardID;
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var version = new BuildVersion(Application.version).ToString().Replace("\"", "");
        var rawData = $$"""{"seed":{{seed}},"shardID":{{shardID}},"time":{{time}},"ver":"{{version}}"}""";

        var seedData = QRCodeGenerator.GenerateQrCode(rawData, QRCodeGenerator.ECCLevel.L);

        var height = seedData.ModuleMatrix.Count;
        var width = seedData.ModuleMatrix[0].Count;
        var colors = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                Color color;
                if (y >= seedData.ModuleMatrix.Count || x >= seedData.ModuleMatrix[y].Count)
                    color = Color.white;
                else
                    color = seedData.ModuleMatrix[y][x] ? Color.clear : Color.white;
                color.a *= 0.12f;

                // The modules use a bottom-left origin, but the texture uses top-left
                // We need to flip the texture.
                colors[(height - 1 - y) * width + x] = color;
            }
        }
        var texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
}
