using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using XRay.DocumentExtraction;

#pragma warning disable MEDE0001, EXTEXP0019 // the abstraction is experimental

namespace XRay.DocumentExtraction.Tests;

public class ClientTests
{
    private const string Html = """
        <html><head><title>Quarterly report</title></head><body>
        <h1>Results</h1>
        <p>Revenue grew.</p>
        <h2>By region</h2>
        <p>Europe led.</p>
        <table><tr><th>Region</th><th>Growth</th></tr><tr><td>EMEA</td><td>12%</td></tr></table>
        <h1>Outlook</h1>
        <ul><li>Hire</li><li>Ship</li></ul>
        </body></html>
        """;

    private static Task<ExtractionDocument> Extract(string text, string name, string type, DocumentExtractionClient? client = null)
    {
        client ??= new XRayDocumentExtractionClient();
        return client.ExtractAsync(new MemoryStream(Encoding.UTF8.GetBytes(text)), name, type, null, TestContext.Current.CancellationToken);
    }

    private static IEnumerable<ExtractionDocumentTextElement> Texts(ExtractionDocument d) =>
        d.EnumerateContent().OfType<ExtractionDocumentTextElement>();

    [Fact]
    public async Task Html_headings_become_nested_sections()
    {
        var doc = await Extract(Html, "report.html", "text/html");

        Assert.Equal("report.html", doc.Identifier);
        var roots = doc.Sections.ToList();
        Assert.Contains(roots, s => Heading(s) == "Results");
        Assert.Contains(roots, s => Heading(s) == "Outlook");

        var results = roots.First(s => Heading(s) == "Results");
        var nested = results.Elements.OfType<ExtractionDocumentSection>().Single();
        Assert.Equal("By region", Heading(nested));
        Assert.Equal(2, ((ExtractionDocumentTextElement)nested.Elements.First()).HeadingLevel);
        Assert.Contains(Texts(doc), t => ((TextContent)t.Content!).Text.Contains("Europe led"));
    }

    [Fact]
    public async Task Html_table_has_addressable_cells()
    {
        var doc = await Extract(Html, "report.html", "text/html");

        var table = doc.EnumerateContent().OfType<ExtractionDocumentTableElement>().Single();
        Assert.Equal(2, table.Cells.RowCount);
        Assert.Equal(2, table.Cells.ColumnCount);
        Assert.Equal("Region", ((TextContent)table.Cells[0, 0]!.Content!.Content!).Text);
        Assert.Equal("columnHeader", table.Cells[0, 0]!.Role);
        Assert.Equal("12%", ((TextContent)table.Cells[1, 1]!.Content!.Content!).Text);
    }

    [Fact]
    public async Task Metadata_is_copied()
    {
        var doc = await Extract(Html, "report.html", "text/html");
        Assert.Equal("Quarterly report", doc.Metadata["title"]);
    }

    [Fact]
    public async Task Media_type_parameters_are_ignored()
    {
        var doc = await Extract("a,b\n1,2\n", "t.csv", "text/csv; charset=utf-8");
        Assert.NotEmpty(Texts(doc).Concat<object>(doc.EnumerateContent().OfType<ExtractionDocumentTableElement>()));
    }

    [Fact]
    public async Task Cancelled_token_throws_OperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var client = new XRayDocumentExtractionClient();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.ExtractAsync(new MemoryStream("x"u8.ToArray()), "a.txt", "text/plain", null, cts.Token));
    }

    [Fact]
    public async Task ExtractorName_is_rejected()
    {
        var client = new XRayDocumentExtractionClient();
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.ExtractAsync(new MemoryStream("x"u8.ToArray()), "a.txt", "text/plain",
                new DocumentExtractionOptions { ExtractorName = "other" }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Unknown_format_is_NotSupported()
    {
        var client = new XRayDocumentExtractionClient();
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.ExtractAsync(new MemoryStream([1, 2, 3]), "a.bin", "application/x-no-such-format", null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Corrupt_document_is_an_InvalidDataException()
    {
        var client = new XRayDocumentExtractionClient();
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            client.ExtractAsync(new MemoryStream([1, 2, 3, 4]), "a.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Source_stream_is_left_open()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Html));
        await new XRayDocumentExtractionClient().ExtractAsync(stream, "r.html", "text/html", null, TestContext.Current.CancellationToken);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task Works_through_the_middleware_builder()
    {
        var client = new DocumentExtractionClientBuilder(new XRayDocumentExtractionClient()).Build(new EmptyServices());
        var doc = await Extract(Html, "r.html", "text/html", client);
        Assert.NotEmpty(doc.Sections);
    }

    private static string? Heading(ExtractionDocumentSection s) =>
        (s.Elements.FirstOrDefault() as ExtractionDocumentTextElement)?.Content is TextContent t ? t.Text : null;

    private sealed class EmptyServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
