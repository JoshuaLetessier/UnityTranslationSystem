using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.faolline.dialoguesystem
{
    public class SentenceInspectorView : VisualElement
    {
        private Dialogue _asset;
        private SentenceNodeModel _model;

        // Header / infos
        private Label _title;
        private Label _idLabel;

        // Layout
        private ScrollView _scroll;
        private VisualElement _rootForm;
        private HelpBox _emptyState;

        // Champs communs
        private PopupField<string> _speakerField;
        private PopupField<Mood> _moodField;
        private PopupField<string> _textKeyField;

        // Bloc Conditions/Actions (factorisé)
        private ConditionsActionsIMGUI _nodeCAEditor;
        private IMGUIContainer _nodeCAContainer;

        // Bloc Options (visible seulement pour Choice)
        private VisualElement _choiceSection;
        private ChoiceInspectorView _choiceInspector;

        public SentenceInspectorView()
        {
            style.paddingLeft = 10;
            style.paddingRight = 10;
            style.paddingTop = 10;
            style.paddingBottom = 10;
            style.flexGrow = 1;

            _title = new Label("Inspector")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12, marginBottom = 6 }
            };
            Add(_title);

            _scroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
            Add(_scroll);

            _emptyState = new HelpBox("Sélectionne un node pour éditer ses propriétés.", HelpBoxMessageType.Info);
            _emptyState.style.marginTop = 8;
            _scroll.Add(_emptyState);

            _rootForm = new VisualElement { style = { display = DisplayStyle.None, marginTop = 8 } };
            _scroll.Add(_rootForm);

            _idLabel = new Label("ID: -")
            {
                style = { unityFontStyleAndWeight = FontStyle.Italic, marginBottom = 6 }
            };
            _rootForm.Add(_idLabel);

            // ─── Speaker / Mood ──────────────────────────────────────────────────
            _rootForm.Add(Separator());

            EnsureSpeakerPopup();
            _moodField = new PopupField<Mood>("Mood", new List<Mood> { Mood.Neutral }, 0)
            { name = "MoodPopup" };
            _moodField.tooltip = "Humeur/portrait du speaker.";
            _moodField.RegisterValueChangedCallback(OnMoodChanged);
            _rootForm.Add(_moodField);

            // ─── Text Key ────────────────────────────────────────────────────────
            _rootForm.Add(Separator());

            _textKeyField = new PopupField<string>("Text Key", new List<string> { string.Empty }, 0)
            { name = "TextKeyPopup" };
            _textKeyField.tooltip = "Clé de traduction.";
            _textKeyField.RegisterValueChangedCallback(OnTextKeyChanged);
            _rootForm.Add(_textKeyField);

            // ─── Conditions / Actions (IMGUI factorisé) ─────────────────────────
            _rootForm.Add(Separator());
            _nodeCAContainer = new IMGUIContainer(() => _nodeCAEditor?.DoLayoutGUI());
            _rootForm.Add(_nodeCAContainer);

            // ─── Options (uniquement pour Choice) ───────────────────────────────
            _rootForm.Add(Separator());
            _choiceSection = new VisualElement { style = { display = DisplayStyle.None } };
            _rootForm.Add(_choiceSection);
        }

        // API
        public void SetAsset(Dialogue dialogueAsset)
        {
            _asset = dialogueAsset;
            RebuildSpeakerChoices();
            RebuildMoodChoices();
            RebuildTextKeyChoices();
        }

        public void BindNode(SentenceNodeModel model)
        {
            _model = model;

            if (_model.conditions == null) _model.conditions = new List<DialogueCondition>();
            if (_model.actions == null) _model.actions = new List<DialogueAction>();

            _emptyState.style.display = DisplayStyle.None;
            _rootForm.style.display = DisplayStyle.Flex;

            _title.text = $"Inspector — {_model.type}";
            _idLabel.text = $"ID: {_model.id}";

            // Champs communs
            RebuildSpeakerChoices();
            RebuildMoodChoices();
            RebuildTextKeyChoices();
            ApplyCommonValues();

            // Bind de l’éditeur Conditions/Actions (niveau node)
            _nodeCAEditor = ConditionsActionsIMGUI.ForNodeLevel(
                asset: _asset,
                ownerNodeId: _model.id,
                getConditions: () => _model.conditions,
                getActions: () => _model.actions,
                onChanged: OnListsChanged,
                prefKeySuffix: _model.id
            );
            _nodeCAContainer?.MarkDirtyRepaint();

            // Section Choice (affichée uniquement si node de type Choice)
            if (_model.type == SentenceType.Choice)
            {
                _choiceSection.style.display = DisplayStyle.Flex;
                _choiceSection.Clear();

                _choiceInspector = new ChoiceInspectorView(
                    asset: _asset,
                    model: _model,
                    getAllTextKeys: () => _asset?.AllTranslationKeys?.ToList() ?? new List<string>(),
                    onChanged: OnListsChanged
                );
                _choiceSection.Add(_choiceInspector);
            }
            else
            {
                _choiceSection.style.display = DisplayStyle.None;
                _choiceSection.Clear();
                _choiceInspector = null;
            }
        }

        public void ClearInspector()
        {
            _model = null;
            _title.text = "Inspector";
            _idLabel.text = "ID: -";

            _speakerField?.SetValueWithoutNotify(string.Empty);
            _moodField?.SetValueWithoutNotify(Mood.Neutral);
            _textKeyField?.SetValueWithoutNotify(string.Empty);

            _nodeCAEditor = null;
            _nodeCAContainer?.MarkDirtyRepaint();

            _choiceSection?.Clear();
            _choiceSection.style.display = DisplayStyle.None;
            _choiceInspector = null;

            _rootForm.style.display = DisplayStyle.None;
            _emptyState.style.display = DisplayStyle.Flex;
        }

        // ────────────────────────────────────────────────────────────────────────
        // Utilitaires UI
        // ────────────────────────────────────────────────────────────────────────

        private VisualElement Separator()
        {
            return new VisualElement
            {
                style =
                {
                    height = 1,
                    marginTop = 6,
                    marginBottom = 6,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.6f)
                }
            };
        }

        private void OnListsChanged()
        {
            MarkDirty();
            if (_model != null)
                DialogueNode.BroadcastNodeDataChanged(_model.id);

            _nodeCAContainer?.MarkDirtyRepaint();
            _choiceInspector?.MarkDirtyRepaint();
        }

        private void MarkDirty()
        {
            if (_asset == null) return;
            Undo.RecordObject(_asset, "Edit Dialogue");
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets(); // sécurité : écrit sur disque tout de suite
        }


        // ────────────────────────────────────────────────────────────────────────
        // Champs communs
        // ────────────────────────────────────────────────────────────────────────

        private void EnsureSpeakerPopup()
        {
            var existing = this.Q<PopupField<string>>("SpeakerPopup");
            if (existing != null) _rootForm.Remove(existing);

            var choices = GetSpeakerNames();
            _speakerField = new PopupField<string>("Speaker", choices, 0) { name = "SpeakerPopup" };
            _speakerField.tooltip = "Identifiant logique du Speaker.";
            _speakerField.RegisterValueChangedCallback(OnSpeakerChanged);
            _rootForm.Add(_speakerField);
        }

        private void RebuildSpeakerChoices()
        {
            if (_speakerField == null) { EnsureSpeakerPopup(); return; }
            _speakerField.choices = GetSpeakerNames();

            var desired = (_model != null && !string.IsNullOrEmpty(_model.speakerId))
                ? _model.speakerId
                : _speakerField.choices.FirstOrDefault();
            _speakerField.SetValueWithoutNotify(_speakerField.choices.Contains(desired) ? desired : _speakerField.choices.FirstOrDefault());
        }

        private List<string> GetSpeakerNames()
        {
            if (_asset?.Speakers == null || _asset.Speakers.Count == 0)
                return new List<string> { string.Empty };
            return _asset.Speakers
                         .Select(s => s.SpeakerName)
                         .Where(n => !string.IsNullOrEmpty(n))
                         .Distinct()
                         .ToList();
        }

        private Speaker FindSpeakerByName(string name)
        {
            if (_asset?.Speakers == null) return null;
            return _asset.Speakers.FirstOrDefault(s => s.SpeakerName == name);
        }

        private void RebuildMoodChoices()
        {
            var speakerName = _speakerField != null ? _speakerField.value : _model?.speakerId;
            var speaker = string.IsNullOrEmpty(speakerName) ? null : FindSpeakerByName(speakerName);

            List<Mood> moods;
            if (speaker?.MoodAvatars == null || speaker.MoodAvatars.Count == 0)
                moods = new List<Mood> { Mood.Neutral };
            else
                moods = speaker.MoodAvatars.Select(m => m.mood).Distinct().ToList();

            if (moods.Count == 0) moods.Add(Mood.Neutral);

            if (_moodField == null)
                _moodField = new PopupField<Mood>("Mood", moods, 0);
            else
                _moodField.choices = moods;

            var desired = (_model != null) ? _model.speakerMood : Mood.Neutral;
            _moodField.SetValueWithoutNotify(moods.Contains(desired) ? desired : moods.First());
        }

        private void RebuildTextKeyChoices()
        {
            var keys = (_asset != null && _asset.AllTranslationKeys != null)
                ? _asset.AllTranslationKeys.ToList()
                : new List<string> { string.Empty };

            if (keys.Count == 0) keys.Add(string.Empty);
            _textKeyField.choices = keys;

            var desired = (_model != null && !string.IsNullOrEmpty(_model.textKey)) ? _model.textKey : keys.FirstOrDefault();
            _textKeyField.SetValueWithoutNotify(keys.Contains(desired) ? desired : keys.FirstOrDefault());
        }

        private void ApplyCommonValues()
        {
            if (_speakerField != null)
            {
                var choices = _speakerField.choices ?? new List<string> { string.Empty };
                var desiredSpeaker = string.IsNullOrEmpty(_model.speakerId) ? choices.FirstOrDefault() : _model.speakerId;
                _speakerField.SetValueWithoutNotify(choices.Contains(desiredSpeaker) ? desiredSpeaker : choices.FirstOrDefault());
            }

            var moodChoices = _moodField.choices ?? new List<Mood> { Mood.Neutral };
            _moodField.SetValueWithoutNotify(moodChoices.Contains(_model.speakerMood) ? _model.speakerMood : moodChoices.FirstOrDefault());

            var keyChoices = _textKeyField.choices ?? new List<string> { string.Empty };
            var desiredKey = string.IsNullOrEmpty(_model.textKey) ? keyChoices.FirstOrDefault() : _model.textKey;
            _textKeyField.SetValueWithoutNotify(keyChoices.Contains(desiredKey) ? desiredKey : keyChoices.FirstOrDefault());
        }

        // Callbacks communs
        private void OnSpeakerChanged(ChangeEvent<string> evt)
        {
            if (_asset == null || _model == null) return;
            Undo.RecordObject(_asset, "Edit Speaker");
            _model.speakerId = evt.newValue ?? string.Empty;
            RebuildMoodChoices();
            _model.speakerMood = _moodField.value;
            EditorUtility.SetDirty(_asset);
        }

        private void OnMoodChanged(ChangeEvent<Mood> evt)
        {
            if (_asset == null || _model == null) return;
            Undo.RecordObject(_asset, "Edit Speaker Mood");
            _model.speakerMood = evt.newValue;
            EditorUtility.SetDirty(_asset);
        }

        private void OnTextKeyChanged(ChangeEvent<string> evt)
        {
            if (_asset == null || _model == null) return;
            Undo.RecordObject(_asset, "Edit Text Key");
            _model.textKey = evt.newValue ?? string.Empty;
            EditorUtility.SetDirty(_asset);
        }
    }
}
