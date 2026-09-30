# Resumen del estado actual del juego — WaifuCafe

> Estado relevado el 30 de septiembre de 2026 directamente desde el codigo en disco.
> Este documento es la fuente de verdad del estado actual. GDD.md y ROADMAP.md estan desactualizados e invertidos (ver seccion 8).

## 1. Snapshot del proyecto

| Aspecto | Valor | Evidencia en disco |
|---|---|---|
| Build | 5 escenas activas | `ProjectSettings/EditorBuildSettings.asset` |
| Escenas del build | `Intro.unity`, `WaifuSelector.unity`, `TutorialDay_1/2/3.unity` | `ProjectSettings/EditorBuildSettings.asset` |
| Fuera del build | `SampleScene.unity`, `Version_1.unity`, `Version2/Version_2.unity`, `Version3/MainMenu.unity` | carpetas de `Assets/Scenes/` vs `EditorBuildSettings.asset` |
| Scripts C# | 101 en `Assets/Scripts/` | `Assets/Scripts/**/*.cs` |
| Tests automatizados | 0 (sin carpeta `Assets/Tests` ni asmdefs de test propios) | `Assets/` |
| Ultima build Android | `WaifuCafe_202607022259.aab` (jul 2026) | `Build/Playstore/` |
| Build Web | `index.html` + `Web.zip` (jun 2026) | `Build/Web/` |
| Loop vivo / legacy | V2 vivo (en el build) / V1 legacy (fuera del build) | `EditorBuildSettings.asset`, `Assets/Scripts/V2/` |

## 2. Loop jugable

```
Intro ──> WaifuSelector ──> TutorialDay_1 ──> TutorialDay_2 ──> TutorialDay_3 ──> (vuelve a Intro)
```

- **Intro**: `Assets/Scenes/Version3/Intro.unity`. Dialogs y transiciones via `IntroMediator.cs` (`Assets/Scripts/V2/MainMenu/IntroMediator.cs`).
- **WaifuSelector**: `Assets/Scenes/Version2/WaifuSelector.unity`. El jugador elige entre 1 y 4 waifus (`WaifuSelectorRules.cs`, `maxCardSelected` con `[Range(1,4)]`). Las elegidas se guardan en `SaveGame.cs` y determinan el staff de la partida.
- **Tutorial de 3 dias**: `Assets/Scenes/Version3/Tutorial/TutorialDay_1.unity`, `TutorialDay_2.unity`, `TutorialDay_3.unity`. El avance de dia se persiste en `PlayerPrefs` (`TutorialProgress.cs`).
- **Fin del juego**: al completar el Dia 3, `GameRules.cs` dispara `TutorialProgress.NextDay()` y `SceneManager.LoadScene(0)` (vuelve a Intro). **No hay free-play**: el juego termina al completar el Dia 3.

### Limites del loop

- **Gap tutorial → free-play**: cuando ya no es tutorial y `TutorialProgress.CurrentDay >= 3`, `MainManuRules.cs` (linea 46) entra en `tutorialDia4` que vuelve a `_mainMenuStates.Play()` — bucle infinito sin contenido nuevo.
- **nextScene inexistente**: `WaifuSelectorRules.cs` (linea 39) solo asigna `nextScene = $"TutorialDay_{CurrentDay}"` cuando `IsShowTutorial()` es true. Si ya no es tutorial, `nextScene` queda null y `SceneManager.LoadScene(null)` falla al pulsar "Continuar".

## 3. Sistemas V2

| Sistema | Rol | Archivos clave |
|---|---|---|
| Run | Temporiza la partida, avanza dias y carga escenas | `Assets/Scripts/V2/GameRules/GameRules.cs`, `IGameRules.cs` |
| Spawn | Genera clientes por pasos y el staff seleccionado | `Assets/Scripts/V2/Customer/CustomerSpawnerCoroutine.cs`, `Assets/Scripts/V2/Staff/StaffSpawnerManager.cs` |
| Cliente | Maquina de estados del cliente, paciencia y recepcion de drops | `Assets/Scripts/V2/Customer/CustomerClient.cs`, `CustomerStateMachine.cs`, `ICustomerClient.cs` |
| Staff | Maquina de estados del staff y ciclo de servicio | `Assets/Scripts/V2/Staff/StaffClient.cs`, `StaffStateMachine.cs`, `IDragControllerHandle.cs`, `IStaffMediator.cs` |
| Waifus | 10 modelos de staff con especialidades distintas | `Assets/Scripts/V2/Staff/Models/StaffModel.cs` + 10 subclases, `StaffFactory.cs`, `StaffNames.cs` |
| Combos | Detecta combos de comida/cliente y los muestra | `Assets/Scripts/V2/GameRules/ComboManager.cs`, `FoodComboManager.cs`, `CustomerComboManager.cs` |
| Drag & Drop | Resolucion de drops entre UI y mundo 2D | `Assets/Scripts/DragAndDrop/DragManager.cs`, `DraggableSprite.cs`, `IDropReceiver.cs` |
| Audio | SFX por singleton | `Assets/Scripts/V2/Audio/AudioService.cs` |
| Meta / Tutorial | Progreso, guardado, selector de waifus e intros | `Assets/Scripts/V2/WaifuSelector/SaveGame.cs`, `Assets/Scripts/V2/Helpers/SaveManager.cs`, `TutorialProgress.cs`, `IntroMediator.cs` |

