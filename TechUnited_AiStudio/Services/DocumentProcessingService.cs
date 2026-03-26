using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Components.Forms;
using System.Text;
using System.Text.RegularExpressions;

namespace TechUnited_AiStudio.Services; // Ensure your namespace is correct

public class DocumentProcessingService
{
    public async Task<List<string>> ExtractAndChunkAsync(IBrowserFile file, int chunkSize = 1000)
    {
        string fullText = "";

        // Use a 10MB limit for the stream
        using var stream = file.OpenReadStream(10 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        var extension = Path.GetExtension(file.Name).ToLower();
        fullText = extension switch
        {
            ".pdf" => ExtractPdfText(ms),
            ".docx" => ExtractDocxText(ms),
            ".txt" => Encoding.UTF8.GetString(ms.ToArray()),
            _ => throw new NotSupportedException($"File type {extension} is not supported")
        };

        if (string.IsNullOrWhiteSpace(fullText))
        {
            return new List<string>();
        }

        return CreateChunks(fullText, chunkSize);
    }

    private string ExtractPdfText(MemoryStream ms)
    {
        try
        {
            using var reader = new PdfReader(ms);
            using var pdfDoc = new PdfDocument(reader);
            var sb = new StringBuilder();
            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                if (page != null)
                {
                    sb.Append(PdfTextExtractor.GetTextFromPage(page));
                }
            }
            return sb.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ExtractDocxText(MemoryStream ms)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(ms, false);
            // This fix addresses CS8602 by checking every part of the object chain
            var body = doc.MainDocumentPart?.Document?.Body;
            return body?.InnerText ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private List<string> CreateChunks(string text, int size)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();

        var chunks = new List<string>();
        // Split by whitespace following sentence-ending punctuation
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
        var currentChunk = new StringBuilder();

        foreach (var sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence)) continue;

            if (currentChunk.Length + sentence.Length > size && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }
            currentChunk.Append(sentence + " ");
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}