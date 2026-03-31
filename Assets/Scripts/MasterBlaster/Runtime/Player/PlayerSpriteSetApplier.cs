using HybridGame.MasterBlaster.Scripts.Player.Abilities;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerDualModeController))]
    public class PlayerSpriteSetApplier : MonoBehaviour
    {
        [Header("Sprite Source")]
        [SerializeField] private PlayerSpriteSheet spriteSheet;

        private PlayerDualModeController _dual;

        private void Awake()
        {
            _dual = GetComponent<PlayerDualModeController>();
        }

        public void ApplyForPlayerId(int playerId)
        {
            // #region agent log
            Log("run1", "H0", "PlayerSpriteSetApplier.cs:ApplyForPlayerId:entry",
                $"{{\"go\":\"{Escape(name)}\",\"playerId\":{playerId}}}");
            // #endregion
            UnityEngine.Debug.Log($"[SPRITEAPPLY] entry go={name} playerId={playerId}", this);

            if (spriteSheet == null)
            {
                UnityEngine.Debug.LogWarning($"[PlayerSpriteSetApplier] No spriteSheet assigned on {name}", this);
                // #region agent log
                Log("run1", "H3", "PlayerSpriteSetApplier.cs:ApplyForPlayerId:no_sheet",
                    $"{{\"go\":\"{Escape(name)}\"}}");
                // #endregion
                UnityEngine.Debug.LogWarning($"[SPRITEAPPLY] no_sheet go={name}", this);
                return;
            }

            var sprites = spriteSheet.orderedSprites;
            int stride = spriteSheet.spritesPerPlayer > 0 ? spriteSheet.spritesPerPlayer : 30;

            if (sprites == null || sprites.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"[PlayerSpriteSetApplier] spriteSheet has no sprites ({spriteSheet.name})", spriteSheet);
                // #region agent log
                Log("run1", "H3", "PlayerSpriteSetApplier.cs:ApplyForPlayerId:empty_sprites",
                    $"{{\"sheet\":\"{Escape(spriteSheet.name)}\"}}");
                // #endregion
                return;
            }

            if (playerId <= 0)
            {
                UnityEngine.Debug.LogWarning($"[PlayerSpriteSetApplier] Invalid playerId={playerId} on {name}", this);
                // #region agent log
                Log("run1", "H1", "PlayerSpriteSetApplier.cs:ApplyForPlayerId:bad_playerId",
                    $"{{\"go\":\"{Escape(name)}\",\"playerId\":{playerId}}}");
                // #endregion
                return;
            }

            int baseIndex = (playerId - 1) * stride;
            // #region agent log
            Log("run1", "H2", "PlayerSpriteSetApplier.cs:ApplyForPlayerId:computed",
                $"{{\"go\":\"{Escape(name)}\",\"playerId\":{playerId},\"stride\":{stride},\"baseIndex\":{baseIndex},\"spritesLen\":{sprites.Length},\"sheet\":\"{Escape(spriteSheet.name)}\"}}");
            // #endregion
            UnityEngine.Debug.Log($"[SPRITEAPPLY] computed go={name} playerId={playerId} stride={stride} baseIndex={baseIndex} spritesLen={sprites.Length} sheet={spriteSheet.name}", this);

            if (baseIndex < 0 || baseIndex + 30 > sprites.Length)
            {
                UnityEngine.Debug.LogError(
                    $"[PlayerSpriteSetApplier] Not enough sprites for playerId={playerId}. " +
                    $"Need indices [{baseIndex}..{baseIndex + 29}] but array length is {sprites.Length}. " +
                    $"(stride={stride}, sheet={spriteSheet.name})",
                    this
                );
                // #region agent log
                Log("run1", "H2", "PlayerSpriteSetApplier.cs:ApplyForPlayerId:range_error",
                    $"{{\"go\":\"{Escape(name)}\",\"playerId\":{playerId},\"stride\":{stride},\"baseIndex\":{baseIndex},\"spritesLen\":{sprites.Length}}}");
                // #endregion
                UnityEngine.Debug.LogError($"[SPRITEAPPLY] range_error go={name} playerId={playerId} stride={stride} baseIndex={baseIndex} spritesLen={sprites.Length}", this);
                return;
            }

            // 0–11: 3 frames each: Down, Right, Left, Up
            ApplyDirectional(_dual.spriteDown, Slice(sprites, baseIndex + 0, 3));
            ApplyDirectional(_dual.spriteRight, Slice(sprites, baseIndex + 3, 3));
            ApplyDirectional(_dual.spriteLeft, Slice(sprites, baseIndex + 6, 3));
            ApplyDirectional(_dual.spriteUp, Slice(sprites, baseIndex + 9, 3));

            // 12–20: death (9 frames)
            ApplyAnimOnly(_dual.spriteDeath, Slice(sprites, baseIndex + 12, 9));

            // 21–23: remote control (3 frames)
            ApplyDirectional(_dual.spriteRemoteBomb, Slice(sprites, baseIndex + 21, 3));

            // 24–29: ghost (6 frames) (Ghost component drives animation; we just replace its renderer frames)
            var ghost = GetComponentInChildren<Ghost>(true);
            if (ghost != null)
                ApplyAnimOnly(ghost.GhostSpriteRenderer, Slice(sprites, baseIndex + 24, 6));

            // #region agent log
            Log("run1", "H0", "PlayerSpriteSetApplier.cs:ApplyForPlayerId:assigned",
                $"{{\"go\":\"{Escape(name)}\",\"playerId\":{playerId},\"stride\":{stride},\"baseIndex\":{baseIndex}," +
                $"\"down0\":\"{Escape(SName(sprites[baseIndex + 0]))}\",\"right0\":\"{Escape(SName(sprites[baseIndex + 3]))}\"," +
                $"\"left0\":\"{Escape(SName(sprites[baseIndex + 6]))}\",\"up0\":\"{Escape(SName(sprites[baseIndex + 9]))}\"}}");
            // #endregion
            UnityEngine.Debug.Log(
                $"[SPRITEAPPLY] assigned go={name} playerId={playerId} baseIndex={baseIndex} " +
                $"down0={SName(sprites[baseIndex + 0])} right0={SName(sprites[baseIndex + 3])} " +
                $"left0={SName(sprites[baseIndex + 6])} up0={SName(sprites[baseIndex + 9])}",
                this
            );
        }

        private static void ApplyDirectional(AnimatedSpriteRenderer r, Sprite[] frames)
        {
            if (r == null || frames == null || frames.Length == 0) return;
            r.idleSprite = frames[0];
            r.animationSprites = frames;
            r.idle = true;
        }

        private static void ApplyAnimOnly(AnimatedSpriteRenderer r, Sprite[] frames)
        {
            if (r == null || frames == null || frames.Length == 0) return;
            r.idleSprite = frames[0];
            r.animationSprites = frames;
        }

        private static Sprite[] Slice(Sprite[] src, int start, int count)
        {
            var dst = new Sprite[count];
            for (int i = 0; i < count; i++)
                dst[i] = src[start + i];
            return dst;
        }

        // #region agent log
        private const string DebugLogPath = "debug-9c4db8.log";
        private const string SessionId = "9c4db8";

        private static void Log(string runId, string hypothesisId, string location, string dataJson)
        {
            try
            {
                var sb = new StringBuilder(256);
                sb.Append('{');
                sb.Append("\"sessionId\":\"").Append(SessionId).Append("\",");
                sb.Append("\"timestamp\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Append(',');
                sb.Append("\"runId\":\"").Append(runId).Append("\",");
                sb.Append("\"hypothesisId\":\"").Append(hypothesisId).Append("\",");
                sb.Append("\"location\":\"").Append(location).Append("\",");
                sb.Append("\"message\":\"sprite_apply\",");
                sb.Append("\"data\":").Append(string.IsNullOrEmpty(dataJson) ? "{}" : dataJson);
                sb.Append('}');
                sb.Append('\n');
                var logPath = Path.Combine(Application.dataPath, "..", DebugLogPath);
                File.AppendAllText(logPath, sb.ToString());
            }
            catch { /* ignore */ }
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string SName(Sprite s) => s != null ? s.name : "NULL";
        // #endregion
    }
}

