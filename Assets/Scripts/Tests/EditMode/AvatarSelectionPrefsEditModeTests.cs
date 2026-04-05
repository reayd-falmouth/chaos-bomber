using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class AvatarSelectionPrefsEditModeTests
    {
        [Test]
        public void TryMapPerkToItemType_None_ReturnsFalse()
        {
            Assert.IsFalse(
                AvatarSelectionPrefs.TryMapPerkToItemType(AvatarStartingPerk.None, out _));
        }

        [Test]
        public void TryMapPerkToItemType_MapsKnownPerks()
        {
            AssertMapped(AvatarStartingPerk.ExtraBomb, ItemPickup.ItemType.ExtraBomb);
            AssertMapped(AvatarStartingPerk.BlastRadius, ItemPickup.ItemType.BlastRadius);
            AssertMapped(AvatarStartingPerk.Superman, ItemPickup.ItemType.Superman);
            AssertMapped(AvatarStartingPerk.Protection, ItemPickup.ItemType.Protection);
            AssertMapped(AvatarStartingPerk.Ghost, ItemPickup.ItemType.Ghost);
            AssertMapped(AvatarStartingPerk.SpeedIncrease, ItemPickup.ItemType.SpeedIncrease);
            AssertMapped(AvatarStartingPerk.TimeBomb, ItemPickup.ItemType.TimeBomb);
            AssertMapped(AvatarStartingPerk.RemoteBomb, ItemPickup.ItemType.RemoteBomb);
        }

        static void AssertMapped(AvatarStartingPerk perk, ItemPickup.ItemType expected)
        {
            Assert.IsTrue(AvatarSelectionPrefs.TryMapPerkToItemType(perk, out var t), perk.ToString());
            Assert.AreEqual(expected, t);
        }

        [Test]
        public void ResolveMatchDisplayName_Player1_UsesPrefsWhenNonEmpty()
        {
            const string key = AvatarSelectionPrefs.PlayerDisplayNameKey;
            PlayerPrefs.SetString(key, "  Hero  ");
            PlayerPrefs.Save();

            Assert.AreEqual("Hero", AvatarSelectionPrefs.ResolveMatchDisplayName(1, "Fallback"));

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        [Test]
        public void ResolveMatchDisplayName_Player1_EmptyPrefs_UsesFallback()
        {
            PlayerPrefs.DeleteKey(AvatarSelectionPrefs.PlayerDisplayNameKey);
            PlayerPrefs.Save();

            Assert.AreEqual("GoName", AvatarSelectionPrefs.ResolveMatchDisplayName(1, "GoName"));
        }

        [Test]
        public void ResolveMatchDisplayName_OtherPlayer_IgnoresPrefs()
        {
            PlayerPrefs.SetString(AvatarSelectionPrefs.PlayerDisplayNameKey, "Solo");
            PlayerPrefs.Save();

            Assert.AreEqual("P2", AvatarSelectionPrefs.ResolveMatchDisplayName(2, "P2"));

            PlayerPrefs.DeleteKey(AvatarSelectionPrefs.PlayerDisplayNameKey);
            PlayerPrefs.Save();
        }
    }
}
