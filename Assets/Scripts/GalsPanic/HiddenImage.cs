using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GalsPanic
{
    /// <summary>
    /// 영역을 차지하면 드러나는 숨겨진 그림.
    /// StreamingAssets/HiddenImages 폴더에 PNG/JPG 파일이 있으면 스테이지 순서대로 사용하고,
    /// 없으면 스테이지마다 색과 지형이 달라지는 풍경을 절차적으로 생성합니다.
    /// </summary>
    public static class HiddenImage
    {
        public const string FolderName = "HiddenImages";
        private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg" };

        /// <summary>스테이지에 쓸 그림을 보드 크기의 픽셀 배열로 돌려줍니다.</summary>
        public static Color32[] ForStage(int w, int h, int stage)
        {
            Color32[] fromFile = LoadFromStreamingAssets(w, h, stage);
            return fromFile ?? Generate(w, h, stage);
        }

        /// <summary>StreamingAssets/HiddenImages 의 파일을 이름순으로 정렬해 (stage-1) 번째(순환)를 읽습니다. 파일이 없거나 실패하면 null.</summary>
        public static Color32[] LoadFromStreamingAssets(int w, int h, int stage)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, FolderName);
            if (!Directory.Exists(dir)) return null;

            string[] files = Directory.GetFiles(dir)
                .Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (files.Length == 0) return null;

            string path = files[(Mathf.Max(1, stage) - 1) % files.Length];
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!tex.LoadImage(File.ReadAllBytes(path)))
                {
                    Debug.LogWarning($"[HiddenImage] 이미지를 읽을 수 없습니다: {path}");
                    return null;
                }
                Debug.Log($"[HiddenImage] Loaded {Path.GetFileName(path)} ({tex.width}x{tex.height}) for stage {stage}");
                return Resample(tex, w, h);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HiddenImage] {path}: {e.Message}");
                return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        /// <summary>
        /// 텍스처를 보드 크기로 다시 샘플링합니다. 비율이 다르면 가운데를 기준으로 잘라서 꽉 채웁니다(center crop).
        /// </summary>
        public static Color32[] Resample(Texture2D tex, int w, int h)
        {
            float boardAspect = (float)w / h;
            float texAspect = (float)tex.width / tex.height;
            // 보드 비율에 맞춰 텍스처에서 사용할 UV 범위를 정한다.
            float uScale = 1f, vScale = 1f;
            if (texAspect > boardAspect) uScale = boardAspect / texAspect; // 가로가 남음 → 좌우를 자름
            else vScale = texAspect / boardAspect;                          // 세로가 남음 → 위아래를 자름
            float u0 = (1f - uScale) * 0.5f, v0 = (1f - vScale) * 0.5f;

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = v0 + (y + 0.5f) / h * vScale;
                for (int x = 0; x < w; x++)
                {
                    float u = u0 + (x + 0.5f) / w * uScale;
                    px[y * w + x] = tex.GetPixelBilinear(u, v);
                }
            }
            return px;
        }

        /// <summary>절차 생성 풍경. 스테이지 번호가 시드라서 같은 스테이지는 항상 같은 그림입니다.</summary>
        public static Color32[] Generate(int w, int h, int seed)
        {
            var rng = new System.Random(seed * 7919 + 17);
            float hue = (float)rng.NextDouble();

            Color skyTop = Color.HSVToRGB(hue, 0.6f, 0.35f);
            Color skyBottom = Color.HSVToRGB((hue + 0.08f) % 1f, 0.45f, 1f);
            Color sunColor = Color.HSVToRGB((hue + 0.12f) % 1f, 0.3f, 1f);
            float sunX = w * (0.25f + 0.5f * (float)rng.NextDouble());
            float sunY = h * (0.6f + 0.25f * (float)rng.NextDouble());
            float sunR = h * 0.12f;

            const int layers = 3;
            var phase = new float[layers];
            var freq = new float[layers];
            var baseH = new float[layers];
            var hillColor = new Color[layers];
            for (int l = 0; l < layers; l++)
            {
                phase[l] = (float)rng.NextDouble() * 10f;
                freq[l] = 1.5f + l + (float)rng.NextDouble();
                baseH[l] = h * (0.42f - l * 0.11f);
                hillColor[l] = Color.HSVToRGB((hue + 0.35f + l * 0.05f) % 1f, 0.55f, 0.75f - l * 0.2f);
            }

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    Color c = Color.Lerp(skyBottom, skyTop, t);

                    float dx = x - sunX, dy = y - sunY;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < sunR) c = sunColor;
                    else if (d < sunR * 2.2f) c = Color.Lerp(c, sunColor, 0.35f * (1f - (d - sunR) / (sunR * 1.2f)));

                    float u = x / (float)w;
                    for (int l = 0; l < layers; l++)
                    {
                        float hy = baseH[l]
                                   + Mathf.Sin(u * freq[l] * 6.2832f + phase[l]) * h * 0.06f
                                   + Mathf.Sin(u * freq[l] * 2.7f * 6.2832f + phase[l] * 1.7f) * h * 0.025f;
                        if (y < hy) c = hillColor[l];
                    }

                    px[y * w + x] = c;
                }
            }
            return px;
        }
    }
}
