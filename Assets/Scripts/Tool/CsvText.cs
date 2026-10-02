using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>Quoted CSV reader shared by runtime localization and editor card tools.</summary>
public static class CsvText
{
    public static List<List<string>> Parse(string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        text = text.TrimStart('\uFEFF');
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool quoted = false, closed = false;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"')
                { field.Append('"'); i++; }
                else if (ch == '"') { quoted = false; closed = true; }
                else field.Append(ch);
                continue;
            }
            if (ch == '"' && field.Length == 0 && !closed) quoted = true;
            else if (ch == ',') { row.Add(field.ToString()); field.Length = 0; closed = false; }
            else if (ch == '\r' || ch == '\n')
            {
                row.Add(field.ToString());
                if (row.Any(value => value.Length > 0)) rows.Add(row);
                row = new List<string>(); field.Length = 0; closed = false;
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
            }
            else if (closed || ch == '"') throw new FormatException($"Invalid CSV quoting near character {i}.");
            else field.Append(ch);
        }
        if (quoted) throw new FormatException("CSV ends inside a quoted field.");
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            if (row.Any(value => value.Length > 0)) rows.Add(row);
        }
        return rows;
    }
}
