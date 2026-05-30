using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WinCalc;

public partial class MainWindow : Window
{
    private readonly Calculator _c = new();
    private readonly App _app = (App)Application.Current;
    private string _mode = "Basic";
    private bool _suppressTextChange;

    // Characters allowed in the expression input
    private static readonly HashSet<char> ValidChars =
        [.. "0123456789.+-*/×÷−%^() πe"];

    public MainWindow()
    {
        InitializeComponent();
        Dispatcher.BeginInvoke(() =>
        {
            try { BuildButtons(); txtExpr.Focus(); }
            catch (Exception ex) { MessageBox.Show($"Build error:\n{ex.Message}", "Error"); }
        });
    }

    private void Window_Loaded(object s, RoutedEventArgs e) { Activate(); Focus(); }

    // ── Title bar ──────────────────────────────────────────────────────────
    private void TitleBar_Drag(object s, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Close_Click(object s, RoutedEventArgs e) => Close();

    private void Menu_Click(object s, RoutedEventArgs e)
    {
        var cm = new ContextMenu();
        cm.Items.Add(MkItem("☀  Light",         () => _app.SetTheme(App.ThemeMode.Light)));
        cm.Items.Add(MkItem("🌙  Dark",           () => _app.SetTheme(App.ThemeMode.Dark)));
        cm.Items.Add(MkItem("⚙  System Default", () => _app.SetTheme(App.ThemeMode.System)));
        cm.Items.Add(new Separator());
        cm.Items.Add(MkItem("📋  Copy Result",   CopyResult));
        cm.Items.Add(MkItem("🗑  Clear History", ClearHistory));
        cm.PlacementTarget = btnMenu;
        cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        cm.IsOpen = true;
    }

    private static MenuItem MkItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private void Mode_Changed(object s, SelectionChangedEventArgs e)
    {
        if (btnGrid == null) return;
        if (cmbMode.SelectedItem is ComboBoxItem item)
        {
            _mode = item.Content.ToString()!;
            BuildButtons();
        }
    }

    // ── Input filtering ────────────────────────────────────────────────────
    private void txtExpr_PreviewInput(object s, TextCompositionEventArgs e)
    {
        // Block any character not in ValidChars
        e.Handled = !e.Text.All(c => ValidChars.Contains(c));
    }

    private void Expr_Changed(object s, TextChangedEventArgs e)
    {
        if (_suppressTextChange) return;

        // Strip invalid chars (handles paste)
        var raw      = txtExpr.Text;
        var filtered = new string(raw.Where(c => ValidChars.Contains(c)).ToArray());
        if (filtered != raw)
        {
            _suppressTextChange = true;
            int caret = Math.Max(0, txtExpr.CaretIndex - (raw.Length - filtered.Length));
            txtExpr.Text = filtered;
            txtExpr.CaretIndex = Math.Min(caret, filtered.Length);
            _suppressTextChange = false;
        }

        _c.Expr = txtExpr.Text;
    }

    private void Expr_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  { e.Handled = true; Calculate(); }
        if (e.Key == Key.Escape) { e.Handled = true; ClearAll(); }
    }

