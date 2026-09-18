# PROMPT — Phase 5: Search Precision & Ranking

Continue from the previous phases.

Improve phone-book search precision while keeping tolerance for realistic Persian typing mistakes.

Do not use external search engines, Elasticsearch, Lucene, fuzzy-search NuGet packages, database full-text extensions, AI/embedding search, phonetic search, or paid services.

The phone-book dataset is small enough for a lightweight deterministic in-memory solution.

## Goal

The search system must become significantly more precise without losing useful tolerance for common Persian typing mistakes.

The current search is too permissive because short names may effectively match with edit distance up to 2.

Mandatory reference behavior:

- Searching `نسیری` may match `نصیری`, because it is a plausible one-character typo.
- Searching `نسیری` must not match `عسگری` merely because a generic edit-distance threshold considers it close enough.
- Searching `آقای نسیری` may match `آقای نصیری`.
- Searching `آقای نسیری` must not match `آقای عسگری`.

These behaviors are mandatory.

---

## Architectural constraints

Follow the architecture established in previous phases.

Place responsibilities as follows:

- Persian text normalization: `PhoneBook.Core`
- Tokenization helpers: `PhoneBook.Core`
- Edit-distance algorithm: `PhoneBook.Core`
- Search use-case and ranking logic: `PhoneBook.Application`
- Search result models: `PhoneBook.Application`
- Retrieval of source records: behind Application persistence abstractions
- In-memory search index ownership: Application
- Live-search rendering and debounce: `PhoneBook.Web`

Do not introduce broad architectural rewrites outside what is required for this phase.

Do not allow Application to reference EF Core, SQLite, or Web.

---

# 1. Persian normalization

Keep and improve the existing `PersianTextNormalizer`.

Do not replace it with a third-party package.

Continue to normalize:

- Arabic Yeh → Persian Yeh
- Arabic Kaf → Persian Kaf
- Persian digits → ASCII digits for search
- Arabic-Indic digits → ASCII digits for search
- whitespace
- ZWNJ behavior
- Unicode normalization

If needed, safely remove common Unicode combining marks and Persian/Arabic diacritics for search.

Do not aggressively normalize unrelated Persian characters merely because they are phonetically similar.

Do not implement phonetic matching.

Normalization must be deterministic and idempotent.

---

# 2. Tokenization

Normalize and tokenize query strings, names, and group titles using the same rules.

Create or adapt a Core-level tokenization helper.

Tokens should be separated using whitespace and safe textual separators as appropriate.

Do not repeatedly normalize and tokenize every stored record while the user is typing.

Precompute normalized values and tokens in the in-memory index.

Each indexed entry must retain at least:

- `EntryId`
- `GroupId`
- original GroupTitle
- original Name
- original Extension
- `GroupDisplayOrder`
- `EntryDisplayOrder`
- normalized full name
- normalized name tokens
- normalized full group title
- normalized group title tokens
- normalized extension

Search must not perform additional database queries merely to obtain ordering metadata.

---

# 3. Low-signal Persian title tokens

Treat the following title/honorific tokens as low-signal tokens:

- `آقای`
- `خانم`
- `دکتر`
- `مهندس`
- `جناب`
- `سرکار`

Rules:

- When other meaningful text tokens exist in the same query, title tokens are not required by the "every meaningful token must match" rule.
- Exact matching title tokens may contribute a small ranking boost.
- Never fuzzy-match title tokens.
- If the query consists only of one or more title tokens, perform exact token matching.
- A title-only query must not be treated as empty.

Example:

Query:

`آقای نسیری`

should effectively require the meaningful token:

`نسیری`

while `آقای` may provide an exact-match boost if present.

---

# 4. Numeric intent words

Treat these query tokens as generic numeric intent words:

- `داخلی`
- `شماره`
- `تلفن`
- `extension`

When a query contains at least one numeric token:

- ignore these generic numeric intent words for candidate eligibility.
- they may not independently cause a result to match.
- they do not need to be present in the candidate.

Example:

`داخلی 188`

should effectively search for numeric token `188`.

---

# 5. Numeric token detection

