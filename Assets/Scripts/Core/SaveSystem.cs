using UnityEngine;
using System.IO;

// ---------- Core data format ----------
[System.Serializable]
public class SaveData {
    public string lastScene;
    public string[] unlockedNodeIds;
    public int playerHP;
    // add other fields later (inventory, settings, etc.)
}

// ---------- Save / Load manager ----------
public static class SaveSystem {
    static string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(SaveData data) {
        var json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(Path, json);
        Debug.Log($"Game saved to {Path}");
    }

    public static bool TryLoad(out SaveData data) {
        if (File.Exists(Path)) {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
            Debug.Log($"Loaded save from {Path}");
            return true;
        }
        data = null;
        return false;
    }

    public static void DeleteSave() {
        if (File.Exists(Path)) File.Delete(Path);
    }
}