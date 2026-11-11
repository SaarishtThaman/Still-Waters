using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SW.EditorTools
{
    /// <summary>Import settings for the jam's pixel art (Resources/Art) and the app icon.</summary>
    public class ArtImport : AssetPostprocessor
    {
        public override uint GetVersion() => 3;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteGenerateFallbackPhysicsShape = false;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(s);
        }

        void OnPreprocessAudio()
        {
            if (assetPath.StartsWith("Assets/Resources/Radio/"))
            {
                // radio clips: mono, low-ish quality is fine (they're band-limited anyway); songs stream
                var ri = (AudioImporter)assetImporter;
                var rs = ri.defaultSampleSettings;
                bool song = System.IO.Path.GetFileName(assetPath).StartsWith("m_");
                rs.loadType = song ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
                rs.compressionFormat = AudioCompressionFormat.Vorbis;
                rs.quality = 0.35f;
                rs.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                rs.sampleRateOverride = 22050;
                ri.defaultSampleSettings = rs;
                ri.forceToMono = true;
                ri.loadInBackground = song;
                return;
            }
            if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            var st = ai.defaultSampleSettings;
            bool longClip = assetPath.Contains("bgm") || assetPath.Contains("lake_sound") || assetPath.Contains("night_time")
                || assetPath.Contains("seagull") || assetPath.Contains("walk") || assetPath.Contains("motorboat");
            st.loadType = longClip ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            st.compressionFormat = AudioCompressionFormat.Vorbis;
            st.quality = longClip ? 0.55f : 0.7f;
            ai.defaultSampleSettings = st;
            ai.forceToMono = !assetPath.Contains("bgm");
        }
    }

    public static class Setup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        const string IconPath = "Assets/Branding/app_icon.png";

        [MenuItem("Still Waters/Setup Project")]
        public static void Run()
        {
            // one empty scene: the game builds itself at runtime (Game.Boot)
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (go.GetComponent<Camera>() == null) Object.DestroyImmediate(go);
            var cam = Camera.main;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "WillForge Games";
            PlayerSettings.productName = "Still Waters";
            PlayerSettings.bundleVersion = "2.0.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.WillForgeGames.StillWaters");
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SplashScreen.show = false;
            // the whole game is built from code, so engine stripping can't see what's used
            PlayerSettings.stripEngineCode = false;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            // the water shader is found by name at runtime
            var shader = Shader.Find("SW/Water");
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            bool has = false;
            for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) has = true;
            if (!has && shader != null)
            {
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
                so.ApplyModifiedProperties();
            }

            if (File.Exists(IconPath))
            {
                var ii = (TextureImporter)AssetImporter.GetAtPath(IconPath);
                ii.textureType = TextureImporterType.Default;
                ii.npotScale = TextureImporterNPOTScale.None;
                ii.filterMode = FilterMode.Point;
                ii.textureCompression = TextureImporterCompression.Uncompressed;
                ii.SaveAndReimport();
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[SW] Setup done");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }

    public static class Builds
    {
        [MenuItem("Still Waters/Build Web")]
        public static void Web() => Build(BuildTarget.WebGL, "Builds/Web", BuildOptions.None);

        [MenuItem("Still Waters/Build Mac")]
        public static void Mac() => Build(BuildTarget.StandaloneOSX, "Builds/Mac/Still Waters.app", BuildOptions.None);

        [MenuItem("Still Waters/Build Android (dev APK)")]
        public static void AndroidDev()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            var t = UnityEditor.Build.NamedBuildTarget.Android;
            var id = PlayerSettings.GetApplicationIdentifier(t);
            PlayerSettings.SetApplicationIdentifier(t, id.EndsWith(".dev") ? id : id + ".dev");
            try { Build(BuildTarget.Android, "Builds/Android/StillWaters-dev.apk", BuildOptions.Development); }
            finally { PlayerSettings.SetApplicationIdentifier(t, id.Replace(".dev", "")); }
        }

        /// <summary>A normal (non-development) APK to send to friends. Signed with Unity's debug key, so
        /// it installs by sideloading but isn't for the Play Store.</summary>
        [MenuItem("Still Waters/Build Android (share APK)")]
        public static void AndroidShare()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.WillForgeGames.StillWaters");
            Build(BuildTarget.Android, "Builds/Android/StillWaters.apk", BuildOptions.None);
        }

        static void Build(BuildTarget target, string path, BuildOptions opts)
        {
            PlayerSettings.stripEngineCode = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = target,
                options = opts,
            });
            Debug.Log($"[SW] Build {target}: {report.summary.result}, {report.summary.totalSize / (1024 * 1024f):F1} MB, {report.summary.totalErrors} errors");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
