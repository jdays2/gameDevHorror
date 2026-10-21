using System;
using EmergencyShift.Features.AccessControl;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class AccessCardRestoreTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("live")]
        public void InvalidSnapshotIsRejectedBeforeReplacingCardsOrPublishingChanges(string invalid)
        {
            var owner = new GameObject("Isolated card snapshot");
            try
            {
                var cards = owner.AddComponent<PlayerAccessCards2D>();
                cards.Construct(new AccessControlFactory());
                cards.Add("live");
                int events = 0;
                cards.Changed += () => events++;
                Assert.Throws<InvalidOperationException>(() => cards.Restore(new[] { "live", invalid }));
                Assert.That(cards.Capture(), Is.EqualTo(new[] { "live" }));
                Assert.That(events, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
