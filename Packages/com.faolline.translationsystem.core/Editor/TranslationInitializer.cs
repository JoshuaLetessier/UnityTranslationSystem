using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class TranslationInitializer
{
    static TranslationInitializer()
    {
        string translationsPath = Path.Combine(Application.dataPath, "Translations");
        string csvPath = Path.Combine(translationsPath, "CSV");
        string genPath = Path.Combine(translationsPath, "Generated");

        bool created = false;

        if (!Directory.Exists(translationsPath))
        {
            Directory.CreateDirectory(translationsPath);
            created = true;
        }

        if (!Directory.Exists(csvPath))
        {
            Directory.CreateDirectory(csvPath);
            created = true;
        }

        if (!Directory.Exists(genPath))
        {
            Directory.CreateDirectory(genPath);
            created = true;
        }

        if (created)
        {
            Debug.Log("📁 Translation folders created in Assets/Translations/");
            AssetDatabase.Refresh();
        }

        string sampleSource = Path.Combine(Path.GetFullPath("Packages/com.faolline.translationsystem.core/Samples~/Example/UI_MainMenu.csv"));
        string sampleTarget = Path.Combine(csvPath, "UI_MainMenu.csv");

        if (File.Exists(sampleSource) && !File.Exists(sampleTarget))
        {
            File.Copy(sampleSource, sampleTarget);
            Debug.Log("📝 Copied example CSV to Translations/CSV/UI_MainMenu.csv");
        }

    }
}
