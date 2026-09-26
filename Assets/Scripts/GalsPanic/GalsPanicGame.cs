using System.Collections.Generic;
using UnityEngine;

namespace GalsPanic
{
    /// <summary>
    /// 갈스패닉 스타일 게임 진행 관리. 스테이지 생성, 목숨, 점수, 시간 제한, 클리어/게임오버 판정, HUD를 담당합니다.
    /// 씬에는 이 컴포넌트가 붙은 오브젝트와 메인 카메라만 있으면 됩니다. 나머지는 런타임에 생성합니다.
    /// </summary>
    public class GalsPanicGame : MonoBehaviour
    {
        [SerializeField] private int _width = 120;
        [SerializeField] private int _height = 90;
        [SerializeField, Range(0.5f, 1f)] private float _targetRatio = 0.8f;
        [SerializeField] private int _startLives = 3;
        [SerializeField] private float _bossBaseSpeed = 12f;
        [SerializeField] private float _bossSpeedPerStage = 3f;
        [Header("Time Limit")]
        [SerializeField] private float _stageTimeBase = 100f;
        [SerializeField] private float _stageTimeStepPerStage = 5f;
        [SerializeField] private float _stageTimeMin = 60f;
        [Header("Patrol Enemies")]
        [SerializeField] private float _sparxBaseSpeed = 9f;
        [SerializeField] private float _sparxSpeedPerStage = 1.5f;
        [SerializeField] private int _sparxMax = 3;
        [SerializeField] private float _invulnerableSeconds = 1.5f;

        private enum State { Playing, StageClear, GameOver }

        private State _state;
        private int _stage = 1;
        private int _lives;
        private int _score;
        private Board _board;
        private PlayerMover _player;
        private Boss _boss;
        private readonly List<Sparx> _sparx = new List<Sparx>();
        private SpriteRenderer _playerRenderer;
        private float _timeLeft;
        private float _invulnerableUntil;
        private Sprite _circle;
        private string _flash = "";
        private float _flashTimer;
        private GUIStyle _hudStyle, _bigStyle, _subStyle, _timeStyle, _timeLowStyle;

        public bool IsPlaying => _state == State.Playing;
        public float StageTime => Mathf.Max(_stageTimeMin, _stageTimeBase - _stageTimeStepPerStage * (_stage - 1));

