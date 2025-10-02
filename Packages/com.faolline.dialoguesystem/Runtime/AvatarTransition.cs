using System.Collections;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    /// <summary>
    /// Transition "pluggable" pour les avatars. 
    /// Surclasse si tu veux animer (fade in/out, slide, etc).
    /// </summary>
    public abstract class AvatarTransition : ScriptableObject
    {
        /// <summary>Appelé quand l’avatar courant passe en slot "previous". Par défaut: instantané.</summary>
        public virtual IEnumerator DemoteToPrevious(GameObject current, Transform previousRoot)
        {
            yield break; // fait rien → l’adapter fera le reparent instantané
        }

        /// <summary>Appelé juste avant la destruction d’un avatar. Par défaut: rien.</summary>
        public virtual IEnumerator Despawn(GameObject go)
        {
            yield break; // pas d’attente → l’adapter détruit tout de suite après
        }

        /// <summary>Appelé juste après l’Instantiate du nouvel avatar courant. Par défaut: rien.</summary>
        public virtual IEnumerator Spawn(GameObject instance)
        {
            yield break; // pas d’attente → avatar déjà visible
        }
    }

    /// <summary>Implémentation "no-op" (instantané).</summary>
    [CreateAssetMenu(menuName = "Dialogue/Avatar Transition/Instant")]
    public class InstantAvatarTransition : AvatarTransition
    { }
}
