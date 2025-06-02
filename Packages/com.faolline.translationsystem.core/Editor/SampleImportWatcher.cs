using UnityEditor;

namespace com.faolline.translationsystem
{
    public class SampleImportWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] _, string[] __, string[] ___)
        {
            bool containsSample = false;
            foreach (string path in importedAssets)
            {
                if (path.StartsWith("Assets/Samples/Translation_System"))
                {
                    containsSample = true;
                    break;
                }
            }

            if (containsSample)
            {
                TranslationPrefabBrowser.GenerateMenuFromPrefabs();
            }
        }
    }
}
