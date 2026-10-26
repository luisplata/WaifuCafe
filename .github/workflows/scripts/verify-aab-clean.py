#!/usr/bin/env python3
"""Gate de Data Safety: falla el build si un SDK de desarrollo/telemetria
volvio a entrar al AAB.

Play Store escanea el binario, no el codigo. Si cualquiera de estos assemblies
reaparece, la declaracion "no se recopilan datos" pasa a ser falsa.
"""
from __future__ import annotations

import glob
import json
import pathlib
import sys
import zipfile

FORBIDDEN = [
    "UnityAnalyticsModule",
    "UnityAnalyticsCommonModule",
    "UnityConnectModule",
    "UnityConsentModule",
    "Unity.AI.",
    "MCPForUnity",
    "PrimeTween.Demo",
    "PrimeTween.Debug",
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


def main() -> int:
    aab = find_aab()
    print(f"AAB: {aab}")

    with zipfile.ZipFile(aab) as archive:
        raw = archive.read(ASSEMBLIES_ENTRY)

    data = json.loads(raw.decode("utf-8"))
    names = data["names"] if isinstance(data, dict) else data

    hits = sorted({name for name in names for forbidden in FORBIDDEN if forbidden in name})
    print(f"assemblies: {len(names)}")

    if hits:
        print("::error::SDKs de desarrollo/telemetria encontrados en el build:")
        for hit in hits:
            print(f"  - {hit}")
        print("Revisa el strip de paquetes o el gate de Data Safety de Play.")
        return 1

    print("OK: sin Analytics, sin AI, sin MCP, sin samples de PrimeTween")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
