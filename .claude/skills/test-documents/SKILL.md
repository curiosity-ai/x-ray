---
name: test-documents
description: How the shared curiosity-ai/test_documents fixture corpus works across the X-Ray family - its folders, how each repository consumes it as a git submodule, how tests find it, and how to add a fixture with its licence. Use when a test needs a real document, when tests skip with "test_documents not found", when adding or licensing a fixture, or when bumping the submodule pin in any X-Ray repository.
---

# The `test_documents` corpus

[curiosity-ai/test_documents](https://github.com/curiosity-ai/test_documents) holds the real
files every X-Ray package is tested against. It is a fork of `xberg-io/test_documents`, the
corpus X-Ray.Content's Rust upstream tests with, plus the X-Ray additions, so it is a superset
of upstream and can stand in for it.

## Layout

One folder per format, named for the format: `docx/`, `xlsx/`, `pptx/`, `doc/`, `pdf/`,
`email/`, `html/`, `images/`, `archives/`, ... The X-Ray additions:

| Folder | Read by |
|---|---|
| `psd/` | X-Ray.Psd, X-Ray.Content |
| `ics/` | X-Ray.Ics |
| `ps/`, `xps/` | X-Ray.PS |
| `onenote/` | X-Ray.OneNote |
| `email/` (and per-source subfolders) | X-Ray.Email, X-Ray.Content |

- Binaries are committed directly in git: no LFS. (Older notes in x-ray-content describe a
  bucket fetch through `corpus.lock.json`; check the corpus's own README for which applies to the
  revision you have.)
- `vendored/<project>/` and other third-party sets carry their own `LICENSE` and a `README.md`
  naming the source.
- `ground_truth/` and similar folders hold expected outputs upstream ships. X-Ray.Content's
  `*-results-rust.json` goldens are **generated locally** by its reference generator and never
  committed.

## Consuming it

Each repository carries it as a submodule at `test_documents/`:

```sh
git submodule update --init --depth 1 test_documents
```

Tests resolve the corpus root the same way everywhere:

1. `XRAY_TEST_DOCUMENTS`, if set.
2. Otherwise walk up from `AppContext.BaseDirectory` to the first directory named
   `test_documents` (the submodule, or a clone next to the repository).

No root: corpus tests **skip** with a message naming the submodule command. Root present but a
fixture missing: the test **fails**, because the pin is behind the test.

In agent sessions the corpus is usually cloned at `/home/user/test_documents`; point
`XRAY_TEST_DOCUMENTS` at it instead of initialising a second copy.

## Adding a fixture

1. Put the file in the folder for its format, in a clone of `curiosity-ai/test_documents`. Name
   it for what it exercises: `psd/text-warp-arc.psd`, not `psd/test3.psd`.
2. Licence:
   - Made by you for the test: fine; say so in the folder's README if it has one.
   - Third-party: confirm the licence allows redistribution, then add or extend the
     `LICENSE`/`README.md` next to it with the source URL and licence. Never add a file whose
     licence you cannot state.
   - Never add real personal data (mailboxes, scans of private documents).
3. Keep it small: the smallest file that still shows the behaviour.
4. Commit and push to `curiosity-ai/test_documents` (default branch `main`; open a PR if you
   cannot push).
5. Bump the pin in each repository whose tests use it:

   ```sh
   git -C test_documents fetch origin main && git -C test_documents checkout origin/main
   git add test_documents && git commit -m "Bump test_documents for <reason>"
   ```

6. Reference the file through the resolver, by relative path under the corpus root.

Expected outputs that belong to one package's tests (snapshots, manifests) live with those
tests, not in the corpus, unless they are large.
