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
        [Header("Staff Settings")]
        [SerializeField] private List<StaffFront> staffFronts = new List<StaffFront>();

        // Eventos útiles para otros sistemas
        public event Action<StaffFront, Customer> OnServiceCompleted;
        public event Action<StaffFront> OnStaffBecameBusy;
        public event Action<StaffFront> OnStaffBecameFree;

        public int GetStaffCount() => staffFronts?.Count ?? 0;

        public StaffFront GetStaffByIndex(int index)
        {
            if (staffFronts == null) return null;
            return staffFronts.Find(sf => sf != null && sf.GetIndex() == index);
        }

        // Devuelve el primer staff libre o null
        public StaffFront GetAvailableStaff()
        {
            if (staffFronts == null) return null;
            foreach (var sf in staffFronts)
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

        // Maneja la simulación de servicio: marca ocupado, espera ServiceTime y libera
        private IEnumerator HandleServiceCoroutine(StaffFront staffFront, Customer customer)
        {
            if (staffFront == null) yield break;

            // Mark busy
            staffFront.MarkBusy();
            OnStaffBecameBusy?.Invoke(staffFront);

            float time = Mathf.Max(0f, staffFront.GetServiceTime());
            // Espera el tiempo de servicio
            yield return new WaitForSeconds(time);

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

