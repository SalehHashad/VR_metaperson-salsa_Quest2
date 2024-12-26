using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TherapistTest : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(TestMemoryAndSessions());
    }

    private IEnumerator RunSession(TherapistClient client, List<string> messages)
    {
        foreach (var message in messages)
        {
            yield return client.SendMessage(message).AsCoroutine();
            yield return new WaitForSeconds(1);
        }

        yield return client.Close().AsCoroutine();
    }

    private IEnumerator TestMemoryAndSessions()
    {
        Debug.Log("\n=== Test Scenario 1: Mohammed's First Session ===");
        var mohammed = new TherapistClient(1, "Mohammed");
        var mohammedFirstSession = new List<string>
        {
            "Hi, I'm Mohammed and I've been feeling anxious about my new job",
            "I'm particularly worried about the presentation next week",
            "I'm afraid I'll mess it up"
        };
        yield return RunSession(mohammed, mohammedFirstSession);

        Debug.Log("\n=== Test Scenario 2: Sarah's Session ===");
        var sarah = new TherapistClient(2, "Sarah");
        var sarahSession = new List<string>
        {
            "Hello, I'm Sarah. I've been dealing with grief",
            "I lost my father last month",
            "It's hard to cope with daily life"
        };
        yield return RunSession(sarah, sarahSession);

        Debug.Log("\n=== Test Scenario 3: Mohammed Returns (Testing Memory) ===");
        var mohammedReturn = new TherapistClient(1, "Mohammed");
        var mohammedReturnSession = new List<string>
        {
            "Hi, it's Mohammed again. Remember I mentioned being anxious about a presentation?",
            "Well, I did the presentation today",
            "I'm feeling relieved but still a bit shaky"
        };
        yield return RunSession(mohammedReturn, mohammedReturnSession);
    }
}
