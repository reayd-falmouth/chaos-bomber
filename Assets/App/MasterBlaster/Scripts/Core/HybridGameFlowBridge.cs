using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using Unity.FPS.Game;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>
    /// Bridges the FPS GameFlowManager's PlayerDeathEvent to the HybridGame SceneFlowManager.
    /// Attach this to any persistent GameObject in the hybrid scene.
    /// When present, player death calls GameManager.CheckWinState so Standings vs Overs
    /// matches the arena rules (e.g. all players dead → Standings). If no GameManager, falls back to Overs.
    /// </summary>
    public class HybridGameFlowBridge : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (FindFirstObjectByType<HybridGameFlowBridge>() != null) return;
            var go = new GameObject("[HybridGameFlowBridge]");
            go.AddComponent<HybridGameFlowBridge>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            EventManager.AddListener<PlayerDeathEvent>(OnPlayerDeath);
            EventManager.AddListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
        }

        private void OnDestroy()
        {
            EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeath);
            EventManager.RemoveListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
        }

        private void OnPlayerDeath(PlayerDeathEvent evt)
        {
            var flow = SceneFlowManager.Instance;
            if (flow == null || flow.CurrentState != FlowState.Game) return;

            // Let GameManager decide Standings vs Overs (same as 2D PlayerController path). When everyone
            // is dead, ArenaLogic returns GoToStandings(null) so the round still goes to Standings.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckWinState();
                return;
            }

            flow.GoToOvers();
        }

        private void OnAllObjectivesCompleted(AllObjectivesCompletedEvent evt)
        {
            var flow = SceneFlowManager.Instance;
            if (flow == null || flow.CurrentState != FlowState.Game) return;

            flow.SignalRoundFinished();
        }
    }
}
