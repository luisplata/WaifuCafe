# Proposal: Core Loop v2 — "el café es un tablero"

## Intent

Reemplazar el loop actual —una sola decisión real (arrastrar staff→cliente con la comida ya sorteada) y un "match-3" que es un historial invisible de combos— por el modelo Puzzle & Dragons / Empires & Puzzles de café: el tablero sirve pedidos, los clientes son objetivos y las waifus son la party RPG.

Ahora, porque el core no prueba nada y la capa waifu es potencialmente decorativa. Hay que responder **"¿el tablero hace que las waifus importen?"** antes de invertir en arte, colección o las 100 waifus.

## Scope

### In Scope — slice A1–A6 + selector (6–9 días)

| # | Slice | Contenido | Días |
|---|-------|-----------|------|
| A1 | Tablero | 7x7, 5 tipos, swap adyacente, match 3+, gravedad/refill, cascadas, sin match inicial, mouse/touch | 2–3 |
| A2 | Pedido + asignación | `Order` (1–2 platos, 3–6 unidades), burbuja con contador, unidades por match, derrame, regla urgente | 1–2 |
| A3 | Cola + expulsión | Llegada siempre encola; sin asiento se va el que más esperó; marca "sale en la próxima"; reputación 3 | 0,5–1 |
| A4 | Waifu runtime | Carga por elemento, 4 skills (una por rol), party en mundo + anillo de ready | 1,5–2 |
| A5 | Shell del día | 180–240 s, monedas, objetivo, fin de día; reusar `GameRules` | 0,5–1 |
| A6 | Tuning + playtest | Llegadas 8–10 s, curva de pedidos/carga, 2 sesiones | 1 |
| B1 | Selector de party | 6 waifus jugables, elegís 4, misma escena | 0,5–1 |

Tests: **3–5 EditMode, solo del modelo puro del tablero** (match, cascada, sin match inicial).

### Out of Scope (explícito)

| Fuera | Por qué |
|-------|---------|
| 100 waifus, colección, roster persistente | Es un proyecto de contenido; no responde la pregunta riesgosa |
| Economía de monedas real (sinks, contratación) | No hay nada que comprar en el slice; monedas solo como score |
| Niveles, ascensión, skins, vínculo | Progresión vertical; el GDD pide horizontal |
| Recetas, ingredientes, 6.º plato | El tablero de 5 tipos ya está calibrado (21 % de drought) |
| Tutorial de 3 días | Su objetivo es "3 combos de historial"; se reescribe después |
| Arte final de waifus nuevas | Primero validar que la party importa |
| Gacha, historia | Decisión de negocio no tomada |

## Capabilities

### New Capabilities
- `match3-board`: grilla, swap válido, gravedad/refill, cascadas, detección de matches.
- `customer-orders`: pedido como dato, unidades por match, burbuja de contador.
- `seat-queue`: cola de llegada, asignación urgente con derrame, expulsión y reputación.
- `waifu-party`: carga por elemento, skills que manipulan tablero/reloj, composición.
- `day-shell`: duración, objetivo, monedas, fin de día y derrota suave.

### Modified Capabilities
- None. No existen specs previos en `openspec/specs/`.

## Approach

**Reusar**: `CustomerSpawnerCoroutine` (preview/cola de próximos), `PrototypeWaves` (único reloj), `PrototypeNextUpHud` (slide → "quién queda afuera"), `CameraFit` (26,9 u), escena `Prototype_Match3`, los 10 modelos de waifu, `StaffEspeciality`, `VfxMatch`, el shell de `GameRules`, `SaveManager`.

**Reemplazar**: `CustomerClient.OnDrop` (el drag staff→cliente deja de ser la acción), `CustomerStateMachine` (paciencia → `EnCola/Sentado/Servido/Expulsado`), `CustomerFoodClient` (sprite → burbuja con contador). `StaffStateMachine` deja de ser el verbo: el tablero sirve al instante.

**Borrar** (después de desconectar `GameRules.cs:130-133` y `:285`): `FoodComboManager` + `ComboManager` + los ~8 archivos de combo. El combo pasa a multiplicador de cascada sobre monedas, nunca sobre unidades.

