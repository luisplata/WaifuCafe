using System;
using System.Collections.Generic;
using UnityEngine;
using V2.Waifu;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Persistencia del perfil en PlayerPrefs (<c>waifu_profile_v1</c>). Carga perezosa,
    /// sanea contra el catálogo y re-guarda si el documento cambió. No toca las claves
    /// legacy de SaveGame/SaveManager/TutorialProgress.
    /// </summary>
    public sealed class ProfileService : IProfileStore
    {
        public const string DefaultPrefsKey = "waifu_profile_v1";

        private readonly IWaifuCatalog _catalog;
        private readonly IContractCatalog _contracts;
        private readonly string _key;

        private PlayerProfile _current;

        public ProfileService(IWaifuCatalog catalog, IContractCatalog contracts = null,
            string prefsKey = DefaultPrefsKey)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _contracts = contracts;
            if (string.IsNullOrWhiteSpace(prefsKey)) throw new ArgumentException("prefsKey vacio", nameof(prefsKey));
            _key = prefsKey;
        }

        public event Action Changed;

        public PlayerProfile Current
        {
            get
            {
                if (_current == null) Reload();
                return _current;
            }
        }

        public void Save()
        {
            if (_current == null) Reload();
            SanitizeCurrent();
            Persist(_current);
            Changed?.Invoke();
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(_key);
            _current = new PlayerProfile();
            SanitizeCurrent();
            Persist(_current);
            Changed?.Invoke();
        }

        /// <summary>Relee PlayerPrefs y descarta la copia en memoria.</summary>
        public void Reload()
        {
            bool hasKey = PlayerPrefs.HasKey(_key);
            string raw = hasKey ? PlayerPrefs.GetString(_key, string.Empty) : string.Empty;

            if (!hasKey || string.IsNullOrWhiteSpace(raw))
            {
                _current = new PlayerProfile();
                SanitizeCurrent();
                Persist(_current);
                return;
            }

            if (!ProfileCodec.TryRead(raw, out PlayerProfile decoded, out bool corrupted))
            {
                if (corrupted) Debug.LogWarning("[ProfileService] Perfil invalido; se reescribe con defaults.");
                _current = new PlayerProfile();
                SanitizeCurrent();
                Persist(_current);
                return;
            }

            _current = decoded;
            if (SanitizeCurrent()) Persist(_current);
        }

        private bool SanitizeCurrent()
        {
            if (_current == null) return false;

            IReadOnlyList<WaifuDefinitionSO> all = _catalog.All;
            var knownIds = new List<string>(all == null ? 0 : all.Count);
            if (all != null)
            {
                foreach (WaifuDefinitionSO definition in all)
                {
                    if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
                        knownIds.Add(definition.Id);
                }
            }

            // Los contratos conocidos llegan del catálogo (E3); sin catálogo cualquier
            // lastContractId se considera desconocido y se limpia.
            var knownContracts = new List<string>();
            IReadOnlyList<ContractSO> contractList = _contracts != null ? _contracts.All : null;
            if (contractList != null)
            {
                foreach (ContractSO contract in contractList)
                {
                    if (contract != null && !string.IsNullOrWhiteSpace(contract.Id))
                        knownContracts.Add(contract.Id);
                }
            }

            return ProfileSanitizer.Sanitize(_current, knownIds, knownContracts);
        }

        private void Persist(PlayerProfile profile)
        {
            PlayerPrefs.SetString(_key, ProfileCodec.Encode(profile));
            PlayerPrefs.Save();
        }
    }
}
