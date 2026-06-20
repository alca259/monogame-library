using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Controls;

/// <summary>
/// <see cref="ListView"/> con la paleta del tema oscuro del editor.
/// </summary>
internal sealed class ThemedListView : ListView
{
    public ThemedListView()
    {
        BackColor     = EditorColors.PanelBackground;
        ForeColor     = EditorColors.TextPrimary;
        BorderStyle   = BorderStyle.None;
        FullRowSelect = true;
        GridLines     = false;
        HeaderStyle   = ColumnHeaderStyle.Nonclickable;
        HideSelection = false;
        OwnerDraw     = true;

        DrawColumnHeader += DrawHeader;
        DrawItem         += DrawRow;
        DrawSubItem      += DrawCell;
    }

    private void DrawHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using SolidBrush bg = new(EditorColors.BgChrome);
        e.Graphics.FillRectangle(bg, e.Bounds);
        TextRenderer.DrawText(
            e.Graphics,
            e.Header?.Text ?? string.Empty,
            EditorFonts.SmallBold,
            e.Bounds,
            EditorColors.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void DrawRow(object? sender, DrawListViewItemEventArgs e)
    {
        if (View != View.Details)
            e.DrawDefault = true;
    }

    private void DrawCell(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (View != View.Details || e.Item is null) return;

        bool  selected = e.Item.Selected;
        Color bg = selected
            ? EditorColors.RowSelected
            : e.ItemIndex % 2 == 0 ? EditorColors.PanelBackground : EditorColors.InputBackground;

        using SolidBrush bgBrush = new(bg);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        if (e.Bounds.Width <= 0) return;
        TextRenderer.DrawText(
            e.Graphics,
            e.SubItem?.Text ?? string.Empty,
            EditorFonts.Primary,
            e.Bounds,
            selected ? EditorColors.TextPrimary : EditorColors.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

/// <summary>
/// <see cref="TreeView"/> con la paleta del tema oscuro del editor.
/// </summary>
internal sealed class ThemedTreeView : TreeView
{
    public ThemedTreeView()
    {
        BackColor     = EditorColors.PanelBackground;
        ForeColor     = EditorColors.TextPrimary;
        BorderStyle   = BorderStyle.None;
        DrawMode      = TreeViewDrawMode.OwnerDrawAll;
        FullRowSelect = true;
        HideSelection = false;
        ShowLines     = true;
        LineColor     = EditorColors.Border;

        DrawNode += DrawTreeNode;
    }

    private void DrawTreeNode(object? sender, DrawTreeNodeEventArgs e)
    {
        if (e.Node is null) return;

        bool selected = (e.State & TreeNodeStates.Selected) != 0 ||
                        (e.State & TreeNodeStates.Focused)  != 0;

        using SolidBrush bg = new(selected ? EditorColors.RowSelected : EditorColors.PanelBackground);
        e.Graphics.FillRectangle(bg, e.Bounds);

        if (e.Node.Nodes.Count > 0)
        {
            int x  = e.Node.Level * Indent + 4;
            string arrow = e.Node.IsExpanded ? "▼" : "▶";
            TextRenderer.DrawText(
                e.Graphics, arrow, EditorFonts.Tiny,
                new Rectangle(x, e.Bounds.Top, 14, e.Bounds.Height),
                EditorColors.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        int textX = e.Node.Level * Indent + 20;
        TextRenderer.DrawText(
            e.Graphics,
            e.Node.Text,
            EditorFonts.Primary,
            new Rectangle(textX, e.Bounds.Top, e.Bounds.Width - textX, e.Bounds.Height),
            selected ? EditorColors.TextPrimary : EditorColors.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
