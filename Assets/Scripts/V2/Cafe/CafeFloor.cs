using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using V2.Board;
using V2.Helpers;
using WaifuCafe.Core;

namespace V2.Cafe
{
    /// <summary>
    /// Suelo del cafe del prototipo: 5 asientos visibles, spawner de pedidos y cableado
    /// de la entrega del tablero. Un pedido completo paga, libera el asiento y sale del
    /// libro. Sin expulsión todavia: con los 5 ocupados no entra nadie.
    /// </summary>
    public sealed class CafeFloor : MonoBehaviour
    {
        [Header("Asientos")]
        [SerializeField, Range(1, 5)] private int seatCount = 5;

        [Header("Spawner de pedidos")]
        [Tooltip("Tiempo de rampa (s): la dificultad normalizada 0..1 se completa aca")]
        [SerializeField] private float rampDuration = 180f;
        [Tooltip("X = dificultad 0..1 sobre rampDuration. Y = intervalo entre llegadas (s). " +
                 "Despues de X = 1 se queda en el ultimo valor.")]
        [SerializeField] private AnimationCurve arrivalCurve = DefaultArrivalCurve();
        [SerializeField, Range(1, 2)] private int orderPlatesMin = 1;
        [SerializeField, Range(1, 2)] private int orderPlatesMax = 2;
        [SerializeField, Range(1, 6)] private int orderUnitsMin = 3;
        [SerializeField, Range(1, 6)] private int orderUnitsMax = 6;
        [Tooltip("0 = semilla aleatoria")]
        [SerializeField] private int seed;

        [Header("Entrega y pago")]
        [SerializeField] private int coinsPerUnit = 1;
        [SerializeField] private float retireDelay = 0.35f;
        [SerializeField] private float flyTime = 0.32f;

        private const float ColumnCenterX = 6.7f;
        private const float RowTopY = 5.55f;
        private const float RowStep = 1.85f;
        private const int FlightPool = 8;

        private sealed class Seat
        {
            public Transform Root;
            public CustomerOrderBubble Bubble;
            public Order Order;
            public bool Retiring;
        }

        private Seat[] _seats;

        private BoardSession _session;
        private BoardView _view;
        private BoardLayoutMock _layout;
        private OverflowBankHud _bank;
        private OrderBook _book;

        private SpriteRenderer[] _flights;
        private Tween[] _flightTweens;
        private int _flightCursor;

        private System.Random _rng;
        private float _interval;
        private float _arrivalClock;
        private float _rampClock;
        private int _seq;
        private int _right = -1;

        /// <summary>Monedas pagadas en el dia (HUD completo llega en otro corte).</summary>
        public int Coins { get; private set; }

        public OrderBook Book => _book;

        /// <summary>Pedidos creados (diagnostico: sirve para probar que el reloj se pausa).</summary>
        public int Arrived => _seq;

        /// <summary>Segundos de rampa acumulados; solo avanza cuando el gameplay no esta bloqueado.</summary>
        public float DifficultySeconds => _rampClock;

        public float ArrivalInterval => _interval;

        private int Side => _right;
        private void Start()
        {
            _session = FindFirstObjectByType<BoardSession>();
            _view = _session != null ? _session.GetComponentInChildren<BoardView>() : null;
            _layout = FindFirstObjectByType<BoardLayoutMock>();
            _right = _layout != null && !_layout.IsRight ? 1 : -1;
            if (_layout != null) _layout.SideChanged += OnSideChanged;

            _rng = new System.Random(seed != 0 ? seed : System.Environment.TickCount);
            _book = new OrderBook();
            _interval = IntervalAt(0f);

            BuildSeats();
            _bank = gameObject.AddComponent<OverflowBankHud>();
            _bank.Build(Side);
            BuildFlights();

            if (_session != null) _session.Delivered += OnDelivered;
            TrySpawn();
            StartCoroutine(SpawnLoop());
        }

        private void OnDestroy()
        {
            if (_session != null) _session.Delivered -= OnDelivered;
            if (_layout != null) _layout.SideChanged -= OnSideChanged;
        }

