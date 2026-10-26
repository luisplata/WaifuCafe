#!/usr/bin/env python3
"""Gate de Data Safety: falla el build si un SDK de desarrollo/telemetria
volvio a entrar al AAB, o si se reactivo Unity Services.

Play Store escanea el binario, no el codigo. Ojo: en IL2CPP los assemblies
NO viajan como .dll (Data/Managed esta vacio), asi que la lista de
ScriptingAssemblies.json incluye modulos internos del engine que no se
pueden quitar (UnityAnalytics/UnityConnect/UnityConsent). Esos se reportan
como informacion; lo que se controla de verdad es:
  - que no shipee codigo de desarrollo (MCP, Unity.AI, samples de PrimeTween)
  - que Unity Services / engine diagnostics esten apagados en ProjectSettings
  - que la API de device id (GetCustomDeviceId) no este en el binario
"""
from __future__ import annotations

import glob
import json
import pathlib
import sys
import zipfile

# Codigo de desarrollo o de terceros: si aparece, es un error real.
FORBIDDEN = [
    "MCPForUnity",
    "Unity.AI.Assistant",
    "Unity.AI.Tracing",
    "Unity.AI.MCP",
    "PrimeTween.Demo",
    "PrimeTween.Debug",
]

# Modulos internos del engine: se informan, no rompen el build.
ENGINE_MODULES = [
    "UnityAnalyticsModule",
    "UnityAnalyticsCommonModule",
    "UnityConnectModule",
    "UnityConsentModule",
]

# Firma de la API que Play marco como "Dispositivo u otros IDs".
DEVICE_ID_MARKERS = [b"GetCustomDeviceId"]

CONNECT_SETTINGS = "ProjectSettings/UnityConnectSettings.asset"
CONNECT_REQUIRED = [
    "  m_Enabled: 0\n  m_TestMode: 0",
    "    m_EngineDiagnosticsEnabled: 0",
]

ASSEMBLIES_ENTRY = "base/assets/bin/Data/ScriptingAssemblies.json"


def find_aab() -> pathlib.Path:
    if len(sys.argv) > 1:
        return pathlib.Path(sys.argv[1])
    candidates = sorted(glob.glob("build/Android/**/*.aab", recursive=True))
    if not candidates:
        print("::error::no se encontro ningun .aab para verificar")
        raise SystemExit(1)
    return pathlib.Path(candidates[-1])


def check_connect_settings() -> list[str]:
    path = pathlib.Path(CONNECT_SETTINGS)
    if not path.exists():
        return [f"no existe {CONNECT_SETTINGS}"]
    text = path.read_text(encoding="utf-8")
    return [flag.strip() for flag in CONNECT_REQUIRED if flag not in text]


def main() -> int:
    aab = find_aab()
    print(f"AAB: {aab}")

    errors: list[str] = []

    with zipfile.ZipFile(aab) as archive:
        raw = archive.read(ASSEMBLIES_ENTRY)
        entries = archive.namelist()
        binaries = {
            name: archive.read(name)
            for name in entries
            if name.endswith(("global-metadata.dat", "libil2cpp.so"))
        }

    data = json.loads(raw.decode("utf-8"))
    names = data["names"] if isinstance(data, dict) else data
    print(f"assemblies: {len(names)}")

    dev_hits = sorted({n for n in names for f in FORBIDDEN if f in n})
    if dev_hits:
        errors.append("codigo de desarrollo en el build: " + ", ".join(dev_hits))

    engine_hits = sorted({n for n in names for m in ENGINE_MODULES if m in n})
    if engine_hits:
        print(f"info: modulos internos del engine (no removibles en IL2CPP): {len(engine_hits)}")

    for marker in DEVICE_ID_MARKERS:
        found = [name for name, blob in binaries.items() if marker in blob]
        if found:
            errors.append(f"API de device id '{marker.decode()}' presente en {', '.join(found)}")

    missing = check_connect_settings()
    if missing:
        errors.append("Unity Services/diagnostics reactivados en ProjectSettings: " + "; ".join(missing))

    if errors:
        print("::error::Gate de Data Safety fallo:")
        for err in errors:
            print(f"  - {err}")
        return 1

    print("OK: sin MCP, sin Unity.AI, sin samples de PrimeTween, sin device id, Unity Services apagado")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
