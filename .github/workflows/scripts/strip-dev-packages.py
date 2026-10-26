#!/usr/bin/env python3
"""Quita del manifest los paquetes que no deben shipear a produccion.

Play Store rechazo el AAB por "Dispositivo u otros IDs". El build incluia:
  - UnityAnalyticsModule / UnityAnalyticsCommonModule  (GetCustomDeviceId)
  - Unity.AI.Assistant / Unity.AI.Tracing              (telemetria)
  - MCPForUnity.Runtime                                (puente de desarrollo)

Estos paquetes se necesitan en desarrollo, no en el producto. Se quitan
SOLO en el build de Play Store; itch queda como entorno de QA.
"""
from __future__ import annotations

import json
import pathlib
import shutil
import sys

DEV_ONLY = [
    "com.unity.modules.unityanalytics",
    "com.unity.ai.assistant",
    "com.coplaydev.unity-mcp",
]

# Por si se reinstala PrimeTween con samples: no deben compilarse.
DEMO_PATHS = [
    "Assets/Plugins/PrimeTween/Demo",
]

# El Editor vuelve a poner estos flags en 1 cuando abre el proyecto, asi que
# se fuerzan aca. Con m_Enabled: 1 el linker corre con EnableUnityConnect y
# arrastra UnityConnectModule / UnityAnalyticsModule / UnityConsentModule al
# player, ademas de telemetria de "engine diagnostics".
CONNECT_SETTINGS = "ProjectSettings/UnityConnectSettings.asset"
CONNECT_FLAGS = [
    ("  m_Enabled: 1\n  m_TestMode: 0\n  m_EventOldUrl", "  m_Enabled: 0\n  m_TestMode: 0\n  m_EventOldUrl"),
    ("  InsightsSettings:\n    m_EngineDiagnosticsEnabled: 1", "  InsightsSettings:\n    m_EngineDiagnosticsEnabled: 0"),
    ("    m_CaptureEditorExceptions: 1", "    m_CaptureEditorExceptions: 0"),
    (
        "  UnityAnalyticsSettings:\n    m_Enabled: 0\n    m_TestMode: 0\n    m_InitializeOnStartup: 1",
        "  UnityAnalyticsSettings:\n    m_Enabled: 0\n    m_TestMode: 0\n    m_InitializeOnStartup: 0",
    ),
    ("  UnityAdsSettings:\n    m_Enabled: 0\n    m_InitializeOnStartup: 1", "  UnityAdsSettings:\n    m_Enabled: 0\n    m_InitializeOnStartup: 0"),
]


def disable_unity_connect() -> None:
    path = pathlib.Path(CONNECT_SETTINGS)
    if not path.exists():
        print(f"::warning::no existe {CONNECT_SETTINGS}")
        return
    text = path.read_text(encoding="utf-8")
    changed = 0
    for old, new in CONNECT_FLAGS:
        if old in text:
            text = text.replace(old, new, 1)
            changed += 1
        elif new not in text:
            print(f"::warning::no se pudo forzar flag en {CONNECT_SETTINGS}: {old.splitlines()[-1].strip()}")
    path.write_text(text, encoding="utf-8")
    print(f"Unity Services: {changed} flag(s) apagados")


def main() -> int:
    manifest_path = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "Packages/manifest.json")
    data = json.loads(manifest_path.read_text(encoding="utf-8"))
    deps: dict = data.setdefault("dependencies", {})

    removed, missing = [], []
    for pkg in DEV_ONLY:
        if pkg in deps:
            del deps[pkg]
            removed.append(pkg)
        else:
            missing.append(pkg)

    manifest_path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")

    for raw in DEMO_PATHS:
        demo = pathlib.Path(raw)
        meta = demo.with_suffix(".meta")
        if demo.exists():
            shutil.rmtree(demo)
            print(f"samples borrados: {raw}")
        if meta.exists():
            meta.unlink()

    disable_unity_connect()

    print(f"paquetes removidos: {', '.join(removed) or '(ninguno)'}")
    if missing:
        print(f"::warning::ya no estaban en el manifest: {', '.join(missing)}")
    print(f"dependencias restantes: {len(deps)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
