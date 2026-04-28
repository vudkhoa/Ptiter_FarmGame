using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Objects", menuName = "Data/Map/Objects")]
public class ObjectDatabaseSO : ScriptableObject
{
    public List<ObjectData> objects;
}

[Serializable]
public struct ObjectData
{
    public string name;
    public int ID;
    public Vector2 Size;
    public GameObject Prefab;
}
