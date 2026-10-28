using System.Collections.Generic;
using UnityEngine;
using V2.Board;
using V2.Cafe;
using WaifuCafe.Core;
using WaifuCafe.Core.Meta;

namespace V2.Waifu
{
    /// <summary>
    /// Ejecuta las skills de la party. Vive fuera de la vista: valida la carga propia de la
    /// waifu, cooldown medido en matches y que el tablero no este resolviendo; aplica el efecto
    /// y lo devuelve por el mismo camino que un swap (olas, snapshots, cascadas, telegraph).
    /// La potencia sale de <see cref="WaifuPartyRuntime.PotencyFor"/> (estrellas + nivel +
    /// sinergia) y el lider con ultimate viva castea la ultimate de facción en lugar de su
    /// activa normal.
    /// </summary>
    public sealed class SkillExecutor
    {
        private readonly WaifuPartyRuntime _party;
        private readonly BoardSession _session;
        private readonly BoardView _view;
        private readonly CafeFloor _floor;
        private readonly PartyModifiers _mods;
        private readonly int[] _cooldown = new int[WaifuPartyRuntime.MaxSlots];

        public SkillExecutor(WaifuPartyRuntime party, BoardSession session, CafeFloor floor = null,
            PartyModifiers modifiers = null)
        {
            _party = party;
            _session = session;
            _view = session != null ? session.GetComponentInChildren<BoardView>() : null;
            _floor = floor;
            _mods = modifiers ?? (party != null ? party.Modifiers : null) ?? PartyModifiers.Neutral();
        }

        /// <summary>Ultimate de la party (puede estar bloqueada o ser chase/debug).</summary>
        public UltimateInfo Ultimate => _mods.Ultimate;

        /// <summary>Última ultimate efectivamente ejecutada (None = ninguna).</summary>
        public UltimateId LastUltimate { get; private set; }

        public string LastUltimateTitle => LastUltimate == UltimateId.None
            ? string.Empty
            : UltimateTable.Title(LastUltimate);

        /// <summary>Intentos restantes de cooldown (en matches) para ese slot.</summary>
        public int CooldownLeft(int slot) =>
            slot >= 0 && slot < _cooldown.Length ? _cooldown[slot] : 0;

        /// <summary>Un match del jugador descuenta un paso de cooldown a todos los slots.</summary>
        public void TickCooldowns()
        {
            for (int i = 0; i < _cooldown.Length; i++)
                if (_cooldown[i] > 0) _cooldown[i]--;
        }

        /// <summary>Valida READY + cooldown + tablero libre. No muta nada.</summary>
        public bool CanFire(int slot)
        {
            WaifuDefinitionSO def = _party != null ? _party[slot] : null;
            if (def == null || _session == null || _session.Board == null) return false;
            if (_session.IsResolving || _session.Locked) return false;
            return CooldownLeft(slot) <= 0 && _party.IsReady(slot);
        }

        /// <summary>
        /// Compromete el costo antes de la cinematica: gasta SOLO la carga de ese slot y
        /// arranca su cooldown (Control 2 lo reduce; FasterService descuenta a las companeras).
        /// Si no valida, no toca nada.
        /// </summary>
        public bool CommitFire(int slot)
        {
            if (!CanFire(slot)) return false;

            WaifuDefinitionSO def = _party[slot];
            _party.Spend(slot);

            int reduction = def.Role == WaifuRole.Control ? _mods.ControlCooldownReduction : 0;
            _cooldown[slot] = Mathf.Max(0, def.CooldownMatches - reduction);

            if (def.PassiveId == PassiveId.FasterService)
            {
                for (int i = 0; i < _cooldown.Length; i++)
                    if (i != slot && _cooldown[i] > 0) _cooldown[i]--;
            }

            return true;
        }

