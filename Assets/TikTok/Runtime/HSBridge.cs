using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AmobearTTMNG.Ads
{
public struct HSAdResult
{
    public bool Success;

    public bool Completed;

    public int ErrorCode;

    public string ErrorMessage;
}

public class HSBridge : MonoBehaviour
{
    private const string ObjectName = "HSBridge";

    public static HSBridge Instance { get; private set; }

    public static bool IsLoggedIn { get; private set; }

    public static bool LoginResolved { get; private set; }

    private const float AdTimeoutSeconds = 180f;    // user có thể xem hết video dài
    private const float QueryTimeoutSeconds = 30f;  // status

    private const float LoginWarnSeconds = 20f;

    private static int nextRequestId = 1;
    private static float loginStartTime = -1f;
    private static bool loginWarned;

    private static readonly Dictionary<int, PendingRequest> PendingRequests =
        new Dictionary<int, PendingRequest>();

    private struct PendingRequest
    {
        public Action<HSMessage> Callback;
        public string Op;
        public float Deadline;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void HS_BridgeReady();
    [DllImport("__Internal")] private static extern void HS_ShowAdsRemote(int reqId, string label);
    [DllImport("__Internal")] private static extern void HS_RequestStatus(int reqId);
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject(ObjectName);
        Instance = go.AddComponent<HSBridge>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Phòng trường hợp object được đặt sẵn trong scene với tên khác — jslib SendMessage tìm theo TÊN.
        gameObject.name = ObjectName;
        DontDestroyOnLoad(gameObject);
        loginStartTime = Time.realtimeSinceStartup;

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            HS_BridgeReady();
            Debug.Log("[HSBridge] Đã báo JS là C# sẵn sàng, chờ kết quả login...");
        }
        catch (Exception e)
        {
            // EntryPointNotFoundException ở đây = HSBridge.jslib không được nhúng vào build.
            Debug.LogError($"[HSBridge] Gọi HS_BridgeReady thất bại: {e.GetType().Name} — " +
                           "HSBridge.jslib không có trong build? Kiểm tra file nằm ở Assets/TikTok/WebGL/ " +
                           "và Platform của nó có tick WebGL.");
        }
#else
        Debug.Log("[HSBridge] Không phải WebGL build -> bridge chạy chế độ giả lập (ads luôn 'xem hết').");
#endif
    }

    private void Update()
    {
        float now = Time.realtimeSinceStartup;

        WarnIfLoginStuck(now);

        if (PendingRequests.Count == 0)
            return;

        List<int> expired = null;

        foreach (KeyValuePair<int, PendingRequest> entry in PendingRequests)
        {
            if (now < entry.Value.Deadline)
                continue;

            expired = expired ?? new List<int>();
            expired.Add(entry.Key);
        }

        if (expired == null)
            return;

        for (int i = 0; i < expired.Count; i++)
            FailExpired(expired[i]);
    }

    private static void WarnIfLoginStuck(float now)
    {
#if UNITY_EDITOR
        // Trong Editor không có cầu JS nên login không bao giờ resolve — cảnh báo ở đây chỉ là báo động giả,
        // làm rối Console đúng lúc đang truy bug thật.
        return;
#else
        if (loginWarned || LoginResolved || loginStartTime < 0f)
            return;

        if (now - loginStartTime < LoginWarnSeconds)
            return;

        loginWarned = true;
        Debug.LogError(
            $"[HSBridge] Sau {LoginWarnSeconds:F0}s vẫn chưa có kết quả login. Ba nguyên nhân hay gặp:\n" +
            "  1. Domain backend HS chưa được whitelist bên TikTok Developer Portal " +
            "(log JS sẽ có errorCode 21100 'url not in domain list'). Test nhanh: bật 'Disable Domain " +
            "Verification' trong Developer Options của DevTool.\n" +
            "  2. Bootstrap trong game.js không chạy -> xem console JS có dòng '[HSBridge] init ok' không.\n" +
            "  3. Mạng chậm hoặc backend HS không phản hồi.");
#endif
    }

    private static void FailExpired(int id)
    {
        if (!PendingRequests.TryGetValue(id, out PendingRequest request))
            return;

        PendingRequests.Remove(id);

        Debug.LogError($"[HSBridge] Lệnh '{request.Op}' (id={id}) quá hạn — JS không trả lời. " +
                       "Trả kết quả thất bại để game không kẹt. Kiểm tra console JS xem có exception không.");

        HSMessage timeout = new HSMessage
        {
            kind = "result",
            op = request.Op,
            id = id,
            success = false,
            completed = false,
            error_code = -2,
            error_message = $"timeout: JS không trả lời lệnh '{request.Op}'"
        };

        try
        {
            request.Callback(timeout);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HSBridge] Callback timeout ném exception: {e}");
        }
    }

    #region Lệnh gọi sang JS

    public static void ShowAdsRemote(string place, Action<HSAdResult> onResult)
    {
        if (string.IsNullOrEmpty(place))
        {
            Invoke(onResult, new HSAdResult { ErrorCode = -1, ErrorMessage = "place label rỗng" });
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        EnsureInstance();

        int id = Register(msg => Invoke(onResult, new HSAdResult
        {
            Success = msg.success,
            Completed = msg.completed,
            ErrorCode = msg.error_code,
            ErrorMessage = msg.error_message
        }), $"ads:{place}", AdTimeoutSeconds);

        HS_ShowAdsRemote(id, place);
#else
        Debug.Log($"[HSBridge] Editor: giả lập xem hết ad '{place}'.");
        Invoke(onResult, new HSAdResult { Success = true, Completed = true });
#endif
    }

    //Log trạng thái SDK. Doc HS: khi bí thì gửi platform / hostName / version cho họ.
    public static void LogStatus()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        EnsureInstance();
        int id = Register(msg => Debug.Log(
            $"[HSBridge] v{msg.version} platform={msg.platform} host={msg.hostName} " +
            $"testMode={msg.isTestMode} auth={msg.isAuthenticated} openId={msg.openId}"),
            "status", QueryTimeoutSeconds);
        HS_RequestStatus(id);
#else
        Debug.Log("[HSBridge] Editor: không có trạng thái SDK.");
#endif
    }

    #endregion

    #region Nhận message từ JS

    //Điểm vào duy nhất từ phía JS. Tên phải khớp SendMessage trong HSBridge.jslib.
    public void OnHSMessage(string json)
    {
        HSMessage msg;

        try
        {
            msg = JsonUtility.FromJson<HSMessage>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HSBridge] Không parse được message: {e}\n{json}");
            return;
        }

        if (msg == null)
            return;

        switch (msg.kind)
        {
            case "login":
                LoginResolved = true;
                IsLoggedIn = msg.success;

                if (msg.success)
                {
                    Debug.Log("[HSBridge] Login HS (JS) thành công — bảng placements ads đã tải về.");
                    // Log ngay thông tin môi trường: doc HS bảo khi cần hỗ trợ thì gửi họ
                    // platform / hostName / version, có sẵn trong log đỡ phải test lại lần nữa.
                    LogStatus();
                }
                else
                {
                    Debug.LogError(
                        $"[HSBridge] Login HS (JS) THẤT BẠI: {msg.error}\n" +
                        "  -> ads sẽ không chạy (cần bảng placements tải về lúc login JS).\n" +
                        "  Tra theo thông báo:\n" +
                        "  - 'url not in domain list' / 21100  : chưa whitelist domain backend HS.\n" +
                        "  - 'TikTok login returned no code'   : chạy ngoài app TikTok, hoặc account chưa có quyền test.\n" +
                        "  - TT_ENV_MISSING                    : không phải môi trường TikTok.\n" +
                        "  - 'SDK is not configured'           : sai gameId/sdkKey trong hs-bootstrap.js.txt.\n" +
                        "  - 'HSTikTokSDK undefined'           : game.js chưa nhúng SDK (chạy menu Tools/TikTok/5).");
                }
                break;

            case "result":
                ResolveRequest(msg);
                break;

            default:
                Debug.LogWarning($"[HSBridge] Message lạ kind='{msg.kind}'.");
                break;
        }
    }

    private static void ResolveRequest(HSMessage msg)
    {
        if (!PendingRequests.TryGetValue(msg.id, out PendingRequest request))
        {
            // Hai khả năng: JS trả hai lần, hoặc lệnh đã bị timeout hủy trước đó rồi kết quả mới về muộn.
            Debug.LogWarning($"[HSBridge] Kết quả op='{msg.op}' id={msg.id} không có ai chờ " +
                             "(trả hai lần, hoặc về sau khi đã timeout).");
            return;
        }

        PendingRequests.Remove(msg.id);

        try
        {
            request.Callback(msg);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HSBridge] Callback op='{msg.op}' ném exception: {e}");
        }
    }

    #endregion

    #region Nội bộ

    private static void EnsureInstance()
    {
        if (Instance == null)
            Bootstrap();
    }

    private static int Register(Action<HSMessage> callback, string op, float timeoutSeconds)
    {
        int id = nextRequestId++;

        PendingRequests[id] = new PendingRequest
        {
            Callback = callback,
            Op = op,
            Deadline = Time.realtimeSinceStartup + timeoutSeconds
        };

        return id;
    }

    private static void Invoke<T>(Action<T> callback, T arg)
    {
        if (callback == null)
            return;

        try
        {
            callback(arg);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HSBridge] Callback của game ném exception: {e}");
        }
    }
    
#pragma warning disable 0649
    [Serializable]
    private class HSMessage
    {
        public string kind;
        public string op;
        public int id;

        public bool success;
        public bool completed;
        public int error_code;
        public string error_message;
        public string error;

        public string label;

        public bool isAuthenticated;
        public bool isTestMode;
        public string openId;
        public string version;
        public string platform;
        public string hostName;
    }
#pragma warning restore 0649

    #endregion
}
}
