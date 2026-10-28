namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Un cliente guionado del tutorial: aparece en <see cref="AtSecond"/> con líneas fijas.
    /// E5 lo consume; el core solo lo declara para que el RunConfig lo transporte.
    /// </summary>
    public readonly struct ScriptedOrder
    {
        public readonly float AtSecond;
        public readonly Dish[] Dishes;
        public readonly int[] Units;

        public ScriptedOrder(float atSecond, params (Dish Dish, int Units)[] lines)
        {
            AtSecond = atSecond;
            if (lines == null || lines.Length == 0)
            {
                Dishes = System.Array.Empty<Dish>();
                Units = System.Array.Empty<int>();
                return;
            }

            Dishes = new Dish[lines.Length];
            Units = new int[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                Dishes[i] = lines[i].Dish;
                Units[i] = lines[i].Units;
            }
        }

        public int LineCount => Dishes == null ? 0 : Dishes.Length;

        /// <summary>Construye el pedido real para el piso del café.</summary>
        public WaifuCafe.Core.Order ToOrder(int id, int seq)
        {
            int count = LineCount;
            var needs = new (WaifuCafe.Core.Dish Dish, int Units)[count];
            for (int i = 0; i < count; i++) needs[i] = (Dishes[i], Units[i]);
            return new WaifuCafe.Core.Order(id, seq, needs);
        }
    }
}
