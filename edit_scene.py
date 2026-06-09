#!/usr/bin/env python3
"""Edit Version_1.unity for PR4: Add Input GO, fix _Environment children, rename GameManager+."""

import re

SCENE_PATH = "D:/Unity/WaifuCafe/Assets/Scenes/Version_1.unity"

with open(SCENE_PATH, 'r', encoding='utf-8') as f:
    content = f.read()

# ========== 1. Fix _Environment children: remove duplicates ==========
# The _Environment Transform has children [100012, 100023, 100032, 100103, 100023, 100032]
# 100023 and 100032 appear twice. Let's fix it to have unique children.

old_children = """  m_Children:
  - {fileID: 100012}
  - {fileID: 100023}
  - {fileID: 100032}
  - {fileID: 100103}
  - {fileID: 100023}
  - {fileID: 100032}"""

new_children = """  m_Children:
  - {fileID: 100012}
  - {fileID: 100023}
  - {fileID: 100032}
  - {fileID: 100103}"""

assert old_children in content, "Could not find _Environment children block!"
content = content.replace(old_children, new_children, 1)
print("[OK] Fixed _Environment children (removed duplicates)")

# ========== 2. Rename "GameManager+" to "_GameManager" ==========
content = content.replace("  m_Name: GameManager+", "  m_Name: _GameManager", 1)
print("[OK] Renamed GameManager+ to _GameManager")

# ========== 3. Add "Input" root GO with PlayerInput + QueueInputHandler ==========
# The new GO goes before "SceneRoots:" section which is at the end.
# We need to insert the YAML block for the Input GameObject.
# 
# Using fileIDs 100144-100148 (next available after 100143).
# 
# PlayerInput guid: 62899f850307741f2a39c98a8b639597 (from SampleScene)
# QueueInputHandler guid: c01450f45f2647c499f2c6285ad53619 (from SampleScene)
# InputSystem_Actions asset: guid 2bcd2660ca9b64942af0de543d8d7100
# Main Camera fileID: 100010
# EventSystem InputSystemUIInputModule: 100041
# CustomerQueue component: 100108

new_input_go = """--- !u!1 &100144
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 100145}
  - component: {fileID: 100146}
  - component: {fileID: 100147}
  m_Layer: 0
  m_Name: Input
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100145
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100144}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &100146
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100144}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 62899f850307741f2a39c98a8b639597, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Unity.InputSystem::UnityEngine.InputSystem.PlayerInput
  m_Actions: {fileID: -944628639613478452, guid: 2bcd2660ca9b64942af0de543d8d7100, type: 3}
  m_NotificationBehavior: 0
  m_UIInputModule: {fileID: 100041}
  m_DeviceLostEvent:
    m_PersistentCalls:
      m_Calls: []
  m_DeviceRegainedEvent:
    m_PersistentCalls:
      m_Calls: []
  m_ControlsChangedEvent:
    m_PersistentCalls:
      m_Calls: []
  m_ActionEvents: []
  m_NeverAutoSwitchControlSchemes: 0
  m_DefaultControlScheme: Keyboard&Mouse
  m_DefaultActionMap: 
  m_SplitScreenIndex: -1
  m_Camera: {fileID: 100010}
--- !u!114 &100147
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100144}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: c01450f45f2647c499f2c6285ad53619, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::Customers.Queue.Input.QueueInputHandler
  queue: {fileID: 100108}
"""

# Insert before "SceneRoots:"
scene_roots_marker = "\nSceneRoots:"
assert scene_roots_marker in content, "Could not find SceneRoots marker!"
content = content.replace(scene_roots_marker, new_input_go + "\nSceneRoots:", 1)
print("[OK] Added Input GO with PlayerInput + QueueInputHandler")

# ========== 4. Add Input to SceneRoots ==========
# Input GO (100144) transform is 100145, needs to be added as a root
scene_roots_block_marker = """SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {fileID: 100001}
  - {fileID: 100043}
  - {fileID: 100053}
  - {fileID: 100061}
  - {fileID: 100071}
  - {fileID: 100081}
  - {fileID: 100091}
  - {fileID: 100101}"""

new_scene_roots = """SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {fileID: 100001}
  - {fileID: 100043}
  - {fileID: 100053}
  - {fileID: 100061}
  - {fileID: 100071}
  - {fileID: 100081}
  - {fileID: 100091}
  - {fileID: 100101}
  - {fileID: 100145}"""

assert scene_roots_block_marker in content, "Could not find SceneRoots block!"
content = content.replace(scene_roots_block_marker, new_scene_roots, 1)
print("✅ Added Input to SceneRoots")

# Write back
with open(SCENE_PATH, 'w', encoding='utf-8') as f:
    f.write(content)

print("\n✅ All scene edits applied successfully!")
