using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TranslationKeyGroup
{
    public string category;
    public List<string> keys;
}

[CreateAssetMenu(menuName = "Translation/Translation Key Database")]
public class TranslationKeyDatabase : ScriptableObject
{
    [SerializeField]
    private List<TranslationKeyGroup> keyGroups = new();

    public List<string> GetCategories() => keyGroups.ConvertAll(g => g.category);

    public List<string> GetKeysInCategory(string category)
    {
        return keyGroups.Find(g => g.category == category)?.keys ?? new List<string>();
    }

    public void SetKeyGroups(List<TranslationKeyGroup> groups)
    {
        keyGroups.Clear();
        keyGroups.AddRange(groups);
    }

    public void SetKeys(List<string> allKeys)
    {
        var grouped = new Dictionary<string, List<string>>();

        foreach (var fullKey in allKeys)
        {
            var parts = fullKey.Split('.');
            if (parts.Length < 2) continue;

            string group = parts[0];
            string subKey = fullKey;

            if (!grouped.ContainsKey(group))
                grouped[group] = new List<string>();

            grouped[group].Add(subKey);
        }

        keyGroups.Clear();
        foreach (var pair in grouped)
        {
            keyGroups.Add(new TranslationKeyGroup
            {
                category = pair.Key,
                keys = pair.Value
            });
        }
    }
}
