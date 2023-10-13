using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.UI;
using Scripts.Dialogues;
using System.Reflection;

namespace Scripts.Dialogues
{
    [System.Serializable]
    public class Message : IDialogueElement
    {

        public List<DialogueOption> optionArray = new List<DialogueOption>();
        public NPC.NPC nextNPC;
        [HideInInspector] public bool showChoices = true;
     //   public int repuetationModifier = 0;
     //   public int mentalityModifier = 0;
        public Quest.Quest quest;
        public Inventory.ItemDataHolder itemToGive;
        public int amountToGive;
        public bool oneTimeToUse = false;

        [Header("Actions")]
        public string methodName;
        public string objectName;
        public string componentName;

        [Header("Voice Lines")]
        [HideInInspector] public string clipName;

        public void InvokeMethod()
        {
            Debug.Log("invokuje metode");
            UnityEngine.Object target = Resources.Load(objectName);

            GameObject obj = target as GameObject;

            Component targetComponent = obj.GetComponent(componentName);

            Type componentType = targetComponent.GetType();

            MethodInfo info = componentType.GetMethod(methodName);
            if (info != null) info.Invoke(targetComponent, null);
            else Debug.Log("zle podana funkcja, Target: " + target.name);
        }

        public void PlayClip(AudioSource source)
        {
            if (clipName == null) return;
            AudioClip clip = Resources.Load<AudioClip>(clipName);
            source.PlayOneShot(clip);
        }

        public string GetOptionContent(int index)
        {
            return optionArray[index].textIdentifier;
        }

        public Message GetOptionMessage(int index)
        {
            return optionArray[index].nextMessage;
        }
    }
}
