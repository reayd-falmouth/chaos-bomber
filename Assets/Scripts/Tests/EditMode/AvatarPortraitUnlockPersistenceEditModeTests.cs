using HybridGame.MasterBlaster.Scripts.Levels;
using NUnit.Framework;
using UnityEngine;

namespace fps.Tests.EditMode
{
    public class AvatarPortraitUnlockPersistenceEditModeTests
    {
        const string Key = AvatarPortraitUnlockPersistence.MaskPrefsKey;

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        [Test]
        public void IsUnlocked_Default_False()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            Assert.IsFalse(AvatarPortraitUnlockPersistence.IsUnlocked(0));
        }

        [Test]
        public void Unlock_ThenIsUnlocked_True()
        {
            AvatarPortraitUnlockPersistence.Unlock(2);
            Assert.IsTrue(AvatarPortraitUnlockPersistence.IsUnlocked(2));
            Assert.IsFalse(AvatarPortraitUnlockPersistence.IsUnlocked(1));
        }

        [Test]
        public void Unlock_Idempotent_NoExtraWrites()
        {
            AvatarPortraitUnlockPersistence.Unlock(1);
            int afterFirst = PlayerPrefs.GetInt(Key, 0);
            AvatarPortraitUnlockPersistence.Unlock(1);
            int afterSecond = PlayerPrefs.GetInt(Key, 0);
            Assert.AreEqual(afterFirst, afterSecond);
        }

        [Test]
        public void IsUnlocked_InvalidId_False()
        {
            Assert.IsFalse(AvatarPortraitUnlockPersistence.IsUnlocked(-1));
            Assert.IsFalse(AvatarPortraitUnlockPersistence.IsUnlocked(AvatarPortraitUnlockPersistence.MaxSupportedAvatarId + 1));
        }

        [Test]
        public void Unlock_InvalidId_NoOp()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            AvatarPortraitUnlockPersistence.Unlock(-1);
            AvatarPortraitUnlockPersistence.Unlock(AvatarPortraitUnlockPersistence.MaxSupportedAvatarId + 1);
            Assert.IsFalse(PlayerPrefs.HasKey(Key));
        }
    }
}
