using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    private static object _lock = new object();

    public static T Instance
    {
        get
        {
            lock(_lock)
            {
                if (_instance == null)
                {
                    _instance = (T) FindObjectOfType<T>();

                    if (FindObjectsOfType<T>().Length > 1)
                    {
                        Debug.LogError("[Singleton] Something went really wrong "
                            + " - there should never be more than 1 singleton!"
                            + " Reopening the scene might fix it.");
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(Singleton)" + typeof(T).ToString();
                    }
                }
                return _instance;
            }
        }
    }
}