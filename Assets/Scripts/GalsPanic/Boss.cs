using UnityEngine;

namespace GalsPanic
{
    /// <summary>
    /// 미차지 영역 안을 튕기며 돌아다니는 보스. 플레이어의 궤적에 닿으면 플레이어가 목숨을 잃습니다.
    /// 차지된 셀에서는 튕기지만 궤적은 통과합니다.
    /// </summary>
    public class Boss : MonoBehaviour
    {
        public float Radius = 2.5f;

        public Vector2 Pos { get; private set; }
        public Vector2Int GridPos => new Vector2Int(Mathf.FloorToInt(Pos.x), Mathf.FloorToInt(Pos.y));

        private Board _board;
        private GalsPanicGame _game;
        private Vector2 _vel;
        private float _speed;
        private float _jitterTimer;

        public void Init(Board board, GalsPanicGame game, Vector2 start, float speed)
        {
            _board = board;
            _game = game;
            Pos = start;
            _speed = speed;
            float angle = Random.Range(0.5f, 1.1f) * (Random.value < 0.5f ? 1f : -1f);
            if (Random.value < 0.5f) angle += Mathf.PI;
            _vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            transform.position = board.PointToWorld(Pos);
            transform.localScale = Vector3.one * (Radius * 2f);
        }

        private void Update()
        {
            if (_game == null || !_game.IsPlaying) return;
            float dt = Time.deltaTime;

            Vector2 next = Pos + _vel * dt;
            if (Blocked(next.x + Mathf.Sign(_vel.x) * Radius, Pos.y))
            {
                _vel.x = -_vel.x;
                next.x = Pos.x;
            }
            if (Blocked(Pos.x, next.y + Mathf.Sign(_vel.y) * Radius))
            {
                _vel.y = -_vel.y;
                next.y = Pos.y;
            }
            Pos = next;

            _jitterTimer -= dt;
            if (_jitterTimer <= 0f)
            {
                _jitterTimer = Random.Range(0.6f, 1.6f);
                _vel = Rotate(_vel, Random.Range(-35f, 35f)).normalized * _speed;
            }

            if (Blocked(Pos.x, Pos.y)) Unstick();

            transform.position = _board.PointToWorld(Pos);
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 6f);
            transform.localScale = Vector3.one * (Radius * 2f * pulse);
        }

        /// <summary>보스 원 안에 궤적 셀이 있으면 true.</summary>
        public bool TouchesTrail()
        {
            int r = Mathf.CeilToInt(Radius);
            Vector2Int c = GridPos;
            float rr = Radius * Radius;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = c.x + dx, y = c.y + dy;
                    if (_board.Get(x, y) != Cell.Trail) continue;
                    float cx = x + 0.5f - Pos.x, cy = y + 0.5f - Pos.y;
                    if (cx * cx + cy * cy <= rr) return true;
                }
            }
            return false;
        }

        private bool Blocked(float fx, float fy)
        {
            return _board.Get(Mathf.FloorToInt(fx), Mathf.FloorToInt(fy)) == Cell.Claimed;
        }

        private void Unstick()
        {
            Vector2Int c = GridPos;
            for (int r = 1; r < 12; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int x = c.x + dx, y = c.y + dy;
                        if (_board.Get(x, y) != Cell.Unclaimed) continue;
                        Pos = new Vector2(x + 0.5f, y + 0.5f);
                        return;
                    }
                }
            }
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