    private void Window_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  { e.Handled = true; Calculate(); }
        if (e.Key == Key.Escape) { e.Handled = true; ClearAll(); }
    }

    private void Backspace_Click(object s, RoutedEventArgs e) { _c.Backspace(); RefreshDisplay(); }

    private void RefreshDisplay()
    {
        _suppressTextChange = true;
        txtExpr.Text = _c.Expr;
        txtExpr.CaretIndex = txtExpr.Text.Length;
        txtExpr.Foreground = _c.HasError
            ? (Brush)Application.Current.Resources["WarnFg"]
            : (Brush)Application.Current.Resources["Fg"];
        _suppressTextChange = false;
    }

    // ── Calculation ────────────────────────────────────────────────────────
    private void Calculate()
    {
        if (string.IsNullOrWhiteSpace(_c.Expr)) return;
        var before = _c.Expr;
        var result = _c.Evaluate();
        if (result == "Error") { RefreshDisplay(); return; }
        AddHistoryRow(before, result);
        RefreshDisplay();
        txtExpr.Focus();
    }

    private void ClearAll()     { _c.Clear(); histPanel.Children.Clear(); RefreshDisplay(); }
    private void ClearHistory() { _c.History.Clear(); histPanel.Children.Clear(); }
    private void CopyResult()   { if (!string.IsNullOrEmpty(_c.Expr)) Clipboard.SetText(_c.Expr); }

    // ── History ────────────────────────────────────────────────────────────
    private void AddHistoryRow(string expr, string result)
    {
        var subFg = (Brush)Application.Current.Resources["SubFg"];
        var fg    = (Brush)Application.Current.Resources["Fg"];
        var line  = (Brush)Application.Current.Resources["HistLine"];

        histPanel.Children.Add(new Border { Height = 1, Background = line, Margin = new Thickness(0,0,0,1) });

        var row = new Grid { Margin = new Thickness(0,1,0,1), Cursor = Cursors.Hand };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var t1 = new TextBlock { Text = expr,   Foreground = subFg, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis };
        var t2 = new TextBlock { Text = " = ",  Foreground = subFg, FontSize = 14, Margin = new Thickness(4,0,4,0) };
        var t3 = new TextBlock { Text = result, Foreground = fg,    FontSize = 14, FontWeight = FontWeights.Bold };

        t1.SetValue(Grid.ColumnProperty, 0);
        t2.SetValue(Grid.ColumnProperty, 1);
        t3.SetValue(Grid.ColumnProperty, 2);
        row.Children.Add(t1); row.Children.Add(t2); row.Children.Add(t3);
        row.MouseLeftButtonDown += (_, _) => { _c.Expr = result; RefreshDisplay(); };

        histPanel.Children.Add(row);
        histScroll.ScrollToBottom();
    }

    // ── Button Grid ────────────────────────────────────────────────────────
    private void BuildButtons()
    {
        if (btnGrid == null) return;
        btnGrid.Children.Clear();
        btnGrid.RowDefinitions.Clear();
        btnGrid.ColumnDefinitions.Clear();

        if (_mode == "Advanced") BuildAdvanced();
        else                     BuildBasic();
    }

    // Basic: 4 cols × 5 rows
    private static readonly (string L, string K)[][] BasicLayout =
    [
        [("C","clear"),  ("( )","paren"), ("%","percent"), ("÷","op")],
        [("7","num"),    ("8","num"),     ("9","num"),     ("×","op")],
        [("4","num"),    ("5","num"),     ("6","num"),     ("−","op")],
        [("1","num"),    ("2","num"),     ("3","num"),     ("+","op")],
        [("±","negate"), ("0","num"),     (".","num"),     ("=","eq")],
    ];

    // Advanced: 7 cols × 5 rows, = at (row3, col6) spanning to row4
    private static readonly (string L, string K)[][] AdvancedLayout =
    [
        [("x²","sq"),   ("xʸ","pow"),  ("C","clear"),   ("↑n","ceil"),  ("↓n","floor"), ("÷","op"), ("mod","mod")],
        [("sin","fn"),  ("cos","fn"),  ("tan","fn"),    ("7","num"),    ("8","num"),    ("9","num"), ("×","op")   ],
        [("ln","ln"),   ("log","log"), ("√","sqrt"),    ("4","num"),    ("5","num"),    ("6","num"), ("−","op")   ],
        [("π","pi"),    ("e","euler"), ("|x|","abs"),   ("1","num"),    ("2","num"),    ("3","num"), ("=","eq")   ],
        [("x⁻¹","inv"), ("x!","fact"), ("±","negate"),  ("0","num"),    (".","num"),    ("+","op"),  ("","skip")  ],
    ];

    private void BuildBasic()    => PlaceLayout(BasicLayout,    4, 5, 54, eqRow: -1, eqCol: -1);
    private void BuildAdvanced() => PlaceLayout(AdvancedLayout, 7, 5, 52, eqRow: 3, eqCol: 6);

    private void SetupGrid(int cols, int rows, double rowH)
    {
        for (int i = 0; i < cols; i++)
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < rows; i++)
            btnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowH) });
    }

    private void PlaceLayout((string L, string K)[][] layout, int cols, int rows,
                              double rowH, int eqRow, int eqCol)
    {
        SetupGrid(cols, rows, rowH);
        bool spanEq = eqRow >= 0;
        for (int r = 0; r < layout.Length; r++)
        for (int c = 0; c < layout[r].Length; c++)
        {
            var (label, kind) = layout[r][c];
            if (string.IsNullOrEmpty(label) || kind == "skip") continue;
            if (spanEq && r == eqRow + 1 && c == eqCol) continue;

            var btn = MakeBtn(label, kind);
            btn.SetValue(Grid.RowProperty, r);
            btn.SetValue(Grid.ColumnProperty, c);
            if (spanEq && kind == "eq") btn.SetValue(Grid.RowSpanProperty, 2);
            btnGrid.Children.Add(btn);
        }
    }

    private Button MakeBtn(string label, string kind)
    {
        var styleKey = kind switch
        {
            "eq"                                          => "EqBtn",
            "op" or "mod" or "percent" or "paren"
                 or "clear"                               => "OpBtn",
            _                                             => "Btn"
        };
        var btn = new Button
        {
            Content  = label,
            Style    = (Style)Application.Current.Resources[styleKey],
            Cursor   = Cursors.Hand
        };
        btn.Click += (_, _) => HandleBtn(label, kind);
        return btn;
    }

    // ── Button actions ─────────────────────────────────────────────────────
    private void HandleBtn(string label, string kind)
    {
        switch (kind)
        {
            case "clear":   ClearAll(); return;
            case "eq":      Calculate(); return;
            case "num":     _c.AppendToExpr(label); break;
            case "op":      _c.AppendToExpr(" " + label + " "); break;
            case "mod":     _c.AppendToExpr(" mod "); break;
            case "percent": _c.AppendToExpr("%"); break;
            case "paren":
                int o = _c.Expr.Count(ch => ch == '(');
                int cl = _c.Expr.Count(ch => ch == ')');
                _c.AppendToExpr(o > cl ? ")" : "(");
                break;
            case "ceil":   _c.Expr = $"ceil({_c.Expr})";  break;
            case "floor":  _c.Expr = $"floor({_c.Expr})"; break;
            case "abs":    _c.Expr = $"abs({_c.Expr})";   break;
            case "sqrt":   _c.Expr = $"sqrt({_c.Expr})";  break;
            case "sq":
                if (!string.IsNullOrEmpty(_c.Expr)) _c.Expr = $"({_c.Expr})^2";
                break;
            case "pow":    _c.AppendToExpr("^");           break;
            case "inv":
                if (!string.IsNullOrEmpty(_c.Expr)) _c.Expr = $"1/({_c.Expr})";
                break;
            case "fn":     _c.Expr = $"{label}({_c.Expr})"; break;
            case "log":    _c.Expr = $"log({_c.Expr})";     break;
            case "ln":     _c.Expr = $"ln({_c.Expr})";      break;
            case "fact":
                if (long.TryParse(_c.Expr, out long n) && n >= 0 && n <= 20)
                { long f = 1; for (long i = 2; i <= n; i++) f *= i; _c.Expr = f.ToString(); }
                break;
            case "negate":
                _c.Expr = _c.Expr.StartsWith('-') ? _c.Expr[1..] :
                          string.IsNullOrEmpty(_c.Expr) ? "" : "-" + _c.Expr;
                break;
            case "pi":    _c.AppendToExpr("π"); break;
            case "euler": _c.AppendToExpr("e"); break;
        }
        RefreshDisplay();
        txtExpr.Focus();
        txtExpr.CaretIndex = txtExpr.Text.Length;
    }
}
