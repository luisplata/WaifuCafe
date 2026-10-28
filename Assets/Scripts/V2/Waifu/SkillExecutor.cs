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
    /// </summary>
    public sealed class SkillExecutor
    {
        private readonly WaifuPartyRuntime _party;
        private readonly BoardSession _session;
        private readonly BoardView _view;
        private readonly CafeFloor _floor;
        private readonly int[] _cooldown = new int[WaifuPartyRuntime.MaxSlots];

        public SkillExecutor(WaifuPartyRuntime party, BoardSession session)
        {
            _party = party;
            _session = session;
            _view = session != null ? session.GetComponentInChildren<BoardView>() : null;
            _floor = UnityEngine.Object.FindFirstObjectByType<CafeFloor>();
        }

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
        /// arranca su cooldown. Si no valida, no toca nada.
        /// </summary>
        public bool CommitFire(int slot)
        {
            if (!CanFire(slot)) return false;
            _party.Spend(slot);
            _cooldown[slot] = Mathf.Max(0, _party[slot].CooldownMatches);
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
            return Apply(def);
        }

        /// <summary>Sin cinemática: validar, comprometer y ejecutar. El costo no se devuelve.</summary>
        public bool TryFire(int slot) => CanFire(slot) && CommitFire(slot) && ExecuteFire(slot);

        private bool Apply(WaifuDefinitionSO def)
        {
            switch (def.SkillArchetype)
            {
                case SkillArchetype.ClearCross:
                    return _session.ResolveSkill(_session.Board.ClearCells(Cross(TargetCell())));

                case SkillArchetype.ConvertToElement:
                {
                    List<BoardPos> cells = PickConversionCells(_session.Board, def.Element, def.Potency);
                    if (cells.Count == 0) return false;
                    return _session.ResolveSkill(_session.Board.ConvertCells(cells, def.Element));
                }

                case SkillArchetype.InstantServeUnits:
                    return ServeDirect(def);

                case SkillArchetype.ReshuffleBoard:
                    if (!_session.Board.ReshuffleWithMove()) return false;
                    _session.SyncBoard();
                    return true;

                default:
                    // DelayEviction esta reservado: la expulsion no existe hasta PR9.
                    return false;
            }
        }

        /// <summary>Celda elegida por tap; sin seleccion, el centro del tablero.</summary>
        private BoardPos TargetCell()
        {
            if (_view != null && _view.SelectedPos.HasValue) return _view.SelectedPos.Value;
            return new BoardPos(_session.Board.Width / 2, _session.Board.Height / 2);
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
        private bool ServeDirect(WaifuDefinitionSO def)
        {
            if (_floor == null || _floor.Book == null || def.Potency <= 0) return false;
            return !_floor.DeliverDirect(def.Element, def.Potency).IsEmpty;
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
