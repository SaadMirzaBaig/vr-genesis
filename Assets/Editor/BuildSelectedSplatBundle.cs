using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildSelectedSplatBundle
{
    [MenuItem("Tools/Splats/Build Bundle (Windows) From Selected Splat Asset")]
    public static void BuildWindows()
    {
        BuildForTarget(BuildTarget.StandaloneWindows64, "Windows", "_splat_windows");
    }

    [MenuItem("Tools/Splats/Build Bundle (Android/Quest) From Selected Splat Asset")]
    public static void BuildAndroid()
    {
        BuildForTarget(BuildTarget.Android, "Android", "_splat_android");
    }

    private static void BuildForTarget(BuildTarget target, string folderName, string suffix)
    {
        var selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogError("[SPLAT BUNDLE] No asset selected. Select a GaussianSplatAsset in the Project window.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("[SPLAT BUNDLE] Could not get asset path.");
            return;
        }

        string baseName = Path.GetFileNameWithoutExtension(assetPath).ToLower();
        string bundleName = baseName + suffix;

        string outputPath = $"Assets/AssetBundles/{folderName}";
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        AssetBundleBuild build = new AssetBundleBuild
        {
            assetBundleName = bundleName,
            assetNames = new[] { assetPath }
        };

        BuildPipeline.BuildAssetBundles(
            outputPath,
            new[] { build },
            BuildAssetBundleOptions.None,
            target
        );

        Debug.Log($"[SPLAT BUNDLE] Built {folderName} bundle '{bundleName}' with asset '{assetPath}' into '{outputPath}'");
    }
}
