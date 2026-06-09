using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Customers;

namespace Staff
{
    /// <summary>
    /// Administra el pool de staff disponibles y ofrece métodos para asignar clientes.
    /// Cada staff solo puede atender un cliente a la vez.
    /// </summary>
    public class StaffManager : MonoBehaviour
    {
        [Header("Staff Settings")] [SerializeField]
        private List<StaffFrontSo> staffPrefabs = new();
        
        [SerializeField] private List<GameObject> staffPositions;
        private int _staffIndex = 0;

        private List<StaffFront> _staffInstances;

        private bool _configured;

        // Eventos útiles para otros sistemas
        public event Action<StaffFront, Customer> OnServiceCompleted;
        public event Action<StaffFront> OnStaffBecameBusy;
        public event Action<StaffFront> OnStaffBecameFree;

        public int GetStaffCount() => _staffInstances?.Count ?? 0;

        private void Start()
        {
            // Initialization is deferred to Configure() to allow GameManager to control startup timing.
            // Si Configure() ya corrió antes de este Start, no pisar la lista configurada.
            _staffInstances ??= new List<StaffFront>();
        }

        // Inicializa/Configura el pool de staff. Idempotente: si ya hay instancias, las reemplaza.
        public void Configure()
        {
            // Clean previous instances if any
            if (_staffInstances != null && _staffInstances.Count > 0)
            {
                foreach (var inst in _staffInstances)
                {
                    if (inst != null) Destroy(inst.gameObject);
                }

                _staffInstances.Clear();
            }

            _staffInstances = new List<StaffFront>();
            foreach (var staffPrefab in staffPrefabs)
            {
                if (staffPrefab != null)
                {
                    GameObject prefab = staffPrefab.GetPrefab();
                    if (prefab == null) continue;

                    GameObject go = Instantiate(prefab);
                    StaffFront staffFront = go.GetComponent<StaffFront>();
                    if (staffFront == null)
                    {
                        Debug.LogWarning($"[StaffManager] Prefab {prefab.name} lacks StaffFront component");
                        Destroy(go);
                        continue;
                    }

                    // Posicionar en el siguiente slot disponible
                    GameObject staffPosition = staffPositions[_staffIndex % staffPositions.Count];
                    go.transform.position = staffPosition.transform.position;

                    // Asignar índice al modelo Staff embebido en el prefab
                    Staff staff = staffFront.GetStaff();
                    if (staff != null)
                    {
                        staff.Index = _staffIndex;
                    }

                    _staffIndex++;
                    _staffInstances.Add(staffFront);
                }
            }

            _configured = true;
        }

        public StaffFront GetStaffByIndex(int index)
        {
            if (_staffInstances == null) return null;
            return _staffInstances.Find(sf => sf != null && sf.GetStaff() != null && sf.GetStaff().Index == index);
        }

        // Devuelve el primer staff libre o null
        public StaffFront GetAvailableStaff()
        {
            if (_staffInstances == null) return null;
            foreach (var sf in _staffInstances)
            {
                if (sf != null && sf.CanAttend()) return sf;
            }

            return null;
        }

        // Devuelve el index del primer staff libre o -1 si ninguno
        public int GetAvailableStaffIndex()
        {
            var sf = GetAvailableStaff();
            return sf != null ? sf.GetIndex() : -1;
        }

        // Intenta asignar un cliente a un staff libre. Devuelve true si se asignó.
        public bool TryAssignCustomer(Customer customer)
        {
            if (customer == null) return false;
            var sf = GetAvailableStaff();
            if (sf == null) return false;

            StartCoroutine(HandleServiceCoroutine(sf, customer));
            return true;
        }

        // Intenta asignar un cliente a un staff específico por índice.
        public bool TryAssignCustomerToStaff(Customer customer, int staffIndex)
        {
            if (customer == null) return false;

            var sf = GetStaffByIndex(staffIndex);
            if (sf == null || !sf.CanAttend()) return false;

            StartCoroutine(HandleServiceCoroutine(sf, customer));
            return true;
        }

        // Maneja la simulación de servicio: marca ocupado, espera el ciclo completo de servicio y libera
        private IEnumerator HandleServiceCoroutine(StaffFront staffFront, Customer customer)
        {
            if (staffFront == null) yield break;

            Staff staff = staffFront.GetStaff();
            if (staff == null) yield break;

            // Iniciar la máquina de estados del staff
            // Esto inicia AtendiendoCliente (2s) → PreparandoPedido (ServiceTime) → LlevandoPedido (2s) → EnEspera
            bool started = staff.CommandAttend();
            if (!started)
            {
                // Si no pudo iniciar, no continuar la corutina para evitar bloqueo.
                yield break;
            }

            // Mark busy - controla la interactividad del drag/drop
            staffFront.MarkBusy();
            OnStaffBecameBusy?.Invoke(staffFront);

            // Esperar a que el staff complete TODO el ciclo de servicio
            // La máquina de estados avanza automáticamente cada frame via UpdatePhase()
            // Solo esperamos a que vuelva a CanAttend() (es decir, EnEspera fase)
            while (!staff.CanAttend())
            {
                yield return null;
            }

            // Liberar
            staffFront.MarkFree();
            OnStaffBecameFree?.Invoke(staffFront);
            OnServiceCompleted?.Invoke(staffFront, customer);
        }

        // Opcional: forzar liberar o ocupar por index
        public void MarkStaffBusyByIndex(int index)
        {
            var sf = GetStaffByIndex(index);
            sf?.MarkBusy();
            if (sf != null) OnStaffBecameBusy?.Invoke(sf);
        }

        public void MarkStaffFreeByIndex(int index)
        {
            var sf = GetStaffByIndex(index);
            sf?.MarkFree();
            if (sf != null) OnStaffBecameFree?.Invoke(sf);
        }

        // Limpia el estado de todos los staff: detiene corutinas pendientes y los pone en EnEspera.
        // Útil al terminar la run (GameOver) para evitar que queden ocupados y puedan seguir siendo usados.
        public void ClearAllStaff()
        {
            // Detener cualquier corutina de servicio en curso
            try
            {
                StopAllCoroutines();
            }
            catch (Exception)
            {
                // StopAllCoroutines puede lanzar si el objeto está siendo destruido; ignorar en ese caso.
            }

            if (_staffInstances == null) return;

            foreach (var sf in _staffInstances)
            {
                if (sf == null) continue;

                Staff staff = sf.GetStaff();
                if (staff != null)
                {
                    // Forzar fase EnEspera para restablecer estado interno
                    staff.StartPhase(StateMachines.StaffPhase.EnEspera);
                }

                // Asegurar representación visual e interactividad
                sf.MarkFree();
                OnStaffBecameFree?.Invoke(sf);
            }
        }

        void Update()
        {
            // Bloquear Update hasta que Configure() sea llamado por GameManager
            if (!_configured) return;

            float dt = Time.deltaTime;
            foreach (var s in _staffInstances)
            {
                if (s == null) continue;

                Staff staff = s.GetStaff();
                if (staff == null) continue;

                staff.UpdatePhase(dt);
            }
        }
    }
}