using HybridGame.MasterBlaster.Scripts.Mobile.Layout;
using NUnit.Framework;

namespace HybridGame.MasterBlaster.Tests.EditMode
{
    public class MobileHandheldDeviceDefinitionParserEditModeTests
    {
        private const string SampleDeviceJson = @"{
    ""friendlyName"": ""Apple iPhone 12"",
    ""version"": 1,
    ""systemInfo"": {
        ""deviceModel"": ""iPhone13,2"",
        ""deviceType"": 1
    }
}";

        [Test]
        public void TryGetFriendlyNameFromDeviceJson_MatchesSystemInfoDeviceModel_ReturnsFriendlyName()
        {
            Assert.IsTrue(
                MobileHandheldDeviceDefinitionParser.TryGetFriendlyNameFromDeviceJson(
                    SampleDeviceJson,
                    "iPhone13,2",
                    out var friendly));
            Assert.AreEqual("Apple iPhone 12", friendly);
        }

        [Test]
        public void TryGetFriendlyNameFromDeviceJson_WrongModel_ReturnsFalse()
        {
            Assert.IsFalse(
                MobileHandheldDeviceDefinitionParser.TryGetFriendlyNameFromDeviceJson(
                    SampleDeviceJson,
                    "OtherPhone",
                    out _));
        }
    }
}
