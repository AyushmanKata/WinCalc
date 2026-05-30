using System.Data;
using System.Text.RegularExpressions;

namespace WinCalc;

public record HistoryEntry(string Expression, string Result);

public partial class Calculator
{
    public List<HistoryEntry> History { get; } = [];
    public string Expr { get; set; } = "";
    public bool HasError { get; private set; }

    public string Evaluate()
    {
        var raw = Expr.Trim();
        if (string.IsNullOrEmpty(raw)) return "";

        try
        {
            var processed = Preprocess(raw);
            var dt = new DataTable();
            var obj = dt.Compute(processed, null);
            var d = Convert.ToDouble(obj);
            var result = Format(d);
            History.Add(new HistoryEntry(raw, result));
            Expr = result;
            HasError = false;
            return result;
        }
        catch
        {
            HasError = true;
            return "Error";
        }
    }

    private static string Preprocess(string s)
    {
        // Replace display symbols with DataTable operators
        s = s.Replace("×", "*")
             .Replace("÷", "/")
             .Replace("−", "-")
             .Replace(",", "");

        // mod keyword → %
        s = ModRegex().Replace(s, "%");

        // π, e
        s = s.Replace("π", Math.PI.ToString("R"))
             .Replace("e",  Math.E.ToString("R"));

        // ceil(x) / floor(x) — evaluate inner first
        s = FuncRegex().Replace(s, m =>
        {
            var fn  = m.Groups[1].Value.ToLower();
            var arg = m.Groups[2].Value;
            try
            {
                var inner = Convert.ToDouble(new DataTable().Compute(
                    arg.Replace("×","*").Replace("÷","/").Replace("−","-"), null));
                return fn switch
                {
                    "ceil"  => Math.Ceiling(inner).ToString("R"),
                    "floor" => Math.Floor(inner).ToString("R"),
                    "abs"   => Math.Abs(inner).ToString("R"),
                    "sqrt"  => Math.Sqrt(inner).ToString("R"),
                    "sin"   => Math.Sin(inner * Math.PI / 180).ToString("R"),
                    "cos"   => Math.Cos(inner * Math.PI / 180).ToString("R"),
                    "tan"   => Math.Tan(inner * Math.PI / 180).ToString("R"),
                    "ln"    => Math.Log(inner).ToString("R"),
                    "log"   => Math.Log10(inner).ToString("R"),
                    _       => m.Value
                };
            }
            catch { return m.Value; }
        });

        return s;
    }

    public static string Format(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) return "Error";
        if (d == Math.Truncate(d) && Math.Abs(d) < 1e15)
            return ((long)d).ToString();
        return d.ToString("G12");
    }

    public void Backspace()
    {
        if (Expr.Length > 0) Expr = Expr[..^1];
        HasError = false;
    }

    public void Clear()
    {
        History.Clear();
        Expr = "";
        HasError = false;
    }

    public void AppendToExpr(string s)
    {
        if (HasError) { Expr = ""; HasError = false; }
        Expr += s;
    }

    [GeneratedRegex(@"\bmod\b", RegexOptions.IgnoreCase)]
    private static partial Regex ModRegex();

    [GeneratedRegex(@"(ceil|floor|abs|sqrt|sin|cos|tan|ln|log)\(([^)]*)\)",
        RegexOptions.IgnoreCase)]
    private static partial Regex FuncRegex();
}
