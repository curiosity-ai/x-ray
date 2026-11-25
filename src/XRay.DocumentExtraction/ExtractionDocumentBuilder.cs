using Microsoft.Extensions.AI;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using XRay.Content.Types;
using ContentTable = XRay.Content.Types.Table;

namespace XRay.DocumentExtraction;

/// <summary>Maps an X-Ray.Content <see cref="ExtractedDocument"/> onto an <see cref="ExtractionDocument"/>.</summary>
internal sealed class ExtractionDocumentBuilder
{
    private readonly ExtractedDocument _source;
    private readonly ExtractionDocument _target;
    private readonly bool _imageBytes;
    private readonly Dictionary<uint, ExtractionDocumentSourcePage> _pages = new();
    // The open sections, outermost first, with the heading level that opened each.
    private readonly List<(int Level, ExtractionDocumentSection Section)> _open = new();

    private ExtractionDocumentBuilder(string identifier, ExtractedDocument source, bool imageBytes)
    {
        _source = source;
        _imageBytes = imageBytes;
        _target = new ExtractionDocument(identifier, null) { RawRepresentation = source };
    }

    public static ExtractionDocument Build(string identifier, ExtractedDocument source, bool imageBytes)
    {
        var builder = new ExtractionDocumentBuilder(identifier, source, imageBytes);
        builder.CopyMetadata();
        builder.CopyBody();
        return builder._target;
    }

