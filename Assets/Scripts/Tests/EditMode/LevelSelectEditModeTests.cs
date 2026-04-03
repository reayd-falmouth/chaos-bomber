using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Levels;
using HybridGame.MasterBlaster.Scripts.Scenes.LevelSelectLocal;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class LevelSelectEditModeTests
    {
        [Test]
        public void LevelWinPersistence_Key_IsStable()
        {
            Assert.AreEqual("LevelWin:arena_01:2", LevelWinPersistence.MakePrefsKey("arena_01", 2));
        }

        [Test]
        public void LevelWinPersistence_MarkAndHas_Works()
        {
            const string key = "LevelWin:__test_level__:3";
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Assert.IsFalse(LevelWinPersistence.HasPlayerWonLevel("__test_level__", 3));
            LevelWinPersistence.MarkPlayerWonLevel("__test_level__", 3);
            Assert.IsTrue(LevelWinPersistence.HasPlayerWonLevel("__test_level__", 3));
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        [Test]
        public void LevelLibrary_TryGetById_FindsMatch()
        {
            var lib = ScriptableObject.CreateInstance<LevelLibrary>();
            var a = ScriptableObject.CreateInstance<LevelDefinition>();
            a.levelId = "alpha";
            var b = ScriptableObject.CreateInstance<LevelDefinition>();
            b.levelId = "beta";
            lib.levels = new[] { a, b };

            Assert.IsTrue(lib.TryGetById("beta", out var found));
            Assert.AreSame(b, found);
            Assert.IsFalse(lib.TryGetById("missing", out _));

            Object.DestroyImmediate(lib);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void LevelSelectController_HorizontalStep_Wraps()
        {
            var go = new GameObject("LevelSelectEditModeTests_Controller");
            var c = go.AddComponent<LevelSelectController>();

            var lib = ScriptableObject.CreateInstance<LevelLibrary>();
            var d0 = ScriptableObject.CreateInstance<LevelDefinition>();
            d0.levelId = "0";
            d0.displayName = "Zero";
            var d1 = ScriptableObject.CreateInstance<LevelDefinition>();
            d1.levelId = "1";
            d1.displayName = "One";
            var d2 = ScriptableObject.CreateInstance<LevelDefinition>();
            d2.levelId = "2";
            d2.displayName = "Two";
            lib.levels = new[] { d0, d1, d2 };

            c.Editor_SetLevelLibrary(lib);
            c.Editor_SetLevelIndex(0);

            c.Editor_StepLevelHorizontal(-1);
            Assert.AreEqual(2, c.Editor_GetLevelIndex());

            c.Editor_StepLevelHorizontal(1);
            Assert.AreEqual(0, c.Editor_GetLevelIndex());

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(lib);
            Object.DestroyImmediate(d0);
            Object.DestroyImmediate(d1);
            Object.DestroyImmediate(d2);
        }

        [Test]
        public void LevelSelectionPrefs_SelectedLevelIdKey_MatchesControllerConstant()
        {
            Assert.AreEqual(LevelSelectionPrefs.SelectedLevelIdKey, LevelSelectController.SelectedLevelIdPrefsKey);
        }

        [Test]
        public void SceneFlowMapper_MapsLevelSelectLocal_ToAndFromSceneName()
        {
            var config = new SceneNamesConfig { LevelSelectLocal = "LevelSelectLocal" };
            Assert.AreEqual("LevelSelectLocal", SceneFlowMapper.SceneFor(FlowState.LevelSelectLocal, config));
            Assert.AreEqual(FlowState.LevelSelectLocal, SceneFlowMapper.StateForSceneName("LevelSelectLocal", config));
        }
    }
}
