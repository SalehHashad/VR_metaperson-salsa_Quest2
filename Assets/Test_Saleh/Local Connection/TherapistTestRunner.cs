using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TherapistTestRunner : MonoBehaviour
{
    private void Start()
    {
        // Start the test coroutine
        StartCoroutine(TestTherapist());
    }

    private IEnumerator TestTherapist()
    {
        Debug.Log("=== Test Scenario 1: Mohammed's First Session ===");

        // Create a TherapistClient instance for Mohammed
        var mohammed = new TherapistClient(1, "Mohammed");

        // Define Mohammed's session messages
        var mohammedFirstSession = new List<string>
        {
            "Hi, I'm Mohammed and I've been feeling anxious about my new job",
            "I'm particularly worried about the presentation next week",
            "I'm afraid I'll mess it up"
        };

        // Run the session
        yield return RunSession(mohammed, mohammedFirstSession);

        Debug.Log("=== Test Completed ===");
    }

    private IEnumerator RunSession(TherapistClient client, List<string> messages)
    {
        foreach (var message in messages)
        {
            // Send each message and wait for the response
            yield return client.SendMessage(message).AsCoroutine();
            yield return new WaitForSeconds(1); // Pause for 1 second between messages
        }

        // Close the WebSocket connection
        yield return client.Close().AsCoroutine();
    }
}
