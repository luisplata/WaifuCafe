using UnityEngine;
using V2.Staff.Infra;

namespace Staff
{
    [CreateAssetMenu(fileName = "Staff", menuName = "ScriptableObjects/Staff", order = 1)]
    public class StaffFrontSo : ScriptableObject
    {
        [SerializeField, InterfaceType(typeof(IStaffMediator))]
        private Object maidObject;
        
        private IStaffMediator Maid => maidObject as IStaffMediator;

        public StaffMediatorComponent GetPrefab()
        {
            return Maid  as StaffMediatorComponent;
        }
    }
}