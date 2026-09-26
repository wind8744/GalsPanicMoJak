using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GalsPanic.Tests
{
    public class BoardTests
    {
        private const int W = 40, H = 30;
        private GameObject _go;
        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Board");
            _board = _go.AddComponent<Board>();
            _board.Init(W, H, seed: 1);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void InitialBoard_HasClaimedBorderAndUnclaimedInterior()
        {
            Assert.AreEqual(0f, _board.ClaimedRatio);
            Assert.AreEqual(Cell.Claimed, _board.Get(0, 0));
            Assert.AreEqual(Cell.Claimed, _board.Get(W - 1, H - 1));
            Assert.AreEqual(Cell.Unclaimed, _board.Get(W / 2, H / 2));
            Assert.AreEqual(Cell.Claimed, _board.Get(-1, 5), "보드 밖은 벽으로 취급");
        }

        [Test]
        public void Edge_BorderCellsIncludingCornersAreWalkable()
        {
            Assert.IsTrue(_board.IsEdge(0, 0), "모서리");
            Assert.IsTrue(_board.IsEdge(W / 2, 0), "아래 변");
            Assert.IsTrue(_board.IsEdge(W - 1, H / 2), "오른쪽 변");
            Assert.IsFalse(_board.IsEdge(W / 2, H / 2), "미차지 셀은 길이 아님");
        }

        [Test]
        public void Claim_VerticalTrail_ClaimsSideWithoutBoss()
        {
            int col = W / 2;
            var trail = new List<Vector2Int>();
            for (int y = 1; y < H - 1; y++)
            {
                var c = new Vector2Int(col, y);
                _board.Set(c, Cell.Trail);
                trail.Add(c);
            }

            int gained = _board.Claim(new Vector2Int(col + 5, H / 2)); // 보스는 오른쪽

            int interiorRows = H - 2;
            int expected = (col - 1) * interiorRows + interiorRows; // 왼쪽 열들 + 궤적
            Assert.AreEqual(expected, gained);
            Assert.AreEqual(Cell.Claimed, _board.Get(2, H / 2), "왼쪽은 차지됨");
            Assert.AreEqual(Cell.Claimed, _board.Get(col, H / 2), "궤적은 차지됨");
            Assert.AreEqual(Cell.Unclaimed, _board.Get(col + 5, H / 2), "보스 쪽은 그대로");
            Assert.AreEqual((float)expected / _board.InteriorCount, _board.ClaimedRatio, 1e-5f);
            Assert.IsTrue(_board.IsEdge(col, H / 2), "새 경계는 걸을 수 있음");
            Assert.IsFalse(_board.IsEdge(0, H / 2), "안쪽으로 묻힌 옛 테두리는 더 이상 길이 아님");
        }

        [Test]
        public void ClearTrail_RestoresUnclaimed()
        {
            var trail = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 6) };
            foreach (var c in trail) _board.Set(c, Cell.Trail);
            _board.ClearTrail(trail);
            Assert.AreEqual(Cell.Unclaimed, _board.Get(5, 5));
            Assert.AreEqual(Cell.Unclaimed, _board.Get(5, 6));
        }

        [Test]
        public void HiddenImage_HasExpectedSizeAndIsDeterministic()
        {
            var a = HiddenImage.Generate(W, H, 3);
            var b = HiddenImage.Generate(W, H, 3);
            Assert.AreEqual(W * H, a.Length);
            for (int i = 0; i < a.Length; i += 97) Assert.AreEqual(a[i], b[i]);
        }
    }

    public class BossTests
    {
        [Test]
        public void TouchesTrail_DetectsTrailInsideRadiusOnly()
        {
            var boardGo = new GameObject("Board");
            var board = boardGo.AddComponent<Board>();
            board.Init(40, 30, 1);
            var bossGo = new GameObject("Boss");
            var boss = bossGo.AddComponent<Boss>();
            boss.Radius = 2.5f;
            boss.Init(board, null, new Vector2(20.5f, 15.5f), 10f);
            try
            {
                Assert.IsFalse(boss.TouchesTrail());
                board.Set(new Vector2Int(20, 25), Cell.Trail);
                Assert.IsFalse(boss.TouchesTrail(), "멀리 있는 궤적");
                board.Set(new Vector2Int(21, 16), Cell.Trail);
                Assert.IsTrue(boss.TouchesTrail(), "반경 안의 궤적");
            }
            finally
            {
                Object.DestroyImmediate(bossGo);
                Object.DestroyImmediate(boardGo);
            }
        }
    }
}
