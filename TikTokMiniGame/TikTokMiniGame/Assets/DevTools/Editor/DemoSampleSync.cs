using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmobearTTMNG.DevTools
{
	/// <summary>
	/// Chỉ dùng trong project dev TTMNGDoc: chép Assets/Demo (bản gốc) sang Samples~/Demo của package để game khác
	/// import qua Package Manager. Chạy trước mỗi lần release (tag ttmng-v*). Giữ nguyên .meta để GUID script / scene
	/// không đổi.
	/// </summary>
	public static class DemoSampleSync
	{
		private const string Source = "Assets/Demo";
		private const string Target = "Packages/com.amobear.ttmng/Samples~/Demo";

		[MenuItem("AmobearTTMNG/Dev/Sync Demo → Samples~", false, 300)]
		public static void Sync()
		{
			string root = Path.GetDirectoryName(Application.dataPath);
			string source = Path.Combine(root, Source);
			string target = Path.GetFullPath(Path.Combine(root, Target));
			if (!Directory.Exists(source))
			{
				Debug.LogError($"[DemoSampleSync] Không có {Source}.");
				return;
			}

			if (Directory.Exists(target))
				Directory.Delete(target, true);
			int count = Copy(source, target);
			Debug.Log($"[DemoSampleSync] Copied {count} files {Source} → {Target}.");
		}

		private static int Copy(string from, string to)
		{
			Directory.CreateDirectory(to);
			int count = 0;
			foreach (string file in Directory.GetFiles(from))
			{
				File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
				count++;
			}
			foreach (string dir in Directory.GetDirectories(from))
				count += Copy(dir, Path.Combine(to, Path.GetFileName(dir)));
			return count;
		}
	}
}
