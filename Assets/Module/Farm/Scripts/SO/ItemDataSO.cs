using UnityEngine;

namespace Core.Module.Farm
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "GDD/Inventory/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        public string ItemId => name; 
        public string displayName;
        public Sprite icon;
    }
}
