namespace MieMieFrameWork
{
    using UnityEngine;

    /// <summary>
    /// Mono 单例基类 主工程与引用本程序集的包均可继承
    /// </summary>
    public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        private static readonly object locked = new();
        public static T Instance { get; private set; }

        protected virtual bool DontDestroyOnLoadEnabled => false;

        protected virtual void Awake()
        {
            lock (locked)
            {
                if (Instance != null)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this as T;

                if (DontDestroyOnLoadEnabled)
                {
                    // 提升到根节点 避免子物体 DontDestroyOnLoad 报错
                    if (transform.parent != null)
                        transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
