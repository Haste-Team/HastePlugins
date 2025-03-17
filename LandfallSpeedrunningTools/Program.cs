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
    private GameObject? _qrCodeHolder;

    private void Update()
    {
        var isTransitioning = UI_TransitionHandler.IsTransitioning;
        if (isTransitioning != _qrCodeHolder)
        {
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
                _qrCodeHolder = new GameObject("QR Code Holder", typeof(RectTransform), typeof(AspectRatioFitter), typeof(RawImage));
                _qrCodeHolder.transform.SetParent(activeChild.transform, false);
                var rectTf = (RectTransform)_qrCodeHolder.transform;
                rectTf.anchorMin = Vector2.zero;
                rectTf.anchorMax = Vector2.one;
                rectTf.offsetMin = Vector2.zero;
                rectTf.offsetMax = Vector2.zero;
                var texture = GetTexture(BitConverter.GetBytes(RunHandler.RunData.currentSeed));
                var arf = _qrCodeHolder.GetComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                arf.aspectRatio = (float)texture.width / texture.height;
                var image = _qrCodeHolder.GetComponent<RawImage>();
                image.texture = texture;
                // var mask = activeChild.AddComponent<Mask>();
            }
            else
            {
                Debug.Log("SeedQRCode: disable");
                Destroy(_qrCodeHolder!.transform.parent.gameObject.GetComponent<Mask>());
                Destroy(_qrCodeHolder);
                _qrCodeHolder = null;
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
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
}
