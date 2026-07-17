using UnityEngine;

namespace Core.Module.Farm
{
    [CreateAssetMenu(fileName = "NewBoosterItem", menuName = "GDD/Inventory/Booster Item Data")]
    public class BoosterItemDataSO : ItemDataSO
    {
        [Header("Booster Attributes")]
        public float boostAmountSec; // Số giây rút ngắn tiến trình sinh trưởng
    }
}
