using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TapPop.EditorTools
{
    public static class TapPopBuild
    {
        static bool Dev => System.Environment.GetEnvironmentVariable("TAPPOP_DEV") == "1";
        static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, Dev ? "../../Build/WebGL-dev" : "../../Build/WebGL"));

        [MenuItem("TapPop/Build WebGL", priority = 30)]
        public static void BuildWebGL()
        {
            PlayerSettings.companyName = "Yagmur Simsek";
            PlayerSettings.productName = "TapPop";
            // Own page template: white page, 9:16 canvas on desktop, full screen on phones.
            PlayerSettings.WebGL.template = "PROJECT:TapPop";
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            // Gzip with fallback runs on any static host (GitHub Pages, itch.io, Netlify) without server config.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { TapPopSceneBuilder.ScenePath },
                locationPathName = OutputFolder,
                target = BuildTarget.WebGL,
                options = Dev ? BuildOptions.Development : BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"TapPop WebGL build: {summary.result}, {summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime:mm\\:ss} -> {OutputFolder}");

            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
