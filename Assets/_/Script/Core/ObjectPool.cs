using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int prewarm = 10;

    readonly Queue<GameObject> _pool = new Queue<GameObject>();

    void Awake()
    {
        for (int i = 0; i < prewarm; i++)
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            _pool.Enqueue(go);
        }
    }

    public GameObject Get()
    {
        if (_pool.Count == 0)
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            _pool.Enqueue(go);
        }
        var obj = _pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject go)
    {
        go.SetActive(false);
        _pool.Enqueue(go);
    }
}
