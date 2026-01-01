using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TextToCommand : MonoBehaviour
{
    [SerializeField] private Driver driver;

    private Dictionary<List<string>, Action> voiceCommands;
    private List<LearnedCommand> learnedCommands = new();
    private string learnedFilePath;



    private void Awake()
    {
        voiceCommands = new()
        {
            { new(){ "go 2", "go two", "go to" }, driver.Go2 },
            { new(){ "go 3", "go three", "go free" }, driver.Go3 },
            { new(){ "go 4", "go for" }, driver.Go4 },
            { new(){ "go", "go1", "go one" }, driver.Go1 },
            { new(){ "back 2", "back two", "back to" }, driver.Reverse2 },
            { new(){ "back 1", "back one", "back" }, driver.Reverse1 },
            { new(){ "reverse 2", "reverse two", "reverse to" }, driver.Reverse2 },
            { new(){ "reverse 1", "reverse one", "reverse" }, driver.Reverse1 },
            { new(){ "stop", "halt" }, driver.Stop },
            { new(){ "left left" }, driver.TurnLeft2 },
            { new(){ "left" }, driver.TurnLeft1 },
            { new(){ "right right" }, driver.TurnRight2 },
            { new(){ "right" }, driver.TurnRight1 },
            { new(){ "straight" }, driver.Straight },
        };

        learnedFilePath = Path.Combine(Application.persistentDataPath, "learned_commands.json");
        LoadLearnedCommands();
    }

    public void GetCommandFromText(string text)
    {
        // At top of GetCommandFromText:
        foreach (var lc in learnedCommands)
        {
            if (text.Contains(lc.heardPhrase.ToLower()))
            {
                Debug.Log($"Matched learned phrase \"{lc.heardPhrase}\" -> {lc.matchedCommand}");
                InvokeCommand(lc.matchedCommand);
                lc.usageCount++;
                SaveLearnedCommands();
                return;
            }
        }

        text = text.ToLower();
        foreach (var voiceCommand in voiceCommands)
        {
            foreach (string cmd in voiceCommand.Key)
            {
                if (text.Contains(cmd))
                {
                    voiceCommand.Value.Invoke();
                    return;
                }
            }
        }
    }
    private void InvokeCommand(string command)
    {
        foreach (var vc in voiceCommands)
            foreach (var c in vc.Key)
                if (c == command)
                {
                    vc.Value.Invoke();
                    return;
                }
    }


    public void LearnNewPhrase(string heardPhrase, float confidence)
    {
        // Try to find which command was closest
        string bestCmd = GetClosestCommand(heardPhrase);

        learnedCommands.Add(new LearnedCommand
        {
            heardPhrase = heardPhrase,
            matchedCommand = bestCmd,
            confidence = confidence,
            usageCount = 1
        });

        SaveLearnedCommands();
        Debug.Log($"Learned new phrase: \"{heardPhrase}\" -> \"{bestCmd}\"");
    }
    private string GetClosestCommand(string phrase)
    {
        string best = "";
        float bestScore = 0f;

        foreach (var cmdList in voiceCommands.Keys)
        {
            foreach (string cmd in cmdList)
            {
                float score = 1f - (float)LevenshteinDistance(phrase, cmd) / Mathf.Max(phrase.Length, cmd.Length);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = cmd;
                }
            }
        }

        return best;
    }
    private void SaveLearnedCommands()
    {
        string json = JsonUtility.ToJson(new Wrapper<List<LearnedCommand>> { data = learnedCommands }, true);
        File.WriteAllText(learnedFilePath, json);
    }

    private void LoadLearnedCommands()
    {
        if (File.Exists(learnedFilePath))
        {
            string json = File.ReadAllText(learnedFilePath);
            learnedCommands = JsonUtility.FromJson<Wrapper<List<LearnedCommand>>>(json).data;
        }
    }
    // Helper for JsonUtility
    [System.Serializable]
    private class Wrapper<T> { public T data; }


    public float GetTextConfidence(string recognized)
    {
        recognized = recognized.ToLower();
        float best = 0f;

        // Compare recognized text with every command variant
        foreach (var cmdList in voiceCommands.Keys)
        {
            foreach (string cmd in cmdList)
            {
                float score = 1f - (float)LevenshteinDistance(recognized, cmd) / Mathf.Max(recognized.Length, cmd.Length);
                if (score > best) best = score;
            }
        }

        return Mathf.Clamp01(best);
    }
    private int LevenshteinDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                dp[i, j] = Mathf.Min(
                    Mathf.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                );
            }
        }
        return dp[a.Length, b.Length];
    }



    public List<string> GetAllCommandNames()
    {
        var names = new List<string>();
        foreach (var keyList in voiceCommands.Keys)
            names.AddRange(keyList);
        return names.Distinct().ToList();
    }
    public Dictionary<string, List<string>> GetTrainingGroups()
    {
        var groups = new Dictionary<string, List<string>>();
        foreach (var keyList in voiceCommands.Keys)
        {
            // Use first phrase as display name
            string mainName = keyList[0];
            groups[mainName] = new List<string>(keyList);
        }
        return groups;
    }
}


[System.Serializable]
public class LearnedCommand
{
    public string heardPhrase;
    public string matchedCommand;
    public float confidence;
    public int usageCount;
}
