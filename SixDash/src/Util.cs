using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using BepInEx.Logging;

using HarmonyLib;

using JetBrains.Annotations;

using SixDash.Patches;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SixDash;

/// <summary>
/// Utilities.
/// </summary>
[PublicAPI]
public static class Util {
    internal static ManualLogSource? logger { get; set; }

    /// <summary>
    /// Finds a resource of the specified type with a specified name.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="name">The name.</param>
    /// <returns>The found resource. Null if no resource was found.</returns>
    public static Object? FindResourceOfTypeWithName(Type type, string name) {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach(Object obj in Resources.FindObjectsOfTypeAll(type)) {
            if(obj.name != name)
                continue;
            return obj;
        }

        return null;
    }

    /// <summary>
    /// Finds a resource of the specified type with a specified name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns>The found resource. Null if no resource was found.</returns>
    public static T? FindResourceOfTypeWithName<T>(string name) where T : Object =>
        (T?)FindResourceOfTypeWithName(typeof(T), name);

    /// <summary>
    /// Applies all patches inheriting from <see cref="IPatch"/>.
    /// </summary>
    public static void ApplyAllPatches() {
        Assembly assembly = Assembly.GetCallingAssembly();
        ForEachImplementation(assembly, typeof(IPatch), Action);
        void Action(Type type) {
            IPatch? patch = null;
            try { patch = Activator.CreateInstance(type) as IPatch; }
            catch(TargetInvocationException ex) { logger?.LogError(ex); } // constructor exception
            catch { /* ignored */ }

            try { patch?.Apply(); }
            catch(Exception ex) { logger?.LogError(ex); }

            ForEachImplementation(assembly, type, Action);
        }
    }

    /// <summary>
    /// Iterates each implementation of an type in all assemblies.
    /// </summary>
    /// <param name="baseType">The type.</param>
    /// <param name="action">Execute for each found type.</param>
    public static void ForEachImplementation(Type baseType, Action<Type> action) {
        foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            ForEachImplementation(assembly, baseType, action);
    }

    /// <summary>
    /// Iterates each implementation of an type in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="baseType">The type.</param>
    /// <param name="action">Execute for each found type.</param>
    public static void ForEachImplementation(Assembly assembly, Type baseType, Action<Type> action) {
        try {
            foreach(Type type in assembly.GetTypes())
                if(new List<Type>(type.GetNestedTypes(AccessTools.all)).Contains(baseType) ||
                   new List<Type>(type.GetInterfaces()).Contains(baseType))
                    action(type);
        }
        catch(ReflectionTypeLoadException ex) {
            LogReflectionTypeLoadException(ex);
        }
    }

    private static void LogReflectionTypeLoadException(ReflectionTypeLoadException ex) {
        if(logger is null)
            Debug.LogWarning(ex);
        else
            logger.LogWarning(ex);
        foreach(Exception loaderException in ex.LoaderExceptions)
            if(logger is null)
                Debug.LogWarning(loaderException);
            else
                logger.LogWarning(loaderException);
    }

    /// <summary>
    /// Loads an AssetBundle from <see cref="Application.streamingAssetsPath"/> with the specified path with
    /// a platform identifier appended at the end.
    /// </summary>
    /// <param name="path">AssetBundle path inside <see cref="Application.streamingAssetsPath"/>.</param>
    /// <returns>The loaded AssetBundle.</returns>
    public static AssetBundle LoadPlatformAssetBundle(string path) {
        string platform = Application.platform switch {
            RuntimePlatform.WindowsEditor or RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsServer =>
                "StandaloneWindows64",
            RuntimePlatform.LinuxEditor or RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxServer =>
                "StandaloneLinux64",
            RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer or RuntimePlatform.OSXServer => "StandaloneOSX",
            _ => "Unknown"
        };
        return AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, $"{path}-{platform}"));
    }

    // i ain't gonna document every easing hhh
#pragma warning disable CS1591
    /// <summary>
    /// Easings.
    /// </summary>
    [PublicAPI] public enum Easing { Linear, Sine, Ease, Exponential, Circular, Back, Elastic, Bounce }

    /// <summary>
    /// Easing modes.
    /// </summary>
    [PublicAPI] public enum EasingMode { In, Out, InOut }
