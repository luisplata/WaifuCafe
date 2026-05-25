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
        private List<StaffFront> staffPrefabs = new();

        private List<StaffFront> _staffInstances;

        [SerializeField] private Transform canvasParent;

        // Eventos útiles para otros sistemas
        public event Action<StaffFront, Customer> OnServiceCompleted;
        public event Action<StaffFront> OnStaffBecameBusy;
        public event Action<StaffFront> OnStaffBecameFree;

        public int GetStaffCount() => _staffInstances?.Count ?? 0;


        private void Start()
        {
            _staffInstances = new List<StaffFront>();
            foreach (var staffPrefab in staffPrefabs)
            {
                if (staffPrefab != null)
                {
                    var instance = Instantiate(staffPrefab);
                    instance.Configure(canvasParent, staffPrefab.GetStaff(), staffPrefab.GetIndex());
                    _staffInstances.Add(instance);
                }
            }
        }

        public StaffFront GetStaffByIndex(int index)
        {
            if (_staffInstances == null) return null;
            return _staffInstances.Find(sf => sf != null && sf.GetIndex() == index);
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

        // Maneja la simulación de servicio: marca ocupado, espera ServiceTime y libera
        private IEnumerator HandleServiceCoroutine(StaffFront staffFront, Customer customer)
        {
            if (staffFront == null) yield break;

            // Mark busy
            staffFront.MarkBusy();
            OnStaffBecameBusy?.Invoke(staffFront);

            float time = Mathf.Max(0f, staffFront.GetServiceTime());

            float elapsed = 0f;
            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                staffFront.UpdateBusyProgress(Mathf.Min(elapsed, time));
                yield return null;
            }

            if (time <= 0f)
            {
                staffFront.UpdateBusyProgress(0f);
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
    }
}