using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using UnityEditor;

using UnityEngine;

[UsedImplicitly]
// ReSharper disable once CheckNamespace
public static class BuildScript {
    private static readonly string[] secrets =
        { "androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass" };

    [UsedImplicitly]
    public static void Build() {
        Dictionary<string, string> options = GetValidatedOptions();

        BuildTarget buildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), options["targetPlatform"]);
        switch (buildTarget) {
            case BuildTarget.Android:
                Console.WriteLine("Android build not supported.");
                EditorApplication.Exit(1000);
                break;
            case BuildTarget.StandaloneOSX:
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                break;
        }

        AssetBundleManifest manifest =
            BuildPipeline.BuildAssetBundles(options["customBuildPath"], BuildAssetBundleOptions.None, buildTarget);
        Console.WriteLine($"Built {manifest.GetAllAssetBundles().Length.ToString()} AssetBundles.");
        EditorApplication.Exit(0);
    }

    [NotNull]
    private static Dictionary<string, string> GetValidatedOptions() {
        ParseCommandLineArguments(out Dictionary<string, string> validatedOptions);

        if(!validatedOptions.TryGetValue("projectPath", out string _)) {
            Console.WriteLine("Missing argument -projectPath");
            EditorApplication.Exit(110);
        }

        if(!validatedOptions.TryGetValue("buildTarget", out string buildTarget)) {
            Console.WriteLine("Missing argument -buildTarget");
            EditorApplication.Exit(120);
        }

        if(!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
            EditorApplication.Exit(121);

        if(!validatedOptions.TryGetValue("customBuildPath", out string _)) {
            Console.WriteLine("Missing argument -customBuildPath");
            EditorApplication.Exit(130);
        }

        return validatedOptions;
    }

    private static void ParseCommandLineArguments([NotNull] out Dictionary<string, string> providedArguments) {
        providedArguments = new Dictionary<string, string>();
        string[] args = Environment.GetCommandLineArgs();

        // Extract flags with optional values
        for(int current = 0, next = 1; current < args.Length; current++, next++) {
            // Parse flag
            bool isFlag = args[current].StartsWith("-", StringComparison.Ordinal);
            if(!isFlag)
                continue;
            string flag = args[current].TrimStart('-');

            // Parse optional value
            bool flagHasValue = next < args.Length && !args[next].StartsWith("-", StringComparison.Ordinal);
            string value = flagHasValue ? args[next].TrimStart('-') : "";
            bool secret = secrets.Contains(flag);
            string displayValue = secret ? "*HIDDEN*" : "\"" + value + "\"";

            // Assign
            Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
            providedArguments.Add(flag, value);
        }
    }
}
