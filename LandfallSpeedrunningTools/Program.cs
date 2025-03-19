using Landfall.Modding;
using QRCoder.Core;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LandfallSpeedrunningTools;

[LandfallPlugin]
public class SpeedrunningPlugin
{
    public const string SpeedrunningSetting = "Speedrunning";

    static SpeedrunningPlugin()
    {
        var go = new GameObject(nameof(SeedQRCode));
        Object.DontDestroyOnLoad(go);
        go.AddComponent<SeedQRCode>();
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
            if (isTransitioning)
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
        var seedData = QRCodeGenerator.GenerateQrCodeNumeric(((uint)RunHandler.RunData.currentSeed).ToString(), QRCodeGenerator.ECCLevel.Q);
        var nowData = QRCodeGenerator.GenerateQrCodeNumeric(((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString(), QRCodeGenerator.ECCLevel.Q);
        var height = Mathf.Max(seedData.ModuleMatrix.Count, nowData.ModuleMatrix.Count);
        var seedWidth = seedData.ModuleMatrix[0].Count;
        var width = seedWidth + nowData.ModuleMatrix[0].Count;
        var colors = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                QRCodeData data;
                var imgX = x;
                if (imgX > seedWidth)
                {
                    imgX -= seedWidth;
                    data = nowData;
                }
                else
                {
                    data = seedData;
                }
                Color color;
                if (y >= data.ModuleMatrix.Count || imgX >= data.ModuleMatrix[y].Count)
                    color = Color.white;
                else
                    color = data.ModuleMatrix[y][imgX] ? Color.black : Color.white;
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
