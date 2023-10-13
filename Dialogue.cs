using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Dialogues
{
    [CreateAssetMenu(fileName = "Dialogue", menuName = "CubeLand Studio/Dialogues/Normal Dialogue")]
    public class Dialogue : ScriptableObject
    {
        public Message message;
    }

}
