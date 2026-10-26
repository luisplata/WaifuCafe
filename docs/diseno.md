# Brief de diseño — WaifuCafe (para el dev)

Fecha: 2026-09-30. Esto es lo *diseñado*; el estado real está en
`resumen-del-juego.md` y las tareas en `tareas-freeplay-combos.md`.
Regla: ante contradicción diseño vs código, el código manda y se avisa.

## Decisiones de diseño vigentes

1. **Especialidades = tags.** Cada waifu modifica aparición/tiempos/economía
   (Economy, Rush, Combo, Patience...). Waifu nueva = arte + especialidad, sin
   arquitectura nueva.
2. **Match-3 por 3 servicios consecutivos** (cliente o pedido); ambos a la vez =
   doble match con bonus especial. Combos con **decaimiento por tiempo** (del GDD).
3. **Derrota suave:** "casi lo consigo", nunca castigo fuerte.
4. **Tutorial día por día hasta el 8** (4: match cliente, 5: doble, 6: talentos,
   7: preparativos, 8+: combinar) — bloqueado en día 4 por T1.
5. **Pregunta pilar:** ¿el jugador disfruta detectando patrones y decidiendo
   rápido? Todo lo demás (locales, gacha, skins) crece alrededor de un sí.

## Alcance por fase (no mezclar)

- **Ahora (T1-T4):** free-play día 4+, combos que pagan, higiene, calidad. Nada nuevo.
- **Después:** talentos activos (ejemplos en GDD: Sweet Morning, Dash, Relax,
  Encore), locales (4), preparativos (3), oro/tickets/gacha/duplicados cosméticos,
  objetivos y derrota por día.
- **Nunca sin decisión explícita:** más waifus antes de T2 verde, monetización,
  multiplayer, backend.

## Abiertos (los decide el dueño)

Modelo (premium cozy vs gacha), catálogo de triggers/efectos si los talentos
crecen, balance numérico post-T2 (tiempos, paciencia, recompensas 10/20/30 v0.1).
