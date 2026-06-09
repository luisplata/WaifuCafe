# Proposal: GDD & Roadmap Docs

## Intent
Two living doc artifacts (`GDD.md` + `ROADMAP.md`) defining the full "Anime Café Manager" vision. ~70% of planned features are missing from codebase — these docs align all future development.

## Scope

### In Scope
- `Assets/SDD/GDD.md` — Full game design document: systems, mechanics, economy, progression
- `Assets/SDD/ROADMAP.md` — Prioritized implementation roadmap with phases and tasks

### Out of Scope
- No code changes, no bug fixes, no new assets or scenes
- No fixing broken auto-assignment or V2 staff state machine

## Capabilities

### New Capabilities
- `gdd-document`: Living GDD as canonical design reference
- `roadmap-document`: Prioritized build plan with dependency-ordered phases

### Modified Capabilities
- None (first SDD docs for this project)

## Approach
- **GDD**: Single canonical file, 10 sections, progressive-disclosure style with Mermaid diagrams for flows
- **Roadmap**: 5 phases, dependency-ordered, feature-grouped with task breakdowns
- **Location**: `Assets/SDD/` — visible in Unity Project window, source-controlled
- **Source**: User-provided GDD spec + codebase reverse-engineering from exploration

## Document Outline

### GDD.md (10 sections)
| # | Section | Key Content |
|---|---------|-------------|
| 1 | Overview | Concept, genre (Anime Café Manager), target audience |
| 2 | Core Loop | Venue → Prepare → Team → Run → Rewards (Mermaid) |
| 3 | Waifu System | Recruitment, Specialties (Cook/Clean/Charm/Cheer), Talents, Affinities, Synergies, Bonding, Transformation |
| 4 | Customer System | Regular/VIP/Combo types, patience, repeat customers, satisfaction |
| 5 | Combo System | Order chains, timing windows, combo tiers, bonuses |
| 6 | Event System | Runtime events (positive/negative/random), triggers |
| 7 | Venue System | Collection-based progression, unlock conditions, venue rules |
| 8 | Economy | Gold (progression) vs Tickets (gacha), sinks, prices |
| 9 | Gacha System | Carnival games, waifu fragments, duplicate conversion, pity system |
| 10 | Preparation | Pre-run consumables: food buffs, decorations, outfits |

### ROADMAP.md (5 phases)
| Phase | Focus | Key Deliverables | Depends On |
|-------|-------|-----------------|------------|
| 0 | Foundation Bugs | Fix auto-assignment TODO, fix V2 UpdatePhase empty | — |
| 1 | Core Gameplay | Win/lose conditions, scoring, satisfaction rating, VIP behavior | P0 |
| 2 | Waifu + Gacha | Specialties, Talents, Synergies, Bonding, Gacha, Venues | P1 |
| 3 | Depth Systems | Combos, Events, Preparation, repeat customers | P2 |
| 4 | Economy + Polish | Gold/Tickets balance, UI polish, sound, test coverage | P3 |

## Implementation Status
| Feature | Status | Detail |
|---------|--------|--------|
| Game loop state machine | ✅ Complete | Preparacion→Transicion→Game→GameOver |
| Customer spawn/queue | ✅ Complete | 532 lines, patience, removal, stats |
| Drag-drop assignment | ✅ Complete | UI + world targets, manual assignment works |
| Staff pool | ⚠️ Partial | Pool works, V2 state machine broken |
| Gold rewards | ⚠️ Partial | RegardsManager adds gold, no sinks |
| Customer types | ⚠️ Stub | Regular/VIP/Impatient enum only |
| Auto-assignment | ❌ Broken | TryAssignCustomer coroutine TODO'd |
| V2 Staff UpdatePhase | ❌ Broken | Empty method, never advances |
| Waifu (specialties/talents) | ❌ Missing | No system |
| Venues | ❌ Missing | No venue concept |
| Gacha | ❌ Missing | No system |
| Combos | ❌ Missing | No order chains |
| Events | ❌ Missing | No runtime events |
| Win/lose conditions | ❌ Missing | Only 60s timer → GameOver |
| Preparation | ❌ Missing | No pre-run loadout |
| Tests | ❌ Missing | 0 tests in project |

## Risks
| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Doc diverges from implementation | Medium | Living doc — update alongside code |
| Scope creep (new systems in GDD) | High | Pin to user's original GDD spec, defer extras |
| Phase 0 fix scope expands | Medium | Strictly unblock broken paths only |
| GDD too large to maintain | Low | Single file, progressive disclosure sections |

## Rollback Plan
`git revert` on `Assets/SDD/` if docs contain errors. No code changes = zero runtime risk.

## Dependencies
- User's GDD specification text (provided during exploration)
- Codebase exploration results from `sdd/gdd-roadmap-docs/explore`

## Success Criteria
- [ ] `Assets/SDD/GDD.md` exists with all 10 sections
- [ ] `Assets/SDD/ROADMAP.md` exists with 5 phases covering all missing features
- [ ] Both docs pass markdown linting
- [ ] Any developer can read the GDD and understand the full game vision
