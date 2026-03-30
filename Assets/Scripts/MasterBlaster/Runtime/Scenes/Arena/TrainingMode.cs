using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena
{
    /// <summary>
    /// Static training flag for ML-Agents autonomous training. Active when:
    /// - the active scene is named "Train", or
    /// - the game was launched with -training (e.g. headless build).
    /// When active, all players are RL agents and the arena reloads on round end instead of going to Standings.
    /// </summary>
    public static class TrainingMode
    {
        const string TrainingFlag = "-training";
        const string TrainSceneName = "Train";

        static bool? _commandLineCached;

        /// <summary>
        /// True when running in the Train scene or when launched with -training.
        /// All players will be RL agents and the arena will reload on round end instead of going to Standings.
        /// </summary>
        public static bool IsActive
        {
            get
            {
                // Command-line flag: stable for the lifetime of the process, safe to cache.
                if (!_commandLineCached.HasValue)
                {
                    _commandLineCached = Environment.GetCommandLineArgs().Any(
                        arg => string.Equals(arg, TrainingFlag, StringComparison.OrdinalIgnoreCase));
                }
                if (_commandLineCached.Value) return true;

                // Scene name: cheap to re-check; do NOT cache (scene can change mid-session).
                var sceneName = SceneManager.GetActiveScene().name;
                return Application.isPlaying && (
                    sceneName.IndexOf(TrainSceneName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sceneName.IndexOf("ML-Agents", StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
    }
}
