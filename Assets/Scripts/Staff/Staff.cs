using System;

namespace Staff
{
    [Serializable]
    public class Staff
    {
        // Identificador dentro del pool (index)
        public int Index;

        // Tiempo que tarda en atender a un cliente (en segundos)
        public float ServiceTime = 5f;

        // Indica si el personal está ocupado atendiendo a alguien
        [NonSerialized] public bool IsBusy = false;

        // Puedes añadir más stats aquí si hacen falta (skill, speed, mood, etc.)

        // Método simple que indica si puede atender ahora mismo
        public bool CanAttend() => !IsBusy;
    }
}


