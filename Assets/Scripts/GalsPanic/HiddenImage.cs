using UnityEngine;

namespace GalsPanic
{
    /// <summary>
    /// 영역을 차지하면 드러나는 숨겨진 그림을 절차적으로 생성합니다. 스테이지마다 색과 지형이 달라집니다.
    /// </summary>
    public static class HiddenImage
    {
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
