using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using WheelchairSkills.API;


public class RealtimeCoachTrigger : MonoBehaviour
{
    [TextArea]
    public string questionToAsk = "I am in a manual wheelchair and I am facing a curb. How should I safely get up?";

    [Tooltip("Assign a prefab that contains RealtimeCoachTutorial component")]
    public GameObject tutorialPrefab;

    [Tooltip("User id to pass to the tutorial backend calls")]
    public string userId = "sefa001";

    private bool hasAsked = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasAsked) return;
        if (!other.CompareTag("Player")) return;
        hasAsked = true;
        
        var request = new AskPracticeRequest(questionToAsk);
        StartCoroutine(APIClient.Instance.GetAskPractice(request, OnRagSuccess, OnRagError));
    }

    private void OnRagSuccess(AskPracticeResponse ragResp)
    {
        if (ragResp == null || ragResp.steps == null || ragResp.steps.Count == 0)
        {
            Debug.LogError("[Trigger] No steps in RAG response.");
            return;
        }

        if (tutorialPrefab == null)
        {
            Debug.LogError("[Trigger] tutorialPrefab not assigned.");
            return;
        }

        // instantiate tutorial prefab and configure it
        var go = Instantiate(tutorialPrefab);
        var tutorial = go.GetComponent<RealtimeCoachTutorial>();
        if (tutorial == null)
        {
            Debug.LogError("[Trigger] tutorialPrefab missing RealtimeCoachTutorial component.");
            return;
        }

        // copy config
        tutorial.userId = userId;

        // start the tutorial using rag response
        StartCoroutine(tutorial.StartTutorial(ragResp));
    }

    private void OnRagError(string error)
    {
        Debug.LogError($"[Trigger] RAG Error: {error}");
        hasAsked = false; // Allow retry on error
    }
}