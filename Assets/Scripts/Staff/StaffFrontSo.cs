using UnityEngine;

namespace Staff
{
    [CreateAssetMenu(fileName = "Staff", menuName = "ScriptableObjects/Staff", order = 1)]
    public class StaffFrontSo : ScriptableObject
    {
        [SerializeField, InterfaceType(typeof(IStaffMediator))]
        private Object maidObject;

        public GameObject GetPrefab()
        {
            return maidObject as GameObject;
        }
    }
}