        // ---- Spawner ----------------------------------------------------------------

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return null;

                // Un solo concepto: si el gameplay esta bloqueado (resolucion o cinematica)
                // el reloj de llegadas Y el de dificultad quedan congelados.
                if (GameplayLock.IsLocked) continue;

                float dt = Time.deltaTime;
                _arrivalClock += dt;
                _rampClock += dt;
                _interval = IntervalAt(_rampClock);
                if (_arrivalClock < _interval) continue;

                _arrivalClock -= _interval;
                TrySpawn();
            }
        }

        /// <summary>X = dificultad (Clamp01: X = 1 queda fija); Y = segundos entre llegadas.</summary>
        private float IntervalAt(float seconds)
        {
            if (arrivalCurve == null || arrivalCurve.length == 0) return 4f;
            float t = rampDuration > 0f ? Mathf.Clamp01(seconds / rampDuration) : 1f;
            return Mathf.Max(0.5f, arrivalCurve.Evaluate(t));
        }

        /// <summary>Curva por defecto: arranca tranquilo (9 s) y acelera hasta 4 s.</summary>
        private static AnimationCurve DefaultArrivalCurve() =>
            new AnimationCurve(new Keyframe(0f, 9f), new Keyframe(0.5f, 6f), new Keyframe(1f, 4f));

        private void TrySpawn()
        {
            if (GameplayLock.IsLocked) return;
            Seat seat = FreeSeat();
            if (seat == null) return;

            Order order = CreateOrder();
            Delivery auto = _book.Add(order);
            seat.Order = order;
            seat.Bubble.Show(order);

            foreach (OrderFill fill in auto.Used) seat.Bubble.Receive(fill.Dish, fill.Units);
            _bank.Refresh(_book);

            if (order.IsComplete) StartCoroutine(Retire(seat));
        }

        private Order CreateOrder()
        {
            int plates = _rng.Next(orderPlatesMin, orderPlatesMax + 1);
            int total = _rng.Next(orderUnitsMin, orderUnitsMax + 1);
            var pool = new List<Dish> { Dish.Desayuno, Dish.Almuerzo, Dish.Bebida, Dish.Postre, Dish.Snack };

            var needs = new (Dish Dish, int Units)[plates];
            int left = total;

            for (int i = 0; i < plates; i++)
            {
                int index = _rng.Next(pool.Count);
                Dish dish = pool[index];
                pool.RemoveAt(index);

                int remainingPlates = plates - i - 1;
                int units = remainingPlates == 0 ? left : _rng.Next(1, left - remainingPlates + 1);
                left -= units;
                needs[i] = (dish, units);
            }

            int seq = ++_seq;
            return new Order(seq, seq, needs);
        }

        // ---- Entrega ----------------------------------------------------------------

        private void OnDelivered(ResolveResult result)
        {
            Present(_book.Deliver(result));
        }

        /// <summary>
        /// Entrega directa de una skill (Luna): mismo libro, mismas burbujas, mismo pago.
        /// No pasa por el tablero, asi que no carga a la party ni dispara olas.
        /// </summary>
        public Delivery DeliverDirect(Dish dish, int units)
        {
            if (_book == null) return Delivery.Empty;
            return Present(_book.Deliver(dish, units));
        }

        private Delivery Present(Delivery delivery)
        {
            foreach (OrderFill fill in delivery.Used)
            {
                Seat seat = Find(fill.Order);
                if (seat == null) continue;
                seat.Bubble.Receive(fill.Dish, fill.Units);
                FlyDish(fill.Dish, seat);
            }

            foreach (Order order in delivery.Completed)
            {
                Seat seat = Find(order);
                if (seat != null) StartCoroutine(Retire(seat));
            }

            _bank.Refresh(_book);
            if (delivery.DiscardedTotal > 0) _bank.PlayDiscarded(delivery.Discarded);
            return delivery;
        }

        private IEnumerator Retire(Seat seat)
        {
            Order order = seat.Order;
            if (order == null || seat.Retiring) yield break;

            seat.Retiring = true;
            yield return new WaitForSeconds(retireDelay);
            seat.Bubble.Complete();

            int coins = Mathf.Max(1, TotalUnits(order)) * coinsPerUnit;
            Coins += coins;
            if (_view != null)
                _view.ShowFloatText(seat.Root.position + Vector3.up * 0.85f, "+" + coins,
                    new Color(0.96f, 0.86f, 0.42f), 1f);

            yield return new WaitForSeconds(0.32f);
            _book.Remove(order);
            seat.Order = null;
            seat.Retiring = false;
            seat.Bubble.Clear();
            _bank.Refresh(_book);

            yield return new WaitForSeconds(0.1f);
            TrySpawn();
        }

        private void FlyDish(Dish dish, Seat seat)
        {
            int index = _flightCursor++ % _flights.Length;
            SpriteRenderer sprite = _flights[index];
            _flightTweens[index].Stop();

            sprite.gameObject.SetActive(true);
            sprite.sprite = DishSprites.For(dish);
            sprite.color = _view != null ? _view.DishColor(dish) : Color.white;

            Vector3 from = _view != null ? _view.BoardCenter : transform.position;
            Vector3 to = seat.Root.position + Vector3.up * 0.15f;
            Vector3 mid = (from + to) * 0.5f + Vector3.up * 1.6f;

            sprite.transform.position = from;
            sprite.transform.localScale = Vector3.one * 0.9f;

            _flightTweens[index] = Tween.Custom(sprite, 0f, 1f, flyTime, (spark, k) =>
            {
                float t = (float)k;
                spark.transform.position = Vector3.Lerp(Vector3.Lerp(from, mid, t), Vector3.Lerp(mid, to, t), t);
                spark.transform.localScale = Vector3.one * (0.9f - 0.25f * t);
            }).OnComplete(() => sprite.gameObject.SetActive(false));
        }

        // ---- Layout -----------------------------------------------------------------

        private void BuildSeats()
        {
            _seats = new Seat[seatCount];
            var root = new GameObject("Seats").transform;
            root.SetParent(transform, false);

            for (int i = 0; i < seatCount; i++)
            {
                var go = new GameObject("Seat_" + (i + 1));
                go.transform.SetParent(root, false);
                go.transform.position = SeatPosition(i);

                var bubble = go.AddComponent<CustomerOrderBubble>();
                bubble.Build(Side);
                _seats[i] = new Seat { Root = go.transform, Bubble = bubble };
            }
        }

        private void BuildFlights()
        {
            var root = new GameObject("Flights").transform;
            root.SetParent(transform, false);
            _flights = new SpriteRenderer[FlightPool];
            _flightTweens = new Tween[FlightPool];

            for (int i = 0; i < FlightPool; i++)
            {
                var go = new GameObject("Flight_" + i);
                go.transform.SetParent(root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = DishSprites.For(Dish.Almuerzo);
                sr.sortingOrder = 12;
                go.SetActive(false);
                _flights[i] = sr;
            }
        }

        private void OnSideChanged()
        {
            _right = _layout != null && !_layout.IsRight ? 1 : -1;
            for (int i = 0; i < _seats.Length; i++)
            {
                if (_seats[i] == null) continue;
                _seats[i].Root.position = SeatPosition(i);
                _seats[i].Bubble.SetSide(Side);
            }

            if (_bank != null) _bank.SetSide(Side);
        }

        private Vector3 SeatPosition(int index) =>
            new Vector3(Side * ColumnCenterX, RowTopY - index * RowStep, 0f);

        // ---- Helpers ----------------------------------------------------------------

        private Seat FreeSeat()
        {
            foreach (Seat seat in _seats)
                if (seat.Order == null && !seat.Retiring) return seat;
            return null;
        }

        private Seat Find(Order order)
        {
            foreach (Seat seat in _seats)
                if (seat.Order == order) return seat;
            return null;
        }

        private static int TotalUnits(Order order)
        {
            int total = 0;
            foreach (Dish dish in order.Dishes) total += order.Required(dish);
            return total;
        }
    }
}
