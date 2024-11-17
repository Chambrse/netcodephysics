using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class CommandLineBuildScript
{
    // Paths for client and server builds
    private static string clientBuildPath = "Builds/Client";
    private static string serverBuildPath = "Builds/Server";

    // Build the client version using Player Settings from the project
    [MenuItem("Build/Build Client")]
    public static void BuildClient()
    {
        BuildPlayer(BuildTarget.StandaloneWindows64, clientBuildPath, BuildOptions.None, "Client");
    }

    // Build the server version using Player Settings from the project
    [MenuItem("Build/Build Server")]

    public static void BuildServer()
    {
        BuildPlayer(BuildTarget.StandaloneWindows64, serverBuildPath, (BuildOptions)StandaloneBuildSubtarget.Server, "Server");
    }

    private static void BuildPlayer(BuildTarget target, string path, BuildOptions options, string buildName)
    {
        // Use the product name from Player Settings for the build file name
        string productName = PlayerSettings.productName;

        // Configure build options
        var buildOptions = new BuildPlayerOptions
        {
            scenes = GetScenes(), // Gets the enabled scenes from Editor Build Settings
            locationPathName = $"{path}/{productName}_{buildName}.exe", // Adjust for platform if needed
            target = target,
            options = options
        };

        Debug.Log($"Starting {buildName} build with Player Settings configurations...");
        var report = BuildPipeline.BuildPlayer(buildOptions);
        LogBuildResult(report);
    }

    private static string[] GetScenes()
    {
        return Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path);
    }

    private static void LogBuildResult(BuildReport report)
    {
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"{report.summary.result}: {report.summary.totalSize / (1024 * 1024)} MB built at {report.summary.outputPath}");
        }
        else if (report.summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed!");
        }
    }
}
