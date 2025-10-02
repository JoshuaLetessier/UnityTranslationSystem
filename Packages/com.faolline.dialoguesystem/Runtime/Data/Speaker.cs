using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    public enum Mood { Neutral, Happy, Sad, Angry, Surprised }

    [CreateAssetMenu(menuName = "Dialogue System/Speaker", fileName = "NewSpeaker")]
    public class Speaker : ScriptableObject
    {
        [SerializeField] private string speakerName = "New Speaker";
        [SerializeField] private Sprite defaultAvatar;

        [Serializable]
        public struct MoodAvatarPair
        {
            public Mood mood;
            public Sprite avatar;
        }

        [SerializeField] private List<MoodAvatarPair> moodAvatars = new();

        public string SpeakerName { get => speakerName; set => speakerName = value; }
        public Sprite Avatar { get => defaultAvatar; set => defaultAvatar = value; }

        // Expose les moods disponibles (lecture seule)
        public IReadOnlyList<MoodAvatarPair> MoodAvatars => moodAvatars;
        public IEnumerable<Mood> GetAvailableMoods()
        {
            // renvoie la liste des moods distincts présents
            var set = new HashSet<Mood>();
            foreach (var p in moodAvatars) set.Add(p.mood);
            if (set.Count == 0) set.Add(Mood.Neutral);
            return set;
        }
    }
}
