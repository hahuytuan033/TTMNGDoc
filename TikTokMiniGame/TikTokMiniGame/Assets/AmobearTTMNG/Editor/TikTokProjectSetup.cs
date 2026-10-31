using System.IO;
using System.Linq;
using AmobearTTMNG.Ads;
using TTSDK.Tool;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

			if (!EnsureBootstrapScene())
				return;
			bool bulkMemory = ApplyBuilderSetting();
			ApplyPlayerSettings(bulkMemory);
			EnsureAdsConfig();
			AssetDatabase.SaveAssets();

			if (!Application.isBatchMode && string.IsNullOrEmpty(TikTokEditorPaths.ReadAppId(StarkBuilderSettings.LoadSettings())))
				TikTokAppIdWindow.Open();
			Debug.Log($"{LogPrefix} Done. Next: AmobearTTMNG/Validate Project, then AmobearTTMNG/Build and Validate.");
		}

		private static void ApplyPlayerSettings(bool bulkMemory)
		{
			if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
				EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
			// Container WebGL của TikTok chỉ chạy IL2CPP.
			PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);

			PlayerSettings.WebGL.memorySize = WasmMemoryMB;
			PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
			PlayerSettings.WebGL.dataCaching = false;
			PlayerSettings.WebGL.emscriptenArgs = $" -s TOTAL_MEMORY={WasmMemoryMB}MB" + (bulkMemory ? " " + BulkMemoryFlag : string.Empty);
			// Build của TTSDK tự thêm cờ khi enableBulkMemory bật nhưng lúc tắt chỉ gỡ cờ IL2CPP, không gỡ emscriptenArgs
			// (đã kiểm ttsdk_tools.dll 1.1.5) — nên Setup phải tự đồng bộ cả hai. Giữ các tham số IL2CPP khác của game.
			string il2cppArgs = (PlayerSettings.GetAdditionalIl2CppArgs() ?? string.Empty).Replace(BulkMemoryIl2CppArg, string.Empty).Trim();
			if (bulkMemory)
				il2cppArgs = (il2cppArgs + " " + BulkMemoryIl2CppArg).Trim();
			PlayerSettings.SetAdditionalIl2CppArgs(il2cppArgs);
			Debug.Log($"{LogPrefix} PlayerSettings.WebGL: IL2CPP, memory={WasmMemoryMB}MB, compression=Disabled, dataCaching=off, bulk-memory {(bulkMemory ? "on" : "off")}.");
		}

		internal const string BulkMemoryFlag = "-mbulk-memory";
		internal const string BulkMemoryIl2CppArg = "--compiler-flags=\"-mbulk-memory\"";

		/// <summary>Trả về trạng thái bulk memory để PlayerSettings đồng bộ theo.</summary>
		private static bool ApplyBuilderSetting()
		{
			// LoadSettings tự tạo Assets/Editor/StarkBuilderSetting.asset nếu chưa có — ttsdk_tools.dll chỉ đọc đúng đường dẫn đó.
			bool created = !File.Exists(TikTokEditorPaths.BuilderSettingAsset);
			StarkBuilderSettings setting = StarkBuilderSettings.LoadSettings();
			if (setting == null)
			{
				Debug.LogWarning($"{LogPrefix} Không tạo được StarkBuilderSetting. Mở TikTokGame/Build Minigame một lần rồi chạy lại.");
				return true;
			}

			string outputDir = TikTokEditorPaths.DefaultOutputDir;
			var so = new SerializedObject(setting);
			SetString(so, "OutputDir", outputDir);
			SetString(so, "_webglPackagePath", Path.Combine(outputDir, "tt-minigame"));
			SetBool(so, "isWebGL2", true);
			// Mặc định bật (đã chạy thật trên pocket-2). Asset có sẵn thì giữ lựa chọn của dev ở ô "Bulk Memory(beta)"
			// của TTSDK — game cần tách Wasm phải tắt vì bản bulk memory có thể chưa hỗ trợ subpackage.
			if (created)
				SetBool(so, "enableBulkMemory", true);
			so.ApplyModifiedPropertiesWithoutUndo();
			setting.Save();

			string appId = TikTokEditorPaths.ReadAppId(setting);
			Debug.Log($"{LogPrefix} StarkBuilderSetting: OutputDir={outputDir}, WebGL2=on, bulkMemory={(setting.enableBulkMemory ? "on" : "off")}, appId={(string.IsNullOrEmpty(appId) ? "<CHƯA ĐIỀN>" : appId)}");
			return setting.enableBulkMemory;
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

		private static bool EnsureBootstrapScene()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				Debug.LogError($"{LogPrefix} Stop Play mode before setting up the project.");
				return false;
			}
			string scene0 = TikTokEditorPaths.FirstBuildScene();
			if (string.IsNullOrEmpty(scene0) || !File.Exists(scene0))
			{
				Debug.LogError($"{LogPrefix} Add and enable the game's startup scene in Build Settings first.");
				return false;
			}

			var scene = SceneManager.GetSceneByPath(scene0);
			bool openedHere = !scene.IsValid() || !scene.isLoaded;
			if (!openedHere && scene.isDirty)
			{
				// Do not silently save the user's unrelated scene edits.
				if (Application.isBatchMode || !EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene }))
					return false;
				if (scene.isDirty) return false;
			}
			if (openedHere)
				scene = EditorSceneManager.OpenScene(scene0, OpenSceneMode.Additive);
			try
			{
				var bootstraps = scene.GetRootGameObjects()
					.SelectMany(root => root.GetComponentsInChildren<TikTokBootstrap>(true)).ToArray();
				if (bootstraps.Length > 1)
				{
					Debug.LogError($"{LogPrefix} {scene0} has multiple TikTokBootstrap components; keep only one.");
					return false;
				}
				var bootstrap = bootstraps.FirstOrDefault();
				if (bootstrap == null)
				{
					var go = new GameObject(nameof(TikTokBootstrap));
					SceneManager.MoveGameObjectToScene(go, scene);
					bootstrap = go.AddComponent<TikTokBootstrap>();
				}
				else if (bootstrap.transform.parent != null)
				{
					// DontDestroyOnLoad in Awake requires a root object.
					Debug.LogError($"{LogPrefix} Move TikTokBootstrap to the root of {scene0}, then run Setup again.");
					return false;
				}
				bootstrap.gameObject.SetActive(true);
				bootstrap.enabled = true;
				var so = new SerializedObject(bootstrap);
				so.FindProperty("nextScene").stringValue = string.Empty;
				so.ApplyModifiedPropertiesWithoutUndo();
				EditorSceneManager.MarkSceneDirty(scene);
				if (!EditorSceneManager.SaveScene(scene))
				{
					Debug.LogError($"{LogPrefix} Could not save {scene0}.");
					return false;
				}
				Debug.Log($"{LogPrefix} TikTokBootstrap ready in {scene0}; stays in the game scene after SDK initialization.");
				return true;
			}
			finally
			{
				if (openedHere)
					EditorSceneManager.CloseScene(scene, true);
			}
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
