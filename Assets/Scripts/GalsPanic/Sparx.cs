using UnityEngine;

namespace GalsPanic
{
    /// <summary>
    /// 차지한 영역의 가장자리를 따라 순찰하는 적. 플레이어와 닿으면 (그리는 중이 아니어도) 목숨을 잃습니다.
    /// 진행 방향을 최대한 유지하며 8방향으로 가장자리 셀을 따라가고, 막다른 곳에서만 되돌아갑니다.
    /// </summary>
    public class Sparx : MonoBehaviour
    {
        public float StepsPerSecond = 10f;

        public Vector2Int GridPos { get; private set; }

        private static readonly Vector2Int[] Dirs8 =
        {
            new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
            new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        };

        private Board _board;
        private GalsPanicGame _game;
        private Vector2Int _prev;
        private Vector2Int _heading;
        private float _timer;

        public void Init(Board board, GalsPanicGame game, Vector2Int start, Vector2Int heading, float stepsPerSecond)
        {
            _board = board;
            _game = game;
            GridPos = start;
            _prev = start;
            _heading = heading;
            StepsPerSecond = stepsPerSecond;
            transform.position = board.CellToWorld(start);
        }

        private void Update()
        {
            if (_game == null || !_game.IsPlaying) return;

            float interval = 1f / StepsPerSecond;
            _timer += Time.deltaTime;
            int steps = 0;
            while (_timer >= interval && steps < 4)
            {
                _timer -= interval;
                Step();
                steps++;
            }
            transform.position = _board.CellToWorld(GridPos);
        }

        /// <summary>가장자리 셀을 따라 한 칸 이동합니다. 현재 셀이 더 이상 가장자리가 아니면 가장 가까운 가장자리로 옮깁니다.</summary>
        public void Step()
        {
            if (!_board.IsEdge(GridPos))
            {
                Relocate();
                return;
            }

            Vector2 heading = _heading == Vector2Int.zero ? Vector2.right : ((Vector2)_heading).normalized;
            Vector2Int best = GridPos;
            float bestScore = float.NegativeInfinity;
            foreach (Vector2Int d in Dirs8)
            {
                Vector2Int n = GridPos + d;
                if (n == _prev || !_board.IsEdge(n)) continue;
                float score = Vector2.Dot(((Vector2)d).normalized, heading) + Random.value * 0.01f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = n;
                }
            }

            if (best == GridPos)
            {
                if (!_board.IsEdge(_prev) || _prev == GridPos) return;
                best = _prev; // 막다른 길: 되돌아감
            }

            _heading = best - GridPos;
            _prev = GridPos;
            GridPos = best;
        }

        private void Relocate()
        {
            Vector2Int c = GridPos;
            for (int r = 1; r < 40; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var n = new Vector2Int(c.x + dx, c.y + dy);
                        if (!_board.IsEdge(n)) continue;
                        GridPos = n;
                        _prev = n;
                        return;
                    }
                }
            }
        }
    }
}
