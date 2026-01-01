using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class MicrophoneRecorder : MonoBehaviour
{
    public TextMeshProUGUI outputText;
    public AudioClip recordedClip;

    private string filePath;
    private bool isRecording = false;
    private float startTime;

    [SerializeField] private TextToCommand textToCommand;
    [SerializeField] private string modelName;
    public float lastSignalConfidence { get; private set; }

    private VoicePrintDatabase voiceDB;
    [SerializeField] private bool trainingMode = false;
    [SerializeField] private string trainingCommand = "go";
    [SerializeField, Range(0f, 1f)] private float quickMatchThreshold = 0.85f;


    [ContextMenu("Open VoicePrint Folder")]
    void OpenVoicePrintFolder() => Application.OpenURL(Path.Combine(Application.persistentDataPath, "VoicePrints"));


    private Dictionary<string, List<string>> trainingGroups;
    private List<string> trainingLabels;
    private int currentTrainingIndex = 0;



    private void Awake()
    {
        voiceDB = new VoicePrintDatabase();
        voiceDB.PruneDuplicates();
        trainingGroups = textToCommand.GetTrainingGroups();
        trainingLabels = trainingGroups.Keys.ToList();
        trainingCommand = trainingLabels.Count > 0 ? trainingLabels[0] : "none";
    }
    private async void Update()
    {
        // Toggle training mode
        if (Input.GetKeyDown(KeyCode.F1))
        {
            trainingMode = !trainingMode;
            outputText.text = trainingMode
                ? $"[TRAINING MODE ON] Command: {trainingCommand}"
                : "[TRAINING MODE OFF]";
            UnityEngine.Debug.Log(trainingMode ? "[Trainer] Enabled" : "[Trainer] Disabled");
        }

        // Cycle between a few known commands (just for testing)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (trainingLabels == null || trainingLabels.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[Trainer] No training commands available!");
                return;
            }

            currentTrainingIndex = (currentTrainingIndex + 1) % trainingLabels.Count;
            trainingCommand = trainingLabels[currentTrainingIndex];

            outputText.text = $"[TRAINING] Command: {trainingCommand}";
            UnityEngine.Debug.Log($"[Trainer] Selected command group: {trainingCommand}");
        }

        // Start recording
        if (Input.GetKeyDown(KeyCode.Space) && !isRecording)
            StartRecording();

        // Stop and process
        if (Input.GetKeyUp(KeyCode.Space) && isRecording)
        {
            StopRecordingAndSave();
            await TranscribeOrTrainAsync();
        }
    }


    private async Task TranscribeOrTrainAsync()
    {
        if (trainingMode)
        {
            // TRAINING MODE – add sample
            if (trainingGroups.TryGetValue(trainingCommand, out var synonyms))
            {
                foreach (string variant in synonyms)
                {
                    voiceDB.AddSample(variant, recordedClip);
                    UnityEngine.Debug.Log($"[Trainer] Added sample for '{variant}'");
                }
            }
            else
            {
                // fallback (shouldn’t happen)
                voiceDB.AddSample(trainingCommand, recordedClip);
                UnityEngine.Debug.Log($"[Trainer] Added sample for '{trainingCommand}' (no synonyms)");
            }
            outputText.text = $"[Trainer] Added sample for '{trainingCommand}'";
            UnityEngine.Debug.Log($"[Trainer] Added sample for '{trainingCommand}'");
            return;
        }

        // NORMAL MODE
        // Try quick match
        if (voiceDB.Match(recordedClip, quickMatchThreshold, out string quickCmd, out float quickScore))
        {
            outputText.text = $"{quickCmd} (Quick: {quickScore:0.00})";
            textToCommand.GetCommandFromText(quickCmd);
            UnityEngine.Debug.Log($"[VoiceDB] Quick match -> {quickCmd} ({quickScore:0.00})");

            // reinforce strong quick matches
            if (quickScore > 0.95f)
            {
                voiceDB.AddSample(quickCmd, recordedClip);
                UnityEngine.Debug.Log($"[VoiceDB] Reinforced '{quickCmd}' (score {quickScore:0.00})");
            }

            return;
        }

        // Fallback to Whisper
        string result = await Task.Run(() => WhisperBridge.RunWhisper(filePath, modelName));
        result = result.Replace(",", "").Replace(".", "").Trim();

        // Display + execute
        float signalConfidence = lastSignalConfidence;
        float textConfidence = textToCommand.GetTextConfidence(result);
        float finalConfidence = (signalConfidence + textConfidence) / 2f;

        outputText.text = $"{result} ({finalConfidence * 100:0.00}%)";
        textToCommand.GetCommandFromText(result);

        // Optional: auto-learn if confident
        if (finalConfidence > 0.85f)
        {
            voiceDB.AddSample(result, recordedClip);
            UnityEngine.Debug.Log($"[VoiceDB] Auto-learned '{result}' ({finalConfidence:0.00})");
        }
    }


    public void StartRecording()
    {
        isRecording = true;
        recordedClip = Microphone.Start(null, false, 3, 16000);
        startTime = Time.time;
    }
    public void StopRecordingAndSave()
    {
        isRecording = false;

        int recordedLength = (int)((Time.time - startTime) * recordedClip.frequency);
        float[] samples = new float[recordedLength];
        recordedClip.GetData(samples, 0);

        Microphone.End(null);

        lastSignalConfidence = CalculateSignalConfidence(samples, recordedClip.frequency);

        AudioClip tempClip = AudioClip.Create("temp", recordedLength, recordedClip.channels, recordedClip.frequency, false);
        tempClip.SetData(samples, 0);

        AudioClip trimmedClip = TrimSilence(tempClip, 0.01f);

        filePath = Path.Combine(Application.persistentDataPath, "voice.wav");
        WavUtility.SaveWav(filePath, trimmedClip);
    }


    private float CalculateSignalConfidence(float[] samples, int sampleRate)
    {
        if (samples == null || samples.Length == 0) return 0f;

        // 1. RMS (loudness)
        float rms = Mathf.Sqrt(samples.Average(s => s * s));

        // 2. Dynamic range (energy variance)
        int chunkSize = Mathf.Max(1, samples.Length / 32);
        float[] energies = new float[32];
        for (int i = 0; i < 32; i++)
        {
            int start = i * chunkSize;
            float sum = 0f;
            for (int j = 0; j < chunkSize && start + j < samples.Length; j++)
                sum += samples[start + j] * samples[start + j];
            energies[i] = Mathf.Sqrt(sum / chunkSize);
        }
        float avgE = energies.Average();
        float variance = Mathf.Sqrt(energies.Average(e => (e - avgE) * (e - avgE)));

        // 3. Spectral spread (basic 3-band ratio)
        float low = BandEnergy(samples, 200, 800, sampleRate);
        float mid = BandEnergy(samples, 800, 2000, sampleRate);
        float high = BandEnergy(samples, 2000, 4000, sampleRate);
        float spectralBalance = Mathf.Clamp01((mid + high) / (low + 1e-6f));

        // 4. Weighted score
        float loudnessScore = Mathf.InverseLerp(0.02f, 0.15f, rms);
        float varianceScore = Mathf.InverseLerp(0.005f, 0.05f, variance);
        float spectralScore = Mathf.InverseLerp(0.3f, 2f, spectralBalance);

        float final = loudnessScore * 0.5f + varianceScore * 0.3f + spectralScore * 0.2f;
        return Mathf.Clamp01(final);
    }
    private float BandEnergy(float[] samples, float f1, float f2, int fs)
    {
        int N = Mathf.Min(samples.Length, 2048);
        float re = 0f, im = 0f;
        for (int n = 0; n < N; n++)
        {
            float x = samples[n];
            float f = f1 + (f2 - f1) * n / N;
            float w = 2 * Mathf.PI * f / fs;
            re += x * Mathf.Cos(w);
            im += x * Mathf.Sin(w);
        }
        return Mathf.Sqrt(re * re + im * im);
    }
    private AudioClip TrimSilence(AudioClip clip, float minVolume = 0.01f)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int startIndex = 0;
        int endIndex = samples.Length - 1;

        // Find first sample above volume threshold
        while (startIndex < samples.Length && Mathf.Abs(samples[startIndex]) < minVolume)
        {
            startIndex++;
        }

        // Find last sample above volume threshold
        while (endIndex > startIndex && Mathf.Abs(samples[endIndex]) < minVolume)
        {
            endIndex--;
        }

        int trimmedLength = endIndex - startIndex + 1;
        if (trimmedLength <= 0)
        {
            // No audible sound found, return original clip
            return clip;
        }

        float[] trimmedSamples = new float[trimmedLength];
        Array.Copy(samples, startIndex, trimmedSamples, 0, trimmedLength);

        AudioClip trimmedClip = AudioClip.Create("trimmed", trimmedLength / clip.channels, clip.channels, clip.frequency, false);
        trimmedClip.SetData(trimmedSamples, 0);

        return trimmedClip;
    }

}

public static class WhisperBridge
{
    public static string RunWhisper(string wavPath, string modelName)
    {
        string whisperExePath = Path.Combine(Application.streamingAssetsPath, "whisper-cli.exe");
        string modelPath = Path.Combine(Application.streamingAssetsPath, "whisper models", modelName + ".bin");

        // Normalize slashes to forward
        whisperExePath = whisperExePath.Replace("\\", "/");
        modelPath = modelPath.Replace("\\", "/");
        wavPath = wavPath.Replace("\\", "/");

        ProcessStartInfo psi = new()
        {
            FileName = whisperExePath, // full path to whisper-cli.exe
            Arguments = $"-m \"{modelPath}\" -f \"{wavPath}\" --language en -nt",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(whisperExePath) // important for DLL resolution
        };

        using var process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        string errorOutput = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(errorOutput))
            UnityEngine.Debug.LogWarning("Whisper error output: " + errorOutput);

        if (string.IsNullOrWhiteSpace(output))
        {
            UnityEngine.Debug.LogWarning("Whisper output is empty.");
            return "Warning: No output from Whisper.";
        }

        return output;
    }
}
