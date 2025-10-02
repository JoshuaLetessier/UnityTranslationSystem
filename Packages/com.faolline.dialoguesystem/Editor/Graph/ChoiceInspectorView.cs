using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.faolline.dialoguesystem
{
    /// <summary>
    /// Editeur des options pour un node Choice (inline, repliable par option).
    /// </summary>
    public class ChoiceInspectorView : VisualElement
    {
        private readonly Dialogue _asset;
        private readonly SentenceNodeModel _model; // Choice node
        private readonly Func<IReadOnlyList<string>> _getAllTextKeys; // _asset.AllTranslationKeys
        private readonly Action _onChanged; // MarkDirty + Broadcast + refresh titres externes

        private Foldout _optionsFoldout;
        private IMGUIContainer _optionsIMGUI;
        private ReorderableList _optionsRL;

        // cache par optionId
        private readonly Dictionary<string, OptionEditor> _perOptionEditors = new();

        public ChoiceInspectorView(Dialogue asset, SentenceNodeModel model, Func<IReadOnlyList<string>> getAllTextKeys, Action onChanged)
        {
            _asset = asset;
            _model = model;
            _getAllTextKeys = getAllTextKeys;
            _onChanged = onChanged;

            style.marginTop = 6;

            _optionsFoldout = new Foldout { text = "Options (0)", value = true };
            Add(_optionsFoldout);

            _optionsIMGUI = new IMGUIContainer(DrawOptionsGUI);
            _optionsFoldout.Add(_optionsIMGUI);

            EnsureOptionsList();
            RefreshFoldoutTitle();
        }

        public new void MarkDirtyRepaint() => _optionsIMGUI?.MarkDirtyRepaint();

        private void RefreshFoldoutTitle()
        {
            _optionsFoldout.text = $"Options ({_model?.options?.Count ?? 0})";
        }

        private void EnsureOptionsList()
        {
            if (_model.options == null) _model.options = new List<ChoiceOptionModel>();
            if (_optionsRL != null) return;

            _optionsRL = new ReorderableList(_model.options, typeof(ChoiceOptionModel), true, true, true, true);

            _optionsRL.drawHeaderCallback = r => EditorGUI.LabelField(r, "Options", EditorStyles.boldLabel);

            _optionsRL.onAddCallback = _ =>
            {
                Undo.RecordObject(_asset, "Add Choice Option");
                var opt = new ChoiceOptionModel
                {
                    optionId = Guid.NewGuid().ToString(),
                    displayTextKey = string.Empty,
                    conditions = Array.Empty<DialogueCondition>(),
                    sideEffects = Array.Empty<DialogueAction>()
                };
                _model.options.Add(opt);
                EditorUtility.SetDirty(_asset);
                _onChanged?.Invoke();
                RefreshFoldoutTitle();
                _optionsIMGUI.MarkDirtyRepaint();
            };

            _optionsRL.onRemoveCallback = _ =>
            {
                if (_optionsRL.index < 0 || _optionsRL.index >= _model.options.Count) return;

                var opt = _model.options[_optionsRL.index];
                var optId = opt.optionId;

                Undo.RecordObject(_asset, "Remove Choice Option");
                _model.options.RemoveAt(_optionsRL.index);
                EditorUtility.SetDirty(_asset);

                if (!string.IsNullOrEmpty(optId)) _perOptionEditors.Remove(optId);

                _onChanged?.Invoke();
                RefreshFoldoutTitle();
                _optionsIMGUI.MarkDirtyRepaint();
            };

            _optionsRL.onReorderCallback = _ =>
            {
                Undo.RecordObject(_asset, "Reorder Choice Options");
                EditorUtility.SetDirty(_asset);
                _onChanged?.Invoke();
                _optionsIMGUI.MarkDirtyRepaint();
            };

            // --- hauteur dynamique : header + TextKey + (Conditions/Actions) ---
            _optionsRL.elementHeightCallback = (index) =>
            {
                if (index < 0 || index >= _model.options.Count)
                    return EditorGUIUtility.singleLineHeight + 10f;

                var opt = _model.options[index];
                var ed = GetOrCreateOptionEditor(opt);

                const float header = 18f;
                float vsp = EditorGUIUtility.standardVerticalSpacing;

                if (!ed.Expanded) return header + vsp * 2;

                ed.CA.EnsureLists();
                float caH = Mathf.Max(ed.CA.GetHeight(), 90f);

                float h = 0f;
                h += header;                                   // foldout "Option n"
                h += vsp + EditorGUIUtility.singleLineHeight;  // Text Key
                h += vsp + caH;                                // Conditions/Actions
                h += vsp * 2 + 4f;
                return h;
            };

            _optionsRL.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= _model.options.Count) return;
                var opt = _model.options[index];
                var ed = GetOrCreateOptionEditor(opt);

                var r = rect; r.x += 4; r.width -= 8;
                float vsp = EditorGUIUtility.standardVerticalSpacing;

                // Header (foldout)
                var headerRect = new Rect(r.x, r.y + 2, r.width, 16f);
                bool newExpanded = EditorGUI.Foldout(headerRect, ed.Expanded, $"Option {index + 1}", true);
                if (newExpanded != ed.Expanded) { ed.Expanded = newExpanded; _optionsIMGUI.MarkDirtyRepaint(); }
                if (!ed.Expanded) return;

                // Text Key
                var keys = _getAllTextKeys?.Invoke()?.ToArray() ?? Array.Empty<string>();
                int cur = Array.IndexOf(keys, opt.displayTextKey); if (cur < 0) cur = 0;
                var keyRect = new Rect(r.x, headerRect.yMax + vsp, r.width, EditorGUIUtility.singleLineHeight);
                int sel = EditorGUI.Popup(keyRect, "Text Key", cur, keys);
                if (sel != cur && sel >= 0 && sel < keys.Length)
                {
                    Undo.RecordObject(_asset, "Edit Choice TextKey");
                    opt.displayTextKey = keys[sel];
                    EditorUtility.SetDirty(_asset);
                    _onChanged?.Invoke();
                }

                // Zone Conditions/Actions (Rect-based)
                float used = (keyRect.yMax - rect.y) + vsp;
                var area = new Rect(r.x, keyRect.yMax + vsp, r.width, Mathf.Max(0f, rect.height - used - 4f));
                ed.CA.Draw(area);
            };


        }

        private class OptionEditor
        {
            public readonly ChoiceOptionModel Opt;
            public readonly ConditionsActionsIMGUI CA;
            private readonly string _foldKey;

            public OptionEditor(Dialogue asset, SentenceNodeModel owner, ChoiceOptionModel opt, Action onChanged)
            {
                Opt = opt;
                _foldKey = $"ds.choice.option.fold.{owner.id}.{opt.optionId}";

                CA = ConditionsActionsIMGUI.ForOptionLevel(
                    asset, owner.id,
                    () => Opt.conditions, arr => Opt.conditions = arr,
                    () => Opt.sideEffects, arr => Opt.sideEffects = arr,
                    onChanged,
                    prefKeySuffix: $"{owner.id}.{opt.optionId}"
                );
            }

            public bool Expanded
            {
                get => EditorPrefs.GetBool(_foldKey, false);
                set => EditorPrefs.SetBool(_foldKey, value);
            }
        }

        private OptionEditor GetOrCreateOptionEditor(ChoiceOptionModel opt)
        {
            if (opt == null) throw new ArgumentNullException(nameof(opt));
            if (string.IsNullOrEmpty(opt.optionId)) opt.optionId = Guid.NewGuid().ToString();

            if (!_perOptionEditors.TryGetValue(opt.optionId, out var ed))
            {
                ed = new OptionEditor(_asset, _model, opt, _onChanged);
                _perOptionEditors[opt.optionId] = ed;
            }
            return ed;
        }

        private void DrawOptionsGUI()
        {
            EnsureOptionsList();
            _optionsRL?.DoLayoutList();
        }
    }
}
