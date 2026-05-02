using System.Collections.Generic;
using System;
using UnityEngine;

namespace Core.Module.Map
{
    [CreateAssetMenu(fileName = "Objects", menuName = "Data/Map/Objects")]
    public class ObjectDatabaseSO : ScriptableObject
    {
        public List<ObjectData> Objects;
    }

    [Serializable]
    public struct ObjectData
    {
        public string name;
        public int ID;
        public Vector2Int Size;
        public GameObject Prefab;
    }
}