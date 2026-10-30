using System.IO;
using System.Text.RegularExpressions;
using TTSDK.Tool;
using UnityEditor;
using UnityEngine;

namespace AmobearTTMNG.EditorTools
{
	/// <summary>
	/// Nhập App ID của game (TikTok Developer Portal → app → Basic information). Ghi vào StarkBuilderSetting
	/// (TTSDK đưa vào gói khi build) và appid trong project.config.json ở thư mục gốc nếu file đó có.
	/// Mỗi game một App ID riêng — đừng dùng lại của game khác.
	/// </summary>
	public class TikTokAppIdWindow : EditorWindow
	{
		private string appId;

		[MenuItem("AmobearTTMNG/Set App ID...", false, 1)]
		public static void Open()
		{
			var window = GetWindow<TikTokAppIdWindow>(true, "TikTok App ID", true);
			window.minSize = window.maxSize = new Vector2(420, 110);
			window.appId = TikTokEditorPaths.ReadAppId(StarkBuilderSettings.LoadSettings()) ?? string.Empty;
		}

		private void OnGUI()
		{
			EditorGUILayout.HelpBox("App ID lấy trên TikTok Developer Portal (Client Key / App ID của mini game).", MessageType.None);
			appId = EditorGUILayout.TextField("App ID", appId);
			using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(appId)))
			{
				if (GUILayout.Button("Save"))
				{
					Save(appId.Trim());
					Close();
				}
			}
		}

		public static void Save(string value)
		{
			StarkBuilderSettings settings = StarkBuilderSettings.LoadSettings();
			var so = new SerializedObject(settings);
			TikTokProjectSetup.SetString(so, "_appId", value);
			so.ApplyModifiedPropertiesWithoutUndo();
			settings.Save();

			string configJson = TikTokEditorPaths.RootProjectConfigJson;
			if (File.Exists(configJson))
			{
				string json = File.ReadAllText(configJson);
				string updated = Regex.Replace(json, "\"appid\"\\s*:\\s*\"[^\"]*\"", $"\"appid\":\"{value}\"");
				if (updated != json)
					File.WriteAllText(configJson, updated);
			}
			Debug.Log($"[TikTokSetup] App ID = {value}");
		}
	}
}
