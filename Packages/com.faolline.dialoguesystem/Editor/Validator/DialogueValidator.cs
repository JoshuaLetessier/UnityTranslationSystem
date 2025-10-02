using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    public static class DialogueValidator
    {
        [MenuItem("Tools/Dialogue/Validate Selected Dialogue")]
        public static void ValidateSelected()
        {
            var dlg = Selection.activeObject as Dialogue;
            if (dlg == null) { Debug.LogWarning("Select a Dialogue asset."); return; }

            var g = dlg.Graph;
            if (g == null) { Debug.LogWarning("Dialogue has no Graph."); return; }

            Debug.Log($"--- Validate '{dlg.Title}' ---");
            for (int i = 0; i < (g.nodes?.Count ?? 0); i++)
            {
                var n = g.nodes[i];
                if (n == null) continue;
                int c = n.conditions?.Count ?? 0;
                int a = n.actions?.Count ?? 0;
                Debug.Log($"Node {i} [{n.type}] id={n.id}  cond={c}  act={a}");

                if (n.options != null)
                {
                    for (int j = 0; j < n.options.Count; j++)
                    {
                        var opt = n.options[j];
                        if (opt == null) continue;
                        int oc = opt.conditions?.Length ?? 0;
                        int oa = opt.sideEffects?.Length ?? 0;
                        Debug.Log($"   Option {j} id={opt.optionId}  cond={oc}  act={oa}  key={opt.displayTextKey}");
                    }
                }
            }
        }
    }
}
