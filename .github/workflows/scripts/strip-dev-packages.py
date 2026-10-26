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

    print(f"paquetes removidos: {', '.join(removed) or '(ninguno)'}")
    if missing:
        print(f"::warning::ya no estaban en el manifest: {', '.join(missing)}")
    print(f"dependencias restantes: {len(deps)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
