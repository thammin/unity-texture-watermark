using Accord.Math;
using Accord.Math.Wavelets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The embedding flow is adapted from https://github.com/mchall/HiddenWatermark
/// </summary>
public class Watermark : MonoBehaviour
{
    private const int WatermarkSize = 64;
    private const int DWTInteration = 2;
    private const int DCTBlockSize = 4;
    private const float Sigma = 10f / 255f;
    private int EmbedSize => WatermarkSize * DWTInteration * DCTBlockSize * 2;
    private readonly static (int x, int y)[] MidBands = new (int x, int y)[]
    {
        (1, 2), (2, 0), (2, 1), (2, 2)
    };

    public RawImage inputImage;
    public RawImage outputImage;
    public RawImage watermark;
    public RawImage embedWatermark;
    public RawImage recoveredWatermark;

    public bool isJpegAttack;
    public int jpegQuality = 75;

    void Start()
    {
        if (IsEmbedable() == false) return;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var embedWatermark = EmbedWatermark();

        stopwatch.Stop();
        Debug.Log($"EmbedWatermark : {stopwatch.Elapsed.TotalMilliseconds} ms");
        stopwatch.Restart();

        DebugEmbedTexture(embedWatermark);

        stopwatch.Stop();
        Debug.Log($"DebugEmbedTexture : {stopwatch.Elapsed.TotalMilliseconds} ms");
        stopwatch.Restart();

        var output = MergeWatermarkToInputImage(embedWatermark);

        stopwatch.Stop();
        Debug.Log($"MergeWatermarkToInputImage : {stopwatch.Elapsed.TotalMilliseconds} ms");
        stopwatch.Restart();

        DebugOutputTexture(output);

        stopwatch.Stop();
        Debug.Log($"DebugOutputTexture : {stopwatch.Elapsed.TotalMilliseconds} ms");
        stopwatch.Restart();

        var recoveredWatermark = RetrieveWatermark(outputImage.texture as Texture2D);

        stopwatch.Stop();
        Debug.Log($"RetrieveWatermark : {stopwatch.Elapsed.TotalMilliseconds} ms");
        stopwatch.Restart();

        DebugRecoveredWatermark(recoveredWatermark);

        stopwatch.Stop();
        Debug.Log($"DebugRecoveredWatermark : {stopwatch.Elapsed.TotalMilliseconds} ms");
    }

    private bool IsEmbedable()
    {
        var texture = inputImage.texture as Texture2D;

        if (texture.width < EmbedSize || texture.height < EmbedSize)
        {
            Debug.LogWarning("Input texture is smaller than embed watermark size");
            return false;
        }

        return true;
    }

    private Color[] EmbedWatermark()
    {
        var texture = watermark.texture as Texture2D;
        var pixels = texture.GetPixels();

        var data = CreateEmbedData();

        Haar.FWT(data, DWTInteration);
        var subband = data.LL2();

        for (var y = 0; y < WatermarkSize; y++)
        {
            for (int x = 0; x < WatermarkSize; x++)
            {
                var block = GetBlock(subband, x, y);

                CosineTransform.DCT(block);

                var midbandSum = Mathf.Max(2, Mathf.Abs(GetMidBandSum(block)));
                var sigma = pixels[y * WatermarkSize + x].r > 0.5 ? Sigma : -Sigma;

                foreach (var pos in MidBands)
                {
                    block[pos.x, pos.y] += midbandSum * sigma;
                }

                CosineTransform.IDCT(block);

                for (int i = 0; i < DCTBlockSize; i++)
                {
                    for (int j = 0; j < DCTBlockSize; j++)
                    {
                        subband[x * DCTBlockSize + i, y * DCTBlockSize + j] = block[i, j];
                    }
                }
            }
        };

        BackApplySubBand(data, subband);
        Haar.IWT(data, 2);

        var colors = new Color[EmbedSize * EmbedSize];
        for (var x = 0; x < EmbedSize; x++)
        {
            for (var y = 0; y < EmbedSize; y++)
            {
                colors[y * EmbedSize + x] = Utility.YUVToRGB(new Color(0, data[x, y], 0));
            }
        }

        return colors;
    }

