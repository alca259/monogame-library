using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel inferior del editor con pestañas temadas: Scenes, Assets y Console.
/// Sustituye al <c>_pnlDock</c> genérico de <c>MainForm.Designer.cs</c>.
/// </summary>
internal sealed class DockBarPanel : UserControl
{
    public DockBarPanel()
    {
        SuspendLayout();

        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

        TabControl tabs = new()
        {
            Dock      = DockStyle.Fill,
            BackColor = EditorColors.PanelBackground,
            Font      = EditorFonts.Primary,
        };
        ApplyDarkTabStyle(tabs);

        TabPage scenesTab  = MakeTab("Scenes");
        TabPage assetsTab  = MakeTab("Assets");
        TabPage consoleTab = MakeTab("Console");

        scenesTab.Controls.Add(new SceneManagerPanel());
        assetsTab.Controls.Add(new AssetBrowserPanel());
        consoleTab.Controls.Add(new ConsolePanel());

        tabs.TabPages.AddRange([scenesTab, assetsTab, consoleTab]);
        Controls.Add(tabs);

        ResumeLayout(false);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static TabPage MakeTab(string title) => new(title)
    {
        BackColor = EditorColors.PanelBackground,
        ForeColor = EditorColors.TextPrimary,
    };

    private static void ApplyDarkTabStyle(TabControl tabs)
    {
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.DrawItem += (s, e) =>
        {
            TabControl tc  = (TabControl)s!;
            Rectangle  r   = tc.GetTabRect(e.Index);
            bool       sel = e.Index == tc.SelectedIndex;

            using SolidBrush bg = new(sel ? EditorColors.PanelBackground : EditorColors.PanelBackgroundAlt);
            e.Graphics.FillRectangle(bg, r);

            // Línea de acento inferior en la pestaña activa
            if (sel)
            {
                using SolidBrush accent = new(EditorColors.AccentBlue);
                e.Graphics.FillRectangle(accent, new Rectangle(r.X, r.Bottom - 2, r.Width, 2));
            }

            using SolidBrush fg = new(sel ? EditorColors.TextPrimary : EditorColors.TextSecondary);
            using StringFormat sf = new()
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            e.Graphics.DrawString(tc.TabPages[e.Index].Text,
                sel ? EditorFonts.PrimaryBold : EditorFonts.Primary, fg, r, sf);
        };
    }
}
