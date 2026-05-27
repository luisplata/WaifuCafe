using UnityEngine;

namespace GameManager
{
    public class RegardsManager : MonoBehaviour
    {
        [SerializeField] private float gold;

        public void AddGold(float amount)
        {
            gold += amount;
        }

        public void ResetGold()
        {
            gold = 0;
        }

        public void LoseGold(float amount)
        {
            gold -= amount;
        }

        public float GetGold()
        {
            return gold;
        }
    }
}