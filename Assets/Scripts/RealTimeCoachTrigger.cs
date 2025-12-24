using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using WheelchairSkills.API;

public class RealtimeCoachTrigger : MonoBehaviour
{
    [TextArea]
    public string questionToAsk = "I am in a manual wheelchair and I am facing a sidewalk. How should I get on the sidewalk?";

    [Tooltip("Sahnede hazır duran RealtimeCoachTutorial referansı")]
    public RealtimeCoachTutorial tutorialInstance;

    [Tooltip("User id to pass to the tutorial backend calls")]
    public string userId = "sefa001";

    private bool hasAsked = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasAsked) return;
        if (!other.CompareTag("Player")) return;
        
        if (tutorialInstance == null)
        {
            Debug.LogError("[Trigger] tutorialInstance sahnede atanmamış!");
            return;
        }

        hasAsked = true;
        var request = new AskPracticeRequest(questionToAsk);
        StartCoroutine(APIClient.Instance.GetAskPractice(request, OnRagSuccess, OnRagError));
    }

    private void OnRagSuccess(AskPracticeResponse ragResp)
    {
        if (ragResp == null || ragResp.steps == null || ragResp.steps.Count == 0)
        {
            Debug.LogError("[Trigger] RAG yanıtında adım bulunamadı.");
            hasAsked = false;
            return;
        }

        Debug.Log("[Trigger] RAG başarılı, tutorial başlıyor. Adım sayısı: " + ragResp.steps.Count);
        
        tutorialInstance.userId = userId;
        StartCoroutine(tutorialInstance.StartTutorial(ragResp));
    }

    private void OnRagError(string error)
    {
        Debug.LogError($"[Trigger] RAG Hatası: {error}");
        hasAsked = false; 
    }
}