### 3.1 Run (`Assets/Scripts/V2/GameRules/`)

`GameRules.cs` implementa `IGameRules`. Secuencia `intro → game → endGame` con `TeaTime`. `game` avanza una barra de progreso (`percentOfGame = localTime / timeToRun`) y al terminar inicia `endGame`: detiene el spawner, espera 20 s y carga la escena 0 (`SceneManager.LoadScene(0)`, linea 60/65).

### 3.2 Spawn

`CustomerSpawnerCoroutine.cs` genera clientes por pasos definidos en `StepsOfRun.cs` (porcentaje de partida, cantidad por paso, probabilidad de cliente/comida especifica), usando `CustomerFactory.cs` y `FoodFactory.cs`. `StaffSpawnerManager.cs` instancia los staff seleccionados en el WaifuSelector (via `StaffSelected.cs`, referenciado en `StaffSpawnerManager.cs:8` y alimentado por `SaveGame`) y expone modificadores de especialidad (paciencia, economia, combos) consultados por `GameRules`.

### 3.3 Cliente

`CustomerClient.cs` (implementa `IDropReceiver` e `ICustomerClient`) recibe el drop de un staff: `OnDrop` cambia a `EntregandoPedido`, pide el pedido al staff y dispara `OnCustomerAttended`. `CustomerStateMachine.cs` recorre `CustomerPhase` (`Entrando`, `ListoParaPedir`, `EsperandoEntrega`, `EntregandoPedido`, `Consumir`, `Irse`, `Llendose`) con tiempos de paciencia y consumo.

### 3.4 Staff

`StaffClient.cs` (implementa `IDragControllerHandle` e `IStaffMediator`) se mueve con `PrimeTween` entre posicion, cocina y cliente. `StaffStateMachine.cs` recorre `StaffPhase` (`EnEspera`, `AtendiendoCliente`, `PreparandoPedido`, `LlevandoPedidoCocina`, `LlevandoPedidoCliente`, `Moviendose`) y orquesta el ciclo completo de servicio.

### 3.5 Waifus

`StaffModel.cs` es la base; cada waifu es una subclase con especialidad (`StaffEspeciality.cs`): Airi (Economy), Yuki (Breakfast), Luna (Lunch), Neko (Drink), Alice (Vip), Emi (Casual), Hana (Rush), Rika (Speed), Miko (Patience), Sora (Combo). `StaffFactory.cs` mapea `StaffNames` a instancias y les asigna sprite via `StaffModelConfiguration.cs` (SO).

### 3.6 Combos

`ComboManager.cs` centraliza: `RegisterServed` pregunta a `FoodComboManager` y `CustomerComboManager` (ambos `ICustomComboManager`) y actualiza UI de estrellas y textos. Los combos se detectan con 3 servicios consecutivos del mismo tipo (comida o cliente).

### 3.7 Drag & Drop

`Assets/Scripts/DragAndDrop/` es compartido: `DragManager.cs` (singleton) resuelve un drop con prioridad configurable UI vs mundo; `DraggableSprite.cs` implementa los handlers de arrastre y construye el `DropPayload`; `IDropReceiver.cs` define el contrato de recepcion (`OnDrop`, `Accepts`).

### 3.8 Audio

`AudioService.cs` (singleton con `DontDestroyOnLoad`) reproduce SFX con `PlayOneShot`. Se usa en `WaifuSelectorRules`, `CustomerClient`, `StaffClient` y `ComboManager`.

### 3.9 Meta / Tutorial

- `SaveGame.cs` (singleton, `DontDestroyOnLoad`): guarda las waifus seleccionadas como `List<StaffNames>`.
- `SaveManager.cs` (singleton): `IsShowTutorial()` decide si la partida esta en tutorial.
- `TutorialProgress.cs`: `CurrentDay` en `PlayerPrefs` (clave `"TutorialDay"`) y `NextDay()`.
- `WaifuSelectorRules.cs`: instancia las cartas de waifu (`CardOfWaifu.cs`) y carga la escena siguiente.

## 4. Arte y assets

- **Arte 100% IA (ComfyUI)**: `Assets/Art/` contiene PNGs generados (ej. `ComfyUI_00134_.png`, `Assets/Art/Personajes/*.png` con el prompt de generacion en el nombre del archivo).
- **Atlas**: `Assets/Atlases/MainArt_Atlas.spriteatlasv2`.
- **Prefabs V2**: `Assets/Prefabs/V2/Comidas/`, `Assets/Prefabs/V2/Customers/` (Casual, Apurado, VIP, Paciencia), `Assets/Prefabs/V2/Staff/`; waifus en `Assets/Prefabs/V3/WaifuSelector/` (10 prefabs, uno por waifu).
- **Tooling de editor**: `Assets/Editor/` — `SpriteAtlasPrebuildGenerator.cs`, `SetupSpritePrefabs.cs`, `SetupMobileOptimization.cs`, `SetupWebGLOptimization.cs`, `SetupVersion1Scene.cs`.

