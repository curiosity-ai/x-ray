---
label: Choose a package
icon: list-check
order: 3
---

# Choose a package

## By task

| You want to | Use |
|---|---|
| Index documents for search, or feed them to an LLM, as text or Markdown | [X-Ray.Content](/content/) |
| Get tables, metadata, images and a heading tree out of a document | [X-Ray.Content](/content/) |
| Turn a Word, Excel or PowerPoint document into a PDF or page images | [X-Ray.Office](/office/) |
| Show a thumbnail or preview of an office document | [X-Ray.Office](/office/) |
| Composite a Photoshop file, export its layers, or read its type layers | [X-Ray.Psd](/psd/) |
| Read, create or edit an Outlook message, or convert MSG to EML | [X-Ray.Email](/email/) |
| Convert PostScript, EPS or XPS to PDF or images | [X-Ray.PS](/ps/) |
| Read OneNote pages or export them to PDF | [X-Ray.OneNote](/onenote/) |
| Read text from scanned pages or from images inside documents | [X-Ray.Content](/content/) with OCR enabled |

## By file type

Several formats are read by more than one package, at different depths:

| Files | X-Ray.Content | Format package |
|---|---|---|
| docx, xlsx, pptx, doc, xls, ppt, odt, ods, odp, rtf | Text, Markdown, tables, metadata | [X-Ray.Office](/office/): layout and rendering |
| PDF | Text, geometry, tables, form fields, bookmarks | (none; X-Ray.Office and X-Ray.PS write PDF) |
| eml, msg | Body, headers and attachment list | [X-Ray.Email](/email/): the full MAPI model, writing |
| psd, psb | Layer tree as headings, type-layer text, metadata (read through X-Ray.Psd) | [X-Ray.Psd](/psd/): compositing, pixels, the full layer model |
| ps, eps, xps | Not read | [X-Ray.PS](/ps/) |
| one | Not read | [X-Ray.OneNote](/onenote/) |
| html, markdown, epub, csv, json, xml, archives and 70+ more | Read | (none) |

The rule of thumb: X-Ray.Content when you want what a document **says**, the format package
when you want what it **is** (its object model) or what it **looks like** (rendering).

## Combining packages

Apart from X-Ray.Content, which reads PSD files through X-Ray.Psd, the packages do not depend on
each other, so combining them is a matter of referencing both.
A common pairing is X-Ray.Content for the text of a document and X-Ray.Office for a rendered
preview of the same file. Each reads the file independently.