    private void DebugEmbedTexture(Color[] pixels)
    {
        var texture = new Texture2D(EmbedSize, EmbedSize);
        var debugPixels = new Color[pixels.Length];

        for (var i = 0; i < pixels.Length; i++)
        {
            debugPixels[i].r = 0.5f - pixels[i].r;
            debugPixels[i].g = 0.5f - pixels[i].g;
            debugPixels[i].b = 0.5f - pixels[i].b;
            debugPixels[i].a = 1;
        }

        texture.SetPixels(debugPixels);
        texture.Apply();
        embedWatermark.texture = texture;
    }

    private Color[] MergeWatermarkToInputImage(Color[] pixels)
    {
        var inputTexture = inputImage.texture as Texture2D;
        var outputPixels = inputTexture.GetPixels();

        for (var i = 0; i < outputPixels.Length; i++)
        {
            outputPixels[i] += pixels[i];
        }

        return outputPixels;
    }

    private void DebugOutputTexture(Color[] pixels)
    {
        var inputTexture = inputImage.texture as Texture2D;
        var outputTexture = new Texture2D(inputTexture.width, inputTexture.height);

        outputTexture.SetPixels(pixels);
        outputTexture.Apply();
        outputImage.texture = outputTexture;

        if (isJpegAttack)
        {
            var imageBytes = outputTexture.EncodeToJPG(jpegQuality);
            outputTexture.LoadImage(imageBytes);
            outputTexture.Apply();
        }
    }

    private Color[] RetrieveWatermark(Texture2D texture)
    {
        var inputTexture = inputImage.texture as Texture2D;
        Color[] pixels = null;

        // If the size was changed, resize to original size before getting pixels
        if (texture.width != inputTexture.width)
        {
            var scaledTexture = new Texture2D(inputTexture.width, inputTexture.height);
            var currentRT = RenderTexture.active;
            var renderTexture = RenderTexture.GetTemporary(inputTexture.width, inputTexture.height);
            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;
            scaledTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            scaledTexture.Apply();
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
            pixels = scaledTexture.GetPixels();
        }
        else
        {
            pixels = texture.GetPixels();
        }

        var embedData = new float[EmbedSize, EmbedSize];

        for (var x = 0; x < EmbedSize; x++)
        {
            for (var y = 0; y < EmbedSize; y++)
            {
                embedData[x, y] = Utility.RgbToU(pixels[y * EmbedSize + x]);
            }
        }

        var watermarkTexture = watermark.texture as Texture2D;
        var recoveredWatermarkData = new float[watermarkTexture.width, watermarkTexture.height];

        Haar.FWT(embedData, 2);
        var subband = embedData.LL2();

        var width = watermarkTexture.width;
        var height = watermarkTexture.height;

        for (var y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var block = GetBlock(subband, x, y);

                CosineTransform.DCT(block);
                recoveredWatermarkData[x, y] = GetMidBandSum(block) > 0 ? 1 : 0;
            }
        };

        var recoverPixels = new Color[width * height];
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                var value = recoveredWatermarkData[i, j];
                recoverPixels[j * width + i] = new Color(value, value, value, 1);
            }
        }

        return recoverPixels;
    }

    private void DebugRecoveredWatermark(Color[] pixels)
    {
        var texture = new Texture2D(WatermarkSize, WatermarkSize);
        texture.SetPixels(pixels);
        texture.Apply();
        recoveredWatermark.texture = texture;
    }

    private float[,] CreateEmbedData()
    {
        var embedData = new float[EmbedSize, EmbedSize];

        for (int x = 0; x < EmbedSize; x++)
        {
            for (int y = 0; y < EmbedSize; y++)
            {
                embedData[x, y] = 0;
            }
        }

        return embedData;
    }

    private float GetMidBandSum(float[,] block)
    {
        var sum = 0f;

        foreach (var pos in MidBands)
        {
            sum += block[pos.x, pos.y];
        }

        return sum;
    }

    private void BackApplySubBand(float[,] original, float[,] subBandData)
    {
        var width = original.GetUpperBound(0) + 1;
        var height = original.GetUpperBound(1) + 1;

        for (int x = 0; x < width / 4; x++)
        {
            for (int y = 0; y < height / 4; y++)
            {
                original[x, y] = subBandData[x, y];
            }
        }
    }

    private float[,] GetBlock(float[,] subband, int x, int y)
    {
        return subband.Submatrix(
            x * DCTBlockSize,
            x * DCTBlockSize + DCTBlockSize - 1,
            y * DCTBlockSize,
            y * DCTBlockSize + DCTBlockSize - 1
        );
    }
}
