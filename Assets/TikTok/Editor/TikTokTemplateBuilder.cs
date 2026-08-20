using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class TikTokTemplateBuilder
{
    private const string SdkRoot = "Assets/Plugins/com.tiktok.minigame";
    private const string DefaultTemplate = SdkRoot + "/DefaultTemplate";
    private const string CustomizeTemplate = SdkRoot + "/CustomizeTemplate";

    private const string SdkJsSource = "Assets/TikTok/JS/hs-tiktok-sdk.min.js.txt";
    private const string BootstrapSource = "Assets/TikTok/JS/hs-bootstrap.js.txt";

    private const string MainMarker = "function main() {";

    [MenuItem("Tuanvhh/TikTok/Create - Update CustomizeTemplate (HS SDK)", false, 104)]
    public static void Build()
    {
        Debug.Log("===== TikTok: sinh CustomizeTemplate =====");

        if (!Directory.Exists(DefaultTemplate))
        {
            Debug.LogError($"[TikTok] Không thấy {DefaultTemplate}. TikTok SDK chưa import đúng chỗ?");
            return;
        }

        if (!File.Exists(SdkJsSource))
        {
            Debug.LogError($"[TikTok] Thiếu {SdkJsSource} — đây là bản HS SDK JavaScript cần nhúng.");
            return;
        }

        if (!File.Exists(BootstrapSource))
        {
            Debug.LogError($"[TikTok] Thiếu {BootstrapSource}.");
            return;
        }

        Directory.CreateDirectory(CustomizeTemplate);
        int copied = CopyTemplateFiles();

        if (!GenerateGameJs())
            return;

        if (!VerifyNothingMissing())
            return;

        AssetDatabase.Refresh();

        Debug.Log($"[TikTok] Xong. Đã copy {copied} file template + sinh game.js có nhúng HS SDK.\n" +
                  $"Từ giờ build sẽ dùng {CustomizeTemplate} thay cho DefaultTemplate.\n" +
                  "Sửa SDK hoặc bootstrap thì chạy lại menu này — đừng sửa tay game.js đã sinh.");
    }

    [MenuItem("Tuanvhh/TikTok/Delete CustomizeTemplate (Back to Default)", false, 105)]
    public static void Remove()
    {
        if (!Directory.Exists(CustomizeTemplate))
        {
            Debug.Log("[TikTok] Không có CustomizeTemplate, đang dùng DefaultTemplate rồi.");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "Xoá CustomizeTemplate?",
            "Build sẽ quay về DefaultTemplate, tức gói build KHÔNG còn HS SDK (mất login, IAP, ads).\n\n" +
            "Tạo lại bất cứ lúc nào bằng menu \"5. Tạo-cập nhật CustomizeTemplate\".",
            "Xoá",
            "Huỷ");

        if (!ok)
            return;

        Directory.Delete(CustomizeTemplate, true);

        string meta = CustomizeTemplate + ".meta";

        if (File.Exists(meta))
            File.Delete(meta);

        AssetDatabase.Refresh();
        Debug.Log("[TikTok] Đã xoá CustomizeTemplate.");
    }

    private static int CopyTemplateFiles()
    {
        int copied = 0;
        int prefix = Path.GetFullPath(DefaultTemplate).Length + 1;

        foreach (string src in Directory.GetFiles(DefaultTemplate, "*", SearchOption.AllDirectories))
        {
            // .meta chứa GUID; copy sang là Unity báo trùng GUID. Để Unity tự sinh meta mới.
            if (src.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string relative = Path.GetFullPath(src).Substring(prefix);

            // game.js được sinh riêng (template + SDK + bootstrap), không copy nguyên bản.
            if (relative.Replace('\\', '/').Equals("game.js", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string dst = Path.Combine(CustomizeTemplate, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(src, dst, true);
            copied++;
        }

        return copied;
    }

    private static bool VerifyNothingMissing()
    {
        int prefix = Path.GetFullPath(DefaultTemplate).Length + 1;
        var missing = new System.Collections.Generic.List<string>();

        foreach (string src in Directory.GetFiles(DefaultTemplate, "*", SearchOption.AllDirectories))
        {
            if (src.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string relative = Path.GetFullPath(src).Substring(prefix);

            if (!File.Exists(Path.Combine(CustomizeTemplate, relative)))
                missing.Add(relative);
        }

        if (missing.Count == 0)
            return true;

        Debug.LogError($"[TikTok] CustomizeTemplate còn thiếu {missing.Count} file so với DefaultTemplate: " +
                       string.Join(", ", missing) + "\nGói build sẽ hỏng — đừng build khi còn dòng này.");
        return false;
    }

    private static bool GenerateGameJs()
    {
        string template = File.ReadAllText(Path.Combine(DefaultTemplate, "game.js"));

        if (!template.Contains(MainMarker))
        {
            Debug.LogError($"[TikTok] Không tìm thấy '{MainMarker}' trong DefaultTemplate/game.js. " +
                           "TikTok SDK đổi template rồi — phải sửa lại TikTokTemplateBuilder.");
            return false;
        }

        string sdk = File.ReadAllText(SdkJsSource);
        string bootstrap = File.ReadAllText(BootstrapSource);

        // Bỏ sourceMappingURL: file .map không được ship, để lại chỉ tổ sinh 404 trong tab Network của
        // DevTool đúng lúc đang debug — dễ tưởng nhầm là lỗi tải tài nguyên của game.
        sdk = Regex.Replace(sdk, @"^\s*//#\s*sourceMappingURL=.*$", string.Empty, RegexOptions.Multiline);

        // SDK nạp ở top-level (tương đương require ở đầu file như doc HS hướng dẫn), còn init/login thì
        // nằm trong main() — doc cảnh báo để ngoài main() sẽ crash trên thiết bị thật.
        //
        // Bọc IIFE và che 'module'/'define': bundle là UMD, nếu thấy CommonJS nó sẽ gán module.exports,
        // mà ở đây 'module' chính là module của game.js -> ghi đè mất. Che đi thì UMD rơi vào nhánh
        // cuối là gán globalThis.HSTikTokSDK, đúng thứ ta cần.
        string sdkBlock =
            "\n// ─── BẮT ĐẦU HS TikTok SDK (sinh tự động — sửa ở Assets/TikTok/JS/) ───\n" +
            "(function () {\n" +
            "  var module = undefined;\n" +
            "  var define = undefined;\n" +
            sdk +
            "\n})();\n" +
            "// ─── HẾT HS TikTok SDK ───\n\n";

        string bootstrapBlock =
            "\n  // ─── BẮT ĐẦU bootstrap HS (sinh tự động từ Assets/TikTok/JS/hs-bootstrap.js.txt) ───\n" +
            bootstrap +
            "\n  // ─── HẾT bootstrap HS ───\n";

        int mainIndex = template.IndexOf(MainMarker);
        string generated =
            "// !!! FILE NÀY ĐƯỢC SINH TỰ ĐỘNG bởi Tools/TikTok/4. Mọi sửa tay sẽ mất khi chạy lại menu đó.\n" +
            "// Nguồn: DefaultTemplate/game.js + Assets/TikTok/JS/hs-tiktok-sdk.min.js.txt + hs-bootstrap.js.txt\n" +
            template.Substring(0, mainIndex) +
            sdkBlock +
            MainMarker +
            bootstrapBlock +
            template.Substring(mainIndex + MainMarker.Length);

        File.WriteAllText(Path.Combine(CustomizeTemplate, "game.js"), generated);
        return true;
    }
}
