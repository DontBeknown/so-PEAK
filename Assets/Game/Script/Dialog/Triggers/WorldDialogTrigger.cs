using UnityEngine;
using Game.Core.DI;

namespace Game.Dialog.Triggers
{
    [RequireComponent(typeof(Collider))]
    public class WorldDialogTrigger : MonoBehaviour
    {
        [SerializeField] private DialogData dialogData;
        [SerializeField] private bool triggerOnce = true;

        private bool _wasTriggered;

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null)
            {
                c.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_wasTriggered && triggerOnce)
                return;

            if (!other.CompareTag("Player"))
                return;

            if (dialogData == null)
                return;

            var manager = ServiceContainer.Instance.TryGet<IDialogManager>();
            if (manager == null)
                return;

            if (manager.HasTriggered(dialogData.dialogId))
                return;

            manager.StartDialog(dialogData);
            _wasTriggered = true;
        }
    }
}
