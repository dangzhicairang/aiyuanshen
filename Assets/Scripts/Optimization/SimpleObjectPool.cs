using System.Collections.Generic;
using UnityEngine;

namespace GachaDemo.Optimization
{
    public sealed class SimpleObjectPool
    {
        private readonly Stack<GameObject> _pool = new Stack<GameObject>();
        private readonly GameObject _prefab;
        private readonly Transform _root;

        public SimpleObjectPool(GameObject prefab, Transform root, int preload)
        {
            _prefab = prefab;
            _root = root;
            for (var i = 0; i < preload; i++)
            {
                var instance = Create();
                instance.SetActive(false);
                _pool.Push(instance);
            }
        }

        public GameObject Get()
        {
            if (_pool.Count == 0)
            {
                return Create();
            }

            var item = _pool.Pop();
            item.SetActive(true);
            return item;
        }

        public void Release(GameObject item)
        {
            item.SetActive(false);
            item.transform.SetParent(_root, false);
            _pool.Push(item);
        }

        private GameObject Create()
        {
            return Object.Instantiate(_prefab, _root);
        }
    }
}
