using Crosstales.RTVoice;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;


namespace CrazyMinnow.SALSA.OneClicks
{
    public class OneClickAvatarSdkEditor : Editor
    {
        private delegate void SalsaOneClickChoice(GameObject gameObject);
        private static SalsaOneClickChoice _salsaOneClickSetup = OneClickAvatarSdk.Setup;

        private delegate void EyesOneClickChoice(GameObject gameObject);
        private static EyesOneClickChoice _eyesOneClickSetup = OneClickAvatarSdkEyes.Setup;

        [MenuItem("GameObject/Crazy Minnow Studio/SALSA LipSync/One-Clicks/Avatar SDK")]
        public static void OneClickSetup_AvatarSDK()
        {
            _salsaOneClickSetup = OneClickAvatarSdk.Setup;
            _eyesOneClickSetup = OneClickAvatarSdkEyes.Setup;

            OneClickSetup();
        }

        public static void OneClickSetup()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning(
                    "NO OBJECT SELECTED: You must select an object in the scene to apply the OneClick to.");
                return;
            }

            ApplyOneClick(go);
        }

        private static async void ApplyOneClick(GameObject go)
        {
            _salsaOneClickSetup(go);
            _eyesOneClickSetup(go);

            // Add QueueProcessor
            OneClickBase.AddQueueProcessor(go);

            // Generate and configure AudioSource using RT-Voice
        /*    string textToSpeak = await GetTextToSpeakAsync(); // «·Õ’Ê· ⁄·Ï «·‰’
            string filePath = Path.Combine(Application.persistentDataPath, "output.wav");

            GenerateSpeechAndConfigureSalsa(go, textToSpeak, filePath);*/
        }

       /* private static async Task<string> GetTextToSpeakAsync()
        {
            // «·‰’Ê’ „‰ „’«œ— „ ⁄œœ…
            string textToSpeak = "Default text";

            // „‰ „·› Excel
            string excelFilePath = "path/to/your/excel/file.xlsx";
            textToSpeak = ReadTextFromExcel(excelFilePath, 0, 1, 1);

            // „‰ WebSocket
            var webSocketHandler = new WebSocketHandler("ws://websocket-url");
            textToSpeak = webSocketHandler.ReceivedText;
            webSocketHandler.Close();

            // „‰ ChatGPT
            string prompt = "Say something interesting.";
            string apiKey = "openai_api_key";
            textToSpeak = await GetTextFromChatGPT(prompt, apiKey);

            return textToSpeak;
        }*/

        private static void GenerateSpeechAndConfigureSalsa(GameObject go, string text, string filePath)
        {
            var speaker = Speaker.Instance;
            var audioSource = go.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = go.AddComponent<AudioSource>();
            }

            // «” Œœ„ RT-Voice · ÕÊÌ· «·‰’ ≈·Ï ﬂ·«„
            speaker.Speak(text, audioSource, speaker.VoicesForCulture("en")[0]);

            //  √ﬂœ „‰ „‰Õ Êﬁ  ﬂ«›Ú · ÕÊÌ· «·‰’ ≈·Ï ’Ê 
            EditorApplication.delayCall += () => SaveAndConfigureAudio(go, audioSource, filePath);
        }

        private static void SaveAndConfigureAudio(GameObject go, AudioSource audioSource, string filePath)
        {
            SaveAudioClipToFile(audioSource, filePath);

            // »⁄œ –·ﬂ° ﬁ„ » Õ„Ì· „·› «·’Ê  «·„Œ“‰ Ê«” Œœ«„Â „⁄ Salsa
            AssetDatabase.Refresh();
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(filePath);
            if (clip != null)
            {
                OneClickBase.ConfigureSalsaAudioSource(go, clip, true);
            }
            else
            {
                Debug.LogError("Failed to load audio clip from path: " + filePath);
            }
        }

       /* private static string ReadTextFromExcel(string filePath, int sheetIndex, int rowIndex, int columnIndex)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets[sheetIndex];
                return worksheet.Cells[rowIndex, columnIndex].Text;
            }
        }*/

       /* public static async Task<string> GetTextFromChatGPT(string prompt, string apiKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                var requestBody = new
                {
                    model = "text-davinci-003",
                    prompt = prompt,
                    max_tokens = 100
                };
                var response = await client.PostAsync(
                    "https://api.openai.com/v1/completions",
                    new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
                );
                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                return jsonResponse.choices[0].text.Trim();
            }
        }*/

        public static void SaveAudioClipToFile(AudioSource audioSource, string filePath)
        {
            if (audioSource.clip == null)
            {
                Debug.LogError("No audio clip found in the AudioSource.");
                return;
            }

            var clip = audioSource.clip;
            var samples = new float[clip.samples];
            clip.GetData(samples, 0);

            // Convert float array to byte array
            var wavData = ConvertToWav(clip, samples);
            File.WriteAllBytes(filePath, wavData);

            Debug.Log("Audio clip saved to: " + filePath);
        }

        private static byte[] ConvertToWav(AudioClip clip, float[] samples)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    int sampleCount = samples.Length;
                    int sampleRate = clip.frequency;
                    int channels = clip.channels;
                    int bitsPerSample = 16; // Standard 16-bit audio

                    int byteRate = sampleRate * channels * bitsPerSample / 8;
                    int blockAlign = channels * bitsPerSample / 8;
                    int subChunk2Size = sampleCount * channels * bitsPerSample / 8;
                    int chunkSize = 36 + subChunk2Size;

                    // Write WAV header
                    writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                    writer.Write(chunkSize);
                    writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
                    writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                    writer.Write(16); // Subchunk1Size
                    writer.Write((short)1); // AudioFormat (1 for PCM)
                    writer.Write((short)channels);
                    writer.Write(sampleRate);
                    writer.Write(byteRate);
                    writer.Write((short)blockAlign);
                    writer.Write((short)bitsPerSample);
                    writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                    writer.Write(subChunk2Size);

                    // Write audio samples
                    foreach (var sample in samples)
                    {
                        short sampleInt = (short)(sample * short.MaxValue);
                        writer.Write(sampleInt);
                    }

                    return memoryStream.ToArray();
                }
            }
        }

        // WebSocket handler class
     /*   public class WebSocketHandler
        {
            private WebSocket _webSocket;
            public string ReceivedText { get; private set; }

            public WebSocketHandler(string url)
            {
                _webSocket = new WebSocket(url);
                _webSocket.OnMessage += (sender, e) =>
                {
                    ReceivedText = e.Data;
                };
                _webSocket.Connect();
            }

            public void Close()
            {
                _webSocket.Close();
            }
        }*/
    }
}
