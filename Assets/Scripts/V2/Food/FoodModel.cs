using System;
using UnityEngine;

namespace V2.Food
{
    [Serializable]
    public class FoodModel
    {
        public string foodName;
        public Sprite foodSprite;
        public float pointsToAttend;
        public float tiempoDePreparacion;
        public float tiempoDeConsumo;
        public FoodModelType foodModelType;
    }
}