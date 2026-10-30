using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AmobearTTMNG.Installer
{
    [InitializeOnLoad]
    public sealed class SdkUpdateWindow : EditorWindow
    {
        private const string Prefix = "[AmobearTTMNG Update] ";
        private static bool checking;
        private static double nextCheck;
        [SerializeField] private string sdkRoot, current, target, branch, changes, error;
        private bool updating;
        private Vector2 scroll;

        static SdkUpdateWindow()
        {
            nextCheck = EditorApplication.timeSinceStartup + 20;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (Application.isBatchMode || checking || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1800;
            Check(false);
        }

        [MenuItem("AmobearTTMNG/Check for SDK Updates", false, 80)]
        private static void CheckFromMenu() => Check(true);

        private static string FindRoot()
        {
            foreach (string guid in AssetDatabase.FindAssets("SdkUpdateWindow t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadAssetAtPath<MonoScript>(path)?.GetClass() == typeof(SdkUpdateWindow))
                    return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path), "../.."));
            }
            return null;
        }

        private static async void Check(bool manual)
        {
            if (checking) return;
            checking = true;
            try
            {
                string root = FindRoot();
                // Only the SDK's own Git checkout: never update the surrounding game/dev repository.
                if (root == null || (!File.Exists(Path.Combine(root, ".git")) && !Directory.Exists(Path.Combine(root, ".git"))))
                {
                    if (manual) EditorUtility.DisplayDialog("AmobearTTMNG", "Thư mục SDK này chưa phải Git submodule/repository riêng. Hãy cài bản SDK vào Assets/AmobearTTMNG bằng git submodule add.", "OK");
                    return;
                }
                var info = await Task.Run(() => Inspect(root));
                if (info.Current == info.Target)
                {
                    if (manual) EditorUtility.DisplayDialog("AmobearTTMNG", "SDK đang ở phiên bản mới nhất của " + info.Branch, "OK");
                    return;
                }
                string key = "AmobearTTMNG.Update.Notified." + root;
                if (!manual && SessionState.GetString(key, "") == info.Target) return;
                SessionState.SetString(key, info.Target);
                Debug.Log(Prefix + "Có bản cập nhật trên " + info.Branch + "\n" + info.Changes);
                var window = GetWindow<SdkUpdateWindow>(true, "AmobearTTMNG — SDK Update", true);
                window.sdkRoot = root;
                window.current = info.Current;
                window.target = info.Target;
                window.branch = info.Branch;
                window.changes = info.Changes;
                window.error = null;
                window.minSize = new Vector2(540, 360);
                window.Show();
            }
            catch (Exception e)
            {
                // Network/auth failures must not interrupt opening a project.
                if (manual) EditorUtility.DisplayDialog("Không kiểm tra được SDK", e.Message, "OK");
                else Debug.LogWarning(Prefix + e.Message);
            }
            finally { checking = false; }
        }

        internal sealed class UpdateInfo
        {
            public string Current, Target, Branch, Changes;
        }

        internal static UpdateInfo Inspect(string root)
        {
            string actual = Git(root, "rev-parse --show-toplevel").Trim();
            if (!SamePath(actual, root)) throw new Exception("SDK root không khớp Git root; không cập nhật repo game.");
            string branch = TrackingBranch(root);
            Git(root, "check-ref-format --branch " + Quote(branch));
            // Fetch one branch, without changing working files or Git configuration.
            Git(root, "fetch --no-tags origin " + Quote("refs/heads/" + branch));
            string current = Git(root, "rev-parse HEAD").Trim();
            string target = Git(root, "rev-parse FETCH_HEAD").Trim();
            string changes = current == target ? "" : Git(root, "log --max-count=100 --date=short --format=" + Quote("%h | %ad | %s%n%b") + " " + current + ".." + target);
            return new UpdateInfo { Current = current, Target = target, Branch = branch,
                Changes = string.IsNullOrWhiteSpace(changes) ? "Không có commit mới phía remote; lịch sử local khác remote. Hãy kiểm tra Git thủ công." : changes };
        }

        private static string TrackingBranch(string root)
        {
            string parent = Git(root, "rev-parse --show-superproject-working-tree").Trim();
            if (!string.IsNullOrEmpty(parent))
            {
                string paths = Git(parent, "config -f .gitmodules --get-regexp " + Quote("^submodule\\..*\\.path$"), true);
                foreach (string line in paths.Split('\n'))
                {
                    int split = line.IndexOf(' ');
                    if (split < 0 || !SamePath(Path.Combine(parent, line.Substring(split + 1).Trim()), root)) continue;
                    string key = line.Substring(0, split);
                    key = key.Substring(0, key.Length - 4) + "branch";
                    string configured = Git(parent, "config --get " + Quote(key), true).Trim();
                    if (configured.Length == 0) configured = Git(parent, "config -f .gitmodules --get " + Quote(key), true).Trim();
                    if (configured == ".") return Git(parent, "symbolic-ref --short HEAD").Trim();
                    if (configured.Length > 0) return configured;
                }
            }
            string remote = Git(root, "ls-remote --symref origin HEAD");
            const string prefix = "ref: refs/heads/";
            string head = remote.Split('\n').FirstOrDefault(line => line.StartsWith(prefix, StringComparison.Ordinal));
            if (head == null) throw new Exception("Không xác định được nhánh mặc định của origin.");
            return head.Substring(prefix.Length).Split('\t')[0].Trim();
        }

        internal static void UpdateCheckout(string root, string expectedHead, string target)
        {
            if (!SamePath(Git(root, "rev-parse --show-toplevel").Trim(), root)) throw new Exception("Sai SDK Git root.");
            if (Git(root, "rev-parse HEAD").Trim() != expectedHead) throw new Exception("SDK đã đổi commit từ lần kiểm tra. Hãy Check for SDK Updates lại.");
            if (!string.IsNullOrWhiteSpace(Git(root, "status --porcelain"))) throw new Exception("SDK có thay đổi local. Hãy commit/stash chúng trước khi Update.");
            try { Git(root, "merge-base --is-ancestor " + expectedHead + " " + target); }
            catch (Exception) { throw new Exception("Lịch sử SDK local khác remote hoặc clone thiếu lịch sử. Không tự ghi đè; hãy kiểm tra/đồng bộ Git thủ công."); }
            Git(root, "merge --ff-only " + target);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Có phiên bản SDK mới", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Nhánh: " + branch);
            EditorGUILayout.LabelField(Short(current) + " → " + Short(target));
            EditorGUILayout.HelpBox("Nội dung dưới đây lấy từ commit message (tối đa 100 commit). Sau khi cập nhật và kiểm tra game, commit con trỏ Assets/AmobearTTMNG trong repo game để chia sẻ phiên bản với nhóm.", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(changes ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            using (new EditorGUI.DisabledScope(updating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                if (GUILayout.Button(updating ? "Đang cập nhật…" : "Update SDK")) UpdateNow();
                if (GUILayout.Button("Để sau")) Close();
            }
        }

        private async void UpdateNow()
        {
            updating = true;
            error = null;
            // Prevent a domain reload in the middle of Git replacing scripts.
            EditorApplication.LockReloadAssemblies();
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                await Task.Run(() => UpdateCheckout(sdkRoot, current, target));
                Debug.Log(Prefix + "Đã cập nhật " + Short(target) + ". Chạy Validate Project, test game và commit con trỏ submodule vào repo game.");
                Close();
            }
            catch (Exception e) { error = e.Message; Debug.LogWarning(Prefix + error); }
            finally
            {
                updating = false;
                AssetDatabase.AllowAutoRefresh();
                EditorApplication.UnlockReloadAssemblies();
                AssetDatabase.Refresh();
            }
        }

        private static string Short(string value) => string.IsNullOrEmpty(value) ? "?" : value.Substring(0, Math.Min(8, value.Length));
        private static bool SamePath(string a, string b) => string.Equals(Path.GetFullPath(a).TrimEnd('/', '\\'), Path.GetFullPath(b).TrimEnd('/', '\\'), Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

        private static string Git(string directory, string arguments, bool allowMissing = false)
        {
            var start = new ProcessStartInfo("git", arguments) { WorkingDirectory = directory, UseShellExecute = false,
                CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
            start.EnvironmentVariables["GCM_INTERACTIVE"] = "Never";
            using (var process = Process.Start(start))
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var errors = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(45000)) { process.Kill(); throw new Exception("Git timeout. Kiểm tra mạng/VPN hoặc quyền truy cập repo SDK."); }
                string text = output.GetAwaiter().GetResult();
                string stderr = errors.GetAwaiter().GetResult();
                if (process.ExitCode != 0 && !(allowMissing && process.ExitCode == 1))
                    throw new Exception("Git " + arguments + " failed:\n" + stderr);
                return text;
            }
        }
    }
}
