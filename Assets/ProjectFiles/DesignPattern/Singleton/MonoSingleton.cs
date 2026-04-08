using UnityEngine;

namespace Main.DesignPattern
{
    /// <summary>
    /// Lazy MonoBehaviour singleton: tìm trong scene trước, không có thì tạo GameObject mới.
    /// Kế thừa và ghi đè <see cref="PersistAcrossScenes"/> nếu cần giữ qua load scene.
    /// </summary>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static bool HasInstance => _instance != null;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                if (!Application.isPlaying)
                {
                    return null;
                }

                _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
                if (_instance != null)
                {
                    return _instance;
                }

                var go = new GameObject(typeof(T).Name);
                _instance = go.AddComponent<T>();
                return _instance;
            }
        }

        /// <summary>Gọi DontDestroyOnLoad khi true (chỉ khi đây là instance hợp lệ).</summary>
        protected virtual bool PersistAcrossScenes => false;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (PersistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this as T)
            {
                _instance = null;
            }
        }
    }
}