`NormalizeForSearch` must convert Persian and Arabic-Indic digits to ASCII digits before numeric classification.

A query or token is numeric-style when all non-separator characters are ASCII digits.

Allowed separators:

- whitespace
- `-`
- `/`
- `.`

Examples that must be recognized as numeric-style after normalization:

- `۱۸۸`
- `188`
- `188-189`
- `188 / 189`

---

# 6. Fuzzy algorithm

Remove the fixed broad `Levenshtein <= 2` matching behavior.

Implement **Optimal String Alignment (OSA) distance** in `PhoneBook.Core`.

Do not implement full unrestricted Damerau-Levenshtein unless the project already contains a correct implementation that can be reused.

OSA must support:

- insertion
- deletion
- substitution
- adjacent transposition

Keep the implementation deterministic and dependency-free.

The fuzzy algorithm operates on normalized tokens.

Never apply fuzzy edit-distance matching to an entire multi-word query string.

---

# 7. Adaptive fuzzy thresholds

Fuzzy thresholds are based on the length of each normalized query token.

Use these rules exactly:

### Token length 1–2

No fuzzy edit-distance matching.

Only exact, prefix, and allowed substring matching may apply.

### Token length 3–7

Maximum fuzzy edit distance:

`1`

### Token length 8 or more

Maximum fuzzy edit distance:

`2`

but distance 2 is accepted only when normalized similarity is at least:

`0.80`

Similarity formula:

`similarity = 1.0 - distance / max(queryToken.Length, targetToken.Length)`

This formula and threshold are intentional.

With this rule, edit distance 2 effectively requires relatively long tokens before it is accepted.

Example:

`نسیری`

has length 5.

Therefore its maximum fuzzy distance is 1.

It must never receive a distance-2 allowance merely because it appears inside a longer phrase such as:

`آقای نسیری`

---

# 8. Exact, prefix, substring, and fuzzy evaluation order

For each token comparison, use cheap checks first:

1. exact
2. prefix
3. substring
4. fuzzy

Do not run fuzzy comparison if an exact, prefix, or stronger accepted match already exists.

For text fields:

- do not use generic substring matching for query tokens shorter than 3 characters.

This restriction applies to:

- Name
- GroupTitle

It does not apply to Extension.

Extension may always use exact, prefix, and substring matching regardless of numeric token length.

---

# 9. Numeric query behavior

For numeric-only queries:

- disable fuzzy edit-distance matching entirely.
- search Extension using:
  - exact
  - prefix
  - substring
- Name and GroupTitle may only be searched using exact or prefix token matching.
- do not fuzzy-match numeric tokens against any field.

Numeric extension searches must never return nearby numbers purely because they have a small edit distance.

Example:

Searching:

`188`

must not return:

`189`

through fuzzy matching.

For mixed queries such as:

`فروش 188`

apply text rules to `فروش` and numeric rules to `188`.

---

# 10. Candidate field matching

A candidate entry may match through:

- Extension
- full normalized Name
- individual Name tokens
- full normalized GroupTitle
- individual GroupTitle tokens

For multi-token queries, different query tokens may match different fields of the same candidate.

Example:

Query:

`فروش 188`

may be a valid match when:

- `فروش` matches GroupTitle
- `188` matches Extension

Do not require all query tokens to match the same field.

The unit of evaluation is one indexed phone-book entry.

---

# 11. Multi-token candidate eligibility

For every meaningful query token:

- determine whether it has an accepted match somewhere in the candidate.
- title tokens may be optional according to the title-token rules.
- generic numeric intent words may be ignored when numeric tokens are present.

Every required meaningful token must match.

Do not allow one strong token to compensate for another completely unmatched token.

Example:

Query:

`علی نسیری`

must not match a candidate containing only:

`علی رضایی`

even though `علی` is exact.

---

# 12. Match kinds

Introduce a deterministic match classification.

Use an enum or equivalent model containing at least:

- `ExactExtension`
- `ExactFullName`
- `ExactNameToken`
- `ExactFullGroupTitle`
- `ExactGroupToken`
- `Prefix`
- `Substring`
- `FuzzyOneEdit`
- `FuzzyTwoEdits`

