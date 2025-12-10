using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class TelemetryAPI
{
    // change to your backend base if different
    public static string BaseUrl = "http://localhost:8000";

    public static IEnumerator PostJsonCoroutine(string url, string json, Action<string> onSuccess = null, Action<string> onError = null)
    {
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !req.isNetworkError && !req.isHttpError;
#endif
            if (ok)
            {
                onSuccess?.Invoke(req.downloadHandler.text);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }

    // wrapper to send step summary, returns IEnumerator to be started by caller
    public static IEnumerator SendStepSummary(string attemptId, StepTelemetry data, Action<bool> onComplete = null, Action<string> onError = null)
    {
        string url = $"{BaseUrl}/attempt/{attemptId}/record-step";
        string json = JsonUtility.ToJson(data);
        yield return PostJsonCoroutine(url, json, (resp) => onComplete?.Invoke(true), (err) => { onError?.Invoke(err); onComplete?.Invoke(false); });
    }

    public static IEnumerator CompleteAttemptCoroutine(string attemptId, bool success, Action<bool> onComplete = null, Action<string> onError = null)
    {
        string url = $"{BaseUrl}/attempt/{attemptId}/complete";
        var obj = new { success = success };
        string json = JsonUtility.ToJson(obj);
        yield return PostJsonCoroutine(url, json, (r) => onComplete?.Invoke(true), (e) => { onError?.Invoke(e); onComplete?.Invoke(false); });
    }
}