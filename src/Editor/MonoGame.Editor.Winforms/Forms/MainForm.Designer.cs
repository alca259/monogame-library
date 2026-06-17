#nullable disable

using System.ComponentModel;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Controls;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Forms;

partial class MainForm
{
    private IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    // ── Menú ────────────────────────────────────────────────────────────────
    private MenuStrip          _menuStrip;
    private ToolStripMenuItem  _mnuFile;
    private ToolStripMenuItem  _mnuEdit;
    private ToolStripMenuItem  _mnuProject;
    private ToolStripMenuItem  _mnuDebug;
    private ToolStripMenuItem  _mnuView;

    // ── Toolbar ─────────────────────────────────────────────────────────────
    private ToolStrip             _toolStrip;
    private ToolStripToggleButton _tsiToolSelect;
    private ToolStripToggleButton _tsiToolMove;
    private ToolStripToggleButton _tsiToolRotate;
    private ToolStripToggleButton _tsiToolScale;
    private ToolStripToggleButton _tsiToolRect;
    private ToolStripSeparator    _tsiSep1;
    private ToolStripToggleButton _tsiSnap;
    private ToolStripSeparator    _tsiSep2;
    private ToolStripToggleButton _tsiEnableMove;
    private ToolStripToggleButton _tsiEnableRotate;
    private ToolStripToggleButton _tsiEnableScale;
    private ToolStripSeparator    _tsiSep3;
    private ToolStripToggleButton _tsiAxisX;
    private ToolStripToggleButton _tsiAxisY;
    private ToolStripSeparator    _tsiSep4;
    private ToolStripToggleButton _tsiNav;
    private ToolStripToggleButton _tsiRes;
    private ToolStripButton       _tsiBtnPlay;
    private ToolStripButton       _tsiBtnStop;

    // ── Status bar ──────────────────────────────────────────────────────────
    private StatusStrip          _statusStrip;
    private ToolStripStatusLabel _sslStatus;
    private ToolStripStatusLabel _sslObjectCount;
    private ToolStripSeparator   _sslSep;
    private ToolStripStatusLabel _sslFps;

    // ── Layout principal ────────────────────────────────────────────────────
    private SplitContainer _splitMain;
    private SplitContainer _splitCenterRight;
    private SplitContainer _splitViewportDock;

    // ── Paneles (Fase 2 = placeholders) ─────────────────────────────────────
    internal Panel _pnlHierarchy;
    internal Panel _pnlViewport;
    internal Panel _pnlDock;
    internal Panel _pnlInspector;

    private void InitializeComponent()
    {
        components = new Container();
        SuspendLayout();

        // ── MenuStrip ────────────────────────────────────────────────────────
        _menuStrip = new MenuStrip { Dock = DockStyle.Top, Height = EditorStyles.MenuHeight };
        _menuStrip.Renderer = new EditorStyles.EditorToolStripRenderer();
        _menuStrip.BackColor = EditorColors.BgChrome;
        _menuStrip.ForeColor = EditorColors.TextPrimary;
        _menuStrip.Font      = EditorFonts.Primary;

        _mnuFile    = new ToolStripMenuItem("&File")    { ForeColor = EditorColors.TextPrimary };
        _mnuEdit    = new ToolStripMenuItem("&Edit")    { ForeColor = EditorColors.TextPrimary };
        _mnuProject = new ToolStripMenuItem("&Project") { ForeColor = EditorColors.TextPrimary };
        _mnuDebug   = new ToolStripMenuItem("&Debug")   { ForeColor = EditorColors.TextPrimary };
        _mnuView    = new ToolStripMenuItem("&View")    { ForeColor = EditorColors.TextPrimary };
        _menuStrip.Items.AddRange(new ToolStripItem[] { _mnuFile, _mnuEdit, _mnuProject, _mnuDebug, _mnuView });

        // ── ToolStrip ────────────────────────────────────────────────────────
        _toolStrip = new ToolStrip { Dock = DockStyle.Top, Height = EditorStyles.ToolbarHeight };
        _toolStrip.Renderer  = new EditorStyles.EditorToolStripRenderer();
        _toolStrip.BackColor = EditorColors.BgChrome;
        _toolStrip.ForeColor = EditorColors.TextPrimary;
        _toolStrip.Font      = EditorFonts.Primary;
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;

        _tsiToolSelect  = MakeToggle("↖", "Select (Q)");
        _tsiToolMove    = MakeToggle("✥", "Move (W)");
        _tsiToolRotate  = MakeToggle("↻", "Rotate (E)");
        _tsiToolScale   = MakeToggle("⟺", "Scale (R)");
        _tsiToolRect    = MakeToggle("⬜", "Rect (T)");
        _tsiSep1        = new ToolStripSeparator();
        _tsiSnap        = MakeToggle("⊞", "Snap to grid");
        _tsiSep2        = new ToolStripSeparator();
        _tsiEnableMove   = MakeToggle("M", "Enable Move");
        _tsiEnableRotate = MakeToggle("R", "Enable Rotate");
        _tsiEnableScale  = MakeToggle("S", "Enable Scale");
        _tsiSep3        = new ToolStripSeparator();
        _tsiAxisX       = MakeToggle("X", "Lock X axis");
        _tsiAxisY       = MakeToggle("Y", "Lock Y axis");
        _tsiSep4        = new ToolStripSeparator();
        _tsiNav         = MakeToggle("Nav", "Navigation overlay");
        _tsiRes         = MakeToggle("Res", "Resolution overlay");
        _tsiBtnPlay = new ToolStripButton
        {
            Text        = "▶  Play",
            ToolTipText = "Play (F5)",
            BackColor   = EditorColors.BgChrome,
            ForeColor   = EditorColors.PlayGreen,
            Font        = EditorFonts.PrimaryBold,
            Width       = 70,
            Alignment   = ToolStripItemAlignment.Right,
        };
        _tsiBtnStop = new ToolStripButton
        {
            Text        = "⏹  Stop",
            ToolTipText = "Stop",
            BackColor   = EditorColors.BgChrome,
            ForeColor   = EditorColors.TextMuted,
            Font        = EditorFonts.PrimaryBold,
            Width       = 70,
            Alignment   = ToolStripItemAlignment.Right,
        };

        _toolStrip.Items.AddRange(new ToolStripItem[]
        {
            _tsiToolSelect, _tsiToolMove, _tsiToolRotate, _tsiToolScale, _tsiToolRect,
            _tsiSep1, _tsiSnap,
            _tsiSep2, _tsiEnableMove, _tsiEnableRotate, _tsiEnableScale,
            _tsiSep3, _tsiAxisX, _tsiAxisY,
            _tsiSep4, _tsiNav, _tsiRes,
            _tsiBtnPlay, _tsiBtnStop,
        });

        // ── StatusStrip ──────────────────────────────────────────────────────
        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom, Height = EditorStyles.StatusHeight };
        _statusStrip.Renderer  = new EditorStyles.EditorToolStripRenderer();
        _statusStrip.BackColor = EditorColors.BgChrome;
        _statusStrip.Font      = EditorFonts.Small;
        _statusStrip.SizingGrip = false;

