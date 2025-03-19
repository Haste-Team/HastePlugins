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

                var texture = GetTexture(BitConverter.GetBytes(RunHandler.RunData.currentSeed));
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

    private static Texture2D GetTexture(byte[] data) => GetTexture(QRCodeGenerator.GenerateQrCode(data, QRCodeGenerator.ECCLevel.Q));

    private static Texture2D GetTexture(QRCodeData codeData) => GetTexture(codeData, Color.black, Color.white);

    private static Texture2D GetTexture(QRCodeData codeData, Color dark, Color light)
    {
        var size = codeData.ModuleMatrix.Count;
        var colors = new Color[size * size];
        var i = 0;
        foreach (var row in codeData.ModuleMatrix)
            for (var x = 0; x < row.Length; x++)
                colors[i++] = row[x] ? dark : light;
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
}
