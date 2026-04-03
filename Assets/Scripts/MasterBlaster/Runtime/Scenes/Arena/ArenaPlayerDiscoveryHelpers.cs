using System;
using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena
{
    /// <summary>
    /// Maps inspector slot references to the active player list for a given count (matches <see cref="ArenaLogic.GetPlayerSetup"/>).
    /// </summary>
    public static class ArenaPlayerSlotResolver
    {
        public static bool TryResolve(
            int playerCount,
            GameObject topLeft,
            GameObject topRight,
            GameObject bottomLeft,
            GameObject bottomRight,
            GameObject middle,
            GameObject[] result
        )
        {
            if (playerCount <= 0 || result == null || result.Length < playerCount)
                return false;

            var setup = ArenaLogic.GetPlayerSetup(playerCount);
            if (setup == null || setup.Count != playerCount)
                return false;

            for (int i = 0; i < playerCount; i++)
            {
                var go = GetForSlot(setup[i].slot, topLeft, topRight, bottomLeft, bottomRight, middle);
                if (go == null)
                    return false;
                result[i] = go;
            }

            return true;
        }

        private static GameObject GetForSlot(
            PlayerSlot slot,
            GameObject topLeft,
            GameObject topRight,
            GameObject bottomLeft,
            GameObject bottomRight,
            GameObject middle
        )
        {
            switch (slot)
            {
                case PlayerSlot.TopLeft: return topLeft;
                case PlayerSlot.TopRight: return topRight;
                case PlayerSlot.BottomLeft: return bottomLeft;
                case PlayerSlot.BottomRight: return bottomRight;
                case PlayerSlot.Middle: return middle;
                default: return null;
            }
        }
    }

    /// <summary>
    /// Spatial corner assignment when <c>playerId</c> on instances is incomplete. Uses local Y for "rows" when
    /// spawns differ in height; otherwise local Z (hybrid flat floor); if still degenerate, local X.
    /// </summary>
    public static class ArenaPlayerSpatialFallback
    {
        public struct Candidate
        {
            public GameObject go;
            public Vector3 localPos;
        }

        public static void FillResult(IReadOnlyList<Candidate> candidates, int count, GameObject[] result)
        {
            if (candidates == null || count <= 0 || result == null || result.Length < count)
                return;

            const float eps = 0.01f;

            if (!TryPickRowAxis(candidates, eps, out var rowAxis))
                return;

            float centerX = 0f;
            for (int i = 0; i < candidates.Count; i++)
                centerX += candidates[i].localPos.x;
            centerX /= Mathf.Max(1, candidates.Count);

            var topRow = new List<Candidate>();
            var bottomRow = new List<Candidate>();
            foreach (var c in candidates)
            {
                float r = rowAxis.Row(c.localPos);
                if (Mathf.Abs(r - rowAxis.MaxRow) <= eps)
                    topRow.Add(c);
                else if (Mathf.Abs(r - rowAxis.MinRow) <= eps)
                    bottomRow.Add(c);
            }

            Candidate PickTopLeft()
            {
                Candidate best = default;
                float bestX = float.MaxValue;
                for (int i = 0; i < topRow.Count; i++)
                {
                    float x = topRow[i].localPos.x;
                    if (x < bestX)
                    {
                        bestX = x;
                        best = topRow[i];
                    }
                }
                return best;
            }

            Candidate PickTopRight()
            {
                Candidate best = default;
                float bestX = float.MinValue;
                for (int i = 0; i < topRow.Count; i++)
                {
                    float x = topRow[i].localPos.x;
                    if (x > bestX)
                    {
                        bestX = x;
                        best = topRow[i];
                    }
                }
                return best;
            }

            Candidate PickBottomLeft()
            {
                Candidate best = default;
                float bestX = float.MaxValue;
                for (int i = 0; i < bottomRow.Count; i++)
                {
                    float x = bottomRow[i].localPos.x;
                    if (x < bestX)
                    {
                        bestX = x;
                        best = bottomRow[i];
                    }
                }
                return best;
            }

            Candidate PickBottomRight()
            {
                Candidate best = default;
                float bestX = float.MinValue;
                for (int i = 0; i < bottomRow.Count; i++)
                {
                    float x = bottomRow[i].localPos.x;
                    if (x > bestX)
                    {
                        bestX = x;
                        best = bottomRow[i];
                    }
                }
                return best;
            }

            Candidate PickMiddle()
            {
                float midRow = (rowAxis.MaxRow + rowAxis.MinRow) * 0.5f;
                Candidate best = default;
                float bestScore = float.MaxValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    var c = candidates[i];
                    var lp = c.localPos;
                    float r = rowAxis.Row(lp);
                    if (Mathf.Abs(r - rowAxis.MaxRow) <= eps || Mathf.Abs(r - rowAxis.MinRow) <= eps)
                        continue;

                    float dRow = Mathf.Abs(r - midRow);
                    float dx = Mathf.Abs(lp.x - centerX);
                    float score = dRow + dx * 0.5f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = c;
                    }
                }
                return best;
            }

            if (count == 1)
                result[0] = PickTopLeft().go;
            else if (count == 2)
            {
                result[0] = PickTopLeft().go;
                result[1] = PickBottomRight().go;
            }
            else if (count == 3)
            {
                result[0] = PickTopLeft().go;
                result[1] = PickBottomRight().go;
                result[2] = PickMiddle().go;
            }
            else if (count == 4)
            {
                result[0] = PickTopLeft().go;
                result[1] = PickTopRight().go;
                result[2] = PickBottomLeft().go;
                result[3] = PickBottomRight().go;
            }
            else
            {
                result[0] = PickTopLeft().go;
                result[1] = PickTopRight().go;
                result[2] = PickBottomLeft().go;
                result[3] = PickBottomRight().go;
                if (count >= 5)
                    result[4] = PickMiddle().go;
            }
        }

        private struct RowAxis
        {
            public float MaxRow;
            public float MinRow;
            public Func<Vector3, float> Row;
        }

        private static bool TryPickRowAxis(IReadOnlyList<Candidate> candidates, float eps, out RowAxis axis)
        {
            axis = default;
            if (candidates.Count == 0)
                return false;

            axis = BuildAxis(candidates, lp => lp.y);
            if (Mathf.Abs(axis.MaxRow - axis.MinRow) >= eps)
                return true;

            axis = BuildAxis(candidates, lp => lp.z);
            if (Mathf.Abs(axis.MaxRow - axis.MinRow) >= eps)
                return true;

            axis = BuildAxis(candidates, lp => lp.x);
            return true;
        }

        private static RowAxis BuildAxis(IReadOnlyList<Candidate> candidates, Func<Vector3, float> row)
        {
            float maxR = float.MinValue;
            float minR = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                float r = row(candidates[i].localPos);
                maxR = Mathf.Max(maxR, r);
                minR = Mathf.Min(minR, r);
            }
            return new RowAxis { MaxRow = maxR, MinRow = minR, Row = row };
        }
    }
}
