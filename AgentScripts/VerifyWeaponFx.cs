using System;
using System.Collections.Generic;
using System.IO;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class VerifyWeaponFx
{
    public static object Check()
    {
        var checks = new List<string>();
        const string root = "Assets/EmergencyShift/";
        foreach (string name in new[] { "PistolDefinition", "PumpShotgun" })
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(root + "Data/Weapons/" + name + ".asset");
            weapon.Validate();
            weapon.Visuals.Validate();
            if (EditorUtility.IsDirty(weapon.Visuals))
            {
                throw new InvalidOperationException("Shared shot profile is dirty: " + name);
            }
            checks.Add(name + ": configuration valid and shared profile clean");
        }
        var effect = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefabs/Combat/PistolShotEffect.prefab");
        if (effect.GetComponentsInChildren<MuzzleSmoke2D>(true).Length != 0)
        {
            throw new InvalidOperationException("Impacts must not instantiate unused smoke children.");
        }
        var effectData = new SerializedObject(effect.GetComponent<ShotEffect2D>());
        if (effectData.FindProperty("smokePrefab").objectReferenceValue == null)
        {
            throw new InvalidOperationException("Missing muzzle smoke template.");
        }
        checks.Add("Smoke prefab reference is valid; impacts have no unused cloud components");
        foreach (string name in new[] { "PistolShotEffect", "MuzzleSmoke" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefabs/Combat/" + name + ".prefab");
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    throw new InvalidOperationException("Missing component in " + name);
                }
                var data = new SerializedObject(component);
                var property = data.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null &&
                        property.objectReferenceEntityIdValue != default)
                    {
                        throw new InvalidOperationException("Missing reference: " + name + ":" + property.propertyPath);
                    }
                }
            }
            checks.Add(name + ": no missing native components or references");
        }
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(root + "Materials/Effects/MuzzleSmoke.shader");
        if (ShaderUtil.ShaderHasError(shader))
        {
            throw new InvalidOperationException("Muzzle smoke shader compilation failed.");
        }
        checks.Add("Muzzle Smoke shader has no native compilation errors");
        var result = new { passed = true, checks };
        Directory.CreateDirectory("work/weapon-fx50");
        File.WriteAllText("work/weapon-fx50/native-final.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }
}
