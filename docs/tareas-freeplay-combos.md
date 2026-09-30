# Tareas — Free-play + combos WaifuCafe (ordenadas, no saltear)

Fuente de verdad del estado: `resumen-del-juego.md`. Orden: que no se rompa →
que el core pague → higiene → calidad. Cada tarea trae aceptación; sin verde no
se sigue.

## T1 · Free-play día 4+ (CRÍTICO, rompe el juego)

- **T1.1** `MainManuRules.cs:46` — el estado `tutorialDia4` vuelve a `Play()` en
  bucle infinito sin contenido. Cambiar: al completar el día 3, rutear al modo
  free-play (días endless reutilizando escena de día con dificultad escalada vía
  `StepsOfRun.cs`).
- **T1.2** `WaifuSelectorRules.cs:39` — `nextScene` queda null fuera de tutorial
  y `LoadScene(null)` falla en "Continuar". Asignar escena free-play cuando
  `IsShowTutorial()` es false.
- **T1.3** Definir free-play mínimo: días endless, escalado de `StepsOfRun`
  (cantidad/probabilidad por paso), sin contenido nuevo dibujado.
- ✅ Aceptación: completar día 3 → arranca día 4 con clientes nuevos, sin crash
  ni bucles; 3 días endless jugables seguidos.

## T2 · Que los combos paguen (core del diseño)

- **T2.1** `FoodComboManager.cs:19` y `CustomerComboManager.cs:18` — `GetReward()`
  devuelve 0. Implementar valores reales (propuesta v0.1: 10/20/30 por nivel de
  combo, a validar).
- **T2.2** `ComboManager.cs:184` — `CalculateReward` nunca se llama y
  `onComboFinished` nunca se invoca. Cablear: `RegisterServed` → cálculo →
  evento → UI.
- **T2.3** `ComboManager.cs:203` — `pointsText` comentado y `totalPoints`
  (`GameRules.cs:22,75`) invisible. Mostrar puntos en HUD.
- ✅ Aceptación: 3 servicios consecutivos del mismo tipo → estrellas + puntos
  visibles sumando; recompensa distinta por nivel de combo.

## T3 · Higiene barata (misma pasada)

- **T3.1** `StaffMiko.cs` — `name = "Miku"`, unificar a Miko.
- **T3.2** `TutorialProgress.cs:13-14` — `IsCompleted` invertido (true cuando NO
  terminó). Invertir + revisar usos.
- **T3.3** `StaffPosition.cs:9-17` — `Hold()` sin `Release()` en V2. Auditar ciclo
  de posiciones y liberar.
- ✅ Aceptación: `analyze` sin warnings nuevos; loop día 1-4 sin regresiones.

## T4 · Calidad (después del verde T1-T3)

- **T4.1** Borrar código muerto: `TutorialBootstrap.cs`, `DayTransitionMediator.cs`,
  `TutorialDayCompleted.cs`, `DragGhostManager.cs` (verificar 0 referencias antes).
- **T4.2** Infra de tests (no existe: crear asmdefs + carpeta `Tests`) y tests en
  caminos críticos: cálculo de combos y transiciones de día/tutorial.
- **T4.3** Recién acá: contenido nuevo (días, waifus, edificios).
- ✅ Aceptación: tests verdes en CI (o local si no hay CI); muerto borrado sin
  romper build Android + Web.
