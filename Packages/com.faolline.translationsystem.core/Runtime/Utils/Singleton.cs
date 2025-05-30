using UnityEngine;

namespace com.faolline.translationsystem
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _quitting = false;

        protected virtual bool IsPersistent => false;

        public static T Instance
        {
            get
            {
                if (_quitting) return null;

                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();
                    if (_instance == null)
                    {
                        GameObject singletonGO = new GameObject(typeof(T).Name);
                        _instance = singletonGO.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;

            if (IsPersistent)
                DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit()
        {
            _quitting = true;
        }
    }

}