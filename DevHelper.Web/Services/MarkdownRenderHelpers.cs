using System.Globalization;
using System.Text;

namespace DevHelper.Web.Services;

/// <summary>
/// Pure helpers implementing the rendering rules from spec section 5.5.
/// </summary>
public static class MarkdownRenderHelpers
{
    public const string EmptyCollectionText = "None";

    /// <summary>Renders a date as yyyy-MM-dd. Unparseable input is returned unchanged.</summary>
    public static string FormatDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return raw;
    }

    /// <summary>Renders a date as yyyyMMdd for filenames. Unparseable input is sanitized.</summary>
    public static string FormatFileDate(string? raw)
    {
        var formatted = FormatDate(raw);
        return formatted.Replace("-", string.Empty);
    }

    /// <summary>
    /// Escapes a single-line value for safe use inside Markdown tables and list items:
    /// pipes are escaped and line breaks collapsed to spaces.
    /// </summary>
    public static string EscapeInline(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r\n", " ")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("|", "\\|")
            .Trim();
    }

    /// <summary>Missing optional scalar values render as an empty string (long-form preserved).</summary>
    public static string Scalar(string? value) => value ?? string.Empty;

    /// <summary>
    /// Splits a stored multi-line text field on newlines and renders the non-empty
    /// lines as a numbered list. Empty input renders as <see cref="EmptyCollectionText"/>.
    /// </summary>
    public static string NumberedListFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return EmptyCollectionText;
        }

        var lines = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            return EmptyCollectionText;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            builder.Append(i + 1).Append(". ").Append(lines[i]);
            if (i < lines.Count - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }
}