        _sslStatus = new ToolStripStatusLabel
        {
            Text      = "Ready",
            Spring    = true,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            ForeColor = EditorColors.TextSecondary,
        };
        _sslObjectCount = new ToolStripStatusLabel
        {
            Text      = string.Empty,
            ForeColor = EditorColors.TextMuted,
        };
        _sslSep = new ToolStripSeparator();
        _sslFps = new ToolStripStatusLabel
        {
            Text      = "-- FPS",
            ForeColor = EditorColors.TextMuted,
            Width     = 60,
            TextAlign = System.Drawing.ContentAlignment.MiddleRight,
        };

        _statusStrip.Items.AddRange(new ToolStripItem[] { _sslStatus, _sslObjectCount, _sslSep, _sslFps });

        // ── Paneles placeholder ───────────────────────────────────────────────
        _pnlHierarchy = new Panel { Dock = DockStyle.Fill, BackColor = EditorColors.PanelBackground };
        _pnlViewport  = new Panel { Dock = DockStyle.Fill, BackColor = EditorColors.ViewportBackground };
        _pnlDock      = new Panel { Dock = DockStyle.Fill, BackColor = EditorColors.PanelBackground };
        _pnlInspector = new Panel { Dock = DockStyle.Fill, BackColor = EditorColors.PanelBackground };

        // ── SplitContainers anidados ─────────────────────────────────────────
        _splitViewportDock = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Horizontal,
            BackColor        = EditorColors.Border,
            SplitterWidth    = 2,
            Panel1MinSize    = 100,
            Panel2MinSize    = 60,
            SplitterDistance = 400,
        };
        _splitViewportDock.Panel1.Controls.Add(_pnlViewport);
        _splitViewportDock.Panel2.Controls.Add(_pnlDock);

        _splitCenterRight = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Vertical,
            BackColor        = EditorColors.Border,
            SplitterWidth    = 2,
            Panel1MinSize    = 200,
            Panel2MinSize    = EditorStyles.InspectorWidth,
            SplitterDistance = 600,
            FixedPanel       = FixedPanel.Panel2,
        };
        _splitCenterRight.Panel1.Controls.Add(_splitViewportDock);
        _splitCenterRight.Panel2.Controls.Add(_pnlInspector);

        _splitMain = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Vertical,
            BackColor        = EditorColors.Border,
            SplitterWidth    = 2,
            Panel1MinSize    = EditorStyles.HierarchyWidth,
            Panel2MinSize    = 300,
            SplitterDistance = EditorStyles.HierarchyWidth,
            FixedPanel       = FixedPanel.Panel1,
        };
        _splitMain.Panel1.Controls.Add(_pnlHierarchy);
        _splitMain.Panel2.Controls.Add(_splitCenterRight);

        // ── Form ─────────────────────────────────────────────────────────────
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize    = new System.Drawing.Size(1440, 860);
        MinimumSize   = new System.Drawing.Size(900, 600);
        Name          = "MainForm";
        Text          = "MonoGame Editor";
        MainMenuStrip = _menuStrip;

        Controls.Add(_splitMain);
        Controls.Add(_toolStrip);
        Controls.Add(_menuStrip);
        Controls.Add(_statusStrip);

        ResumeLayout(false);
        PerformLayout();
    }

    private static ToolStripToggleButton MakeToggle(string text, string tooltip) => new(text)
    {
        ToolTipText  = tooltip,
        AutoSize     = true,
        TextAlign    = System.Drawing.ContentAlignment.MiddleCenter,
    };
}