        /// <summary>
        /// Aplica el efecto una sola vez. No valida ni cobra: el costo ya lo tomo
        /// <see cref="CommitFire"/>. Se llama al cerrar la cinematica (natural o saltada).
        /// </summary>
        public bool ExecuteFire(int slot)
        {
            WaifuDefinitionSO def = _party != null ? _party[slot] : null;
            if (def == null || _session == null || _session.Board == null) return false;
            if (_session.IsResolving || _session.Locked) return false;
            return Apply(slot, def);
        }

        /// <summary>Sin cinemática: validar, comprometer y ejecutar. El costo no se devuelve.</summary>
        public bool TryFire(int slot) => CanFire(slot) && CommitFire(slot) && ExecuteFire(slot);

        /// <summary>
        /// Dev/tuning: ejecuta una ultimate sin gate, sin costo y sin cinemática. Sólo para
        /// probar Libro Mayor / Encore / Zorro de Fuego (FU2: chase/debug en V1).
        /// </summary>
        public bool DebugUltimate(UltimateId id)
        {
            if (id == UltimateId.None || _session == null || _session.Board == null) return false;
            if (_session.IsResolving || _session.Locked) return false;

            var info = new UltimateInfo(id, 0, WaifuFaction.Cafe, -1, 0);
            return ApplyUltimate(info);
        }

        private bool Apply(int slot, WaifuDefinitionSO def)
        {
            LastUltimate = UltimateId.None;

            if (ShouldCastUltimate(slot)) return ApplyUltimate(_mods.Ultimate);

            switch (def.SkillArchetype)
            {
                case SkillArchetype.ClearCross:
                {
                    int level = _party.LevelFor(slot);
                    return _session.ResolveSkill(_session.Board.ClearCells(Area(TargetCell(), level)));
                }

                case SkillArchetype.ConvertToElement:
                {
                    int count = _party.PotencyFor(slot);
                    List<BoardPos> cells = PickConversionCells(_session.Board, def.Element, count);
                    if (cells.Count == 0) return false;
                    return _session.ResolveSkill(_session.Board.ConvertCells(cells, def.Element));
                }

                case SkillArchetype.InstantServeUnits:
                    return ServeDirect(slot, def);

                case SkillArchetype.ReshuffleBoard:
                    if (!_session.Board.ReshuffleWithMove()) return false;
                    _session.SyncBoard();
                    return true;

                default:
                    // DelayEviction esta reservado: la expulsion no existe todavia.
                    return false;
            }
        }

        /// <summary>La ultimate sólo la castea el líder y sólo si está viva en V1 (FU2).</summary>
        private bool ShouldCastUltimate(int slot) =>
            _mods.Ultimate.IsReady && _mods.Ultimate.Live && slot == _mods.Ultimate.LeaderSlot;

        private bool ApplyUltimate(UltimateInfo ultimate)
        {
            LastUltimate = UltimateId.None;

            switch (ultimate.Id)
            {
                // Café 3: entrega 1 unidad de cada plato pendiente a cada pedido.
                case UltimateId.LibroMayor:
                {
                    bool delivered = _floor != null && _floor.DeliverToAllOrders(
                        UltimateTable.DeliverUnits(UltimateId.LibroMayor)) > 0;
                    if (!delivered) return false;
                    LastUltimate = UltimateId.LibroMayor;
                    return true;
                }

                // Espectáculo 3: 2 unidades por pedido y +15% de paciencia.
                case UltimateId.Encore:
                {
                    bool delivered = _floor != null && _floor.DeliverToAllOrders(
                        UltimateTable.DeliverUnits(UltimateId.Encore)) > 0;
                    if (!delivered) return false;
                    _floor.BoostPatience(1.15f);
                    LastUltimate = UltimateId.Encore;
                    return true;
                }

                // Míticos 3 (LIVE): 3×3 alrededor del objetivo a Postre y +1 potencia de party.
                case UltimateId.LunaDeSangre:
                {
                    ResolveResult result = _session.Board.ConvertCells(Area(TargetCell(), 2), Dish.Postre);
                    if (!_session.ResolveSkill(result)) return false;
                    _mods.AddPotency(1);
                    LastUltimate = UltimateId.LunaDeSangre;
                    return true;
                }

                // Míticos T2 (debug): fila + columna del objetivo a Bebida.
                case UltimateId.ZorroDeFuego:
                {
                    ResolveResult result = _session.Board.ConvertCells(RowAndColumn(TargetCell()), Dish.Bebida);
                    if (!_session.ResolveSkill(result)) return false;
                    LastUltimate = UltimateId.ZorroDeFuego;
                    return true;
                }

                default:
                    return false;
            }
        }