        private void Start()
        {
            _lives = _startLives;
            _score = 0;
            SetupCamera();
            BuildStage();
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Playing:
                    UpdatePlaying();
                    break;
                case State.StageClear:
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        _stage++;
                        BuildStage();
                    }
                    break;
                case State.GameOver:
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        _stage = 1;
                        _lives = _startLives;
                        _score = 0;
                        BuildStage();
                    }
                    break;
            }

            if (_flashTimer > 0f) _flashTimer -= Time.deltaTime;
        }

        private void UpdatePlaying()
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                _timeLeft = StageTime;
                LoseLife("TIME UP!");
                return;
            }

            if (_player.Drawing && _boss.TouchesTrail())
            {
                LoseLife("OUCH!");
                return;
            }

            bool invulnerable = Time.time < _invulnerableUntil;
            _playerRenderer.enabled = !invulnerable || Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
            if (invulnerable) return;

            foreach (Sparx s in _sparx)
            {
                Vector2Int d = s.GridPos - _player.GridPos;
                if (Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) <= 1)
                {
                    LoseLife("OUCH!");
                    return;
                }
            }
        }

        /// <summary>플레이어가 궤적을 닫았을 때 호출됩니다.</summary>
        public void OnLoopClosed()
        {
            int gained = _board.Claim(_boss.GridPos);
            _score += gained * 10;
            if (gained > _board.InteriorCount * 0.15f)
            {
                _score += gained * 10;
                Flash("BIG AREA! x2");
            }

            if (_board.ClaimedRatio >= _targetRatio)
            {
                _score += 5000;
                _state = State.StageClear;
            }
        }

        private void LoseLife(string message)
        {
            _lives--;
            _player.Die();
            _invulnerableUntil = Time.time + _invulnerableSeconds;
            Flash(message);
            if (_lives <= 0)
            {
                _playerRenderer.enabled = true;
                _state = State.GameOver;
            }
        }

        private void Flash(string text)
        {
            _flash = text;
            _flashTimer = 1.2f;
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = true;
            // 위쪽에 HUD 여백을 두기 위해 카메라를 살짝 올린다.
            cam.orthographicSize = _height * 0.5f + 10f;
            cam.transform.position = new Vector3(0f, 6f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        }

        private void BuildStage()
        {
            if (_board != null) Destroy(_board.gameObject);
            if (_player != null) Destroy(_player.gameObject);
            if (_boss != null) Destroy(_boss.gameObject);
            foreach (Sparx s in _sparx) if (s != null) Destroy(s.gameObject);
            _sparx.Clear();

            _board = new GameObject("Board").AddComponent<Board>();
            _board.Init(_width, _height, _stage);

            GameObject playerGo = MakeCircle("Player", new Color(1f, 0.9f, 0.2f), 3f, 2);
            _playerRenderer = playerGo.GetComponent<SpriteRenderer>();
            _player = playerGo.AddComponent<PlayerMover>();
            _player.Init(_board, this, new Vector2Int(_width / 2, 0));

            _boss = MakeCircle("Boss", new Color(1f, 0.25f, 0.7f), 1f, 1).AddComponent<Boss>();
            _boss.Radius = 2.5f;
            float speed = _bossBaseSpeed + _bossSpeedPerStage * (_stage - 1);
            _boss.Init(_board, this, new Vector2(_width * 0.5f, _height * 0.5f), speed);

            // 순찰 적: 플레이어 반대편(위쪽 변)에서 시작. 스테이지 1·2는 1마리, 3·4는 2마리, 5부터 3마리.
            int sparxCount = Mathf.Min(_sparxMax, 1 + (_stage - 1) / 2);
            float sparxSpeed = _sparxBaseSpeed + _sparxSpeedPerStage * (_stage - 1);
            var spawns = new[]
            {
                (new Vector2Int(_width / 2, _height - 1), Vector2Int.right),
                (new Vector2Int(_width / 2, _height - 1), Vector2Int.left),
                (new Vector2Int(0, _height / 2), Vector2Int.up),
            };
            for (int i = 0; i < sparxCount; i++)
            {
                var sparx = MakeCircle("Sparx", new Color(0.3f, 1f, 1f), 2.4f, 3).AddComponent<Sparx>();
                sparx.Init(_board, this, spawns[i].Item1, spawns[i].Item2, sparxSpeed);
                _sparx.Add(sparx);
            }

            _timeLeft = StageTime;
            _invulnerableUntil = Time.time + 1f;
            _state = State.Playing;
            Debug.Log($"[GalsPanic] Stage {_stage} started (boss speed {speed:0.#})");
        }

        private GameObject MakeCircle(string name, Color color, float diameter, int sortingOrder)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CircleSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            go.transform.localScale = Vector3.one * diameter;
            return go;
        }

        /// <summary>지름 1유닛짜리 흰 원 스프라이트를 런타임에 만듭니다. 아트 에셋 없이 동작하기 위함입니다.</summary>
        private Sprite CircleSprite()
        {
            if (_circle != null) return _circle;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float r = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _circle;
        }

        private void OnGUI()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                _hudStyle.normal.textColor = Color.white;
                _bigStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 56, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
                };
                _bigStyle.normal.textColor = new Color(1f, 0.9f, 0.3f);
                _subStyle = new GUIStyle(_bigStyle) { fontSize = 26 };
                _subStyle.normal.textColor = Color.white;
                _timeStyle = new GUIStyle(_hudStyle) { alignment = TextAnchor.UpperRight };
                _timeLowStyle = new GUIStyle(_timeStyle);
                _timeLowStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            }
            if (_board == null) return;

            float pct = _board.ClaimedRatio * 100f;
            GUI.Label(new Rect(20f, 12f, Screen.width - 40f, 40f),
                $"STAGE {_stage}    {pct:0.0}% / {_targetRatio * 100f:0}%    SCORE {_score}    LIVES {_lives}", _hudStyle);
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, _timeLeft));
            bool low = seconds <= 10 && _state == State.Playing;
            GUIStyle timeStyle = low && Mathf.FloorToInt(Time.time * 4f) % 2 == 0 ? _timeLowStyle : _timeStyle;
            GUI.Label(new Rect(Screen.width - 200f, 12f, 180f, 40f), $"TIME {seconds}", low ? _timeLowStyle : timeStyle);

            // 진행 바
            float barW = Mathf.Min(420f, Screen.width - 40f);
            var barRect = new Rect(20f, 46f, barW, 10f);
            GUI.color = new Color(1f, 1f, 1f, 0.25f);
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            GUI.color = pct >= _targetRatio * 100f ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.85f, 0.3f);
            GUI.DrawTexture(new Rect(barRect.x, barRect.y, barW * Mathf.Clamp01(_board.ClaimedRatio), barRect.height),
                Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.DrawTexture(new Rect(barRect.x + barW * _targetRatio - 1f, barRect.y - 3f, 2f, barRect.height + 6f),
                Texture2D.whiteTexture);
            GUI.color = Color.white;

            var center = new Rect(0f, Screen.height * 0.5f - 60f, Screen.width, 80f);
            var below = new Rect(0f, Screen.height * 0.5f + 20f, Screen.width, 40f);
            switch (_state)
            {
                case State.StageClear:
                    GUI.Label(center, "STAGE CLEAR!", _bigStyle);
                    GUI.Label(below, "PRESS SPACE FOR NEXT STAGE", _subStyle);
                    break;
                case State.GameOver:
                    GUI.Label(center, "GAME OVER", _bigStyle);
                    GUI.Label(below, $"FINAL SCORE {_score}   -   PRESS SPACE TO RESTART", _subStyle);
                    break;
                default:
                    if (_flashTimer > 0f) GUI.Label(center, _flash, _bigStyle);
                    break;
            }
        }
    }
}
