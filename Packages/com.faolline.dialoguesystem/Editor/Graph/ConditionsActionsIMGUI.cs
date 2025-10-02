using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    /// <summary>
    /// Editeur IMGUI réutilisable pour 2 listes : Conditions / Actions.
    /// Supporte List<T> (édition directe) OU T[] (buffer + push-back via setters).
    /// </summary>
    public class ConditionsActionsIMGUI
    {
        private readonly Dialogue _asset;
        private readonly string _ownerNodeId;
        private readonly Action _onChanged; // MarkDirty + Broadcast + refresh

        // MODE LIST
        private readonly Func<List<DialogueCondition>> _getCondsList;
        private readonly Func<List<DialogueAction>> _getActsList;

        // MODE ARRAY (ChoiceOptionModel)
        private readonly Func<DialogueCondition[]> _getCondsArray;
        private readonly Action<DialogueCondition[]> _setCondsArray;
        private readonly Func<DialogueAction[]> _getActsArray;
        private readonly Action<DialogueAction[]> _setActsArray;

        private readonly string _prefFoldConds;
        private readonly string _prefFoldActs;

        private ReorderableList _condsRL;
        private ReorderableList _actsRL;

        // Buffers en mode Array
        private List<DialogueCondition> _condBuf;
        private List<DialogueAction> _actBuf;

        private const float _foldHeader = 18f;
        private const float _vsp = 4f;

        public static ConditionsActionsIMGUI ForNodeLevel(
            Dialogue asset, string ownerNodeId,
            Func<List<DialogueCondition>> getConditions,
            Func<List<DialogueAction>> getActions,
            Action onChanged,
            string prefKeySuffix
        )
        {
            return new ConditionsActionsIMGUI(
                asset, ownerNodeId, onChanged,
                getConditions, getActions,
                null, null, null, null,
                $"ds.inspector.{prefKeySuffix}.cond",
                $"ds.inspector.{prefKeySuffix}.acts"
            );
        }

        public static ConditionsActionsIMGUI ForOptionLevel(
            Dialogue asset, string ownerNodeId,
            Func<DialogueCondition[]> getConditions, Action<DialogueCondition[]> setConditions,
            Func<DialogueAction[]> getActions, Action<DialogueAction[]> setActions,
            Action onChanged,
            string prefKeySuffix
        )
        {
            return new ConditionsActionsIMGUI(
                asset, ownerNodeId, onChanged,
                null, null,
                getConditions, setConditions, getActions, setActions,
                $"ds.inspector.{prefKeySuffix}.cond",
                $"ds.inspector.{prefKeySuffix}.acts"
            );
        }

        private ConditionsActionsIMGUI(
            Dialogue asset, string ownerNodeId, Action onChanged,
            Func<List<DialogueCondition>> getCondsList, Func<List<DialogueAction>> getActsList,
            Func<DialogueCondition[]> getCondsArray, Action<DialogueCondition[]> setCondsArray,
            Func<DialogueAction[]> getActsArray, Action<DialogueAction[]> setActsArray,
            string prefFoldConds, string prefFoldActs)
        {
            _asset = asset;
            _ownerNodeId = ownerNodeId;
            _onChanged = onChanged;

            _getCondsList = getCondsList;
            _getActsList = getActsList;

            _getCondsArray = getCondsArray;
            _setCondsArray = setCondsArray;
            _getActsArray = getActsArray;
            _setActsArray = setActsArray;

            _prefFoldConds = prefFoldConds;
            _prefFoldActs = prefFoldActs;
        }

        private bool IsArrayMode => _getCondsArray != null || _getActsArray != null;

        private IList<DialogueCondition> GetCondsCollection()
        {
            if (IsArrayMode)
            {
                if (_condBuf == null)
                    _condBuf = (_getCondsArray?.Invoke() ?? Array.Empty<DialogueCondition>()).ToList();
                return _condBuf;
            }
            return _getCondsList?.Invoke();
        }

        private IList<DialogueAction> GetActsCollection()
        {
            if (IsArrayMode)
            {
                if (_actBuf == null)
                    _actBuf = (_getActsArray?.Invoke() ?? Array.Empty<DialogueAction>()).ToList();
                return _actBuf;
            }
            return _getActsList?.Invoke();
        }

        private void PushBackArraysIfNeeded()
        {
            if (!IsArrayMode) return;
            if (_setCondsArray != null && _condBuf != null) _setCondsArray(_condBuf.ToArray());
            if (_setActsArray != null && _actBuf != null) _setActsArray(_actBuf.ToArray());
        }


        private static float RLHeader => 20f;
        private static float RLFooter => 20f;
        private static float Line => EditorGUIUtility.singleLineHeight;

        private void Commit(string undoLabel)
        {
            EditorUtility.SetDirty(_asset);
            PushBackArraysIfNeeded();
            _onChanged?.Invoke();
            DialogueNode.BroadcastNodeDataChanged(_ownerNodeId);
            AssetDatabase.SaveAssets(); // force la persistance (évite le domaine reload qui vide)
        }

        private ReorderableList BuildRL<TBase>(string header, IList<TBase> source, Action onChange)
            where TBase : UnityEngine.Object
        {
            var rl = new ReorderableList((System.Collections.IList)source, typeof(TBase), true, true, true, true);

            rl.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= rl.count) return;
                rect.y += 2; rect.height = EditorGUIUtility.singleLineHeight;

                var current = rl.list[index] as UnityEngine.Object;
                var newObj = (TBase)EditorGUI.ObjectField(rect, current, typeof(TBase), false);
                if (!Equals(newObj, current))
                {
                    Undo.RecordObject(_asset, $"Edit {header}");
                    rl.list[index] = newObj;
                    Commit($"Edit {header}");
                }
            };

            rl.onAddCallback = _ =>
            {
                Undo.RecordObject(_asset, $"Add {header}");
                rl.list.Add(null);                 // slot vide → l’utilisateur DnD un asset dessus
                Commit($"Add {header}");
            };

            rl.onRemoveCallback = _ =>
            {
                if (rl.index >= 0 && rl.index < rl.count)
                {
                    Undo.RecordObject(_asset, $"Remove {header}");
                    rl.list.RemoveAt(rl.index);
                    rl.index = Mathf.Clamp(rl.index - 1, -1, rl.count - 1);
                    Commit($"Remove {header}");
                }
            };

            rl.onReorderCallback = _ =>
            {
                Undo.RecordObject(_asset, $"Reorder {header}");
                Commit($"Reorder {header}");
            };


            return rl;
        }

        public void EnsureLists()
        {
            // CONDITIONS
            var conds = GetCondsCollection();
            if (conds != null)
            {
                if (_condsRL == null || !ReferenceEquals(_condsRL.list, conds))
                    _condsRL = BuildRL<DialogueCondition>("Conditions", conds, () => Commit("Edit Conditions"));
            }

            // ACTIONS
            var acts = GetActsCollection();
            if (acts != null)
            {
                if (_actsRL == null || !ReferenceEquals(_actsRL.list, acts))
                    _actsRL = BuildRL<DialogueAction>("Actions", acts, () => Commit("Edit Actions"));
            }
        }

        public float GetEstimatedHeight()
        {
            var c = GetCondsCollection()?.Count ?? 0;
            var a = GetActsCollection()?.Count ?? 0;
            float h = 0f;
            if (EditorPrefs.GetBool(_prefFoldConds, true))
                h += RLHeader + (c * (Line + 2)) + RLFooter + 4;
            else
                h += RLHeader + 4;
            if (EditorPrefs.GetBool(_prefFoldActs, true))
                h += RLHeader + (a * (Line + 2)) + RLFooter + 8;
            else
                h += RLHeader + 8;
            return h;
        }

        public void DoLayoutGUI()
        {
            EnsureLists();

            // Foldout Conditions
            var condOpen = EditorPrefs.GetBool(_prefFoldConds, true);
            condOpen = EditorGUILayout.Foldout(condOpen, $"Conditions ({GetCondsCollection()?.Count ?? 0})", true);
            EditorPrefs.SetBool(_prefFoldConds, condOpen);
            if (condOpen && _condsRL != null)
            {
                _condsRL.DoLayoutList();
                HandleDnDOverLastRect(_condsRL, "Drop Conditions");
            }

            // Foldout Actions
            GUILayout.Space(4);
            var actOpen = EditorPrefs.GetBool(_prefFoldActs, true);
            actOpen = EditorGUILayout.Foldout(actOpen, $"Actions ({GetActsCollection()?.Count ?? 0})", true);
            EditorPrefs.SetBool(_prefFoldActs, actOpen);
            if (actOpen && _actsRL != null)
            {
                _actsRL.DoLayoutList();
                HandleDnDOverLastRect(_actsRL, "Drop Actions");
            }
        }

        private void HandleDnDOverLastRect(ReorderableList rl, string undoLabel)
        {
            var dropRect = GUILayoutUtility.GetLastRect();
            HandleDnDAtRect(rl, undoLabel, dropRect);
        }

        private void HandleDnDAtRect(ReorderableList rl, string undoLabel, Rect rect)
        {
            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                // accepte soit DialogueCondition soit DialogueAction (on ajoutera ce qui matche le RL)
                bool hasValid = DragAndDrop.objectReferences.Any(o => o is DialogueCondition || o is DialogueAction);
                DragAndDrop.visualMode = hasValid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                if (hasValid && evt.type == EventType.DragPerform)
                {
                    Undo.RecordObject(_asset, undoLabel);

                    // si la liste courante contient (ou doit contenir) des Conditions
                    bool isCondsList = _condsRL != null && ReferenceEquals(rl, _condsRL);

                    foreach (var o in DragAndDrop.objectReferences)
                    {
                        if (isCondsList && o is DialogueCondition dc) rl.list.Add(dc);
                        else if (!isCondsList && o is DialogueAction da) rl.list.Add(da);
                    }

                    DragAndDrop.AcceptDrag();
                    Commit(undoLabel);
                }

                if (hasValid) evt.Use();
            }
        }


        // Dessin “non-layout” si besoin (utilisé si embed dans un autre RL)
        public float GetHeight()
        {
            EnsureLists();

            bool condOpen = EditorPrefs.GetBool(_prefFoldConds, true);
            bool actOpen = EditorPrefs.GetBool(_prefFoldActs, true);

            float h = 0f;

            h += _foldHeader + _vsp;
            if (condOpen && _condsRL != null) h += _condsRL.GetHeight() + _vsp;

            h += _foldHeader + _vsp;
            if (actOpen && _actsRL != null) h += _actsRL.GetHeight() + _vsp;

            return Mathf.Max(42f, h);
        }

        public void Draw(Rect area)
        {
            EnsureLists();
            float x = area.x, y = area.y, w = area.width;

            // Conditions
            bool condOpen = EditorPrefs.GetBool(_prefFoldConds, true);
            var condHeader = new Rect(x, y, w, _foldHeader);
            bool condNew = EditorGUI.Foldout(condHeader, condOpen, $"Conditions ({GetCondsCollection()?.Count ?? 0})", true);
            if (condNew != condOpen) { EditorPrefs.SetBool(_prefFoldConds, condNew); condOpen = condNew; }
            y += _foldHeader + _vsp;

            if (condOpen && _condsRL != null)
            {
                float ch = _condsRL.GetHeight();
                var condRect = new Rect(x, y, w, ch);
                _condsRL.DoList(condRect);
                HandleDnDAtRect(_condsRL, "Drop Conditions", condRect);
                y += ch + _vsp;
            }

            // Actions
            bool actOpen = EditorPrefs.GetBool(_prefFoldActs, true);
            var actHeader = new Rect(x, y, w, _foldHeader);
            bool actNew = EditorGUI.Foldout(actHeader, actOpen, $"Actions ({GetActsCollection()?.Count ?? 0})", true);
            if (actNew != actOpen) { EditorPrefs.SetBool(_prefFoldActs, actNew); actOpen = actNew; }
            y += _foldHeader + _vsp;

            if (actOpen && _actsRL != null)
            {
                float ah = _actsRL.GetHeight();
                var actRect = new Rect(x, y, w, ah);
                _actsRL.DoList(actRect);
                HandleDnDAtRect(_actsRL, "Drop Actions", actRect);
            }
        }
    }
}
