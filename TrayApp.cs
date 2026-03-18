using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using ClaudeUsageTray.Models;
using ClaudeUsageTray.Services;
using ClaudeUsageTray.Utils;

namespace ClaudeUsageTray;

/// <summary>
/// Main application context: lives entirely in the system tray.
/// The icon face renders the current session utilisation percentage.
/// Hovering shows a tooltip with both session and weekly stats.
/// </summary>
public sealed class TrayApp : ApplicationContext
{
    // ── Color scheme ─────────────────────────────────────────────────────────

    private enum ColorScheme { Orange, OrangeBlack, Transparent, White, OrangeTransparent, DarkOrange }

    private record IconColors(Color Background, Color Text, bool TransparentBg);

    private static readonly Dictionary<ColorScheme, IconColors> Schemes = new()
    {
        [ColorScheme.Orange]           = new(Color.FromArgb(219, 119, 91),  Color.FromArgb(255, 255, 255), false),
        [ColorScheme.OrangeBlack]      = new(Color.FromArgb(219, 119, 91),  Color.FromArgb(19,  20,  20),  false),
        [ColorScheme.Transparent]      = new(Color.Transparent,             Color.FromArgb(255, 255, 255), true),
        [ColorScheme.White]            = new(Color.FromArgb(248, 249, 244), Color.FromArgb(20,  20,  20),  false),
        [ColorScheme.OrangeTransparent]= new(Color.Transparent,             Color.FromArgb(217, 119, 87),  true),
        [ColorScheme.DarkOrange]       = new(Color.FromArgb(20,  20,  20),  Color.FromArgb(217, 119, 87),  false),
    };

    private ColorScheme _scheme = ColorScheme.Orange;

    // ── Fields ───────────────────────────────────────────────────────────────

    private readonly NotifyIcon                  _trayIcon;
    private readonly System.Windows.Forms.Timer  _timer;
    private readonly CredentialsService          _credentials = new();
    private readonly UsageService                _usage       = new();

    /// <summary>The HICON currently shown in the tray. Must be freed manually.</summary>
    private IntPtr _hIcon = IntPtr.Zero;

    /// <summary>Wrapper kept alive so NotifyIcon can re-send it to the shell on demand.</summary>
    private Icon? _iconWrapper;

    /// <summary>Last rendered text, used to redraw when only the color scheme changes.</summary>
    private string _lastIconText = "…";

    // ── Construction ─────────────────────────────────────────────────────────

    public TrayApp()
    {
        _timer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _timer.Tick += OnTick;

        var menu = BuildContextMenu();

        _trayIcon = new NotifyIcon
        {
            Visible          = true,
            Icon             = CreateIcon(_lastIconText),
            Text             = "Claude Usage Tray",
            ContextMenuStrip = menu,
        };

        _timer.Start();

        _ = RefreshAsync();
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private async void OnTick(object? sender, EventArgs e)    => await RefreshAsync();
    private async void OnRefresh(object? sender, EventArgs e) => await RefreshAsync();

    private void OnSchemeSelected(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item) return;

        _scheme = (ColorScheme)item.Tag!;

        // Update check marks
        foreach (ToolStripMenuItem s in item.GetCurrentParent()!.Items.OfType<ToolStripMenuItem>())
            if (s.Tag is ColorScheme)
                s.Checked = s == item;

        // Redraw with the same text but new colors
        SetIcon(_lastIconText);
    }

    // ── Data fetching ─────────────────────────────────────────────────────────

    private async Task RefreshAsync()
    {
        try
        {
            var token = _credentials.GetAccessToken();
            var data  = await _usage.GetUsageAsync(token);
            Apply(data);
        }
        catch (Exception ex)
        {
            SetIcon("E");
            SetTooltip($"Claude Usage Tray — Error\n{ex.Message}");
        }
    }

    private void Apply(UsageData data)
    {
        var session = data.FiveHour;
        var weekly  = data.SevenDay;

        if (session is null || weekly is null)
        {
            SetIcon("?");
            SetTooltip("Claude Usage Tray\nNo data returned by API.");
            return;
        }

        int sessionPct = (int)Math.Round(session.Utilization, MidpointRounding.AwayFromZero);
        int weeklyPct  = (int)Math.Round(weekly.Utilization,  MidpointRounding.AwayFromZero);

        SetIcon($"{sessionPct}");

        var sessionReset = DateUtils.FormatTimeUntil(session.ResetsAt);
        var weeklyReset  = DateUtils.FormatTimeUntil(weekly.ResetsAt);

        SetTooltip(
            $"Claude Usage Tray\n" +
            $"Session: {sessionPct}% (resets in {sessionReset})\n" +
            $"Weekly: {weeklyPct}% (resets in {weeklyReset})");
    }

