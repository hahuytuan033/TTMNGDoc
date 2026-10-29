using System.IO;
using System.Linq;
using AmobearTTMNG.Ads;
using TTSDK.Tool;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmobearTTMNG.EditorTools
{
	/// <summary>
	/// Menu AmobearTTMNG/Setup Project: đưa project về cấu hình WebGL + StarkBuilderSetting đã chạy thật trên TikTok
	/// (lấy từ pocket-2). Chạy lại bao nhiêu lần cũng được. Không đụng color space / render pipeline (tùy game).
	///
	///   1. Kiểm phiên bản TTSDK.
	///   2. WebGL + IL2CPP + PlayerSettings.WebGL.
	///   3. StarkBuilderSetting: thư mục output, WebGL2, bulk memory.
	///   4. TikTokAdsConfig trong Resources CỦA GAME.
	///   5. Scene 0 có TikTokBootstrap (InitSDK phải chạy ở đó).
	/// App ID: menu AmobearTTMNG/Set App ID (Setup tự mở nếu còn trống).
	/// </summary>
	public static class TikTokProjectSetup
	{
		private const string LogPrefix = "[TikTokSetup]";
		private const int WasmMemoryMB = 128;
		private static readonly string AdsConfigPath = $"{TikTokEditorPaths.ConfigFolder}/Resources/{TikTokAdsConfig.ResourcePath}.asset";
		private static readonly string SetupScenePath = $"{TikTokEditorPaths.ConfigFolder}/TikTokSetup.unity";

		[MenuItem("AmobearTTMNG/Setup Project", false, 0)]
		public static void Apply()
		{
			string sdkVersion = TikTokEditorPaths.ReadSdkVersion();
			if (sdkVersion != TikTokEditorPaths.ExpectedSdkVersion
				&& (Application.isBatchMode || !EditorUtility.DisplayDialog("TikTok Setup",
					$"TTSDK đang là {sdkVersion ?? "không rõ"}, package này đã kiểm chứng với {TikTokEditorPaths.ExpectedSdkVersion}.\n\nVẫn tiếp tục?",
					"Tiếp tục", "Huỷ")))
			{
				// Batch mode: sai bản TTSDK thì dừng, không đoán thay người dùng.
				Debug.LogError($"{LogPrefix} TTSDK {sdkVersion ?? "missing"} != {TikTokEditorPaths.ExpectedSdkVersion}, setup aborted.");
				return;
			}

			ApplyPlayerSettings();
			ApplyBuilderSetting();
			EnsureAdsConfig();
			EnsureBootstrapScene();
			AssetDatabase.SaveAssets();

			if (!Application.isBatchMode && string.IsNullOrEmpty(TikTokEditorPaths.ReadAppId(StarkBuilderSettings.LoadSettings())))
				TikTokAppIdWindow.Open();
			Debug.Log($"{LogPrefix} Done. Next: AmobearTTMNG/Validate Project, then AmobearTTMNG/Build and Validate.");
		}

		private static void ApplyPlayerSettings()
		{
			if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
				EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
			// Container WebGL của TikTok chỉ chạy IL2CPP.
			PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);

			PlayerSettings.WebGL.memorySize = WasmMemoryMB;
			PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
			PlayerSettings.WebGL.dataCaching = false;
			PlayerSettings.WebGL.emscriptenArgs = $" -s TOTAL_MEMORY={WasmMemoryMB}MB -mbulk-memory";
			PlayerSettings.SetAdditionalIl2CppArgs("--compiler-flags=\"-mbulk-memory\"");
			Debug.Log($"{LogPrefix} PlayerSettings.WebGL: IL2CPP, memory={WasmMemoryMB}MB, compression=Disabled, dataCaching=off, bulk-memory on.");
		}

		private static void ApplyBuilderSetting()
		{
			// LoadSettings tự tạo Assets/Editor/StarkBuilderSetting.asset nếu chưa có — ttsdk_tools.dll chỉ đọc đúng đường dẫn đó.
			StarkBuilderSettings setting = StarkBuilderSettings.LoadSettings();
			if (setting == null)
			{
				Debug.LogWarning($"{LogPrefix} Không tạo được StarkBuilderSetting. Mở TikTokGame/Build Minigame một lần rồi chạy lại.");
				return;
			}

			string outputDir = TikTokEditorPaths.DefaultOutputDir;
			var so = new SerializedObject(setting);
			SetString(so, "OutputDir", outputDir);
			SetString(so, "_webglPackagePath", Path.Combine(outputDir, "tt-minigame"));
			SetBool(so, "isWebGL2", true);
			SetBool(so, "enableBulkMemory", true);
			so.ApplyModifiedPropertiesWithoutUndo();
			setting.Save();

			string appId = TikTokEditorPaths.ReadAppId(setting);
			Debug.Log($"{LogPrefix} StarkBuilderSetting: OutputDir={outputDir}, WebGL2=on, bulkMemory=on, appId={(string.IsNullOrEmpty(appId) ? "<CHƯA ĐIỀN>" : appId)}");
		}

		private static void EnsureAdsConfig()
		{
			string existing = AssetDatabase.FindAssets($"t:{nameof(TikTokAdsConfig)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.FirstOrDefault(IsLoadableAdsConfig);
			if (existing != null)
			{
				Debug.Log($"{LogPrefix} TikTokAdsConfig: {existing}");
				return;
			}

			TikTokEditorPaths.EnsureFolder(Path.GetDirectoryName(AdsConfigPath)?.Replace('\\', '/'));
			AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TikTokAdsConfig>(), AdsConfigPath);
			Debug.Log($"{LogPrefix} Created {AdsConfigPath} — điền Placement ID lấy từ TikTok Developer Portal.");
		}

		// Resources.Load("TikTokAdsConfig") chỉ thấy asset đúng tên nằm trực tiếp trong một thư mục Resources.
		internal static bool IsLoadableAdsConfig(string assetPath) =>
			Path.GetFileNameWithoutExtension(assetPath) == TikTokAdsConfig.ResourcePath
			&& Path.GetFileName(Path.GetDirectoryName(assetPath)) == "Resources";

		private static void EnsureBootstrapScene()
		{
			string scene0 = TikTokEditorPaths.FirstBuildScene();
			if (TikTokEditorPaths.SceneHasScript(scene0, typeof(TikTokBootstrap)))
			{
				Debug.Log($"{LogPrefix} Scene 0 ({scene0}) has TikTokBootstrap.");
				return;
			}

			// Batch mode (CI): không có ai bấm dialog — tạo luôn.
			if (!Application.isBatchMode && !EditorUtility.DisplayDialog("TikTok Setup",
					$"Scene đầu tiên trong Build Settings ({scene0 ?? "không có"}) chưa có TikTokBootstrap — InitSDK phải chạy ở scene 0.\n\n" +
					$"Tạo {SetupScenePath} có TikTokBootstrap, đặt làm scene 0, rồi chuyển tiếp sang scene game hiện tại?",
					"Tạo", "Bỏ qua"))
				return;
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				return;

			TikTokEditorPaths.EnsureFolder(TikTokEditorPaths.ConfigFolder);
			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			var camera = new GameObject("Main Camera").AddComponent<Camera>();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = Color.black;

			var bootstrap = new GameObject(nameof(TikTokBootstrap)).AddComponent<TikTokBootstrap>();
			var so = new SerializedObject(bootstrap);
			// InitSDK xong thì vào scene game đang đứng đầu Build Settings.
			so.FindProperty("nextScene").stringValue = scene0 == null ? string.Empty : Path.GetFileNameWithoutExtension(scene0);
			so.ApplyModifiedPropertiesWithoutUndo();

			EditorSceneManager.SaveScene(scene, SetupScenePath);
			var scenes = EditorBuildSettings.scenes.Where(s => s.path != SetupScenePath).ToList();
			scenes.Insert(0, new EditorBuildSettingsScene(SetupScenePath, true));
			EditorBuildSettings.scenes = scenes.ToArray();
			Debug.Log($"{LogPrefix} Created {SetupScenePath} as scene 0 (nextScene={so.FindProperty("nextScene").stringValue}).");
		}

		internal static void SetString(SerializedObject so, string name, string value)
		{
			var prop = so.FindProperty(name);
			if (prop != null)
				prop.stringValue = value;
			else
				Debug.LogWarning($"{LogPrefix} StarkBuilderSetting không có field {name} (SDK đổi version?).");
		}

		private static void SetBool(SerializedObject so, string name, bool value)
		{
			var prop = so.FindProperty(name);
			if (prop == null)
			{
				Debug.LogWarning($"{LogPrefix} StarkBuilderSetting không có field {name} (SDK đổi version?).");
				return;
			}
			// Một số version lưu dạng int 0/1 thay vì bool.
			if (prop.propertyType == SerializedPropertyType.Boolean)
				prop.boolValue = value;
			else
				prop.intValue = value ? 1 : 0;
		}
	}
}