    private void CopyMetadata()
    {
        var m = _source.Metadata;
        Set("mimeType", _source.MimeType);
        Set("title", m.Title);
        Set("subject", m.Subject);
        Set("language", m.Language);
        Set("createdAt", m.CreatedAt);
        Set("modifiedAt", m.ModifiedAt);
        Set("createdBy", m.CreatedBy);
        Set("modifiedBy", m.ModifiedBy);
        if (m.Authors is { Count: > 0 } authors) _target.Metadata["authors"] = authors.ToArray();
        if (m.Keywords is { Count: > 0 } keywords) _target.Metadata["keywords"] = keywords.ToArray();

        void Set(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value)) _target.Metadata[key] = value;
        }
    }

    private void CopyBody()
    {
        var structure = _source.Document;
        if (structure is null || structure.Nodes.Count == 0)
        {
            // An extractor that produced no structure still produced text.
            if (Text(_source.Content, null) is { } body) Add(body);
            foreach (var page in _source.Pages ?? [])
            {
                Page(page.PageNumber);
            }
            return;
        }

        var nodes = structure.Nodes;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Parent is null) Visit(nodes, i, null);
        }
    }

    private void Visit(List<DocumentNode> nodes, int index, string? roleOverride)
    {
        var node = nodes[index];
        var c = node.Content;
        var role = roleOverride ?? LayerRole(node.ContentLayer);

        switch (c.Which)
        {
            case NodeContent.Tag.Title:
                Emit(node, Text(c.Text, role ?? "title"));
                break;

            case NodeContent.Tag.Heading:
                OpenSection(node, Math.Max(1, (int)c.Level), c.Text);
                break;

            case NodeContent.Tag.Slide:
                // A slide is a section of its own; its children belong to it.
                OpenSection(node, 1, c.SlideTitle ?? $"Slide {c.Number}");
                Children(nodes, node, null);
                return;

            case NodeContent.Tag.Paragraph:
                Emit(node, Text(c.Text, role));
                break;

            case NodeContent.Tag.ListItem:
                Emit(node, Text(c.Text, role ?? "listItem"));
                break;

            case NodeContent.Tag.Code:
                Emit(node, Text(c.Text, role ?? "code"));
                break;

            case NodeContent.Tag.Formula:
                Emit(node, Text(c.Text, role ?? "formula"));
                break;

            case NodeContent.Tag.Footnote:
                Emit(node, Text(c.Text, role ?? "footnote"));
                break;

            case NodeContent.Tag.Citation:
            case NodeContent.Tag.Admonition:
            case NodeContent.Tag.RawBlock:
                Emit(node, Text(c.Text ?? c.RawContent, role));
                break;

            case NodeContent.Tag.DefinitionItem:
                Emit(node, Text(c.Term, "definitionTerm"));
                Emit(node, Text(c.Definition, "definitionDescription"));
                break;

            case NodeContent.Tag.Table:
                if (c.Grid is { } grid) Emit(node, Table(node, grid));
                break;

            case NodeContent.Tag.Image:
                Emit(node, Image(c));
                break;

            case NodeContent.Tag.Quote:
                Children(nodes, node, "quote");
                return;

            // List, Group and DefinitionList only contain; PageBreak and MetadataBlock carry
            // nothing a reader of the tree needs (the break is implied by the page references).
            case NodeContent.Tag.List:
            case NodeContent.Tag.Group:
            case NodeContent.Tag.DefinitionList:
                Children(nodes, node, roleOverride);
                return;
        }
    }

    private void Children(List<DocumentNode> nodes, DocumentNode node, string? role)
    {
        foreach (var child in node.Children)
        {
            if (child < nodes.Count) Visit(nodes, (int)child, role);
        }
    }

    // A heading closes every open section of its level or deeper, then opens one inside what is left.
    private void OpenSection(DocumentNode node, int level, string? heading)
    {
        while (_open.Count > 0 && _open[^1].Level >= level) _open.RemoveAt(_open.Count - 1);

        var section = new ExtractionDocumentSection(null);
        if (_open.Count == 0) _target.Sections.Add(section);
        else _open[^1].Section.Elements.Add(section);
        _open.Add((level, section));

        if (!string.IsNullOrWhiteSpace(heading))
        {
            var text = new ExtractionDocumentTextElement(new TextContent(heading), "heading")
            {
                HeadingLevel = level,
            };
            Reference(node, text);
            section.Elements.Add(text);
        }
    }

    private void Emit(DocumentNode node, ExtractionDocumentElement? element)
    {
        if (element is null) return;
        Reference(node, element);
        Add(element);
    }

    // Content before the first heading sits in a section of its own, so the document tree has
    // sections at the root only.
    private void Add(ExtractionDocumentElement element)
    {
        if (_open.Count == 0)
        {
            var lead = new ExtractionDocumentSection(null);
            _target.Sections.Add(lead);
            _open.Add((0, lead));
        }
        _open[^1].Section.Elements.Add(element);
    }

    private static ExtractionDocumentTextElement? Text(string? text, string? role) =>
        string.IsNullOrEmpty(text) ? null : new ExtractionDocumentTextElement(new TextContent(text), role);

    private ExtractionDocumentTableElement Table(DocumentNode node, TableGrid grid)
    {
        var table = new ExtractionDocumentTableElement(null, (int)grid.Rows, (int)grid.Cols);
        foreach (var cell in grid.Cells)
        {
            if (cell.Row >= grid.Rows || cell.Col >= grid.Cols) continue;

            var role = cell.IsHeader ? "columnHeader" : "content";
            var rowSpan = (int)Math.Max(1, cell.RowSpan);
            var columnSpan = (int)Math.Max(1, cell.ColSpan);
            var content = Text(cell.Content, null);
            var target = content is null
                ? new ExtractionDocumentTableCell(rowSpan, columnSpan, role)
                : new ExtractionDocumentTableCell(content, rowSpan, columnSpan, role);
            table.Cells[(int)cell.Row, (int)cell.Col] = target;
        }
        return table;
    }

    private ExtractionDocumentImageElement Image(NodeContent c)
    {
        AIContent? payload = null;
        if (_imageBytes && c.ImageIndex is { } index && _source.Images is { } images)
        {
            var image = images.FirstOrDefault(i => i.ImageIndex == index);
            if (image is { Data.Length: > 0 })
            {
                payload = new DataContent(image.Data, MediaType(image.Format));
            }
        }

        return new ExtractionDocumentImageElement(payload, c.Src)
        {
            AlternativeText = c.Description,
        };
    }

    private static string MediaType(string format) =>
        format.Contains('/') ? format : "image/" + format.ToLowerInvariant().TrimStart('.');

    private static string? LayerRole(ContentLayer layer) => layer switch
    {
        ContentLayer.Header => "pageHeader",
        ContentLayer.Footer => "pageFooter",
        ContentLayer.Footnote => "footnote",
        _ => null,
    };

    private void Reference(DocumentNode node, ExtractionDocumentElement element)
    {
        if (node.Page is not { } page) return;

        var polygon = node.Bbox is { } b
            ? new[]
            {
                new ExtractionDocumentPoint(b.X0, b.Y0), new ExtractionDocumentPoint(b.X1, b.Y0),
                new ExtractionDocumentPoint(b.X1, b.Y1), new ExtractionDocumentPoint(b.X0, b.Y1),
            }
            : [];
        element.Sources.Add(new ExtractionDocumentSourceReference(Page(page), polygon, null));
    }

    private ExtractionDocumentSourcePage Page(uint number)
    {
        if (_pages.TryGetValue(number, out var page)) return page;

        page = new ExtractionDocumentSourcePage($"page-{number}", (int)number, null, null, null);
        _pages[number] = page;
        _target.SourcePages.Add(page);
        return page;
    }
}
