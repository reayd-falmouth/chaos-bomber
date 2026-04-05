using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Camera;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class CinemachineModePriorityLogicEditModeTests
    {
        private const int Active = 20;
        private const int Inactive = 10;

        [Test]
        public void FPS_PlayerCameraWins()
        {
            Assert.AreEqual(
                Active,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.FPS,
                    isPlayerCamera: true,
                    registryHasAnyArenaPerspectiveMarker: true,
                    thisCameraHasArenaPerspectiveMarker: false,
                    Active,
                    Inactive));
            Assert.AreEqual(
                Inactive,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.FPS,
                    isPlayerCamera: false,
                    registryHasAnyArenaPerspectiveMarker: true,
                    thisCameraHasArenaPerspectiveMarker: true,
                    Active,
                    Inactive));
        }

        [Test]
        public void Bomberman_NonPlayerWins()
        {
            Assert.AreEqual(
                Inactive,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.Bomberman,
                    isPlayerCamera: true,
                    registryHasAnyArenaPerspectiveMarker: true,
                    thisCameraHasArenaPerspectiveMarker: true,
                    Active,
                    Inactive));
            Assert.AreEqual(
                Active,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.Bomberman,
                    isPlayerCamera: false,
                    registryHasAnyArenaPerspectiveMarker: false,
                    thisCameraHasArenaPerspectiveMarker: false,
                    Active,
                    Inactive));
            Assert.AreEqual(
                Inactive,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.Bomberman,
                    isPlayerCamera: false,
                    registryHasAnyArenaPerspectiveMarker: true,
                    thisCameraHasArenaPerspectiveMarker: true,
                    Active,
                    Inactive));
        }

        [Test]
        public void ArenaPerspective_WithoutMarkers_MatchesBombermanRule()
        {
            Assert.AreEqual(
                Active,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.ArenaPerspective,
                    isPlayerCamera: false,
                    registryHasAnyArenaPerspectiveMarker: false,
                    thisCameraHasArenaPerspectiveMarker: false,
                    Active,
                    Inactive));
            Assert.AreEqual(
                Inactive,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.ArenaPerspective,
                    isPlayerCamera: true,
                    registryHasAnyArenaPerspectiveMarker: false,
                    thisCameraHasArenaPerspectiveMarker: false,
                    Active,
                    Inactive));
        }

        [Test]
        public void ArenaPerspective_WithMarkers_OnlyMarkedWins()
        {
            Assert.AreEqual(
                Active,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.ArenaPerspective,
                    isPlayerCamera: false,
                    registryHasAnyArenaPerspectiveMarker: true,
                    thisCameraHasArenaPerspectiveMarker: true,
                    Active,
                    Inactive));
            Assert.AreEqual(
                Inactive,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.ArenaPerspective,
                    isPlayerCamera: false,
                    registryHasAnyArenaPerspectiveMarker: true,
                    thisCameraHasArenaPerspectiveMarker: false,
                    Active,
                    Inactive));
            Assert.AreEqual(
                Inactive,
                CinemachineModePriorityLogic.Compute(
                    GameModeManager.GameMode.ArenaPerspective,
                    isPlayerCamera: true,
                    registryHasAnyArenaPerspectiveMarker: true,
                    thisCameraHasArenaPerspectiveMarker: false,
                    Active,
                    Inactive));
        }
    }
}
