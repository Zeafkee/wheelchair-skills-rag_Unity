using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class RealtimeCoachTrigger : MonoBehaviour
{
    [System.Serializable]
    public class AskRAGRequest
    {
        public string question;
        public string level;     // opsiyonel ama string olarak yolla
        public string situation; // opsiyonel

        public AskRAGRequest(string q)
        {
            question = q;
            level = "beginner";
            situation = "simulation";
        }
    }

    [TextArea]
    public string questionToAsk =
        "I am in a manual wheelchair and I am facing a curb. How should I safely get up?";

    public string ragEndpoint = "http://localhost:8000/ask";
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
        AskRAGRequest payload = new AskRAGRequest(questionToAsk);

        string json = JsonUtility.ToJson(payload);
        Debug.Log("Sending to RAG:\n" + json);

        byte[] body = Encoding.UTF8.GetBytes(json);

        UnityWebRequest req = new UnityWebRequest(ragEndpoint, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ COACH RESPONSE:\n" + req.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"❌ RAG ERROR {req.responseCode}: {req.downloadHandler.text}");
        }
    }
}
