using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Camera;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class BillboardCameraResolverEditModeTests
    {
        [Test]
        public void ResolveForMode_Fps_ReturnsFpsCamera()
        {
            var fps = new GameObject("fps").AddComponent<Camera>();
            var bomber = new GameObject("bomber").AddComponent<Camera>();
            var arena = new GameObject("arena").AddComponent<Camera>();
            try
            {
                Assert.AreSame(
                    fps,
                    BillboardCameraResolver.ResolveForMode(
                        GameModeManager.GameMode.FPS, fps, bomber, arena));
            }
            finally
            {
                Object.DestroyImmediate(fps.gameObject);
                Object.DestroyImmediate(bomber.gameObject);
                Object.DestroyImmediate(arena.gameObject);
            }
        }

        [Test]
        public void ResolveForMode_Bomberman_ReturnsBombermanCamera()
        {
            var fps = new GameObject("fps").AddComponent<Camera>();
            var bomber = new GameObject("bomber").AddComponent<Camera>();
            var arena = new GameObject("arena").AddComponent<Camera>();
            try
            {
                Assert.AreSame(
                    bomber,
                    BillboardCameraResolver.ResolveForMode(
                        GameModeManager.GameMode.Bomberman, fps, bomber, arena));
            }
            finally
            {
                Object.DestroyImmediate(fps.gameObject);
                Object.DestroyImmediate(bomber.gameObject);
                Object.DestroyImmediate(arena.gameObject);
            }
        }

        [Test]
        public void ResolveForMode_ArenaPerspective_WithDedicated_ReturnsArenaCamera()
        {
            var fps = new GameObject("fps").AddComponent<Camera>();
            var bomber = new GameObject("bomber").AddComponent<Camera>();
            var arena = new GameObject("arena").AddComponent<Camera>();
            try
            {
                Assert.AreSame(
                    arena,
                    BillboardCameraResolver.ResolveForMode(
                        GameModeManager.GameMode.ArenaPerspective, fps, bomber, arena));
            }
            finally
            {
                Object.DestroyImmediate(fps.gameObject);
                Object.DestroyImmediate(bomber.gameObject);
                Object.DestroyImmediate(arena.gameObject);
            }
        }

        [Test]
        public void ResolveForMode_ArenaPerspective_WithoutDedicated_FallsBackToBombermanCamera()
        {
            var fps = new GameObject("fps").AddComponent<Camera>();
            var bomber = new GameObject("bomber").AddComponent<Camera>();
            try
            {
                Assert.AreSame(
                    bomber,
                    BillboardCameraResolver.ResolveForMode(
                        GameModeManager.GameMode.ArenaPerspective, fps, bomber, null));
            }
            finally
            {
                Object.DestroyImmediate(fps.gameObject);
                Object.DestroyImmediate(bomber.gameObject);
            }
        }
    }
}
