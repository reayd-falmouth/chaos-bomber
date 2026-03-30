using System.Collections.Generic;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena
{
    /// <summary>
    /// Player slot positions used by GameManager. Order matches EnablePlayer sequence.
    /// </summary>
    public enum PlayerSlot
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Middle
    }

    /// <summary>
    /// Result of evaluating win state. Used by GameManager.CheckWinState.
    /// </summary>
    public enum WinOutcome
    {
        NoChange,
        GoToStandings,
        GoToOvers
    }

    public struct WinStateResult
    {
        public WinOutcome Outcome;
        public int? LastAliveIndex;

        public static WinStateResult NoChange() =>
            new WinStateResult { Outcome = WinOutcome.NoChange, LastAliveIndex = null };

        public static WinStateResult GoToStandings(int? lastAliveIndex = null) =>
            new WinStateResult
            {
                Outcome = WinOutcome.GoToStandings,
                LastAliveIndex = lastAliveIndex
            };

        public static WinStateResult GoToOvers(int lastAliveIndex) =>
            new WinStateResult { Outcome = WinOutcome.GoToOvers, LastAliveIndex = lastAliveIndex };
    }

    /// <summary>
    /// Pure logic for arena win state and player setup. Testable without Unity.
    /// </summary>
    public static class ArenaLogic
    {
        /// <summary>
        /// Evaluates win state from active flags and win threshold.
        /// </summary>
        /// <param name="playerActive">Per-player active (alive) flags, same length as players array.</param>
        /// <param name="currentWinsOfLastAlive">Current win count of the single alive player (if any); use 0 if not applicable.</param>
        /// <param name="winsNeeded">Wins required to go to game over (Overs).</param>
        public static WinStateResult EvaluateWinState(
            bool[] playerActive,
            int currentWinsOfLastAlive,
            int winsNeeded
        )
        {
            if (playerActive == null || playerActive.Length == 0)
                return WinStateResult.NoChange();

            int aliveCount = 0;
            int? lastAliveIndex = null;

            for (int i = 0; i < playerActive.Length; i++)
            {
                if (playerActive[i])
                {
                    aliveCount++;
                    lastAliveIndex = i;
                }
            }

            if (aliveCount > 1)
                return WinStateResult.NoChange();

            if (aliveCount == 1 && lastAliveIndex.HasValue)
            {
                int winsAfterThisRound = currentWinsOfLastAlive + 1;
                if (winsAfterThisRound >= winsNeeded)
                    return WinStateResult.GoToOvers(lastAliveIndex.Value);
                return WinStateResult.GoToStandings(lastAliveIndex);
            }

            return WinStateResult.GoToStandings(null);
        }

        /// <summary>
        /// Returns which slots are used and which player id (1-based) is in each, for 2-5 players.
        /// </summary>
        public static List<(PlayerSlot slot, int playerId)> GetPlayerSetup(int playerCount)
        {
            var result = new List<(PlayerSlot, int)>();
            if (playerCount < 1 || playerCount > 5)
                return result;

            int id = 1;
            switch (playerCount)
            {
                case 1:
                    result.Add((PlayerSlot.TopLeft, id++));
                    break;
                case 2:
                    result.Add((PlayerSlot.TopLeft, id++));
                    result.Add((PlayerSlot.BottomRight, id++));
                    break;
                case 3:
                    result.Add((PlayerSlot.TopLeft, id++));
                    result.Add((PlayerSlot.BottomRight, id++));
                    result.Add((PlayerSlot.Middle, id++));
                    break;
                case 4:
                    result.Add((PlayerSlot.TopLeft, id++));
                    result.Add((PlayerSlot.TopRight, id++));
                    result.Add((PlayerSlot.BottomLeft, id++));
                    result.Add((PlayerSlot.BottomRight, id++));
                    break;
                case 5:
                    result.Add((PlayerSlot.TopLeft, id++));
                    result.Add((PlayerSlot.TopRight, id++));
                    result.Add((PlayerSlot.BottomLeft, id++));
                    result.Add((PlayerSlot.BottomRight, id++));
                    result.Add((PlayerSlot.Middle, id++));
                    break;
            }

            return result;
        }
    }
}
