using UnityEditor;
using UnityEngine;

namespace MeowMeowDog.EditorTools
{
    /// <summary>命令行打包：-executeMethod MeowMeowDog.EditorTools.BuildScript.BuildMac [-buildPath 路径]</summary>
    public static class BuildScript
    {
        public static void BuildMac()
        {
            string path = "Builds/MeowMeowDog.app";
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-buildPath") path = args[i + 1];

            // 只出 Apple Silicon 包（本机 Editor 为 arm64；需要 Intel 包时再改 Universal）
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;

            var report = BuildPipeline.BuildPlayer(
                new[] { "Assets/Scenes/Main.unity" },
                path,
                BuildTarget.StandaloneOSX,
                BuildOptions.None);

            var summary = report.summary;
            Debug.Log($"[MMDog] Build result={summary.result} size={summary.totalSize} errors={summary.totalErrors} path={path}");
            EditorApplication.Exit(summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