#pragma warning restore CS1591

    // https://easings.net 🙏
    /// <summary>
    /// Applies an easing to a value.
    /// </summary>
    /// <param name="x">The value.</param>
    /// <param name="easing">The easing.</param>
    /// <param name="mode">The easing mode.</param>
    /// <param name="argument">The easing argument.
    /// Applies to: <see cref="Easing.Ease"/>, <see cref="Easing.Bounce"/>. Ignored otherwise.</param>
    /// <returns>The value with the easing applied.</returns>
    public static float ApplyEasing(float x, Easing easing, EasingMode mode, float argument) {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;
        const float c3 = c1 + 1f;
        const float c4 = 2f * Mathf.PI / 3f;
        const float c5 = 2f * Mathf.PI / 4.5f;
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        return easing switch {
            Easing.Sine => mode switch {
                EasingMode.In => 1f - Mathf.Cos(x * Mathf.PI / 2f),
                EasingMode.Out => Mathf.Sin(x * Mathf.PI / 2f),
                EasingMode.InOut => -(Mathf.Cos(Mathf.PI * x) - 1f) / 2f,
                _ => x
            },
            Easing.Ease => mode switch {
                EasingMode.In => Mathf.Pow(x, argument),
                EasingMode.Out => 1f - Mathf.Pow(1f - x, argument),
                EasingMode.InOut => x < 0.5f ? Mathf.Pow(2f, argument - 1f) * Mathf.Pow(x, argument) :
                    1f - Mathf.Pow(-2f * x + 2f, argument) / 2f,
                _ => x
            },
            Easing.Exponential => mode switch {
                EasingMode.In => x == 0f ? 0f : Mathf.Pow(2f, 10f * x - 10f),
                EasingMode.Out => x == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * x),
                EasingMode.InOut => x switch {
                    0f => 0f,
                    1f => 1f,
                    _ => x < 0.5f ? Mathf.Pow(2f, 20f * x - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * x + 10f)) / 2f
                },
                _ => x
            },
            Easing.Circular => mode switch {
                EasingMode.In => 1f - Mathf.Sqrt(1f - x * x),
                EasingMode.Out => Mathf.Sqrt(1f - (x - 1f) * (x - 1f)),
                EasingMode.InOut => x < 0.5f ?
                    // ReSharper disable once ArrangeRedundantParentheses
                    (1f - Mathf.Sqrt(1f - (2f * x) * (2f * x))) / 2f :
                    (Mathf.Sqrt(1f - (-2f * x + 2f) * (-2f * x + 2f)) + 1f) / 2f,
                _ => x
            },
            Easing.Back => mode switch {
                EasingMode.In => c1 * x * x * x - c3 * x * x,
                EasingMode.Out => 1f + c3 * (x - 1f) * (x - 1f) * (x - 1f) + c1 * (x - 1f) * (x - 1f),
                EasingMode.InOut => x < 0.5f ?
                    // ReSharper disable once ArrangeRedundantParentheses
                    (2f * x) * (2f * x) * ((c2 + 1f) * 2f * x - c2) / 2f :
                    ((2f * x - 2f) * (2f * x - 2f) * ((c2 + 1f) * (x * 2f - 2f) + c2) + 2f) / 2f,
                _ => x
            },
            Easing.Elastic => mode switch {
                EasingMode.In => x switch {
                    0f => 0f,
                    1f => 1f,
                    _ => -Mathf.Pow(2f, 10f * x - 10f) * Mathf.Sin((x * 10f - 10.75f) * c4)
                },
                EasingMode.Out => x switch {
                    0f => 0f,
                    1f => 1f,
                    _ => Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f
                },
                EasingMode.InOut => x switch {
                    0f => 0f,
                    1f => 1f,
                    _ => x < 0.5f ?
                        -(Mathf.Pow(2f, 20f * x - 10f) * Mathf.Sin((20f * x - 11.125f) * c5)) / 2f :
                        Mathf.Pow(2f, -20f * x + 10f) * Mathf.Sin((20f * x - 11.125f) * c5) / 2f + 1f
                },
                _ => x
            },
            Easing.Bounce => mode switch {
                EasingMode.In => 1f - ApplyEasing(1f - x, Easing.Bounce, EasingMode.Out, argument),
                EasingMode.Out =>
                    x < 1f / d1 ? n1 * x * x :
                    x < 2f / d1 ? n1 * (x -= 1.5f / d1) * x + 0.75f :
                    x < 2.5 / d1 ? n1 * (x -= 2.25f / d1) * x + 0.9375f :
                    n1 * (x -= 2.625f / d1) * x + 0.984375f,
                EasingMode.InOut => x < 0.5f ?
                    (1f - ApplyEasing(1f - 2f * x, Easing.Bounce, EasingMode.Out, argument)) / 2f :
                    (1f + ApplyEasing(2f * x - 1f, Easing.Bounce, EasingMode.Out, argument)) / 2f,
                _ => x
            },
            _ => x
        };
    }
}
