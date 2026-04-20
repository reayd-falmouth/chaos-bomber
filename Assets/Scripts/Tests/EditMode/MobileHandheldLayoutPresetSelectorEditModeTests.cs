using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Mobile.Layout;
using NUnit.Framework;
using UnityEngine;

namespace HybridGame.MasterBlaster.Tests.EditMode
{
    public class MobileHandheldLayoutPresetSelectorEditModeTests
    {
        [Test]
        public void TrySelectExact_FindsMatchingPixels()
        {
            var a = new MobileHandheldLayoutPresetEntry { screenWidth = 1920, screenHeight = 1080, label = "a" };
            var b = new MobileHandheldLayoutPresetEntry { screenWidth = 1280, screenHeight = 720, label = "b" };
            var list = new List<MobileHandheldLayoutPresetEntry> { a, b };

            Assert.IsTrue(MobileHandheldLayoutPresetSelector.TrySelectExact(list, 1280, 720, out var hit, out var idx));
            Assert.AreSame(b, hit);
            Assert.AreEqual(1, idx);
        }

        [Test]
        public void TrySelectNearestAspect_PicksClosestRatio()
        {
            var sixteenNine = new MobileHandheldLayoutPresetEntry { screenWidth = 1920, screenHeight = 1080, label = "16:9" };
            var fourThree = new MobileHandheldLayoutPresetEntry { screenWidth = 1024, screenHeight = 768, label = "4:3" };
            var list = new List<MobileHandheldLayoutPresetEntry> { sixteenNine, fourThree };

            Assert.IsTrue(
                MobileHandheldLayoutPresetSelector.TrySelectNearestAspect(list, 800, 450, out var hit, out _));
            Assert.AreSame(sixteenNine, hit);
        }

        [Test]
        public void TryBuildInterpolatedEntry_BracketsAspect()
        {
            var low = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 1000,
                screenHeight = 1000,
                label = "1:1",
                cinemachineBrainOutputCamera = new MobileHandheldUnityCameraSnapshot
                {
                    captureEnabled = true,
                    orthographicSize = 5f,
                },
            };
            var high = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 2000,
                screenHeight = 1000,
                label = "2:1",
                cinemachineBrainOutputCamera = new MobileHandheldUnityCameraSnapshot
                {
                    captureEnabled = true,
                    orthographicSize = 10f,
                },
            };
            var list = new List<MobileHandheldLayoutPresetEntry> { low, high };

            Assert.IsTrue(MobileHandheldLayoutPresetSelector.TryBuildInterpolatedEntry(list, 1500, 1000, out var mid));
            Assert.NotNull(mid);
            Assert.Greater(mid.cinemachineBrainOutputCamera.orthographicSize, low.cinemachineBrainOutputCamera.orthographicSize);
            Assert.Less(mid.cinemachineBrainOutputCamera.orthographicSize, high.cinemachineBrainOutputCamera.orthographicSize);
        }

        [Test]
        public void TryBuildInterpolatedEntry_LerpsOverlayBackgroundRect()
        {
            var low = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 1000,
                screenHeight = 1000,
                label = "1:1",
                overlayBackgroundRect = new MobileHandheldRectSnapshot { anchoredPosition = new Vector2(0f, 0f) },
            };
            var high = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 2000,
                screenHeight = 1000,
                label = "2:1",
                overlayBackgroundRect = new MobileHandheldRectSnapshot { anchoredPosition = new Vector2(100f, 0f) },
            };
            var list = new List<MobileHandheldLayoutPresetEntry> { low, high };

            Assert.IsTrue(MobileHandheldLayoutPresetSelector.TryBuildInterpolatedEntry(list, 1500, 1000, out var mid));
            Assert.NotNull(mid);
            Assert.AreEqual(50f, mid.overlayBackgroundRect.anchoredPosition.x, 1e-5f);
        }

        [Test]
        public void MobileHandheldTransformSnapshot_Lerp_Midpoint()
        {
            var a = new MobileHandheldTransformSnapshot
            {
                captureTransform = true,
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity,
                localScale = Vector3.one,
            };
            var b = new MobileHandheldTransformSnapshot
            {
                captureTransform = true,
                localPosition = new Vector3(10f, 0f, 0f),
                localRotation = Quaternion.Euler(0f, 90f, 0f),
                localScale = new Vector3(2f, 2f, 2f),
            };
            var mid = MobileHandheldTransformSnapshot.Lerp(a, b, 0.5f);
            Assert.IsTrue(mid.captureTransform);
            Assert.AreEqual(5f, mid.localPosition.x, 1e-5f);
            Assert.AreEqual(1.5f, mid.localScale.x, 1e-5f);
            var expectedYaw = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(0f, 90f, 0f), 0.5f).eulerAngles.y;
            Assert.AreEqual(expectedYaw, mid.localRotation.eulerAngles.y, 1e-4f);
        }

        [Test]
        public void TryBuildInterpolatedEntry_LerpsBrainAndVcamTransforms()
        {
            var low = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 1000,
                screenHeight = 1000,
                label = "1:1",
                cinemachineBrainOutputCamera = new MobileHandheldUnityCameraSnapshot
                {
                    captureEnabled = true,
                    orthographicSize = 5f,
                    outputCameraTransform = new MobileHandheldTransformSnapshot
                    {
                        captureTransform = true,
                        localPosition = Vector3.zero,
                        localRotation = Quaternion.identity,
                        localScale = Vector3.one,
                    },
                },
                cinemachineVcams = new[]
                {
                    new MobileHandheldCinemachineVcamSnapshotEntry
                    {
                        gameObjectName = "VcamA",
                        vcamTransform = new MobileHandheldTransformSnapshot
                        {
                            captureTransform = true,
                            localPosition = new Vector3(0f, 1f, 0f),
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        },
                    },
                },
            };
            var high = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = 2000,
                screenHeight = 1000,
                label = "2:1",
                cinemachineBrainOutputCamera = new MobileHandheldUnityCameraSnapshot
                {
                    captureEnabled = true,
                    orthographicSize = 10f,
                    outputCameraTransform = new MobileHandheldTransformSnapshot
                    {
                        captureTransform = true,
                        localPosition = new Vector3(10f, 0f, 0f),
                        localRotation = Quaternion.Euler(0f, 90f, 0f),
                        localScale = Vector3.one,
                    },
                },
                cinemachineVcams = new[]
                {
                    new MobileHandheldCinemachineVcamSnapshotEntry
                    {
                        gameObjectName = "VcamA",
                        vcamTransform = new MobileHandheldTransformSnapshot
                        {
                            captureTransform = true,
                            localPosition = new Vector3(0f, 3f, 0f),
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one,
                        },
                    },
                },
            };
            var list = new List<MobileHandheldLayoutPresetEntry> { low, high };

            Assert.IsTrue(MobileHandheldLayoutPresetSelector.TryBuildInterpolatedEntry(list, 1500, 1000, out var mid));
            Assert.NotNull(mid);
            Assert.IsTrue(mid.cinemachineBrainOutputCamera.outputCameraTransform.captureTransform);
            Assert.AreEqual(5f, mid.cinemachineBrainOutputCamera.outputCameraTransform.localPosition.x, 1e-5f);
            Assert.AreEqual(2f, mid.cinemachineVcams[0].vcamTransform.localPosition.y, 1e-5f);
        }
    }
}
