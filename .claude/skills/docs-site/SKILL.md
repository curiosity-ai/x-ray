---
name: docs-site
description: Add or update a page of the X-Ray documentation, which lives in the x-ray/ folder of curiosity-ai/documentation (built with Neko, published at docs.curiosity.ai/x-ray). Use when documenting a package, adding a section for a new family member, fixing a docs page, after changing a package's API or behavior, or when the docs build or link check fails.
---

# The X-Ray docs

The docs are **not in this repository**. They are the `x-ray/` folder of
[curiosity-ai/documentation](https://github.com/curiosity-ai/documentation) (clone it with
`add_repo` in an agent session; it lands at `/home/user/documentation`), one Neko project among
the others in that repository, published at <https://docs.curiosity.ai/x-ray/>. Nothing in the
X-Ray repositories builds or publishes docs.

## When a docs change is required

A change to a package's public API, options, defaults, behavior, limits, licence, install steps
or supported formats is not finished until its page is updated. Open the PR against
`curiosity-ai/documentation` (branch from `main`, never against a source repository or an
upstream), and link it and the code PR to each other. If no page changes, say so in the code PR.

## Where things are

```
x-ray/neko.yml                 section config: branding, url (docs.curiosity.ai/x-ray)
x-ray/index.md                 landing: hero, package cards, how they fit, paths
x-ray/getting-started/         index, installation, choosing-a-package
x-ray/<package>/               content, office, psd, email, ics, ps, onenote: index.yml + index.md plus topic pages
x-ray/contributing/            development notes, working on the docs
x-ray/assets/logo.png          the family logo
```

The header's Developers menu in the documentation root `neko.yml` (and every section's copy of it)
links `/x-ray/`; `open-source.md` has the X-Ray card. A new package gets a folder, a card in
`x-ray/index.md`, rows in `getting-started/installation.md` and `choosing-a-package.md`.

## Setup, build and check

```sh
dotnet tool update --global Neko          # installs when missing
export PATH="$PATH:$HOME/.dotnet/tools"   # the tool is not on PATH by default
cd /home/user/documentation
Neko build -i x-ray -o /tmp/x-ray-site    # just this section (links with /x-ray/ prefix will not resolve alone)
Neko check-links -i .                     # the whole site; the only check that resolves /x-ray/ links
```

The documentation repository's `.claude/CLAUDE.md` is the reference for Neko, its components
(`.claude/skills/<name>/`) and its tone rules; read it before writing.

## Writing a page

Front matter on every page: `label`, `icon` (UIcons regular-rounded name, or `brands-<name>`),
`order`, optional `tags`. Folders have an `index.yml` with `label`, `icon`, `order`.

- **Links** are root-relative from the site root, extensionless, and carry the section prefix:
  `/x-ray/content/`, `/x-ray/getting-started/installation`, assets `/x-ray/assets/logo.png`.
  A link without `/x-ray` leaves the section and breaks.
- **Code samples come from the real API.** Copy from the package README or tests, or read the
  source, and compile them (a scratch console project outside the repository that references the
  package). If you cannot verify a sample, write the page without it and add `!!! warning Stub`.
- **Tone**: reference documentation. State what a thing does and does not do. No "powerful",
  "seamless", "leverage", welcome openers or exclamation marks; no em dashes in prose.
- Do not write a vendor's name that a licence forbids you to repeat; see `CLAUDE.md`.
