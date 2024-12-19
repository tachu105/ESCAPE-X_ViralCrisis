using R3;
using SFEscape.Runtime.Player;
using UnityEngine;

namespace SFEscape.Runtime.InteractableObjects
{
    public class MedicalBaseBehaviour : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private ParticleSystem smokeEffect;
        
        public bool IsDestroyable => false;
        private readonly ReactiveProperty<InteractableObjState> state = new(InteractableObjState.Accessible);
        public ReadOnlyReactiveProperty<InteractableObjState> State => state;
        public ItemType RequiredItem => ItemType.MedicalBox;
        public ItemType SupplyItem => ItemType.None;
        public PlayerCondition InteractableCondition => PlayerCondition.Infected;
        
        [Header("SE関連")]
        [SerializeField]
        private AudioSource seSource;
        [SerializeField]
        private AudioClip sprayClip;
        
        public string GetStateDisplayText(InteractableObjState keyState)
        {
            return keyState switch
            {
                InteractableObjState.Accessible => "治療",
                _ => ""
            };
        }

        public void Interact(GameObject interactor)
        {
            if (state.Value == InteractableObjState.Accessible)
            {
                smokeEffect.Play();
                seSource.PlayOneShot(sprayClip);
                interactor.GetComponent<HealthConditionHandler>()
                    .ChangeCondition(PlayerCondition.Normal);
            }
        }
    }
}