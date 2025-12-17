using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;


public class RealtimeCoachTrigger : MonoBehaviour
{
    [TextArea]
    public string questionToAsk = "I am in a manual wheelchair and I am facing a curb. How should I safely get up?";

    [Tooltip("POST http://host:port/ask/practice")]
    public string ragEndpoint = "http://localhost:8000/ask/practice";

    [Tooltip("Assign a prefab that contains RealtimeCoachTutorial component")]
    public GameObject tutorialPrefab;

    [Tooltip("Backend base URL for training endpoints, e.g. http://localhost:8000")]
    public string backendBaseUrl = "http://localhost:8000";

    [Tooltip("User id to pass to the tutorial backend calls")]
    public string userId = "sefa001";

    private bool hasAsked = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasAsked) return;
        if (!other.CompareTag("Player")) return;
        hasAsked = true;
        StartCoroutine(AskRAG());
    }

    IEnumerator AskRAG()
    {
        var payload = new AskRAGRequest(questionToAsk);
        string json = JsonUtility.ToJson(payload);
        Debug.Log("[Trigger] Sending to RAG:\n" + json);
        byte[] body = Encoding.UTF8.GetBytes(json);

        UnityWebRequest req = new UnityWebRequest(ragEndpoint, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Trigger] RAG ERROR {req.responseCode}: {req.error} - {req.downloadHandler.text}");
            yield break;
        }

        string respText = req.downloadHandler.text;
        Debug.Log("[Trigger] RAW COACH RESPONSE:\n" + respText);

        AskRAGResponse ragResp = null;
        try
        {
            ragResp = JsonUtility.FromJson<AskRAGResponse>(respText);
            if (ragResp == null)
            {
                Debug.LogWarning("[Trigger] JsonUtility returned null. Response may require Newtonsoft.Json for parsing.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Trigger] JSON parse error: " + ex);
        }

        if (ragResp == null || ragResp.steps == null || ragResp.steps.Length == 0)
        {
            Debug.LogError("[Trigger] No steps in RAG response.");
            yield break;
        }

        if (tutorialPrefab == null)
        {
            Debug.LogError("[Trigger] tutorialPrefab not assigned.");
            yield break;
        }

        // instantiate tutorial prefab and configure it
        var go = Instantiate(tutorialPrefab);
        var tutorial = go.GetComponent<RealtimeCoachTutorial>();
        if (tutorial == null)
        {
            Debug.LogError("[Trigger] tutorialPrefab missing RealtimeCoachTutorial component.");
            yield break;
        }

        // copy config
        tutorial.backendBaseUrl = backendBaseUrl;
        tutorial.userId = userId;

        // If you want to automatically set a UI child that exists on the prefab, it can be wired inside the prefab.

        // start the tutorial using rag response
        StartCoroutine(tutorial.StartTutorial(ragResp));
    }
    [Serializable]
    public class AskRAGRequest
    {
        public string question;
        public string level;
        public string situation;
        public AskRAGRequest(string q) { question = q; level = "beginner"; situation = "simulation"; }
    }

    [Serializable]
    public class Step
    {
        public int step_number;
        public string text;
        public string title;
        public string cue;
        public string[] expected_actions;
    }

    [Serializable]
    public class AskRAGResponse
    {
        public string skill_id;
        public Step[] steps;
    }
}