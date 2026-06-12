using UnityEngine;

namespace V2.Food
{
    public class CustomerFoodClient : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer container;
        [SerializeField] private SpriteRenderer message;
        [SerializeField] private Sprite happy;

        public void Configure(FoodModel foodModel)
        {
            message.sprite = foodModel.foodSprite;
            Hide();
        }

        public void Hide()
        {
            container.enabled = false;
            message.enabled = false;
        }

        public void Show()
        {
            message.enabled = true;
            container.enabled = true;
        }

        public void Happy()
        {
            message.sprite = happy;
        }
    }
}