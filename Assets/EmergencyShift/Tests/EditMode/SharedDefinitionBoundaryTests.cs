using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class SharedDefinitionBoundaryTests
    {
        [TestCase("EmergencyShift.StalkerLocomotionProfile, EmergencyShift.Runtime")]
        [TestCase("EmergencyShift.DialogueSequence, EmergencyShift.Runtime")]
        [TestCase("EmergencyShift.DialogueSequence+Speaker, EmergencyShift.Runtime")]
        [TestCase("EmergencyShift.DialogueSequence+Line, EmergencyShift.Runtime")]
        [TestCase("EmergencyShift.DialogueSequence+Choice, EmergencyShift.Runtime")]
        [TestCase("EmergencyShift.DialogueStyle, EmergencyShift.Runtime")]
        [TestCase("EmergencyShift.FurnitureLightStudyProfile, EmergencyShift.Development")]
        [TestCase("EmergencyShift.FurnitureLightStudyProfile+Response, EmergencyShift.Development")]
        public void SharedDefinitionsExposeNoMutablePublicState(string typeName)
        {
            Type type = Type.GetType(typeName, true);
            AssertReadonlyApi(type);
        }

        [Test]
        public void EveryOwnRuntimeAndDevelopmentScriptableDefinitionHasReadonlyApi()
        {
            Assembly runtime = typeof(DialogueSequence).Assembly;
            Assembly development = Type.GetType("EmergencyShift.FurnitureLightStudyProfile, EmergencyShift.Development", true).Assembly;
            foreach (Assembly assembly in new[] { runtime, development })
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(type) &&
                        type.Namespace != null && type.Namespace.StartsWith("EmergencyShift", StringComparison.Ordinal))
                    {
                        AssertReadonlyApi(type);
                    }
                }
            }
        }

        private static void AssertReadonlyApi(Type type)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.That(field.IsInitOnly, Is.True, type.Name + "." + field.Name);
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.That(property.GetSetMethod(), Is.Null, type.Name + "." + property.Name);
                Assert.That(property.PropertyType.IsArray, Is.False, type.Name + "." + property.Name);
            }
        }

        [TestCase(typeof(StalkerLocomotionProfile), "distancePerCycle", float.NaN)]
        [TestCase(typeof(StalkerLocomotionProfile), "supportHold", .7f)]
        [TestCase(typeof(StalkerLocomotionProfile), "supportYawDegrees", 4f)]
        [TestCase(typeof(StalkerLocomotionProfile), "minimumMovingSpeed", 0f)]
        [TestCase(typeof(StalkerLocomotionProfile), "fullWeightSpeed", float.PositiveInfinity)]
        [TestCase(typeof(StalkerLocomotionProfile), "startSeconds", 0f)]
        [TestCase(typeof(StalkerLocomotionProfile), "settleSeconds", -1f)]
        [TestCase(typeof(StalkerLocomotionProfile), "turnSmoothSeconds", float.NaN)]
        [TestCase(typeof(DialogueStyle), "portraitSize", float.NaN)]
        [TestCase(typeof(DialogueStyle), "bottomMargin", -1f)]
        [TestCase(typeof(DialogueStyle), "choiceMinHeight", 0f)]
        [TestCase(typeof(DialogueStyle), "shadowExtraHeight", float.PositiveInfinity)]
        public void InvalidSharedNumbersAreRejectedWithoutChangingTheProfile(Type type, string field, float value)
        {
            ScriptableObject profile = ScriptableObject.CreateInstance(type);
            try
            {
                type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(profile, value);
                string before = JsonUtility.ToJson(profile);
                var exception = Assert.Throws<TargetInvocationException>(() => type.GetMethod("Validate").Invoke(profile, null));
                Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
                Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [TestCase("StalkerLocomotionProfile")]
        [TestCase("DialogueStyle")]
        [TestCase("DialogueSequence")]
        [TestCase("FurnitureLightStudyProfile")]
        public void AllAuthoredProfilesDeserializeAndValidateWithoutMutation(string typeName)
        {
            string assembly = typeName == "FurnitureLightStudyProfile" ? "EmergencyShift.Development" : "EmergencyShift.Runtime";
            Type type = Type.GetType("EmergencyShift." + typeName + ", " + assembly, true);
            string[] found = AssetDatabase.FindAssets("t:" + typeName, new[] { "Assets/EmergencyShift" });
            Assert.That(found, Is.Not.Empty);
            foreach (string guid in found)
            {
                var profile = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type);
                string before = JsonUtility.ToJson(profile);
                Assert.DoesNotThrow(() => type.GetMethod("Validate").Invoke(profile, null));
                Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
            }
        }

        [Test]
        public void LineOwnsItsChoiceArrayAndEditorRejectsInvalidGraphBeforeWriting()
        {
            var options = new[] { new DialogueSequence.Choice("original", "Continue", "end") };
            var line = new DialogueSequence.Line("fork", 0, "Question", choices: options);
            options[0] = new DialogueSequence.Choice("replacement", "Other", "missing");
            Assert.That(line.ChoiceAt(0).Id, Is.EqualTo("original"));
            var sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            try
            {
                Editor.DialogueDefinitionAuthoring.SetSpeakers(sequence, new[] { new DialogueSequence.Speaker("Doctor") });
                Editor.DialogueDefinitionAuthoring.SetGraph(sequence, "safe", new[] { new DialogueSequence.Line("safe", 0, "Safe") });
                string before = JsonUtility.ToJson(sequence);
                Assert.Throws<InvalidOperationException>(() => Editor.DialogueDefinitionAuthoring.SetGraph(sequence, "bad",
                    new[] { new DialogueSequence.Line("bad", 0, "Bad", "missing") }));
                Assert.That(JsonUtility.ToJson(sequence), Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }
    }
}
