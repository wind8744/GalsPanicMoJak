using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GalsPanic.Tests
{
    public class SparxTests
    {
        private const int W = 40, H = 30;
        private GameObject _boardGo, _sparxGo;
        private Board _board;
        private Sparx _sparx;

        [SetUp]
        public void SetUp()
        {
            _boardGo = new GameObject("Board");
            _board = _boardGo.AddComponent<Board>();
            _board.Init(W, H, 1);
            _sparxGo = new GameObject("Sparx");
            _sparx = _sparxGo.AddComponent<Sparx>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sparxGo);
            Object.DestroyImmediate(_boardGo);
        }

        [Test]
        public void Step_StaysOnEdgeAndKeepsMoving()
        {
            _sparx.Init(_board, null, new Vector2Int(W / 2, H - 1), Vector2Int.right, 10f);
            var visited = new HashSet<Vector2Int>();
            for (int i = 0; i < 300; i++)
            {
                _sparx.Step();
                Assert.IsTrue(_board.IsEdge(_sparx.GridPos), $"step {i}: {_sparx.GridPos} 는 가장자리가 아님");
                visited.Add(_sparx.GridPos);
            }
            Assert.Greater(visited.Count, 100, "테두리를 따라 계속 돌아야 함");
        }

        [Test]
        public void Step_RelocatesWhenCellIsNoLongerEdge()
        {
            _sparx.Init(_board, null, new Vector2Int(0, H / 2), Vector2Int.up, 10f);

            // 왼쪽 절반을 차지하면 왼쪽 변은 안쪽으로 묻힌다.
            int col = W / 2;
            for (int y = 1; y < H - 1; y++) _board.Set(new Vector2Int(col, y), Cell.Trail);
            _board.Claim(new Vector2Int(col + 5, H / 2));
            Assert.IsFalse(_board.IsEdge(_sparx.GridPos));

            _sparx.Step();
            Assert.IsTrue(_board.IsEdge(_sparx.GridPos), "가장 가까운 가장자리로 옮겨야 함");
        }
    }

    public class PlayerMoverTests
    {
        [Test]
        public void Die_WhenNotDrawing_KeepsPosition()
        {
            var boardGo = new GameObject("Board");
            var board = boardGo.AddComponent<Board>();
            board.Init(40, 30, 1);
            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<PlayerMover>();
            try
            {
                var start = new Vector2Int(20, 0);
                player.Init(board, null, start);
                player.Die();
                Assert.AreEqual(start, player.GridPos);
                Assert.IsFalse(player.Drawing);
            }
            finally
            {
                Object.DestroyImmediate(playerGo);
                Object.DestroyImmediate(boardGo);
            }
        }
    }
}
