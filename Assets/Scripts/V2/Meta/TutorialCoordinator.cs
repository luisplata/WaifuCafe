using System;
using System.Collections.Generic;
using UnityEngine;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>Máquina de estados del tutorial persistida en el perfil.</summary>
    public interface ITutorialFlow
    {
        TutorialStep Step { get; }
        void Advance(TutorialStep next);
    }

    /// <summary>
    /// Conduce el tutorial completo en el menú: regalo → tour del manager → run guionada →
    /// recompensa → Done. Cada transición se guarda en <c>waifu_profile_v1</c>; al volver
    /// con el paso ya avanzado continúa donde quedó. Con el paso Done no vuelve a mostrar
    /// nada, así que reabrir el menú no repite el tutorial. Un reset del perfil vuelve a
    /// Gift y el regalo se muestra de nuevo.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class TutorialCoordinator : MonoBehaviour, ITutorialFlow
    {
        [SerializeField] private Transform canvasRoot;
        [SerializeField] private StarterConfigSO starterConfig;
        [SerializeField] private GuideSO guide;
        [SerializeField] private GiftCoordinator giftCoordinator;

        private IProfileStore _profile;
        private IRunLauncher _launcher;
        private GuidePanel _guidePanel;
        private bool _busy;
        private bool _launching;

        public TutorialStep Step =>
            _profile != null && _profile.Current != null ? _profile.Current.tutorialStep : TutorialStep.Gift;

        private Transform Canvas => canvasRoot != null ? canvasRoot : transform;

        private void Start()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || !root.IsInitialized)
            {
                Debug.LogError("[TutorialCoordinator] No hay GameRoot inicializado en MetaMenu.");
                return;
            }

            _profile = root.Profile;
            _launcher = root;

            if (_profile == null)
            {
                Debug.LogError("[TutorialCoordinator] GameRoot sin perfil; tutorial inerte.");
                return;
            }

            _guidePanel = new GuidePanel(Canvas);
            _profile.Changed += OnProfileChanged;
            Resume();
        }

        private void OnDestroy()
        {
            if (_profile != null) _profile.Changed -= OnProfileChanged;
            _guidePanel?.Hide();
        }

        private void OnProfileChanged()
        {
            if (!_busy) Resume();
        }

        /// <summary>Guarda la transición. Un paso repetido no vuelve a escribir.</summary>
        public void Advance(TutorialStep next)
        {
            if (_profile == null) return;

            PlayerProfile profile = _profile.Current;
            if (profile == null || profile.tutorialStep == next) return;

            profile.tutorialStep = next;
            _profile.Save();
        }

        private void Resume()
        {
            if (_profile == null || _busy) return;

            PlayerProfile profile = _profile.Current;
            if (profile == null) return;

            switch (profile.tutorialStep)
            {
                case TutorialStep.Gift:
                    if (profile.starterClaimed)
                    {
                        Advance(TutorialStep.MenuTour);
                        return;
                    }

                    PlayGift();
                    break;

                case TutorialStep.MenuTour:
                    PlayTour();
                    break;

                case TutorialStep.ScriptedRun:
                    LaunchTutorialRun();
                    break;

                case TutorialStep.Reward:
                    PlayReward();
                    break;

                case TutorialStep.Done:
                    _guidePanel?.Hide();
                    break;
            }
        }

        private void PlayGift()
        {
            if (giftCoordinator == null)
            {
                Debug.LogError("[TutorialCoordinator] Falta GiftCoordinator; no se puede mostrar el regalo.");
                Advance(TutorialStep.MenuTour);
                return;
            }

            _busy = true;
            giftCoordinator.PlayGift(_profile, () =>
            {
                _busy = false;
                Resume();
            });
        }

        private void PlayTour()
        {
            if (guide == null || _guidePanel == null)
            {
                Debug.LogWarning("[TutorialCoordinator] Sin GuideSO; se salta el tour.");
                Advance(TutorialStep.ScriptedRun);
                return;
            }

            _busy = true;
            _guidePanel.Show(guide, () =>
            {
                _busy = false;
                Advance(TutorialStep.ScriptedRun);
            });
        }

        private void LaunchTutorialRun()
        {
            if (_launching || _launcher == null) return;

            _launching = true;
            _busy = true;

            RunConfig config = TutorialFlow.CreateRun(StarterParty());
            _launcher.Launch(new RunRequest(config));
        }

        private void PlayReward()
        {
            _busy = true;
            GrantReward();

            if (giftCoordinator == null)
            {
                _busy = false;
                Advance(TutorialStep.Done);
                return;
            }

            giftCoordinator.PlayReward(() =>
            {
                _busy = false;
                Advance(TutorialStep.Done);
            });
        }

        /// <summary>Hana 1★ + Yuki 2★, idempotente: si ya están, no duplica ni baja estrellas.</summary>
        private void GrantReward()
        {
            if (_profile == null || starterConfig == null) return;

            PlayerProfile profile = _profile.Current;
            if (profile == null) return;

            if (GiftCoordinator.Grant(profile, starterConfig.Reward))
                _profile.Save();
        }

        /// <summary>Party de la run guionada: los cuatro starters del regalo.</summary>
        private List<string> StarterParty()
        {
            var ids = new List<string>(4);

            if (starterConfig != null)
            {
                foreach (StarterEntryConfig entry in starterConfig.Gift)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
                    ids.Add(entry.Id);
                    if (ids.Count == 4) break;
                }
            }

            if (ids.Count == 0)
            {
                ids.Add("airi");
                ids.Add("neko");
                ids.Add("luna");
                ids.Add("rika");
            }

            return ids;
        }
    }
}
