namespace LexFlow.Core;

public enum DiffKind
{
    Equal,
    Added,
    Removed
}

public readonly record struct DiffSpan(DiffKind Kind, string Text);

public static class TextDiff
{
    public static List<DiffSpan> Compare(string before, string after)
    {
        var left = Tokenize(before);
        var right = Tokenize(after);
        if (left.Count == 0 && right.Count == 0)
        {
            return [];
        }

        var n = left.Count;
        var m = right.Count;
        var lcs = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
        {
            for (var j = m - 1; j >= 0; j--)
            {
                lcs[i, j] = string.Equals(left[i], right[j], StringComparison.Ordinal)
                    ? lcs[i + 1, j + 1] + 1
                    : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        var spans = new List<DiffSpan>();
        var a = 0;
        var b = 0;
        while (a < n && b < m)
        {
            if (string.Equals(left[a], right[b], StringComparison.Ordinal))
            {
                Append(spans, DiffKind.Equal, left[a]);
                a++;
                b++;
            }
            else if (lcs[a + 1, b] >= lcs[a, b + 1])
            {
                Append(spans, DiffKind.Removed, left[a]);
                a++;
            }
            else
            {
                Append(spans, DiffKind.Added, right[b]);
                b++;
            }
        }

        while (a < n)
        {
            Append(spans, DiffKind.Removed, left[a++]);
        }

        while (b < m)
        {
            Append(spans, DiffKind.Added, right[b++]);
        }

        return spans;
    }

    private static void Append(List<DiffSpan> spans, DiffKind kind, string token)
    {
        var piece = token + " ";
        if (spans.Count > 0 && spans[^1].Kind == kind)
        {
            spans[^1] = new DiffSpan(kind, spans[^1].Text + piece);
            return;
        }

        spans.Add(new DiffSpan(kind, piece));
    }

    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