## 5. Arquitectura

- **Eventos `Action` (event-driven)**: `CustomerClient.cs` (`OnCustomerAttended`, `OnLeftGo`), `CustomerStateMachine.cs` (`OnStateChange`), `GameRules.cs` (`onComboFinished`), `IntroMediator.cs` (`OnFinish`), `ComboManager.cs` (`onComboFinished`).
- **Interfaces de mediacion**: `IDropReceiver.cs`, `IDragControllerHandle.cs`, `IStaffMediator.cs` (namespace `Staff`), `ICustomerClient.cs`, `ICustomerSpawn.cs`, `IGameRules.cs`, `IComboManager.cs` (marcador), `ICustomComboManager.cs`.
- **Singletons con `DontDestroyOnLoad`**: `DragManager.cs`, `AudioService.cs`, `SaveGame.cs`, `SaveManager.cs`.
- **Librerias**: PrimeTween (paquete local via tgz, `Packages/manifest.json` → `com.kyrylokuzyk.primetween`) y TeaTime (archivo local vendido `Assets/Scripts/V2/TeaTime.cs`, v0.9).
- **ScriptableObjects**: `FoodDataSO.cs` (datos de comida), `StaffModelConfiguration.cs` (sprite por staff).
- **Naming**: dominio en espanol mezclado con ingles (ej. `StaffEspeciality`, `tiempoDePreparacion`, `ListoParaPedir`, `CustomerPhase`).

## 6. V1: loop legacy fuera del build

- Implementacion paralela completa, **fuera del build**: `Assets/Scripts/GameManager/GameManager.cs` y `GameStateMachine.cs`; `Assets/Scripts/Customers/` (`Customer.cs`, `CustomerComponentUi.cs`, `Queue/`); `Assets/Scripts/Staff/` (`StaffManager.cs`, `ServiceCoordinator.cs`, `StaffServiceUIController.cs`, `StaffFront.cs`); `Assets/Scripts/StateMachines/`.
- **El auto-assignment funciona**: `HandleServiceCoroutine` esta implementado en `StaffManager.cs` (lineas 138-170): `CommandAttend()` → `MarkBusy()` → espera a `CanAttend()` → `MarkFree()` y `OnServiceCompleted`. `TryAssignCustomer` lo dispara (linea 121).
- Escena legacy: `Assets/Scenes/Version_1.unity` (no incluida en el build).

## 7. Gaps, riesgos y deuda

- [ ] Sin contenido post-tutorial: el juego termina al completar el Dia 3; no existe free-play (`GameRules.cs:50-66`, `MainManuRules.cs:46-54`).
- [ ] Puntos sin efecto: `GetReward()` devuelve 0 (`FoodComboManager.cs:19`, `CustomerComboManager.cs:18`); `CalculateReward` nunca se llama (`ComboManager.cs:184`); `onComboFinished` nunca se invoca; `totalPoints` se acumula pero no se muestra (`GameRules.cs:22,75`, `pointsText` comentado en `ComboManager.cs:203`).
- [ ] Codigo muerto sin referencias desde otros scripts: `TutorialBootstrap.cs`, `DayTransitionMediator.cs`, `TutorialDayCompleted.cs` (`V2/MainMenu/`) y `DragGhostManager.cs` (`DragAndDrop/`).
- [ ] `StaffPosition.Hold()` nunca se libera: `Release()` no se llama en ningun script V2 (`StaffPosition.cs:9-17`).
- [ ] Naming invertido: `TutorialProgress.IsCompleted` devuelve true cuando `CurrentDay <= 3`, es decir, mientras el tutorial NO esta completo (`TutorialProgress.cs:13-14`).
- [ ] Typo: `StaffMiko.cs` asigna `name = "Miku"` (clase `StaffMiko`).
- [ ] Docs obsoletos: GDD.md/ROADMAP.md describen el estado invertido (seccion 8).
- [ ] 0 tests automatizados (sin carpeta `Assets/Tests` ni asmdefs de test propios).

## 8. Documentos obsoletos: GDD.md y ROADMAP.md

- `Assets/SDD/GDD.md` y `Assets/SDD/ROADMAP.md` (jun 2026) estan **desactualizados e invertidos**: describen V1 como vivo y V2 como sin contenido real, cuando en realidad el build usa las escenas V2/V3 y V1 esta fuera del build.
- Ejemplos de claims falsos frente al codigo: GDD/ROADMAP reportan un porcentaje minimo de implementacion; dicen que la asignacion automatica no funciona (con un supuesto TODO) cuando `HandleServiceCoroutine` esta implementado (`StaffManager.cs:138`); y describen directorios de V2 como inexistentes cuando `V2/Customer/` contiene 14 archivos.
- **No usar esos documentos como referencia de estado**: el codigo en disco es la fuente de verdad y este documento resume ese codigo, sistema por sistema.