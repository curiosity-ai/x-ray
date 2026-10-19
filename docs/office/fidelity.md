---
label: Fidelity and gaps
icon: check-circle
order: 4
tags: [office, fidelity, libreoffice, encryption]
---

# Fidelity and gaps

X-Ray.Office is developed against headless LibreOffice as the reference. A document is rendered
by both, and the two renderings are compared. This page says what that comparison measures, what
it cannot see, and what is not done.

## What is measured

The project's gate compares, per document, the page count, the extractable text within a tolerance
(the larger of 2% and a small floor, counted in alphanumeric characters), and whether fonts are
embedded. A separate fidelity test suite compares about 550 documents against a freshly rendered
LibreOffice 26.2.4.2 and checks structure such as text positions, table borders and fills, line
heights, footnotes and tab stops.

At the last recorded handover, 519 of those 550 comparisons passed. The 31 that did not were each
classified: some were defects in X-Ray.Office that have been fixed since, some are places where
LibreOffice itself is wrong, and eight are sub-point text position differences that the project
leaves as they are, because matching them would mean writing a less accurate PDF.

The gate is blind to most real defects, so the project also reviews pages by eye against the
reference rendering. Take the figures above as a measure of how close the layout is to
LibreOffice's, not as a guarantee for your documents. A document using features the layout does
not model is the likely place to see a difference.

## Fonts

LibreOffice resolves fonts through the system. X-Ray.Office does the same, and falls back on bundled
metric-compatible fonts (Carlito, Caladea, Liberation and DejaVu faces) searched last, so an
installed face always wins and a machine that lacks one still renders sensibly. A rendering made on
a machine with different fonts than the reference machine will differ.

## Encrypted documents

Encrypted documents are detected but not decrypted. This is the largest known gap.

- `OfficeDocument.Identify` reports `FormatIdentification.IsEncrypted` for an OOXML document that
  has an `EncryptedPackage` stream, without needing a password. `Open` and `Extract` then throw
  `UnsupportedFormatException`, not `PasswordRequiredException`, because the encrypted container is
  not a recognisable office document.
- `OfficeDocument.Open` and `Extract` throw `PasswordRequiredException` (with
  `PasswordWasSupplied` false) for an encrypted legacy `.xls` (a `FILEPASS` record), an encrypted
  `.doc`, and an ODF package with encrypted parts.
- `DocumentSource.Password` exists, but no decryption scheme (OOXML agile and standard encryption,
  ODF Blowfish and AES, RC4 and XOR obfuscation) is implemented yet, so supplying a password does
  not open a document that needs one.
- A legacy `.ppt` is not checked for encryption, so an encrypted deck can read as garbage rather than
  raising an exception.

Write protection is a different thing. Several formats carry a write-protect password that encrypts
nothing, and those documents read without a password.

!!! warning Filter encrypted files first
The exception differs by format, and a `.ppt` raises none. Check `Identify(...).IsEncrypted`
before opening, which catches encrypted OOXML, and treat the exceptions as a second line.
!!!

## Other gaps

- Draw, Math and Base documents, and writing any office format, are out of scope. Macros are
  never run; macro-enabled files are read as data.
- Tracked changes are resolved when extracting content rather than shown as marks.
- The render needs SkiaSharp and HarfBuzzSharp native libraries. Content extraction does not.