**Crear de cero** (no existe hoy): grilla y resolución, `Order`, asignación/derrame, cola/expulsión, carga y skills, `WaifuDefinitionSO`. Una sola escena en build, un assembly, sin arquitectura final.

**Layout**: opción A — tablero 7x7 (celda 0,98 u) a la izquierda en el vacío de 8,4 u, café a la derecha con 5 asientos y 4 waifus a ~3 u.

## Order of Risk

`A1 → A2 → A4 → B1` es el único camino que responde la pregunta riesgosa: **dos parties distintas, mismo día y mismo script de llegadas, producen decisiones distintas**. Tiene que estar verde antes de arte o de las 100 waifus. A3, A5 y A6 completan el día jugable y el tuning. Si A4/B1 falla, se rediseña el pilar waifu antes de producir contenido.

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Waifu decorativa | High | Regla dura: ninguna skill del slice es solo buff porcentual; toda toca tablero, reloj o targeting. Test: nombrar 2 tableros donde esa waifu gana |
| Ritmo no cierra (llegada vs jugadas/pedido) | High | Un solo knob (intervalo de llegada 8–10 s), pedidos chicos al inicio, medir servidos/expulsados por día |
| Targeting: 21 % de tableros sin jugada del plato pedido | Medium | 5 tipos, indicador "no hay jugada de X", skill de conversión/barajado apuntado, derrame al siguiente cliente/banco |

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Assets/Scenes/Prototype/Prototype_Match3.unity` | Modified | Tablero + cola + waifus; única escena en build |
| `Assets/Scripts/Customer/` | Modified | Spawner/estado/cliente reescritos; `OnDrop` eliminado |
| `Assets/Scripts/GameRules/` | Modified | Shell reusado; combos desconectados y borrados |
| `Assets/Scripts/Staff/` | Modified | `StaffStateMachine` fuera del core; `WaifuDefinitionSO` nuevo |
| `Assets/Scripts/Board/` (nuevo) | New | Modelo puro + vista + input del tablero |
| `ProjectSettings/EditorBuildSettings.asset` | Unchanged | Una sola escena durante el slice |
| `.github/workflows/publish-itch.yml` | Unchanged | Manual; compilar local antes de dispatch |
| Tutorial `TutorialDay_1/2/3` | Frozen | Fuera de build; no tocar |
| `SaveManager` / PlayerPrefs | Unchanged | 4 keys int; roster = 1 key JSON post-slice |

## Rollback Plan

Antes de tocar: `git tag prototype-foodonly` y rama nueva. El prototipo actual es la build viva de itch. Rollback = revertir la rama o deployar el tag (el publish es manual). No borrar los tutoriales ni el drag-and-drop en el slice.

## Dependencies

- Sprites existentes: 10 modelos de waifu, ~9 imágenes de comida en `Assets/Art/Comida/`.
- Cero arte nuevo, cero assets externos, cero packages nuevos.

## Delivery

No entra en un PR: 6–9 días. La estrategia (single-pr vs chained/stacked) se decide en `sdd-tasks` con el forecast de líneas; default `ask-on-risk`, presupuesto de 400 líneas por PR.

## Assumptions & Open Questions

**Supuestos** (defaults de la exploración; corregir antes de `sdd-spec`):
1. 5 platos/elementos, 7x7, 5 asientos, día de 180–240 s.
2. Asignación urgente con derrame; solo llegadas + reputación 3.
3. 4 waifus en party, 6 disponibles, carga por elemento compartida.

**Preguntas abiertas (3)**:
1. ¿Reescribimos `Prototype_Match3` o creamos `Cafe_Match3_Prototype`?
2. ¿La build de itch sigue con el prototipo viejo hasta A6 verde, o se reemplaza al cerrar A5?
3. ¿El banco de derrame es visible para el jugador o invisible en el slice?

## Success Criteria

- [ ] Dos composiciones de party distintas producen decisiones distintas y se pueden nombrar 2 tableros donde A > B.
- [ ] 3 días jugables seguidos sin crash; se puede perder y se entiende por qué.
- [ ] 10 jugadas sin excepciones, cascadas encadenadas, tablero nunca muerto.
- [ ] 3–5 tests EditMode del modelo del tablero en verde.
- [ ] Cero waifus del slice con solo buffs porcentuales.
