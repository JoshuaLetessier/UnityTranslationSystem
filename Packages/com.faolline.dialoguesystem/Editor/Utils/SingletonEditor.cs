// SingletonEditorWindow.cs  (Editor only)
#if UNITY_EDITOR
using UnityEditor;

namespace com.faolline.dialoguesystem
{
    public abstract class SingletonEditorWindow<T> : EditorWindow
        where T : EditorWindow
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = EditorWindow.GetWindow<T>();
                return _instance;
            }
        }
    }
}
#endif
