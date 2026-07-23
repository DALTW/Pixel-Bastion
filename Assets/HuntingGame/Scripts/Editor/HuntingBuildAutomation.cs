using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game3.Hunting.Editor
{
    public static class HuntingBuildAutomation
    {
        public static void BuildWindows()
        {
            var output = Path.GetFullPath("Builds/Windows/GAME-3.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? "Builds/Windows");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/HuntingGame.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows 빌드 실패: {report.summary.result}, 오류 {report.summary.totalErrors}");
            }

            Debug.Log($"HUNTING_WINDOWS_BUILD_COMPLETE {output}");
        }
    }
}
