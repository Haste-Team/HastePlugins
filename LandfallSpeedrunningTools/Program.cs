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
        go.AddComponent<SeedQRCode>();
        go.AddComponent<LiveSplitUpdater>();
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

internal class SeedQRCode : MonoBehaviour
{
    private bool _isTransitioning;

    private void Update()
    {
        var isTransitioning = UI_TransitionHandler.IsTransitioning;
        if (isTransitioning != _isTransitioning)
        {
            _isTransitioning = isTransitioning;
            if (isTransitioning && RunHandler.RunData.currentSeed != -1)
            {
                GameObject? GetActiveChild()
                {
                    foreach (Transform child in UI_TransitionHandler.instance.transform)
                        if (child.gameObject.activeSelf)
                            return child.gameObject;
                    return null;
                }

                var activeChild = GetActiveChild();
                if (activeChild == null)
                    return;

                Debug.Log("SeedQRCode: enable");

                var texture = GetTexture();
                var image = activeChild.GetComponent<Image>();
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }
            else
            {
                Debug.Log("SeedQRCode: disable");
            }
        }
    }

    private static Texture2D GetTexture()
    {
        var seed = RunHandler.RunData.currentSeed;
        var nrOfLevels = RunHandler.config.nrOfLevels;
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var version = new BuildVersion(Application.version).ToString().Replace("\"", "");
        var rawData = $$"""{"seed":{{seed}},"nrOfLevels":{{nrOfLevels}},"time":{{time}},"ver":"{{version}}"}""";
        var seedData = QRCodeGenerator.GenerateQrCode(rawData, QRCodeGenerator.ECCLevel.Q);
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
                    color = seedData.ModuleMatrix[y][x] ? Color.black : Color.white;
                colors[y * width + x] = color;
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
