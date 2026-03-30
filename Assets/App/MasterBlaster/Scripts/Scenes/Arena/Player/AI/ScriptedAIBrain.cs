using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.AI
{
    /// <summary>
    /// Rule-based AI: safety first, then offense, collect, chase. Design allows swapping in ML-Agents later.
    /// </summary>
    public class ScriptedAIBrain : MonoBehaviour, IAIBrain
    {
        // Fix 1: static array replaces the local new[] allocation in FindSafestDirection
        private static readonly Vector2[] Dirs4 = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        [Header("Tuning")]
        [Tooltip("How often to re-decide (seconds)")]
        public float tickInterval = 0.15f;
        [Tooltip("Distance to consider 'near' for placing bomb")]
        public float bombRange = 3f;
        [Tooltip("Distance to chase opponent")]
        public float chaseRange = 30f;

        private float _nextTick;
        private Transform _arenaRoot;
        private GameManager _localGameManager;

        private void Awake()
        {
            _arenaRoot = transform.root != transform ? transform.root : null;
            _localGameManager = (_arenaRoot != null ? _arenaRoot.GetComponentInChildren<GameManager>() : null)
                                ?? GameManager.Instance;
        }

        private IReadOnlyList<BombInfo>   GetArenaBombs()  => _localGameManager != null ? _localGameManager.ArenaBombs  : (IReadOnlyList<BombInfo>)   Object.FindObjectsByType<BombInfo>(FindObjectsSortMode.None);
        private IReadOnlyList<ItemPickup> GetArenaItems()  => _localGameManager != null ? _localGameManager.ArenaItems   : (IReadOnlyList<ItemPickup>) Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);

        public void Tick(
            Transform self,
            BombController bombController,
            GameObject[] allPlayers,
            out Vector2 move,
            out bool placeBomb,
            out bool detonateHeld
        )
        {
            if (Time.time < _nextTick)
            {
                move = _lastMove;
                placeBomb = false;
                detonateHeld = _lastDetonateHeld;
                return;
            }
            _nextTick = Time.time + tickInterval;

            Vector2 myPos = ArenaPlane.LogicalXY(self.position);
            Vector2 myCell = RoundToCell(myPos);
            var destructibleTiles = bombController != null ? bombController.destructibleTiles : null;

            // 1) Safety: am I in danger?
            bool inDanger = IsCellInDanger(myCell);
            if (inDanger)
            {
                Vector2 safe = FindSafestDirection(myPos, myCell, destructibleTiles);
                _lastMove = safe;
                _lastDetonateHeld = true; // don't detonate while fleeing
                move = safe;
                placeBomb = false;
                detonateHeld = true;
                return;
            }

            // 2) Offense: can I trap an opponent or break a brick?
            GameObject targetPlayer = GetNearestOtherPlayer(self, allPlayers);
            if (targetPlayer != null && bombController != null && bombController.bombAmount > 0)
            {
                float dist = Vector2.Distance(myPos, ArenaPlane.LogicalXY(targetPlayer.transform.position));
                if (dist <= bombRange && CanPlaceBombHere(myCell, destructibleTiles))
                {
                    _lastMove = Vector2.zero; // stay to place
                    _lastDetonateHeld = true;
                    move = Vector2.zero;
                    placeBomb = true;
                    detonateHeld = true;
                    return;
                }
            }

            // 3) Collect: power-up nearby?
            var item = GetNearestItem(myPos);
            if (item != null)
            {
                Vector2 toItem = ArenaPlane.LogicalXY(item.transform.position) - myPos;
                _lastMove = toItem.normalized;
                _lastDetonateHeld = true;
                move = _lastMove;
                placeBomb = false;
                detonateHeld = true;
                return;
            }

            // 4) Chase or wander
            if (targetPlayer != null)
            {
                Vector2 toTarget = ArenaPlane.LogicalXY(targetPlayer.transform.position) - myPos;
                if (toTarget.sqrMagnitude > 0.5f && toTarget.sqrMagnitude < chaseRange * chaseRange)
                {
                    _lastMove = toTarget.normalized;
                    _lastDetonateHeld = true;
                    move = _lastMove;
                    placeBomb = false;
                    detonateHeld = true;
                    return;
                }
            }

            // Wander: move randomly so we don't stand still when no target in range
            if (Random.value < 0.15f)
                _lastMove = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            if (_lastMove.sqrMagnitude < 0.01f)
                _lastMove = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            move = _lastMove;
            placeBomb = false;
            detonateHeld = true;
        }

        private Vector2 _lastMove;
        private bool _lastDetonateHeld = true;

        private static Vector2 RoundToCell(Vector2 v)
        {
            return new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
        }

        private bool IsCellInDanger(Vector2 cell)
        {
            var bombs = GetArenaBombs();
            foreach (var b in bombs)
            {
                if (b == null) continue;
                Vector2 bombCell = RoundToCell(ArenaPlane.LogicalXY(b.transform.position));
                int r = b.explosionRadius;
                if (Mathf.Abs(cell.x - bombCell.x) <= r && Mathf.Abs(cell.y - bombCell.y) <= r)
                {
                    if (cell.x == bombCell.x || cell.y == bombCell.y)
                        return true;
                }
            }
            return false;
        }

        private Vector2 FindSafestDirection(Vector2 myPos, Vector2 myCell, Tilemap destructibleTiles)
        {
            Vector2 best = Vector2.zero;
            int bestDanger = int.MaxValue;

            foreach (Vector2 d in Dirs4)
            {
                Vector2 nextCell = myCell + d;
                if (!IsWalkable(nextCell, destructibleTiles))
                    continue;
                int danger = CountDangerCells(nextCell);
                if (danger < bestDanger)
                {
                    bestDanger = danger;
                    best = d;
                }
            }
            return best;
        }

        private int CountDangerCells(Vector2 cell)
        {
            int count = 0;
            var bombs = GetArenaBombs();
            foreach (var b in bombs)
            {
                if (b == null) continue;
                Vector2 bombCell = RoundToCell(ArenaPlane.LogicalXY(b.transform.position));
                int r = b.explosionRadius;
                if (cell.x == bombCell.x && Mathf.Abs(cell.y - bombCell.y) <= r) count++;
                else if (cell.y == bombCell.y && Mathf.Abs(cell.x - bombCell.x) <= r) count++;
            }
            return count;
        }

        private bool IsWalkable(Vector2 cell, Tilemap destructibleTiles)
        {
            if (destructibleTiles != null)
            {
                var probe = new Vector3(cell.x, cell.y, destructibleTiles.transform.position.z);
                if (destructibleTiles.GetTile(destructibleTiles.WorldToCell(probe)) != null)
                    return false;
            }
            return true;
        }

        private bool CanPlaceBombHere(Vector2 cell, Tilemap destructibleTiles)
        {
            return IsWalkable(cell, destructibleTiles);
        }

        private GameObject GetNearestOtherPlayer(Transform self, GameObject[] allPlayers)
        {
            if (allPlayers == null) return null;
            GameObject nearest = null;
            float best = chaseRange * chaseRange;
            foreach (var p in allPlayers)
            {
                if (p == null || !p.activeInHierarchy || p.transform == self)
                    continue;
                float sqr = (p.transform.position - self.position).sqrMagnitude;
                if (sqr < best)
                {
                    best = sqr;
                    nearest = p;
                }
            }
            return nearest;
        }

        private ItemPickup GetNearestItem(Vector2 myPos)
        {
            var items = GetArenaItems();
            ItemPickup nearest = null;
            float best = 16f * 16f;
            foreach (var item in items)
            {
                if (item == null) continue;
                float sqr = (ArenaPlane.LogicalXY(item.transform.position) - myPos).sqrMagnitude;
                if (sqr < best)
                {
                    best = sqr;
                    nearest = item;
                }
            }
            return nearest;
        }
    }
}
