using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class WheelRowPrefabEditModeTests
    {
        const string WheelRowPrefabPath = "Assets/Prefabs/MasterBlaster/WheelRow.prefab";

        [Test]
        public void Avatar_HasNonZeroLocalScale()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WheelRowPrefabPath);
            Assert.IsNotNull(prefab, "WheelRow prefab missing at " + WheelRowPrefabPath);

            var avatar = prefab.transform.Find("Avatar");
            Assert.IsNotNull(avatar, "WheelRow missing child Avatar");

            var s = avatar.localScale;
            Assert.Greater(Mathf.Abs(s.x), 0.001f, "Avatar localScale.x must be non-zero for UI Image to render.");
            Assert.Greater(Mathf.Abs(s.y), 0.001f, "Avatar localScale.y must be non-zero for UI Image to render.");
            Assert.Greater(Mathf.Abs(s.z), 0.001f, "Avatar localScale.z must be non-zero for UI Image to render.");
        }
    }
}
