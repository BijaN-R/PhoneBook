# PROMPT — Phase 5: Search Precision & Ranking

Continue from the previous phases.

Redesign phone-book search so it remains tolerant of realistic Persian typing mistakes but stops returning an overly broad set of weak matches.

Do not use an external search engine.

Do not add Elasticsearch, Lucene, fuzzy-search NuGet libraries, database full-text extensions, AI/embedding search or paid services.

The phone-book dataset is small enough for a lightweight in-memory solution.

## Current behavioral problem

The existing matcher effectively allows edit distance <= 2 for all strings.

That is too permissive for short Persian names.

Required reference behavior:

Searching:

`نسیری`

should be allowed to find:

`نصیری`

because it is a plausible one-character typo.

It must not return:

`عسگری`

merely because a generic edit-distance threshold considers it close enough.

This acceptance case is mandatory.

## Normalization

Keep and improve the existing PersianTextNormalizer rather than replacing it.

Continue to normalize:

- Arabic/Persian Yeh
- Arabic/Persian Kaf
- Persian/Arabic/English digits
- whitespace
- ZWNJ behavior

If needed, safely normalize common Unicode combining marks/diacritics for search.

Do not aggressively collapse unrelated Persian letters merely because they sound similar.

## Fuzzy algorithm

Replace the fixed global Levenshtein <= 2 decision with an adaptive token-level strategy.

Implement Damerau-Levenshtein or Optimal String Alignment distance in Core so adjacent transposition typos can be recognized without a package dependency.

Keep the algorithm deterministic.

Suggested fuzzy thresholds:

- normalized query length 1–2: no fuzzy edit-distance matching
- length 3–7: maximum edit distance 1
- length 8 or more: maximum edit distance 2, only if normalized similarity is at least approximately 0.80

Exact/prefix/substring matching is evaluated separately and is not subject to those fuzzy restrictions.

Numeric-only queries must not use fuzzy edit-distance matching.

For numeric queries, prioritize exact/prefix extension matching.

## Token strategy

Pre-tokenize normalized names and group titles.

Do fuzzy comparison against individual meaningful tokens rather than comparing every query to an entire long target indiscriminately.

For multi-word queries, require every meaningful query token to find a sufficiently strong match in the candidate.

Do not allow one weak fuzzy token to make an unrelated multi-word candidate pass.

## Ranking

Search must produce scored/ranked results rather than a boolean matched/not-matched list.

Create an application model such as:

`SearchResult`

with at least:

- GroupId
- GroupTitle
- Name
- Extension
- Score
- MatchKind if useful for diagnostics/UI

Create a match classification enum/model such as:

- ExactExtension
- ExactName
- ExactToken
- ExactGroup
- Prefix
- Substring
- FuzzyOneEdit
- FuzzyTwoEdits

A sensible ranking order is:

1. exact extension
2. exact normalized full name
3. exact name token
4. exact group title
5. prefix match
6. substring match
7. one-edit fuzzy match
8. two-edit fuzzy match

Use deterministic numerical scores internally.

The exact score numbers are implementation details, but the relative ordering above must hold.

Fuzzy distance 2 must score materially lower than distance 1.

Use existing group/display order as a deterministic tie breaker.

## Search index

Retain an in-memory pre-normalized search index because it avoids repeated normalization while the user types.

Move/index responsibilities according to the Application architecture established in Phase 1.

Search index records should precompute:

- normalized group title
- normalized name
- normalized extension
- name tokens
- group tokens

Refresh the index after successful mutations/imports that change phone-book data.

Do not refresh it before the database transaction has committed.

## UI behavior

Debounce live search input approximately 150–250 ms.

Limit displayed results to a sensible maximum such as 20.

Do not flood the page with every weak fuzzy match.

Results should remain deterministic between identical searches.

## Required behavior examples

At minimum ensure these principles:

- `نسیری` can match `نصیری`.
- `نسیری` must not match `عسگری` solely by fuzzy distance.
- exact names rank above fuzzy names.
- exact extension searches rank first.
- numeric extension queries do not return nearby numeric values through fuzzy matching.
- prefix matches rank above generic substring matches.
- empty query retains the application's intended current behavior.

## Architectural placement

Text normalization and edit-distance algorithm belong in Core.

Search use-case, ranking and result models belong in Application.

Database/index source implementation belongs behind Application persistence abstractions.

Live-search rendering/debounce belongs in Web.

## Out of scope

No external search package.

No phonetic AI model.

No embeddings.

No Elasticsearch/Lucene.

No logging work.

No broad testing initiative.

Do not modify exports.

## Completion criteria

The phase is complete when search is noticeably more precise, typo-tolerant for realistic one-character mistakes, ranked by match quality, and the mandatory نسیری/نصیری/عسگری example behaves correctly.

