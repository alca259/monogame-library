namespace MonoGame.Editor.WinForms;

/// <summary>Dark theme renderer for ToolStrip, StatusStrip, and MenuStrip controls.</summary>
internal sealed class EditorToolStripRenderer : ToolStripProfessionalRenderer
{
    private static readonly System.Drawing.Color BgColor       = System.Drawing.Color.FromArgb(30, 30, 30);
    private static readonly System.Drawing.Color HoverColor    = System.Drawing.Color.FromArgb(62, 62, 64);
    private static readonly System.Drawing.Color CheckedColor  = System.Drawing.Color.FromArgb(47, 129, 247);
    private static readonly System.Drawing.Color SeparatorColor = System.Drawing.Color.FromArgb(70, 70, 72);
    private static readonly System.Drawing.Color TextColor     = System.Drawing.Color.FromArgb(204, 204, 204);
    private static readonly System.Drawing.Color BorderColor   = System.Drawing.Color.FromArgb(55, 55, 57);

    public EditorToolStripRenderer() : base(new EditorColorTable()) { }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.FillRectangle(new System.Drawing.SolidBrush(BgColor), e.AffectedBounds);
    }

    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is not ToolStripButton btn)
        {
            base.OnRenderButtonBackground(e);
            return;
        }

        System.Drawing.Rectangle bounds = new(1, 1, btn.Width - 2, btn.Height - 2);

        if (btn.Checked)
        {
            using System.Drawing.SolidBrush b = new(CheckedColor);
            e.Graphics.FillRectangle(b, bounds);
        }
        else if (btn.Selected || btn.Pressed)
        {
            using System.Drawing.SolidBrush b = new(HoverColor);
            e.Graphics.FillRectangle(b, bounds);
        }
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected && !e.Item.Pressed)
            return;

        System.Drawing.Rectangle bounds = new(1, 1, e.Item.Width - 2, e.Item.Height - 2);
        using System.Drawing.SolidBrush b = new(HoverColor);
        e.Graphics.FillRectangle(b, bounds);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? TextColor : System.Drawing.Color.FromArgb(100, 100, 100);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        bool isVert = e.ToolStrip is ToolStrip && !(e.ToolStrip is StatusStrip or MenuStrip);
        using System.Drawing.Pen pen = new(SeparatorColor);
        if (isVert)
        {
            int x = e.Item.Width / 2;
            e.Graphics.DrawLine(pen, x, 4, x, e.Item.Height - 4);
        }
        else
        {
            int y = e.Item.Height / 2;
            e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
        }
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using System.Drawing.Pen pen = new(BorderColor);
        e.Graphics.DrawRectangle(pen,
            e.AffectedBounds.X,
            e.AffectedBounds.Y,
            e.AffectedBounds.Width - 1,
            e.AffectedBounds.Height - 1);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using System.Drawing.SolidBrush b = new(BgColor);
        e.Graphics.FillRectangle(b, e.AffectedBounds);
    }

    protected override void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e) { }

    protected override void OnRenderToolStripStatusLabelBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.BackColor != BgColor && e.Item.BackColor != System.Drawing.Color.Empty)
        {
            using System.Drawing.SolidBrush b = new(e.Item.BackColor);
            e.Graphics.FillRectangle(b, new System.Drawing.Rectangle(0, 0, e.Item.Width, e.Item.Height));
            return;
        }
        base.OnRenderToolStripStatusLabelBackground(e);
    }

    private sealed class EditorColorTable : ProfessionalColorTable
    {
        public override System.Drawing.Color MenuBorder                => BorderColor;
        public override System.Drawing.Color MenuItemBorder            => BorderColor;
        public override System.Drawing.Color MenuItemSelected          => HoverColor;
        public override System.Drawing.Color MenuItemSelectedGradientBegin => HoverColor;
        public override System.Drawing.Color MenuItemSelectedGradientEnd   => HoverColor;
        public override System.Drawing.Color MenuItemPressedGradientBegin  => HoverColor;
        public override System.Drawing.Color MenuItemPressedGradientEnd    => HoverColor;
        public override System.Drawing.Color MenuItemPressedGradientMiddle => HoverColor;
        public override System.Drawing.Color ToolStripDropDownBackground   => BgColor;
        public override System.Drawing.Color ImageMarginGradientBegin      => BgColor;
        public override System.Drawing.Color ImageMarginGradientMiddle     => BgColor;
        public override System.Drawing.Color ImageMarginGradientEnd        => BgColor;
        public override System.Drawing.Color SeparatorDark                 => SeparatorColor;
        public override System.Drawing.Color SeparatorLight                => SeparatorColor;
        public override System.Drawing.Color CheckBackground               => CheckedColor;
        public override System.Drawing.Color CheckSelectedBackground       => CheckedColor;
        public override System.Drawing.Color CheckPressedBackground        => CheckedColor;
    }
}
