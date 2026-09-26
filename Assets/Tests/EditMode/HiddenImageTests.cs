using NUnit.Framework;
using UnityEngine;

namespace GalsPanic.Tests
{
    public class HiddenImageTests
    {
        [Test]
        public void Resample_CenterCropsWiderTexture()
        {
            // 8x2 텍스처: 왼쪽 1/4 빨강, 가운데 절반 초록, 오른쪽 1/4 파랑. 4:3 보드로 샘플링하면 가운데 초록만 남아야 한다.
            var tex = new Texture2D(8, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 8; x++)
                    tex.SetPixel(x, y, x < 2 ? Color.red : x < 6 ? Color.green : Color.blue);
            tex.Apply();
            try
            {
                Color32[] px = HiddenImage.Resample(tex, 4, 3);
                Assert.AreEqual(12, px.Length);
                // 8x2 (4:1) → 4:3 보드: 가로를 3/4 잘라내 가운데 25%~75% 구간(=초록)만 사용
                foreach (Color32 c in px) Assert.AreEqual((Color32)Color.green, c);
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void ForStage_FallsBackToGeneratedWhenNoFiles()
        {
            // 테스트 환경에서는 HiddenImages 폴더에 이미지가 없을 수 있으므로, 결과가 항상 보드 크기여야 한다는 점만 확인
            Color32[] px = HiddenImage.ForStage(30, 20, 1);
            Assert.AreEqual(600, px.Length);
        }
    }
}
