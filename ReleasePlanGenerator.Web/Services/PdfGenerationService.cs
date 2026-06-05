using Markdig;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;

namespace ReleasePlanGenerator.Web.Services;

public class PdfGenerationService : IPdfGenerationService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public Task<byte[]> GenerateFromMarkdownAsync(string markdown, string title)
    {
        try
        {
            var html = Markdig.Markdown.ToHtml(markdown, Pipeline);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Text(title)
                        .SemiBold().FontSize(14).FontColor(Colors.Grey.Darken2);

                    page.Content().Column(col =>
                    {
                        col.Spacing(4);

                        var lines = markdown.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("# "))
                                col.Item().Text(line[2..]).Bold().FontSize(14);
                            else if (line.StartsWith("## "))
                                col.Item().Text(line[3..]).Bold().FontSize(12);
                            else if (line.StartsWith("### "))
                                col.Item().Text(line[4..]).SemiBold().FontSize(11);
                            else if (line.StartsWith("- ") || line.StartsWith("* "))
                                col.Item().Text($"  • {line[2..]}");
                            else if (line.StartsWith("---"))
                                col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                            else if (!string.IsNullOrWhiteSpace(line))
                                col.Item().Text(line);
                        }
                    });

                    page.Footer().AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            var bytes = document.GeneratePdf();
            return Task.FromResult(bytes);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PDF generation failed for title {Title}", title);
            throw;
        }
    }
}
