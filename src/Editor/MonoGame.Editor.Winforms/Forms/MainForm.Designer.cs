#nullable disable

namespace MonoGame.Editor.Winforms.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        ClientSize = new Size(1280, 800);
        Name = "MainForm";
        Text = "MonoGame Editor";
        ResumeLayout(false);
    }
}
