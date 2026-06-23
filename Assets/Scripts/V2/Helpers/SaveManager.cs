using System;
using UnityEngine;

namespace V2.Helpers
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsShowTutorial()
        {
            return TutorialProgress.IsCompleted;
        }
    }
}