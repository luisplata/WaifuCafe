using System;

namespace Customers
{
    [Serializable]
    public class Customer
    {
        public CustomerType Type;
        public float Patience;
        public int Reward;
        
        [NonSerialized] public float WaitTime = 0f;
    }
}