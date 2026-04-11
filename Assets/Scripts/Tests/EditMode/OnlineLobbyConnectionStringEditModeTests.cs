using HybridGame.MasterBlaster.Scripts.Online;
using NUnit.Framework;

namespace fps.Tests.EditMode
{
    public class OnlineLobbyConnectionStringEditModeTests
    {
        [Test]
        public void Normalize_NullOrWhitespace_IsEmpty()
        {
            Assert.AreEqual(string.Empty, OnlineLobbyConnectionString.Normalize(null));
            Assert.AreEqual(string.Empty, OnlineLobbyConnectionString.Normalize(""));
            Assert.AreEqual(string.Empty, OnlineLobbyConnectionString.Normalize("   "));
        }

        [Test]
        public void Normalize_TrimsEdges()
        {
            Assert.AreEqual("abc", OnlineLobbyConnectionString.Normalize("  abc  "));
        }
    }
}
