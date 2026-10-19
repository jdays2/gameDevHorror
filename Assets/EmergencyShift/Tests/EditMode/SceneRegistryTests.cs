using System;
using EmergencyShift.Core.Composition;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class SceneRegistryTests
    {
        [Test]
        public void DuplicateRegistrationAndRepeatedRemovalDoNotDuplicateMembers()
        {
            using (var registry = new SceneRegistry<object>())
            {
                var actor = new object();
                int firstRegistration = registry.Register(actor);
                Assert.That(firstRegistration, Is.GreaterThan(0));
                Assert.That(registry.Register(actor), Is.Zero);
                Assert.That(registry.Members.Count, Is.EqualTo(1));
                registry.Remove(actor);
                registry.Remove(actor);
                Assert.That(registry.Members, Is.Empty);
                Assert.That(registry.Register(actor), Is.GreaterThan(firstRegistration));
            }
        }

        [Test]
        public void UnloadingOneSceneCannotClearAnotherScenesMembers()
        {
            var first = new SceneRegistry<object>();
            using (var second = new SceneRegistry<object>())
            {
                var actor = new object();
                first.Register(actor);
                second.Register(actor);
                first.Dispose();
                first.Dispose();
                first.Remove(actor);
                Assert.That(first.Members, Is.Empty);
                Assert.That(second.Members, Has.Count.EqualTo(1));
                Assert.Throws<ObjectDisposedException>(() => first.Register(actor));
            }
        }
    }
}