    // ── Icon rendering ────────────────────────────────────────────────────────

    private void SetIcon(string text)
    {
        _lastIconText = text;

        var newWrapper = CreateIcon(text);
        var newHandle  = newWrapper.Handle;

        var oldWrapper = _iconWrapper;
        var oldHandle  = _hIcon;

        _trayIcon.Icon = newWrapper;
        _iconWrapper   = newWrapper;
        _hIcon         = newHandle;

        oldWrapper?.Dispose();
        if (oldHandle != IntPtr.Zero)
            NativeMethods.DestroyIcon(oldHandle);
    }

    private Icon CreateIcon(string text)
    {
        const int S = 32;
        var colors = Schemes[_scheme];

        using var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);

        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        // ── Background ───────────────────────────────────────────────────────
        if (!colors.TransparentBg)
        {
            using var bgPath  = RoundedRect(new RectangleF(1, 1, S - 2, S - 2), radius: 5);
            using var bgBrush = new SolidBrush(colors.Background);
            g.FillPath(bgBrush, bgPath);
        }

        // ── Text ─────────────────────────────────────────────────────────────
        float fontSize = text.Length <= 3 ? 11.5f : 8.5f;
        using var font  = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point);
        using var brush = new SolidBrush(colors.Text);

        var fmt = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        g.DrawString(text, font, brush, new RectangleF(0, 0, S, S), fmt);

        var hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.Left,       r.Top,        d, d, 180, 90);
        p.AddArc(r.Right - d,  r.Top,        d, d, 270, 90);
        p.AddArc(r.Right - d,  r.Bottom - d, d, d,   0, 90);
        p.AddArc(r.Left,       r.Bottom - d, d, d,  90, 90);
        p.CloseFigure();
        return p;
    }

    // ── Tooltip helper ────────────────────────────────────────────────────────

    private void SetTooltip(string text)
    {
        const int MaxLen = 127;
        _trayIcon.Text = text.Length > MaxLen ? text[..MaxLen] : text;
    }

    // ── Update interval ───────────────────────────────────────────────────────

    private static readonly (string Label, int Ms)[] Intervals =
    [
        ("1 minute",   1 * 60 * 1_000),
        ("5 minutes",  5 * 60 * 1_000),
        ("30 minutes", 30 * 60 * 1_000),
        ("1 hour",     60 * 60 * 1_000),
    ];

    private void OnIntervalSelected(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item) return;

        _timer.Interval = (int)item.Tag!;

        foreach (ToolStripMenuItem s in item.GetCurrentParent()!.Items.OfType<ToolStripMenuItem>())
            s.Checked = s == item;
    }

    // ── Context menu ─────────────────────────────────────────────────────────

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("Refresh now", null, OnRefresh);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildThemeSubmenu());
        menu.Items.Add(BuildIntervalSubmenu());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Application.Exit());

        return menu;
    }

    private ToolStripMenuItem BuildThemeSubmenu()
    {
        var sub = new ToolStripMenuItem("Theme");
        sub.DropDownItems.Add(SchemeItem("Default",              ColorScheme.Orange,            isDefault: true));
        sub.DropDownItems.Add(SchemeItem("Orange / Black",       ColorScheme.OrangeBlack,       isDefault: false));
        sub.DropDownItems.Add(SchemeItem("Dark / Orange",        ColorScheme.DarkOrange,        isDefault: false));
        sub.DropDownItems.Add(SchemeItem("Transparent / Orange", ColorScheme.OrangeTransparent, isDefault: false));
        sub.DropDownItems.Add(SchemeItem("Transparent / White",  ColorScheme.Transparent,       isDefault: false));
        sub.DropDownItems.Add(SchemeItem("White / Black",        ColorScheme.White,             isDefault: false));
        return sub;
    }

    private ToolStripMenuItem BuildIntervalSubmenu()
    {
        var sub = new ToolStripMenuItem("Update interval");

        foreach (var (label, ms) in Intervals)
        {
            var item = new ToolStripMenuItem(label)
            {
                Tag     = ms,
                Checked = ms == _timer.Interval,
            };
            item.Click += OnIntervalSelected;
            sub.DropDownItems.Add(item);
        }

        return sub;
    }

    private ToolStripMenuItem SchemeItem(string label, ColorScheme scheme, bool isDefault)
    {
        var item = new ToolStripMenuItem(label)
        {
            Tag     = scheme,
            Checked = isDefault,
        };
        item.Click += OnSchemeSelected;
        return item;
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _usage.Dispose();
            _iconWrapper?.Dispose();
            if (_hIcon != IntPtr.Zero)
                NativeMethods.DestroyIcon(_hIcon);
        }
        base.Dispose(disposing);
    }
}