        /// <summary>Celda elegida por tap; sin seleccion, el centro del tablero.</summary>
        private BoardPos TargetCell()
        {
            if (_view != null && _view.SelectedPos.HasValue) return _view.SelectedPos.Value;
            return new BoardPos(_session.Board.Width / 2, _session.Board.Height / 2);
        }

        /// <summary>
        /// Área de ClearCross por nivel (D13): 1 = cruz de 5 celdas, 2 = 3×3, 3 = 5×5.
        /// Las celdas fuera del tablero se descartan en el modelo.
        /// </summary>
        private List<BoardPos> Area(BoardPos center, int level)
        {
            if (level <= 1) return Cross(center);

            int radius = level >= 3 ? 2 : 1;
            var cells = new List<BoardPos>((radius * 2 + 1) * (radius * 2 + 1));
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                    cells.Add(new BoardPos(center.X + dx, center.Y + dy));
            return cells;
        }

        private List<BoardPos> RowAndColumn(BoardPos center)
        {
            int width = _session.Board.Width;
            int height = _session.Board.Height;
            var cells = new List<BoardPos>(width + height);

            for (int x = 0; x < width; x++) cells.Add(new BoardPos(x, center.Y));
            for (int y = 0; y < height; y++) cells.Add(new BoardPos(center.X, y));
            return cells;
        }

        private static List<BoardPos> Cross(BoardPos center) => new List<BoardPos>(5)
        {
            center,
            new BoardPos(center.X + 1, center.Y),
            new BoardPos(center.X - 1, center.Y),
            new BoardPos(center.X, center.Y + 1),
            new BoardPos(center.X, center.Y - 1),
        };

        /// <summary>Entrega directa al libro de pedidos con el mismo feedback que un match.</summary>
        private bool ServeDirect(int slot, WaifuDefinitionSO def)
        {
            int units = _party.PotencyFor(slot);
            if (_floor == null || _floor.Book == null || units <= 0) return false;
            return !_floor.DeliverDirect(def.Element, units).IsEmpty;
        }

        /// <summary>
        /// Prioriza celdas con vecinos del elemento en fila/columna (mas peso si estan
        /// alineados: al convertir arman la linea). Empates por Y y X: determinista.
        /// </summary>
        private static List<BoardPos> PickConversionCells(BoardModel board, Dish element, int count)
        {
            var candidates = new List<(BoardPos Pos, int Score)>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.At(x, y) == element) continue;
                    candidates.Add((new BoardPos(x, y), Score(board, x, y, element)));
                }
            }

            candidates.Sort((p, q) =>
            {
                int byScore = q.Score.CompareTo(p.Score);
                if (byScore != 0) return byScore;
                int byY = p.Pos.Y.CompareTo(q.Pos.Y);
                return byY != 0 ? byY : p.Pos.X.CompareTo(q.Pos.X);
            });

            var picked = new List<BoardPos>(count);
            for (int i = 0; i < candidates.Count && picked.Count < count; i++)
                picked.Add(candidates[i].Pos);
            return picked;
        }

        private static int Score(BoardModel b, int x, int y, Dish dish)
        {
            int h = (x > 0 && b.At(x - 1, y) == dish ? 1 : 0)
                    + (x + 1 < b.Width && b.At(x + 1, y) == dish ? 1 : 0);
            int v = (y > 0 && b.At(x, y - 1) == dish ? 1 : 0)
                    + (y + 1 < b.Height && b.At(x, y + 1) == dish ? 1 : 0);
            return (h > v ? h : v) * 4 + h + v;
        }
    }
}
