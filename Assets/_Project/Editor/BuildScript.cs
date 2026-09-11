using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DropProtocol.Editor
{
/// <summary>
///     Player builds from the menu or the command line:
///     <c>Unity -batchmode -quit -projectPath . -executeMethod DropProtocol.Editor.BuildScript.BuildWindowsFromCommandLine</c>.
///     Scenes come from the Build Settings list so the two entry points can never drift.
/// </summary>
public static class BuildScript
{
    private const string OutputDirectory = "Builds/Windows";
    private const string ExecutableName = "DropProtocol.exe";

    [MenuItem("DropProtocol/Build/Windows x64")]
    public static void BuildWindows()
    {
        BuildReport report = Build(BuildOptions.None);
        if (report.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(OutputDirectory));
        }
    }

    [MenuItem("DropProtocol/Build/Windows x64 (Development)")]
    public static void BuildWindowsDevelopment()
    {
        Build(BuildOptions.Development | BuildOptions.AllowDebugging);
    }

    public static void BuildWindowsFromCommandLine()
    {
        BuildReport report = Build(BuildOptions.None);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    private static BuildReport Build(BuildOptions options)
    {
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        Directory.CreateDirectory(OutputDirectory);

        var buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(OutputDirectory, ExecutableName),
            target = BuildTarget.StandaloneWindows64,
            options = options,
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;
        string message = $"DropProtocol build {summary.result}: {summary.totalSize / (1024 * 1024)} MB in {summary.totalTime.TotalSeconds:0}s, {summary.totalErrors} errors";
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log(message);
        }
        else
        {
            Debug.LogError(message);
        }

        return report;
    }
}
}
