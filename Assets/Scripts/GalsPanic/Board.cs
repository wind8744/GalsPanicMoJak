using System.Collections.Generic;
using UnityEngine;

namespace GalsPanic
{
    public enum Cell : byte { Unclaimed, Claimed, Trail }

    /// <summary>
    /// 격자 보드. 셀 상태 저장, 영역 채우기(flood fill), 텍스처 렌더링을 담당합니다.
    /// 바깥 테두리 한 칸은 처음부터 차지된 상태이며 플레이어가 걸어 다니는 길이 됩니다.
    /// </summary>
    public class Board : MonoBehaviour
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int ClaimedCount { get; private set; }
        public int InteriorCount => (Width - 2) * (Height - 2);
        public float ClaimedRatio => InteriorCount == 0 ? 0f : (float)ClaimedCount / InteriorCount;

        private static readonly Vector2Int[] Dirs4 =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        private Cell[] _cells;
        private Color32[] _image;
        private Color32[] _pixels;
        private Texture2D _texture;
        private bool _dirty = true;

        public void Init(int width, int height, int seed)
        {
            Width = width;
            Height = height;
            _cells = new Cell[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    _cells[y * width + x] = border ? Cell.Claimed : Cell.Unclaimed;
                }
            }
            ClaimedCount = 0;

            _image = HiddenImage.ForStage(width, height, seed);
            _pixels = new Color32[width * height];
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var sprite = Sprite.Create(_texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f, 0,
                SpriteMeshType.FullRect);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 0;

            _dirty = true;
            Redraw();
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        /// <summary>보드 밖은 벽(차지된 셀)으로 취급합니다.</summary>
        public Cell Get(int x, int y) => InBounds(x, y) ? _cells[y * Width + x] : Cell.Claimed;
        public Cell Get(Vector2Int c) => Get(c.x, c.y);

        public void Set(Vector2Int c, Cell value)
        {
            _cells[c.y * Width + c.x] = value;
            _dirty = true;
        }

        /// <summary>플레이어가 걸을 수 있는 셀: 차지된 셀 중 8방향 이웃에 미차지 셀이 있는 것.</summary>
        public bool IsEdge(int x, int y)
        {
            if (Get(x, y) != Cell.Claimed) return false;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (InBounds(nx, ny) && _cells[ny * Width + nx] == Cell.Unclaimed) return true;
                }
            }
            return false;
        }
        public bool IsEdge(Vector2Int c) => IsEdge(c.x, c.y);

        /// <summary>셀 단위 연속 좌표를 월드 좌표로 변환합니다. (x, y)는 셀 (x, y)의 왼쪽 아래 모서리입니다.</summary>
        public Vector3 PointToWorld(Vector2 p) => new Vector3(p.x - Width * 0.5f, p.y - Height * 0.5f, 0f);
        public Vector3 CellToWorld(Vector2Int c) => PointToWorld(new Vector2(c.x + 0.5f, c.y + 0.5f));

        /// <summary>
        /// 궤적(Trail)으로 닫힌 뒤 호출. 보스가 있는 미차지 영역만 남기고 나머지 미차지 영역과 궤적을 모두 차지합니다.
        /// 새로 차지한 셀 수를 반환합니다.
        /// </summary>
        public int Claim(Vector2Int bossCell)
        {
            var reachable = new bool[_cells.Length];
            var queue = new Queue<Vector2Int>();
            Vector2Int start = FindUnclaimedNear(bossCell);
            if (start.x >= 0)
            {
                reachable[start.y * Width + start.x] = true;
                queue.Enqueue(start);
            }

            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int d in Dirs4)
                {
                    Vector2Int n = c + d;
                    if (!InBounds(n.x, n.y)) continue;
                    int i = n.y * Width + n.x;
                    if (reachable[i] || _cells[i] != Cell.Unclaimed) continue;
                    reachable[i] = true;
                    queue.Enqueue(n);
                }
            }

            int gained = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                bool fill = _cells[i] == Cell.Trail || (_cells[i] == Cell.Unclaimed && !reachable[i]);
                if (!fill) continue;
                _cells[i] = Cell.Claimed;
                gained++;
            }

            ClaimedCount += gained;
            _dirty = true;
            return gained;
        }

        public void ClearTrail(List<Vector2Int> trail)
        {
            foreach (Vector2Int c in trail)
            {
                if (Get(c) == Cell.Trail) Set(c, Cell.Unclaimed);
            }
        }

        private Vector2Int FindUnclaimedNear(Vector2Int c)
        {
            for (int r = 0; r < 10; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        int x = c.x + dx, y = c.y + dy;
                        if (Get(x, y) == Cell.Unclaimed) return new Vector2Int(x, y);
                    }
                }
            }
            return new Vector2Int(-1, -1);
        }

        private void LateUpdate()
        {
            if (_dirty) Redraw();
        }

        private void Redraw()
        {
            _dirty = false;
            var white = new Color32(255, 255, 255, 255);
            var trailColor = new Color32(255, 96, 48, 255);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    Color32 img = _image[i];
                    switch (_cells[i])
                    {
                        case Cell.Unclaimed:
                            _pixels[i] = new Color32((byte)(img.r / 7), (byte)(img.g / 7), (byte)(img.b / 6 + 18), 255);
                            break;
                        case Cell.Trail:
                            _pixels[i] = trailColor;
                            break;
                        default:
                            _pixels[i] = IsEdge(x, y) ? Color32.Lerp(img, white, 0.55f) : img;
                            break;
                    }
                }
            }
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }
    }
}
