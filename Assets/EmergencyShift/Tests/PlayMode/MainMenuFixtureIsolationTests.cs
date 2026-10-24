using System.Collections;
using EmergencyShift.Features.MainMenu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class MainMenuFixtureIsolationTests
    {
        [UnityTest]
        public IEnumerator AutomatedTestFixtureDoesNotBootTheInteractiveMenu()
        {
            yield return null;
            Assert.That(Object.FindAnyObjectByType<MainMenuContext>(), Is.Null,
                "Ordinary Play must boot the menu, but Test Runner must retain its isolated fixture scene.");
        }
    }
}
