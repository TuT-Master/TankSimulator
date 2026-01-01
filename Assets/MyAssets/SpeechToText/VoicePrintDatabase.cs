using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class VoiceSample
{
    public string command;
    public string wavPath;     // for reference
    public float[] feature;    // 8 numbers
}

[Serializable]
class VoiceDB { public List<VoiceSample> samples = new(); }

public class VoicePrintDatabase
{
    public readonly string rootFolder;
    public readonly string dbPath;
    private VoiceDB db = new();

    public VoicePrintDatabase()
    {
        rootFolder = Path.Combine(Application.persistentDataPath, "VoicePrints");
        dbPath = Path.Combine(rootFolder, "voiceprints.json");
        Directory.CreateDirectory(rootFolder);
        Load();
    }

    public void AddSample(string command, AudioClip clip)
    {
        if (clip == null) { Debug.LogWarning("AddSample: clip null"); return; }

        // extract raw samples
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        // if stereo, downmix
        if (clip.channels == 2) samples = DownmixStereo(samples);

        // extract feature
        var feat = FeatureExtractor.ExtractFeature(samples, clip.frequency);

        // save wav (optional but handy for debugging)
        string cmdFolder = Path.Combine(rootFolder, Sanitize(command));
        Directory.CreateDirectory(cmdFolder);
        string wavPath = Path.Combine(cmdFolder, $"sample_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        WavUtility.SaveWav(wavPath, clip);

        // add to db
        db.samples.Add(new VoiceSample { command = command, wavPath = wavPath, feature = feat });

        // limit stored samples per command
        int maxPerCommand = 10;
        var sameCmd = db.samples.Where(s => s.command == command).ToList();
        if (sameCmd.Count > maxPerCommand)
        {
            // remove oldest samples
            int removeCount = sameCmd.Count - maxPerCommand;
            foreach (var s in sameCmd.Take(removeCount))
            {
                if (File.Exists(s.wavPath)) File.Delete(s.wavPath);
                db.samples.Remove(s);
            }
        }
        Save();
    }

    public bool Match(AudioClip clip, float threshold, out string bestCommand, out float bestScore)
    {
        bestCommand = null; bestScore = 0f;
        if (clip == null || db.samples.Count == 0) return false;

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        if (clip.channels == 2) samples = DownmixStereo(samples);

        var q = FeatureExtractor.ExtractFeature(samples, clip.frequency);

        float secondBest = 0f;

        foreach (var s in db.samples)
        {
            float sim = FeatureExtractor.Cosine(q, s.feature);

            if (sim > bestScore)
            {
                secondBest = bestScore;
                bestScore = sim;
                bestCommand = s.command;
            }
            else if (sim > secondBest)
            {
                secondBest = sim;
            }
        }

        // Prevent confusion between similar commands
        if (bestScore >= threshold && bestScore - secondBest > 0.05f)
            return true; // confident
        else
            return false; // too close, fallback to Whisper
    }

    public void Save()
    {
        var wrapper = new VoiceDB { samples = db.samples };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(dbPath, json);
    }
    public void Load()
    {
        if (File.Exists(dbPath))
        {
            string json = File.ReadAllText(dbPath);
            db = JsonUtility.FromJson<VoiceDB>(json) ?? new VoiceDB();
        }
    }

    public void PruneDuplicates(float similarity = 0.98f)
    {
        var toRemove = new List<VoiceSample>();

        for (int i = 0; i < db.samples.Count; i++)
        {
            for (int j = i + 1; j < db.samples.Count; j++)
            {
                if (db.samples[i].command == db.samples[j].command)
                {
                    float sim = FeatureExtractor.Cosine(db.samples[i].feature, db.samples[j].feature);
                    if (sim > similarity)
                        toRemove.Add(db.samples[j]);
                }
            }
        }

        foreach (var s in toRemove.Distinct())
        {
            if (File.Exists(s.wavPath)) File.Delete(s.wavPath);
            db.samples.Remove(s);
        }
        if (toRemove.Count > 0)
        {
            Debug.Log($"[VoiceDB] Pruned {toRemove.Count} near-duplicate samples");
            Save();
        }
    }

    private static string Sanitize(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c.ToString(), "_");
        return s.ToLower().Trim();
    }

    private static float[] DownmixStereo(float[] interleaved)
    {
        int n = interleaved.Length / 2;
        var mono = new float[n];
        for (int i = 0, j = 0; i < n; i++, j += 2)
            mono[i] = 0.5f * (interleaved[j] + interleaved[j + 1]);
        return mono;
    }
}
