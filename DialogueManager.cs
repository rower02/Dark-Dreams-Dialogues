using System.Collections;
 using System.Collections.Generic;
 using System.Globalization;
 using System.IO;
 using Newtonsoft.Json;
 using Scripts.Inventory;
 using Scripts.Language;
 using Scripts.NPC;
 using Scripts.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace Scripts.Dialogues
{
    public class DialogueManager : MonoBehaviour, Openable
    {
        Dialogue dialogue;
        NPC.NPC npc;
        Player.Player player;
        public Message message;
        public bool isTalking;

        bool cancelTyping;
        bool isTyping;

        public float textSpeed = 1.5f;

        public static DialogueManager Instance { get; private set; }

        [Header("UI")]
        [SerializeField] GameObject dialougeUI;
        [SerializeField] Text dialogueText;
        [SerializeField] Button[] buttons;
        [SerializeField] Button byeButton;
        [SerializeField] GameObject playerGO;
        [SerializeField] GameObject npcUI;
        [SerializeField] Text reputationText;
        [SerializeField] Slider timeSlider;

        Coroutine typingCoroutine;

        public Dictionary<string, string> translations { get; set; }

        private string jsonPath;
        
        public bool IsOpened()
        {
            return isTalking;
        }
        
        [Header("Colors")]
        [SerializeField] Color normalColor;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(this);
                Debug.LogError("More than 1 DialogueManager instance!");
                return;
            }
        }

        private void Start()
        {
            
            if (LanguageManager.instance.en)
                jsonPath = Path.Combine(Application.dataPath, "translations_en.json");
            else
                jsonPath = Path.Combine(Application.dataPath, "translations_pl.json");
            
            dialougeUI.SetActive(false);
            npcUI.SetActive(false);
            Quest.QuestManager.Instance.questUI.SetActive(false);

            
            StreamReader fileReader = File.OpenText(jsonPath);
            JsonSerializer serializer = new JsonSerializer();
            translations = serializer.Deserialize<Dictionary<string, string>>(new JsonTextReader(fileReader));
        }

        Message GetMessage(Dialogue dialogue)
        {
            LilithDialogue lilith = dialogue as LilithDialogue;
            if (lilith != null)
            {
                // check variables
                // todo clean this shitty code pls
                if (FindObjectOfType<Amanda>().boxOff && FindObjectOfType<Boy>().gaveSweet)
                {
                    return message = lilith.amandaBoy;
                }
                else if (FindObjectOfType<Amanda>().boxOff)
                {
                    return message = lilith.amanda;
                }
                else if (FindObjectOfType<Boy>().gaveSweet)
                {
                    return message = lilith.boy;
                }
                else return message = lilith.message;
            }

            return dialogue.message;
        }
        
        public void StartDialogue(Dialogue dialogue, NPC.NPC npc, Player.Player player)
        {
            if (isTalking) return;
            this.npc = npc;
            this.player = player;
            if (!this.npc.dialogueCreated) this.dialogue = Instantiate(dialogue);


            message = GetMessage(this.dialogue);
            timeSlider.gameObject.SetActive(false);
            if (message.used) return;
            dialougeUI.SetActive(true);
            UpdateUI();
            Player.PlayerController.instance.enabled = false;
            Player.PlayerAnimations.Instance.enabled = false;
            playerGO.GetComponent<Sounds.Footsteps>().enabled = false;
            isTalking = true;
            if (message.clipName != "")
                message.PlayClip(player.GetComponent<AudioSource>());

            //if (message.clip != null)
            //{
            //    player.GetComponent<AudioSource>().PlayOneShot(message.clip);
            //}

            if (message.oneTimeToUse)
            {
                message.used = true;
            }


        }

        public void SelectDialogue(int index)
        {
            EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
        }

        public void IncreaseNPCReputation(int value)
        {
            if (value == 0) return;
            npc.reputation += value;
            reputationText.text = npc.name + " likes you";
            npcUI.SetActive(true);
            Invoke("HideReputation", 4f);
        }

        public void DecreaseNPCReputation(int value)
        {
            if (value == 0) return;
            npc.reputation += value;
            reputationText.text = npc.name + " doesn't like you";
            npcUI.SetActive(true);
            Invoke("HideReputation", 4f);
        }

        public void AddQuest(Quest.Quest quest)
        {
            if (quest == null) return;
            if (quest.isActivated) return;
            message.quest = quest;
            Quest.QuestManager.Instance.GiveQuest(quest);
            Invoke("HideQuest", 4f);
        }

        public void GiveItem(Item item)
        {
            if (item.amount < 1) return;
            Debug.Log(item.data.name + " " + item.amount);
            Debug.Log("Player: " + player.name);
            Debug.Log("Inventory: " + player.inventory.entityInventory);
            if (item.data.equipable) player.equipmentInventory.entityInventory.AddItem(item.data, item.amount);
            else
                player.inventory.entityInventory.AddItem(item.data, item.amount);
        }

        public void HideQuest()
        {
            Quest.QuestManager.Instance.questUI.SetActive(false);
        }

        void HideReputation()
        {
            npcUI.SetActive(false);
        }

        /// <summary>
        /// This function is called after every change of dialogue line
        /// </summary>
        void StateUpdate()
        {
            if (message.methodName != "")
                message.InvokeMethod();
            AddQuest(message.quest);
            if (message.amountToGive > 0)
            {
                Debug.Log(message.itemToGive.data.name);
                GiveItem(new Item(message.itemToGive.data, message.amountToGive));
            }

            if (message.nextDialogue != null)
            {
                EndDialogue();
                StartDialogue(message.nextDialogue, message.nextNPC, Player.Player.main);
            }
            if (message.textIdentifier.Length == 0) EndDialogue();
        }

        private void Update()
        {
            if (timeSlider.value > 0)
            {
                timeSlider.value -= Time.deltaTime;
            }
            if (timeSlider.value <= 0 && timeSlider.gameObject.activeInHierarchy)
            {
                int random = Random.Range(0, message.optionArray.Count);
                NextDialogue(random);
                timeSlider.gameObject.SetActive(false);
            }
        }
        
        bool CheckIfOptionAvailable(DialogueOption option)
        {
            foreach (RequiredAction requiredAction in option.requiredActions)
            {
                if (!CheckIfActionCompleted(requiredAction))
                {
                    return false;
                }
            }
            return true;
        }
        
        bool CheckIfActionCompleted(RequiredAction requiredAction)
        {

            if (requiredAction == null) return true;
            if (requiredAction.actionType == ActionType.HaveSweet)
            {
                ItemDataHolder sweetData = Resources.Load("Items/Sweet") as ItemDataHolder;
                ItemData data = sweetData.data;
                return Player.Player.main.inventory.entityInventory.inventory.HasItem(data);
                //itemslist ??
            }


            if (requiredAction.actionType == ActionType.WoodyItems)
            {
               // ItemDataHolder 
            }
            
            return true;
        }
        
        void ExecuteRequiredAction(RequiredAction requiredAction)
        {
           
            if (requiredAction.actionType == ActionType.HaveSweet)
            {
                Debug.Log("I'm removing sweet");
                ItemDataHolder sweetData = Resources.Load("Items/Sweet") as ItemDataHolder;
                ItemData data = sweetData.data;
                Player.Player.main.inventory.entityInventory.RemoveItem(ItemsList.instance.sweet, 1);
            }
        }

        
        
        void UpdateUI()
        {
            string msg = translations[message.textIdentifier];
            dialogueText.text = msg;
            
            for (int i = 0; i < buttons.Length; i++)
            {
                int j = 0;
                if (i < message.optionArray.Count && !message.optionArray[i].used)
                {
                    j++;
                    if (message.optionArray[i].requiredActions.Count > 0)
                    {
                        bool optionAvailable = CheckIfOptionAvailable(message.optionArray[i]);
                        buttons[i].gameObject.SetActive(optionAvailable);
                    }
                    else
                    {
                        buttons[i].gameObject.SetActive(true);
                    }
                    byeButton.gameObject.SetActive(false);
                    buttons[i].GetComponentInChildren<Text>().text = translations[message.GetOptionContent(i)];
                    Text text = buttons[i].transform.Find("Text").GetComponent<Text>();

                    if (message.optionArray[i].messageType == DialogueOption.MessageType.Neutral || message.messageType == Message.MessageType.Neutral)
                    {
                        text.color = normalColor;
                    }
                    if (message.optionArray[i].messageType == DialogueOption.MessageType.Peaceful || message.messageType == Message.MessageType.Peaceful)
                    {
                        text.color = Color.green;
                    }
                    if (message.optionArray[i].messageType == DialogueOption.MessageType.Aggresive || message.messageType == Message.MessageType.Aggresive)
                    {
                        text.color = Color.red;
                    }
                }
                else
                {
                    byeButton.gameObject.SetActive(true);
                    buttons[i].gameObject.SetActive(false);
                }
            }
        }

        void NextDialogue(int index)
        {
            DialogueOption option = message.optionArray[index];
            timeSlider.gameObject.SetActive(false);
            option.used = true;

            if (option.clip != null)
            {
                option.PlayClip(player.GetComponent<AudioSource>());
            }

            if (option.amount > 0)
            {
                Player.Player.main.inventory.entityInventory.RemoveItem(option.requiredItem.data, option.amount);
            }

            if (option.maxTime > 0)
            {
                timeSlider.gameObject.SetActive(true);
                timeSlider.maxValue = option.maxTime;
                timeSlider.value = option.maxTime;
            }

            if (option.reputationModifier > 0) IncreaseNPCReputation(option.reputationModifier);
            else if (option.reputationModifier < 0) DecreaseNPCReputation(option.reputationModifier);

            if (message.optionArray.Count == 0)
            {
                EndDialogue();
                return;
            }
            if (message.showChoices)
            {
                if (index >= 0 && index <= message.optionArray.Count)
                {
                    message = message.GetOptionMessage(index);
                    StateUpdate();
                    UpdateUI();
                }
            }
            else
            {
                message = message.GetOptionMessage(0);
                StateUpdate();
                UpdateUI();
            }
        }

        public void EndDialogue()
        {
            dialougeUI.SetActive(false);
            playerGO.GetComponent<PlayerController>().enabled = true;
            playerGO.GetComponent<PlayerAnimations>().enabled = true;
            playerGO.GetComponent<Sounds.Footsteps>().enabled = false;
            isTalking = false;
        }

        public void OnButtonClick(int index)
        {
            //   if (player.GetComponent<AudioSource>().isPlaying) return;
            
            
            Debug.Log(message.optionArray[index].textIdentifier);
            
            if (message.optionArray[index].requiredActions != null)
            {
                foreach (var action in message.optionArray[index].requiredActions)
                    ExecuteRequiredAction(action);
            }
            
            dialogueText.text = "";
            NextDialogue(index);

            
            
        }

    }
}
