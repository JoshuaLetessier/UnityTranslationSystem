using UnityEngine;

namespace com.faolline.dialoguesystem
{
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
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

    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static readonly object _lock = new object();
        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                    return _instance;
                }
            }
        }
        protected Singleton() { }
    }
}