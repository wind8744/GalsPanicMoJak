using System.Collections.Generic;
using UnityEngine;

namespace GalsPanic
{
    /// <summary>
    /// 플레이어. 차지된 영역의 가장자리를 따라 움직이고, 미차지 영역으로 들어가면 궤적을 그립니다.
    /// 궤적이 다시 가장자리에 닿으면 영역이 닫힙니다. 방향키 또는 WASD로 조작합니다.
    /// </summary>
    public class PlayerMover : MonoBehaviour
    {
        public float StepsPerSecond = 28f;
        public float DrawStepsPerSecond = 18f;

        public Vector2Int GridPos { get; private set; }
        public bool Drawing { get; private set; }
        public List<Vector2Int> Trail { get; } = new List<Vector2Int>();

        private static readonly Vector2Int[] Dirs4 =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        private static readonly (KeyCode[] keys, Vector2Int dir)[] KeyMap =
        {
            (new[] { KeyCode.UpArrow, KeyCode.W }, Vector2Int.up),
            (new[] { KeyCode.DownArrow, KeyCode.S }, Vector2Int.down),
            (new[] { KeyCode.LeftArrow, KeyCode.A }, Vector2Int.left),
            (new[] { KeyCode.RightArrow, KeyCode.D }, Vector2Int.right),
        };

        private Board _board;
        private GalsPanicGame _game;
        private Vector2Int _trailStart;
        private Vector2Int _held;
        private float _timer;

        public void Init(Board board, GalsPanicGame game, Vector2Int start)
        {
            _board = board;
            _game = game;
            GridPos = start;
            _trailStart = start;
            Drawing = false;
            Trail.Clear();
            transform.position = board.CellToWorld(GridPos);
        }

        private void Update()
        {
            if (_game == null || !_game.IsPlaying) return;

            float interval = 1f / (Drawing ? DrawStepsPerSecond : StepsPerSecond);
            Vector2Int dir = ReadDirection();
            if (dir == Vector2Int.zero)
            {
                _timer = interval; // 다음 입력에 즉시 반응
                return;
            }

            _timer += Time.deltaTime;
            int steps = 0;
            while (_timer >= interval && steps < 4)
            {
                _timer -= interval;
                TryStep(dir);
                steps++;
            }
        }

        private void LateUpdate()
        {
            if (_board != null) transform.position = _board.CellToWorld(GridPos);
        }

        /// <summary>목숨을 잃었을 때: 그리는 중이었다면 궤적을 지우고 궤적 시작점으로 돌아갑니다. 아니면 제자리에 있습니다.</summary>
        public void Die()
        {
            if (!Drawing) return;
            _board.ClearTrail(Trail);
            Trail.Clear();
            Drawing = false;
            GridPos = _trailStart;
        }

        private Vector2Int ReadDirection()
        {
            foreach (var (keys, dir) in KeyMap)
            {
                if (AnyKeyDown(keys)) _held = dir;
            }
            if (_held != Vector2Int.zero && AnyKeyHeld(_held)) return _held;

            _held = Vector2Int.zero;
            foreach (var (keys, dir) in KeyMap)
            {
                if (AnyKey(keys)) { _held = dir; break; }
            }
            return _held;
        }

        private static bool AnyKeyDown(KeyCode[] keys)
        {
            foreach (KeyCode k in keys) if (Input.GetKeyDown(k)) return true;
            return false;
        }

        private static bool AnyKey(KeyCode[] keys)
        {
            foreach (KeyCode k in keys) if (Input.GetKey(k)) return true;
            return false;
        }

        private static bool AnyKeyHeld(Vector2Int dir)
        {
            foreach (var (keys, d) in KeyMap) if (d == dir) return AnyKey(keys);
            return false;
        }

        private void TryStep(Vector2Int dir)
        {
            Vector2Int target = GridPos + dir;
            if (!_board.InBounds(target.x, target.y)) return;
            Cell state = _board.Get(target);

            if (!Drawing)
            {
                if (state == Cell.Claimed && _board.IsEdge(target))
                {
                    GridPos = target;
                }
                else if (state == Cell.Unclaimed)
                {
                    _trailStart = GridPos;
                    Drawing = true;
                    Trail.Clear();
                    Enter(target);
                }
                return;
            }

            if (state == Cell.Unclaimed)
            {
                if (!TouchesOwnTrail(target)) Enter(target);
            }
            else if (state == Cell.Claimed)
            {
                GridPos = target;
                Drawing = false;
                _game.OnLoopClosed();
                Trail.Clear();
            }
        }

        private void Enter(Vector2Int target)
        {
            _board.Set(target, Cell.Trail);
            Trail.Add(target);
            GridPos = target;
        }

        /// <summary>자기 궤적과 나란히 붙는 이동을 막아 궤적이 스스로 닿지 않게 합니다.</summary>
        private bool TouchesOwnTrail(Vector2Int target)
        {
            foreach (Vector2Int d in Dirs4)
            {
                Vector2Int n = target + d;
                if (n != GridPos && _board.Get(n) == Cell.Trail) return true;
            }
            return false;
        }
    }
}
