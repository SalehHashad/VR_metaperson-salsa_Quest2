using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;
using UnityEngine.UI;
using TMPro;

public class TextToSpeech : MonoBehaviour
{
    private string uid; //Unique id of the speech

    AudioSource audioSource;
    public TMP_InputField inputField_AskAI;
    // Start is called before the first frame update
    void Start()
    {


        //startVoive();

        SpeakToClip(" Let's play a game " +
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
            "Touch with your finger the one that looks like this from these ones");
    }

   public void startVoive()
   {
      uid = Speaker.Instance.Speak
            ("Hello Saleh Hashad Did you ever want to make a game for people with visual impairments or reading difficulties? Or " +
            "want your players to not have to read too much? Or would you listen to just the dialogues in your game without consulting" +
            " a voice-actor in early stages of development? Then RT-Voice is your time-saving solution to do so!"
            , null, Speaker.Instance.VoiceForCulture("en"));
   }

    public void SpeakToClip(string message)
    {
        
        audioSource = GetComponent<AudioSource>();
        audioSource.enabled = false;

        // Use SpeakNative to output directly to an AudioSource
        //Speaker.Instance.Speak(message, audioSource, Speaker.Instance.VoiceForCulture("en"), false);
        Speaker.Instance.Speak(message, audioSource, Speaker.Instance.VoiceForGender(Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "en", 1, "en",false),false);
        
        // Now audioSource.clip should contain the generated AudioClip after speaking
        StartCoroutine(WaitForAudioClip(audioSource));
    }

    private System.Collections.IEnumerator WaitForAudioClip(AudioSource audioSource)
    {
        // Wait until the audio source has finished playing
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        // At this point, the AudioClip should be available
        AudioClip generatedClip = audioSource.clip;
        Debug.Log("Speech complete. AudioClip generated.");

        // You can now use the generatedClip as needed
        // For example, save it, process it, etc.
    }

    public void StartSpeek()
    {
        audioSource.enabled = true;
    }
}
