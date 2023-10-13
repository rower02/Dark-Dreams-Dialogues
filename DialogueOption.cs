using System.Collections.Generic;
using UnityEngine;
namespace Scripts.Dialogues
{
    [System.Serializable]
    public class DialogueOption : IDialogueElement
    {
        
        public Message nextMessage;
        public int reputationModifier = 0;
        public int mentalityModifier = 0;
        public float maxTime;

        [HideInInspector] public Inventory.ItemDataHolder requiredItem;
        [HideInInspector] public int amount;

        public List<RequiredAction> requiredActions = new List<RequiredAction>();

        [Header("Voice Lines")]
        [HideInInspector] public string clipName;


        public override void PlayClip(AudioSource source)
        {
            if (clipName == null) return;
            AudioClip clip = Resources.Load<AudioClip>(clipName);
            source.PlayOneShot(clip);
        }
    }
}
