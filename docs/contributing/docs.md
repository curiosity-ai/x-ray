---
label: Working on the docs
icon: book
order: 2
---

# Working on the docs

This site is built with [Neko](https://neko.curiosity.ai) from the `docs/` folder of
[curiosity-ai/x-ray](https://github.com/curiosity-ai/x-ray).

## Build locally

Neko is a .NET global tool:

```sh
dotnet tool update --global Neko     # installs it when missing
./build.sh                           # or build.bat on Windows
```

Both scripts run `neko build -i docs -o .build`. Open `.build/index.html` through a local web
server, since the site uses root-relative links:

```sh
dotnet tool install --global dotnet-serve
dotnet serve -d .build
```

For live reload while editing, `neko watch -i docs` serves the site at `http://localhost:5000`
and rebuilds it on every change.

## Layout

```
docs/
  neko.yml              site configuration: branding, header links, url
  index.md              this site's landing page
  assets/               images (the family logo)
  getting-started/      install and package choice
  content/ office/ psd/ email/ ps/ onenote/
                        one folder per package, each with index.yml and index.md
  contributing/         this section
```

Each folder has an `index.yml` (sidebar label, icon, order) and an `index.md` (the folder's
page). Pages carry front matter with `label`, `icon` and `order`. Icons are
[UIcons](https://www.flaticon.com/uicons) names in the regular rounded style.

## Adding a page

1. Create the `.md` file in the section's folder with front matter:

   ```md
   ---
   label: Output formats
   icon: file-code
   order: 4
   ---

   # Output formats
   ```

2. Link to other pages with root-relative paths without extensions: `/content/`,
   `/getting-started/installation`.
3. Base code samples on the package's real API, and mark a page that is not yet complete with a
   `!!! warning Stub` block.
4. Build and check the output before committing.

## Publishing

`.devops/build-docs.yml` runs on every push to `main` that touches `docs/`. It installs Neko,
builds the site, and force-pushes the output to the `gh-pages` branch, which GitHub Pages
serves at `x-ray.curiosity.ai`.
