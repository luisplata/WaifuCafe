# Skill Registry

Generated: 2026-06-09
Project: WaifuCafe

## User Skills

### work-unit-commits
- Path: `~/.config/opencode/skills/work-unit-commits/SKILL.md`
- Description: Structure commits as deliverable work units instead of file-type batches, with tests and docs kept beside the code they verify.
- Trigger: when implementing a change, preparing commits, splitting PRs, or planning chained or stacked PRs.
- Author: gentleman-programming

### comment-writer
- Path: `~/.config/opencode/skills/comment-writer/SKILL.md`
- Description: Write warm, direct, human comments for PRs, issues, reviews, chats, and async collaboration.
- Trigger: when drafting or posting feedback, review comments, maintainer replies, Slack messages, or GitHub comments.
- Author: gentleman-programming

### cognitive-doc-design
- Path: `~/.config/opencode/skills/cognitive-doc-design/SKILL.md`
- Description: Design documentation that reduces reader cognitive load through progressive disclosure, chunking, signposting, tables, checklists, and recognition over recall.
- Trigger: when writing guides, READMEs, RFCs, onboarding docs, architecture docs, or review-facing documentation.
- Author: gentleman-programming

### gentle-ai-chained-pr
- Path: `~/.config/opencode/skills/chained-pr/SKILL.md`
- Description: Split large changes into chained or stacked pull requests that protect reviewer focus and stay within Gentle AI's 400-line cognitive review budget.
- Trigger: when a PR would exceed 400 changed lines, when planning chained PRs, stacked PRs, or reviewable slices.
- Author: gentleman-programming

### issue-creation
- Path: `~/.config/opencode/skills/issue-creation/SKILL.md`
- Description: Issue creation workflow for Agent Teams Lite following the issue-first enforcement system.
- Trigger: When creating a GitHub issue, reporting a bug, or requesting a feature.
- Author: gentleman-programming

### branch-pr
- Path: `~/.config/opencode/skills/branch-pr/SKILL.md`
- Description: PR creation workflow for Agent Teams Lite following the issue-first enforcement system.
- Trigger: When creating a pull request, opening a PR, or preparing changes for review.
- Author: gentleman-programming
- Version: 2.0

### judgment-day
- Path: `~/.config/opencode/skills/judgment-day/SKILL.md`
- Description: Parallel adversarial review protocol that launches two independent blind judge sub-agents simultaneously to review the same target, synthesizes their findings, applies fixes, and re-judges until both pass or escalates after 2 iterations.
- Trigger: When user says "judgment day", "judgment-day", "review adversarial", "dual review", "doble review", "juzgar", "que lo juzguen".
- Author: gentleman-programming

### go-testing
- Path: `~/.config/opencode/skills/go-testing/SKILL.md`
- Description: Go testing patterns for Gentleman.Dots, including Bubbletea TUI testing.
- Trigger: When writing Go tests, using teatest, or adding test coverage.
- Author: gentleman-programming
- Version: 1.0

### skill-creator
- Path: `~/.config/opencode/skills/skill-creator/SKILL.md`
- Description: Creates new AI agent skills following the Agent Skills spec.
- Trigger: When user asks to create a new skill, add agent instructions, or document patterns for AI.
- Author: gentleman-programming
- Version: 1.0

## Project Conventions

No project-level convention files found (no AGENTS.md, CLAUDE.md, .cursorrules, etc.).

## Compact Rules

### work-unit-commits
```
Work units over file-type batches. Each commit = one deliverable slice (feature, fix, refactor).
Tests + docs beside the code they verify.
Commit message format: conventional commits (type(scope): description).
```

### comment-writer
```
Warm, direct, human tone. Avoid corporate speak. Be specific and technical when appropriate.
Start with the main point. Use CAPS for emphasis, not anger.
```

### cognitive-doc-design
```
Progressive disclosure: short first, details later. Chunking: one concept per section.
Signposting: clear headings. Tables over prose for comparisons.
Recognition over recall: use checklists, don't make reader remember.
```

### gentle-ai-chained-pr
```
400-line cognitive review budget per PR. Split into chained/stacked PRs when exceeded.
Each PR must be independently reviewable. Work-unit commits within each PR.
```

### issue-creation
```
Issue-first enforcement. Create issue before PR. Include: context, expected behavior, actual behavior,
reproduction steps, environment. Use templates when available.
```

### branch-pr
```
Issue-first: PR must reference an issue. Use conventional commits. Keep PR focused.
Include changelog entries for user-facing changes.
```

### judgment-day
```
Two blind judges run in parallel. Synthesize findings. Apply fixes. Re-judge.
Max 2 iterations per round. If still failing after 2, escalate to human.
```

### go-testing
```
Use teatest for Bubbletea TUIs. Table-driven tests. Test package internals when needed.
Mock external dependencies. Keep tests fast and focused.
```

### skill-creator
```
Frontmatter with name, description, trigger, license, metadata.
Clear Purpose section. Step-by-step instructions. Rules section.
Include examples when helpful.
```
