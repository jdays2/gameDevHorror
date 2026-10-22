using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.MainMenu.Logic;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class MainMenuTests
    {
        private static MenuSceneCatalog Catalog() => new MenuSceneCatalog("Assets/Home.unity", "sandbox", new[]
        {
            new MenuScene("sandbox", "Sandbox", "Assets/Sandbox.unity"),
            new MenuScene("other", "Other", "Assets/Other.unity")
        });

        [Test]
        public void ReleasePlayerCannotOpenOwnerScenesOrLaunchArbitraryScene()
        {
            var navigation = new Navigation();
            using var controller = new MainMenuController(new MainMenuModel(Catalog(), false), navigation);
            Assert.That(controller.Open(MenuPage.Scenes).Success, Is.False);
            Assert.That(controller.Launch("sandbox").Success, Is.False);
            controller.Open(MenuPage.NewGame);
            Assert.That(controller.Launch("other").Success, Is.False);
            Assert.That(navigation.Loads, Is.Zero);
            Assert.That(controller.Launch("sandbox").Success, Is.True);
        }

        [Test]
        public void RepeatedLaunchAndPageCommandsCannotInterruptPendingLoad()
        {
            var navigation = new Navigation();
            using var controller = new MainMenuController(new MainMenuModel(Catalog(), true), navigation);
            controller.Open(MenuPage.Scenes);
            controller.Launch("other");
            Assert.That(controller.Launch("sandbox").Success, Is.False);
            Assert.That(controller.Open(MenuPage.Home).Success, Is.False);
            Assert.That(controller.Quit().Success, Is.False);
            Assert.That(controller.Model.Page, Is.EqualTo(MenuPage.Loading));
            Assert.That(navigation.Loads, Is.EqualTo(1));
        }

        [Test]
        public void UnavailableSceneDoesNotBeginLoadAndCanBeRetried()
        {
            var navigation = new Navigation { Available = false };
            using var controller = new MainMenuController(new MainMenuModel(Catalog(), true), navigation);
            controller.Open(MenuPage.Scenes);
            Assert.That(controller.Launch("other").Success, Is.False);
            Assert.That(controller.Model.Page, Is.EqualTo(MenuPage.Scenes));
            Assert.That(controller.Model.Error, Is.Not.Empty);
            navigation.Available = true;
            Assert.That(controller.Launch("other").Success, Is.True);
        }

        [Test]
        public void LoadFailureRestoresOriginAndDisposedScopeStopsReceivingEvents()
        {
            var navigation = new Navigation();
            var controller = new MainMenuController(new MainMenuModel(Catalog(), true), navigation);
            controller.Open(MenuPage.NewGame);
            controller.Launch("sandbox");
            navigation.Fail("disk error");
            Assert.That(controller.Model.Page, Is.EqualTo(MenuPage.NewGame));
            Assert.That(controller.Model.Error, Is.EqualTo("disk error"));
            controller.Dispose();
            navigation.Fail("late event");
            Assert.That(controller.Model.Error, Is.EqualTo("disk error"));
            Assert.That(controller.Open(MenuPage.Home).Success, Is.False);
        }

        [Test]
        public void CatalogCopiesEntriesAndRejectsDuplicateIdentity()
        {
            var entries = new[] { new MenuScene("one", "One", "Assets/One.unity") };
            var catalog = new MenuSceneCatalog("Assets/Home.unity", "one", entries);
            entries[0] = new MenuScene("two", "Two", "Assets/Two.unity");
            Assert.That(catalog.Find("one"), Is.Not.Null);
            Assert.Throws<ArgumentException>(() => new MenuSceneCatalog("Assets/Home.unity", "one",
                new[] { catalog.At(0), catalog.At(0) }));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        public void InvalidPreferencesNeverReachPlatform(float volume)
        {
            var platform = new Preferences();
            var controller = new MenuPreferencesController(platform);
            Assert.That(controller.Apply(new MenuPreferences(volume, true)).Success, Is.False);
            Assert.That(platform.Writes, Is.Zero);
            Assert.That(controller.Applied.volume, Is.EqualTo(1));
        }

        [Test]
        public void PreferenceInstancesAreIndependentAndFailureDoesNotCommitState()
        {
            var first = new MenuPreferencesController(new Preferences());
            var failing = new MenuPreferencesController(new Preferences { Fail = true });
            Assert.That(first.Apply(new MenuPreferences(0.3f, false)).Success, Is.True);
            Assert.Throws<InvalidOperationException>(() => failing.Apply(new MenuPreferences(0.2f, true)));
            Assert.That(failing.Applied.volume, Is.EqualTo(1));
            Assert.That(first.Applied.volume, Is.EqualTo(0.3f));
        }

        private sealed class Navigation : IMenuNavigation
        {
            public bool Available = true;
            public int Loads;
            public bool OwnerAccess => true;
            public bool IsBusy => false;
            public event Action<string> LoadFailed;
            public event Action OwnerMenuRequested { add { } remove { } }
            public bool CanLoad(string path) => Available;
            public CommandResult Load(string path)
            {
                Loads++;
                return CommandResult.Ok();
            }
            public void Quit() { }
            public void Fail(string reason) => LoadFailed?.Invoke(reason);
        }

        private sealed class Preferences : IMenuPreferencesPlatform
        {
            public int Writes;
            public bool Fail;
            public MenuPreferences Read() => new MenuPreferences(1, false);
            public void ApplyAndSave(MenuPreferences values)
            {
                if (Fail)
                {
                    throw new InvalidOperationException("Save failed");
                }
                Writes++;
            }
        }
    }
}
