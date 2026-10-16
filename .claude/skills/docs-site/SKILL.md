---
name: docs-site
description: Add or update a page of the X-Ray documentation site (docs/ in curiosity-ai/x-ray), built with Neko and published to x-ray.curiosity.ai through gh-pages. Use when documenting a package, adding a section for a new family member, fixing a docs page, changing the site's navigation or branding, or when the docs build or link check fails.
---

# The X-Ray docs site

`docs/` is one Neko project. `docs/neko.yml` configures it; every `.md` is a page; every folder
is a sidebar section configured by its `index.yml`.

## Setup

```sh
dotnet tool update --global Neko          # installs when missing
export PATH="$PATH:$HOME/.dotnet/tools"   # the tool is not on PATH by default
Neko --version                            # executable is `Neko`; `neko` only where symlinked
```

## Build and check

```sh
./build.sh                                # neko build -i docs -o .build (build.bat on Windows)
Neko check-links -i docs                  # must end with "No broken links found"
Neko watch -i docs                        # live preview at http://localhost:5000
```

`.build/`, `docs/.neko/` and `docs/.neko-cache/` are build output and gitignored.

## Structure

```
docs/neko.yml                  branding, header links (one "Packages" dropdown), url, footer
docs/index.md                  landing: hero, package cards, how they fit, paths
docs/getting-started/          index, installation, choosing-a-package
docs/<package>/                content, office, psd, email, ps, onenote: index.yml + index.md
docs/contributing/             development notes, working on the docs
docs/assets/logo.png           the family logo (branding logo and favicon)
```

## Writing a page

Front matter on every page:

```md
---
label: Output formats      # sidebar text
icon: file-code            # UIcons regular-rounded name, or brands-<name>
order: 2                   # lower sorts first within the folder
tags: [extraction]
---

# Output formats
```

Folders: `index.yml` with `label`, `icon`, `order` (and `expanded: true` to open by default).

- **Links**: root-relative and extensionless: `/content/`, `/getting-started/installation`.
  Assets: `/assets/logo.png`. Never `assets/...` without the slash (it resolves per page and
  breaks), never the full `https://x-ray.curiosity.ai` for internal links.
- **Icons** must exist in UIcons, or they render blank. Check a name against the built CSS:
  `grep -c "fi-rr-<name>:before" .build/assets/uicons-regular-rounded.css`.
- **Components** used here: `[!hero ...]`, `:::: card-grid` / `::: card {...}`,
  `!!! warning Title ... !!!`, `+++ Tab ... +++`, `[!ref icon=... text=...](url)`. The full
  reference for every Neko component is in `curiosity-ai/documentation/.claude/skills/<name>/`
  (cloned at `/home/user/documentation` in agent sessions).
- **Code samples come from the real API.** Copy from the package README or tests, or read the
  source. If you cannot verify a sample, do not guess: write the page without it and add
  `!!! warning Stub` explaining what is missing.
- **Tone**: reference documentation. State what a thing does and does not do. No "powerful",
  "seamless", "leverage", welcome openers or exclamation marks; no em dashes in prose. The
  documentation repository's `.claude/CLAUDE.md` ("Tone and voice") has the full list.

## A section for a new package

1. `docs/<name>/index.yml` and `index.md` (overview, install, quick start, licence, when to use
   it versus X-Ray.Content).
2. A card in `docs/index.md`, rows in `getting-started/installation.md` and
   `choosing-a-package.md`, an entry in the "Packages" dropdown of `neko.yml`.
3. Build, check links, commit.

## Publishing

`.devops/build-docs.yml` runs on pushes to `main` that touch `docs/`, `build.bat` or itself:
`dotnet tool update Neko --global`, `build.bat`, then it turns `.build/` into a fresh git repo,
adds `.nojekyll`, and force-pushes it to `gh-pages` of curiosity-ai/x-ray with the `$(username)`,
`$(pat)` and `$(email)` pipeline variables.

The site is served at `x-ray.curiosity.ai`, not `curiosity-ai.github.io/x-ray/`: Neko writes
root-relative URLs and has no base-path setting, so it needs a hostname of its own. Neko writes
the `CNAME` file from `url:` in `neko.yml`. If you change the hostname, change `url:`, the DNS
record, and the links in `README.md`, `src/XRay/README.md` and `Directory.Build.props`.