Additional internal match kinds are acceptable if useful, but do not make the public model unnecessarily complex.

---

# 13. Search result model

Create or adapt a result model such as:

`SearchResult`

It must contain at least:

- `EntryId`
- `GroupId`
- `GroupTitle`
- `Name`
- `Extension`
- `GroupDisplayOrder`
- `EntryDisplayOrder`
- `Score`
- `MatchKind`

If another consistent existing model is preferable, it may be extended instead.

Do not expose internal algorithm implementation details unnecessarily to the UI.

---

# 14. Ranking priorities

Search must return scored and deterministically ordered results.

Use deterministic numeric scoring.

The exact numeric values are implementation details, but the relative priority must be:

1. exact extension
2. exact normalized full name
3. exact name token
4. exact normalized full group title
5. exact group token
6. prefix match
7. substring match
8. one-edit fuzzy match
9. two-edit fuzzy match

Additional rules:

- fuzzy distance 2 must score materially lower than fuzzy distance 1.
- exact names must rank above fuzzy names.
- exact extension results must rank first.
- prefix matches must rank above substring matches.

---

# 15. Multi-token ranking

Do not rank a multi-token candidate using only the single strongest match.

For each required meaningful query token:

- determine that token's best accepted match against the candidate.
- assign a quality/score for that token.

A candidate's multi-token ranking must consider all required tokens.

Use this strategy:

1. all required meaningful tokens must match.
2. compare candidates by the quality of their weakest required token match.
3. after that, compare by aggregate or average per-token match score.
4. optional title-token exact matches may add only a small bonus.
5. deterministic ordering metadata is used only after match-quality comparison.

This ensures:

- exact + exact outranks exact + fuzzy.
- a single excellent token match cannot hide a weak match on another required token.

For single-token queries, the candidate's best match is sufficient.

---

# 16. Deterministic tie breaking

When candidates have equivalent search quality, use:

1. `GroupDisplayOrder`
2. `EntryDisplayOrder`
3. normalized Name
4. normalized Extension
5. `EntryId`

This ordering must remain deterministic between identical searches.

Do not depend on incidental database or collection iteration order.

---

# 17. Empty query behavior

An empty or whitespace-only query returns all active indexed records.

Ordering must be:

1. `GroupDisplayOrder`
2. `EntryDisplayOrder`
3. stable fallback ordering if necessary

Do not change the underlying search semantics to only return 20 records.

The live-search UI may display only the first approximately 20 results.

Application search itself should retain the complete result set unless the existing public contract already defines a limit.

---

# 18. In-memory search index

Retain the lightweight in-memory index.

The index must store pre-normalized and pre-tokenized values.

Do not normalize every candidate during each keystroke.

Keep index reads thread-safe.

Avoid unnecessary allocations where practical, but do not over-optimize this small dataset.

Do not introduce a separate search server or database index subsystem.

---

# 19. Index refresh

Refresh the in-memory index after successful operations that change searchable phone-book data, including:

- entry create
- entry update
- entry delete
- entry activate/deactivate
- group update where title or active state changes
- group delete
- completed import/restore operations

Refresh only after the relevant database transaction has committed successfully.

Do not refresh the index before persistence succeeds.

If multiple mutations are part of one logical operation, refresh once after completion rather than after each individual write.

---

# 20. Live-search UI behavior

In Web:

- debounce search input approximately 150–250 ms.
- use the Application search service.
- display a sensible maximum of approximately 20 live results.
- do not display every weak fuzzy candidate.
- preserve RTL/Persian presentation.
- do not expose numeric internal Score by default.

If useful, MatchKind may be used internally for diagnostics but should not clutter normal UI.

---

# 21. Mandatory behavior examples

The completed implementation must satisfy all of these:

### Persian typo tolerance

`نسیری`

must match:

`نصیری`

### Excessive fuzzy prevention

`نسیری`

must not match:

`عسگری`

solely because of edit distance.

### Multi-word title handling

`آقای نسیری`

