using UnityEngine;
using Crosstales.RTVoice;

public class ChangeVoiceTuneExample : MonoBehaviour
{
    public AudioSource audioSource;
    public string message = " Let's play a game " +
            "Look at this" +
            "Touch with your finger the one that looks like it" +
            "See.This one is just like this one" +
            "Now, it's your turn" +
            "Touch with your finger the one that looks exactly like this from these ones" +
            "See that red one" +
            "This one looks exactly like this one" +
            "Let's try another one" +
            "Look at " +
            "Touch with your finger the one that looks like it from these ones" +
            "Touch with your finger the one that looks like this from these ones";
    public float pitch = 1.2f;  // Adjust pitch (default is 1.0)
    public float rate = 1.0f;   // Adjust rate (default is 1.0)
    public float volume = 1.0f; // Adjust volume (default is 1.0)

    void Start()
    {
        // Get the list of available voices
        var voices = Speaker.Instance.Voices;

        // Find the first female voice
        Crosstales.RTVoice.Model.Voice femaleVoice = null;

        foreach (var voice in voices)
        {
            // Compare the gender using the enum (or a similar property if available)
            if (voice.Gender == Crosstales.RTVoice.Model.Enum.Gender.FEMALE)
            {
                femaleVoice = voice;
                break;
            }
        }

        // If a female voice is found, use it to speak the message
        if (femaleVoice != null)
        {
            Speaker.Instance.Speak(message, audioSource, femaleVoice, false, rate, pitch, volume);
        }
        else
        {
            Debug.LogWarning("No female voice found!");
        }
    }
}
