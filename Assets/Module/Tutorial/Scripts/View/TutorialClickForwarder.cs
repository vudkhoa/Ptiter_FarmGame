using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Module.Tutorial
{
    /// <summary>
    /// Invisible catcher sitting over the highlighted widget's clone. The clone is a dead copy and
    /// the dim swallows everything below it, so the player's tap has to be relayed by hand to the
    /// real widget underneath.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialClickForwarder : MonoBehaviour, IPointerClickHandler
    {
        private GameObject _target;

        public void Bind(GameObject target) => _target = target;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_target == null) return;

            // ExecuteHierarchy, not Execute: the anchor may sit on a child of the object that
            // actually carries the Button, which is how ObjectSelectScreen's rows are built.
            ExecuteEvents.ExecuteHierarchy(_target, eventData, ExecuteEvents.pointerClickHandler);
        }
    }
}
