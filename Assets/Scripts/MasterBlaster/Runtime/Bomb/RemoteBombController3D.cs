using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Bomb
{
    /// <summary>
    /// 3D analogue of <see cref="HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb.RemoteBombController"/> <c>BombMode.Remote</c>:
    /// freeze placer, steer bomb on the XZ grid while detonate is held, explode on release.
    /// Directional billboards under <c>BillBox</c> match <see cref="PlayerDualModeController.SetSpriteDirection"/>.
    /// </summary>
    public class RemoteBombController3D : MonoBehaviour
    {
        private BombController3D m_Spawner;
        private PlayerDualModeController m_Owner;
        private KeyCode m_DetonateKey;
        private float m_BaseY;
        private bool m_Detonated;

        private Vector3 m_TargetWorld;
        private bool m_Moving;
        private float m_MoveElapsed;
        private Vector2Int m_MoveFacing = Vector2Int.down;

        [SerializeField]
        private AnimatedSpriteRenderer m_SpriteIdle;
        [SerializeField]
        private AnimatedSpriteRenderer m_SpriteUp;
        [SerializeField]
        private AnimatedSpriteRenderer m_SpriteDown;
        [SerializeField]
        private AnimatedSpriteRenderer m_SpriteLeft;
        [SerializeField]
        private AnimatedSpriteRenderer m_SpriteRight;

        private MMF_Player m_MoveFeedbacks;

        public void Init(BombController3D spawner, PlayerDualModeController owner, KeyCode detonateKeyFallback)
        {
            m_Spawner = spawner;
            m_Owner = owner;
            m_DetonateKey = detonateKeyFallback;
            m_BaseY = transform.position.y;

            ResolveBillboardSprites();
            SyncDirectionalWalkAnimationIntervals();
            m_MoveFeedbacks = GetComponentInChildren<MMF_Player>(true);

            if (m_Owner != null)
                m_Owner.stop = true;
            m_Owner?.BeginRemoteBombSteerSession();

            RefreshBombSpriteVisual();
            UpdateMoveFeedbacks(false);
        }

        private void ResolveBillboardSprites()
        {
            var bill = transform.Find("BillBox");
            if (bill == null) return;

            m_SpriteIdle = bill.Find("Idle")?.GetComponent<AnimatedSpriteRenderer>();
            m_SpriteUp = bill.Find("Up")?.GetComponent<AnimatedSpriteRenderer>();
            m_SpriteDown = bill.Find("Down")?.GetComponent<AnimatedSpriteRenderer>();
            m_SpriteLeft = bill.Find("Left")?.GetComponent<AnimatedSpriteRenderer>();
            m_SpriteRight = bill.Find("Right")?.GetComponent<AnimatedSpriteRenderer>();
        }

        private void Update()
        {
            if (m_Detonated) return;
            if (m_Spawner == null || m_Owner == null) return;

            var ip = m_Spawner.GetComponent<IPlayerInput>();
            bool detonateHeld = ip != null ? ip.GetDetonateHeld() : Input.GetKey(m_DetonateKey);
            if (!detonateHeld)
            {
                Detonate();
                return;
            }

            bool wasMoving = m_Moving;
            UpdateRemoteMovement();

            m_Owner?.SetRemoteBombSteerPose(detonateHeld && m_Moving);

            if (m_Moving != wasMoving)
            {
                RefreshBombSpriteVisual();
                UpdateMoveFeedbacks(m_Moving);
            }
        }

        private void Detonate()
        {
            if (m_Detonated) return;
            m_Detonated = true;

            UpdateMoveFeedbacks(false);
            ApplyDirectionalVisual(Vector2Int.zero, moving: false);

            if (m_Owner != null)
                m_Owner.stop = false;
            m_Owner?.EndRemoteBombSteerSession();

            if (m_Spawner != null)
                m_Spawner.ExplodeBomb(gameObject);
        }

        private void OnDisable()
        {
            UpdateMoveFeedbacks(false);
            ApplyDirectionalVisual(Vector2Int.zero, moving: false);
            if (!m_Detonated && m_Owner != null)
            {
                m_Owner.stop = false;
                m_Owner.EndRemoteBombSteerSession();
            }
        }

        /// <summary>Show Idle when parked; show Up/Down/Left/Right walk frames while sliding to the next cell.</summary>
        private void RefreshBombSpriteVisual()
        {
            if (m_Moving)
                ApplyDirectionalVisual(m_MoveFacing, moving: true);
            else
                ApplyDirectionalVisual(Vector2Int.zero, moving: false);
        }

        private void SyncDirectionalWalkAnimationIntervals()
        {
            if (m_Owner == null) return;
            float t = m_Owner.GetScaledWalkAnimationFrameInterval();
            m_SpriteUp?.SetFrameInterval(t);
            m_SpriteDown?.SetFrameInterval(t);
            m_SpriteLeft?.SetFrameInterval(t);
            m_SpriteRight?.SetFrameInterval(t);
        }

        private void ApplyDirectionalVisual(Vector2Int dir, bool moving)
        {
            if (moving)
                SyncDirectionalWalkAnimationIntervals();

            void DeactivateAllDirectional()
            {
                if (m_SpriteIdle)    m_SpriteIdle.gameObject.SetActive(false);
                if (m_SpriteUp)     m_SpriteUp.gameObject.SetActive(false);
                if (m_SpriteDown)   m_SpriteDown.gameObject.SetActive(false);
                if (m_SpriteLeft)   m_SpriteLeft.gameObject.SetActive(false);
                if (m_SpriteRight)  m_SpriteRight.gameObject.SetActive(false);
            }

            DeactivateAllDirectional();

            AnimatedSpriteRenderer active = null;
            if (!moving || dir == Vector2Int.zero)
                active = m_SpriteIdle;
            else if (dir == Vector2Int.up)
                active = m_SpriteUp;
            else if (dir == Vector2Int.down)
                active = m_SpriteDown;
            else if (dir == Vector2Int.left)
                active = m_SpriteLeft;
            else if (dir == Vector2Int.right)
                active = m_SpriteRight;

            if (active == null) return;

            active.gameObject.SetActive(true);
            active.idle = !moving;
        }

        private void UpdateMoveFeedbacks(bool moving)
        {
            if (m_MoveFeedbacks == null) return;
            if (moving)
                m_MoveFeedbacks.PlayFeedbacks(transform.position);
            else
                m_MoveFeedbacks.StopFeedbacks();
        }

        private void SnapBombToGrid()
        {
            Vector3 snapped = ArenaGrid3D.SnapToCell(transform.position);
            snapped.y = m_BaseY;
            if (Vector3.Distance(snapped, transform.position) <= 0.001f) return;
            transform.position = snapped;
        }

        private void UpdateRemoteMovement()
        {
            float speed = Mathf.Max(m_Owner.bombermanSpeed, 0.01f);
            float snapThreshold = m_Owner.snapThreshold;

            if (!m_Moving)
            {
                SnapBombToGrid();

                Vector2 rawDir = m_Owner.GetBombermanMoveInputForRemoteBomb();
                if (rawDir.sqrMagnitude < 0.25f) return;

                Vector2Int dir4 = GetCardinalDirection(rawDir);
                Vector2Int currentCell = ArenaGrid3D.WorldToCell(transform.position);
                Vector2Int nextCell = currentCell + dir4;
                Vector3 nextWorld = ArenaGrid3D.CellToWorld(nextCell);
                nextWorld.y = m_BaseY;

                bool canMove = HybridArenaGrid.Instance == null
                    || HybridArenaGrid.Instance.IsWalkable(nextCell);

                if (canMove)
                {
                    m_TargetWorld = nextWorld;
                    m_Moving = true;
                    m_MoveElapsed = 0f;
                    m_MoveFacing = dir4;
                }
            }

            if (m_Moving)
            {
                m_MoveElapsed += Time.deltaTime;
                float maxMoveTime = ArenaGrid3D.CellSize / Mathf.Max(speed, 0.01f) * 2.5f;
                if (m_MoveElapsed > maxMoveTime)
                {
                    SnapBombToGrid();
                    m_Moving = false;
                    return;
                }

                Vector3 pos = transform.position;
                Vector3 toTarget = m_TargetWorld - pos;
                toTarget.y = 0f;
                float step = speed * Time.deltaTime;

                if (toTarget.magnitude <= step || toTarget.magnitude <= snapThreshold)
                {
                    pos = m_TargetWorld;
                    pos.y = m_BaseY;
                    transform.position = pos;
                    m_Moving = false;
                }
                else
                {
                    pos += toTarget.normalized * step;
                    pos.y = m_BaseY;
                    transform.position = pos;
                }
            }
        }

        private static Vector2Int GetCardinalDirection(Vector2 input)
        {
            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
                return input.x > 0 ? Vector2Int.right : Vector2Int.left;
            return input.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
    }
}