must be able to match:

`آقای نصیری`

### Full-string fuzzy prevention

`آقای نسیری`

must not match:

`آقای عسگری`

through whole-phrase edit distance.

### Exact vs fuzzy ranking

An exact `نسیری` result must rank above a fuzzy `نصیری` result.

### Numeric behavior

Exact Extension `188` must rank above all non-exact alternatives.

Searching `188` must not fuzzy-match `189`.

### Prefix behavior

A prefix match must rank above a generic substring match.

### Multi-token behavior

For query:

`علی نسیری`

a candidate with:

- exact `علی`
- exact `نسیری`

must rank above a candidate with:

- exact `علی`
- fuzzy `نصیری`

### Cross-field behavior

Query:

`فروش 188`

may match an entry where:

- GroupTitle matches `فروش`
- Extension matches `188`

### Title-only behavior

Query:

`آقای`

must perform exact title-token matching.

It must not:

- use fuzzy matching
- be treated as an empty query

### Empty query

An empty query returns all active records in deterministic display order.

---

# 22. Focused regression tests

Add focused automated regression tests for the search behavior introduced by this phase.

Do not start a broad testing initiative.

At minimum add tests covering:

- `نسیری` → `نصیری` = match
- `نسیری` → `عسگری` = no match
- `آقای نسیری` → `آقای نصیری` = match
- `آقای نسیری` → `آقای عسگری` = no match
- exact name ranks above fuzzy name
- exact extension ranks above all weaker matches
- numeric query does not fuzzy-match nearby extensions
- distance-2 token shorter than the effective allowed threshold is rejected
- prefix ranks above substring
- multi-token exact+exact ranks above exact+fuzzy
- multi-token query requires every meaningful token
- cross-field matching works for `فروش 188`
- title-only query is not treated as empty
- empty query ordering is deterministic
- adjacent transposition typo is handled by OSA where threshold allows it

Keep tests focused on the matching/ranking contract.

---

# 23. Performance expectations

The dataset is small.

Prefer clarity and determinism over complex optimization.

Expected strategy:

- one in-memory index
- normalized values cached once
- token lists cached once
- cheap match checks before fuzzy distance
- fuzzy only when necessary
- no database query per search result
- no database query per keystroke beyond index refresh events

Do not introduce caching frameworks.

---

# 24. Out of scope

Do not add:

- Elasticsearch
- Lucene
- external fuzzy-search packages
- database full-text search
- AI/embedding search
- phonetic matching
- paid services
- broad logging changes
- broad testing initiatives
- unrelated architecture refactors

Do not modify:

- DOCX export
- PDF export
- PNG export

unless a compile-time dependency change from prior architecture phases strictly requires a small adjustment.

---

# 25. Completion criteria

This phase is complete when:

- search is materially more precise than the previous implementation.
- realistic one-character Persian mistakes still work.
- short Persian names no longer receive overly broad fuzzy matches.
- fuzzy matching is token-based rather than whole-query-based.
- numeric searches never use fuzzy edit distance.
- multi-token queries require all meaningful tokens.
- multi-token ranking considers every required token.
- cross-field matching works within the same indexed entry.
- results are deterministically ranked.
- exact matches consistently outrank weaker matches.
- the mandatory `نسیری / نصیری / عسگری` behavior is correct.
- the focused regression tests pass.
- the full solution builds successfully.

# 26. Implementation discipline
Before creating new classes or helpers, inspect the existing search, normalization, repository and application-service implementation and extend/reuse them where responsibilities already fit.
Do not duplicate existing normalization, tokenization, search-index or result-model concepts under new names.
Prefer modifying the smallest number of files necessary to satisfy this phase.
Avoid unrelated cleanup, renaming, formatting-only refactors, or architectural redesign.

# 27. Execution strategy
Implement this phase incrementally:
1. Core matching/tokenization primitives
2. Application search/ranking
3. Index metadata and refresh integration
4. Web debounce/display changes
5. focused regression tests
Build after each major step and fix compile errors before proceeding.
Do not generate a large speculative implementation across all layers before validating the previous step.