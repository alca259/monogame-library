#nullable enable
namespace MonoGame.Editor.WinForms.Dialogs;

partial class ProjectSettingsDialog
{
    private System.ComponentModel.IContainer? components = null;

    // ── Tabs ──────────────────────────────────────────────────────────────
    private System.Windows.Forms.TabControl _tabControl = null!;
    private System.Windows.Forms.TabPage _generalTab = null!;
    private System.Windows.Forms.TabPage _contentTab = null!;
    private System.Windows.Forms.TabPage _localizationTab = null!;
    private System.Windows.Forms.TabPage _codeGenTab = null!;

    // ── General tab ───────────────────────────────────────────────────────
    private System.Windows.Forms.TableLayoutPanel _generalLayout = null!;
    private System.Windows.Forms.Label _projectNameLabel = null!;
    private System.Windows.Forms.TextBox _projectNameBox = null!;
    private System.Windows.Forms.Label _versionLabel = null!;
    private System.Windows.Forms.TextBox _versionBox = null!;
    private System.Windows.Forms.Label _csprojLabel = null!;
    private System.Windows.Forms.Panel _csprojRow = null!;
    private System.Windows.Forms.TextBox _csprojBox = null!;
    private System.Windows.Forms.Button _browseCsprojButton = null!;
    private System.Windows.Forms.Label _editorFolderLabel = null!;
    private System.Windows.Forms.TextBox _editorFolderBox = null!;
    private System.Windows.Forms.Label _rootNamespaceLabel = null!;
    private System.Windows.Forms.TextBox _rootNamespaceBox = null!;
    private System.Windows.Forms.Label _virtualResLabel = null!;
    private System.Windows.Forms.Panel _virtualResRow = null!;
    private System.Windows.Forms.NumericUpDown _virtualWidthInput = null!;
    private System.Windows.Forms.Label _virtualResXLabel = null!;
    private System.Windows.Forms.NumericUpDown _virtualHeightInput = null!;

    // ── Content tab ───────────────────────────────────────────────────────
    private System.Windows.Forms.TableLayoutPanel _contentLayout = null!;
    private System.Windows.Forms.Label _contentFolderLabel = null!;
    private System.Windows.Forms.Panel _contentFolderRow = null!;
    private System.Windows.Forms.TextBox _contentFolderBox = null!;
    private System.Windows.Forms.Button _browseContentFolderButton = null!;
    private System.Windows.Forms.Label _mgcbFileLabel = null!;
    private System.Windows.Forms.Panel _mgcbFileRow = null!;
    private System.Windows.Forms.TextBox _mgcbFileBox = null!;
    private System.Windows.Forms.Button _browseMgcbButton = null!;
    private System.Windows.Forms.Label _buildConfigLabel = null!;
    private System.Windows.Forms.ComboBox _buildConfigCombo = null!;
    private System.Windows.Forms.CheckBox _autoBuildCheckBox = null!;

    // ── Localization tab ──────────────────────────────────────────────────
    private System.Windows.Forms.TableLayoutPanel _localizationLayout = null!;
    private System.Windows.Forms.Label _locFolderLabel = null!;
    private System.Windows.Forms.Panel _locFolderRow = null!;
    private System.Windows.Forms.TextBox _locFolderBox = null!;
    private System.Windows.Forms.Button _browseLocFolderButton = null!;
    private System.Windows.Forms.Label _defaultLocaleLabel = null!;
    private System.Windows.Forms.ComboBox _defaultLocaleCombo = null!;
    private System.Windows.Forms.Label _supportedLocalesLabel = null!;
    private System.Windows.Forms.DataGridView _supportedLocalesGrid = null!;
    private System.Windows.Forms.DataGridViewTextBoxColumn _localeColumn = null!;

    // ── Code Generation tab ───────────────────────────────────────────────
    private System.Windows.Forms.TableLayoutPanel _codeGenLayout = null!;
    private System.Windows.Forms.Label _outputFolderLabel = null!;
    private System.Windows.Forms.TextBox _outputFolderBox = null!;
    private System.Windows.Forms.CheckBox _generateOnSaveCheckBox = null!;
    private System.Windows.Forms.Label _previewPathLabel = null!;
    private System.Windows.Forms.Label _previewOutputLabel = null!;
    private System.Windows.Forms.Button _generateAllScenesButton = null!;

    // ── Buttons ───────────────────────────────────────────────────────────
    private System.Windows.Forms.FlowLayoutPanel _buttonPanel = null!;
    private System.Windows.Forms.Button _cancelButton = null!;
    private System.Windows.Forms.Button _okButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _tabControl                 = new System.Windows.Forms.TabControl();
        _generalTab                 = new System.Windows.Forms.TabPage();
        _contentTab                 = new System.Windows.Forms.TabPage();
        _localizationTab            = new System.Windows.Forms.TabPage();
        _codeGenTab                 = new System.Windows.Forms.TabPage();
        _generalLayout              = new System.Windows.Forms.TableLayoutPanel();
        _projectNameLabel           = new System.Windows.Forms.Label();
        _projectNameBox             = new System.Windows.Forms.TextBox();
        _versionLabel               = new System.Windows.Forms.Label();
        _versionBox                 = new System.Windows.Forms.TextBox();
        _csprojLabel                = new System.Windows.Forms.Label();
        _csprojRow                  = new System.Windows.Forms.Panel();
        _csprojBox                  = new System.Windows.Forms.TextBox();
        _browseCsprojButton         = new System.Windows.Forms.Button();
        _editorFolderLabel          = new System.Windows.Forms.Label();
        _editorFolderBox            = new System.Windows.Forms.TextBox();
        _rootNamespaceLabel         = new System.Windows.Forms.Label();
        _rootNamespaceBox           = new System.Windows.Forms.TextBox();
        _virtualResLabel            = new System.Windows.Forms.Label();
        _virtualResRow              = new System.Windows.Forms.Panel();
        _virtualWidthInput          = new System.Windows.Forms.NumericUpDown();
        _virtualResXLabel           = new System.Windows.Forms.Label();
        _virtualHeightInput         = new System.Windows.Forms.NumericUpDown();
        _contentLayout              = new System.Windows.Forms.TableLayoutPanel();
        _contentFolderLabel         = new System.Windows.Forms.Label();
        _contentFolderRow           = new System.Windows.Forms.Panel();
        _contentFolderBox           = new System.Windows.Forms.TextBox();
        _browseContentFolderButton  = new System.Windows.Forms.Button();
        _mgcbFileLabel              = new System.Windows.Forms.Label();
        _mgcbFileRow                = new System.Windows.Forms.Panel();
        _mgcbFileBox                = new System.Windows.Forms.TextBox();
        _browseMgcbButton           = new System.Windows.Forms.Button();
        _buildConfigLabel           = new System.Windows.Forms.Label();
        _buildConfigCombo           = new System.Windows.Forms.ComboBox();
        _autoBuildCheckBox          = new System.Windows.Forms.CheckBox();
        _localizationLayout         = new System.Windows.Forms.TableLayoutPanel();
        _locFolderLabel             = new System.Windows.Forms.Label();
        _locFolderRow               = new System.Windows.Forms.Panel();
        _locFolderBox               = new System.Windows.Forms.TextBox();
        _browseLocFolderButton      = new System.Windows.Forms.Button();
        _defaultLocaleLabel         = new System.Windows.Forms.Label();
        _defaultLocaleCombo         = new System.Windows.Forms.ComboBox();
        _supportedLocalesLabel      = new System.Windows.Forms.Label();
        _supportedLocalesGrid       = new System.Windows.Forms.DataGridView();
        _localeColumn               = new System.Windows.Forms.DataGridViewTextBoxColumn();
        _codeGenLayout              = new System.Windows.Forms.TableLayoutPanel();
        _outputFolderLabel          = new System.Windows.Forms.Label();
        _outputFolderBox            = new System.Windows.Forms.TextBox();
        _generateOnSaveCheckBox     = new System.Windows.Forms.CheckBox();
        _previewPathLabel           = new System.Windows.Forms.Label();
        _previewOutputLabel         = new System.Windows.Forms.Label();
        _generateAllScenesButton    = new System.Windows.Forms.Button();
        _buttonPanel                = new System.Windows.Forms.FlowLayoutPanel();
        _cancelButton               = new System.Windows.Forms.Button();
        _okButton                   = new System.Windows.Forms.Button();

        ((System.ComponentModel.ISupportInitialize)_supportedLocalesGrid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_virtualWidthInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_virtualHeightInput).BeginInit();
        _generalTab.SuspendLayout();
        _contentTab.SuspendLayout();
        _localizationTab.SuspendLayout();
        _codeGenTab.SuspendLayout();
        _tabControl.SuspendLayout();
        SuspendLayout();

        // ── _tabControl ───────────────────────────────────────────────────
        _tabControl.Controls.Add(_generalTab);
        _tabControl.Controls.Add(_contentTab);
        _tabControl.Controls.Add(_localizationTab);
        _tabControl.Controls.Add(_codeGenTab);
        _tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
        _tabControl.Location = new System.Drawing.Point(0, 0);
        _tabControl.Name = "_tabControl";
        _tabControl.SelectedIndex = 0;
        _tabControl.Size = new System.Drawing.Size(580, 480);
        _tabControl.TabIndex = 0;

        // ── General tab ───────────────────────────────────────────────────

        // _projectNameLabel
        _projectNameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _projectNameLabel.AutoSize = true;
        _projectNameLabel.Name = "_projectNameLabel";
        _projectNameLabel.Text = "Project Name:";

        // _projectNameBox
        _projectNameBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _projectNameBox.Name = "_projectNameBox";
        _projectNameBox.Size = new System.Drawing.Size(400, 23);

        // _versionLabel
        _versionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _versionLabel.AutoSize = true;
        _versionLabel.Name = "_versionLabel";
        _versionLabel.Text = "Version:";

        // _versionBox
        _versionBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _versionBox.Name = "_versionBox";
        _versionBox.Size = new System.Drawing.Size(400, 23);

        // _csprojLabel
        _csprojLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _csprojLabel.AutoSize = true;
        _csprojLabel.Name = "_csprojLabel";
        _csprojLabel.Text = "Game .csproj:";

        // _csprojBox
        _csprojBox.Dock = System.Windows.Forms.DockStyle.Fill;
        _csprojBox.Name = "_csprojBox";

        // _browseCsprojButton
        _browseCsprojButton.Dock = System.Windows.Forms.DockStyle.Right;
        _browseCsprojButton.Name = "_browseCsprojButton";
        _browseCsprojButton.Size = new System.Drawing.Size(30, 23);
        _browseCsprojButton.Text = "...";
        _browseCsprojButton.Click += OnBrowseCsprojClick;

        // _csprojRow
        _csprojRow.Controls.Add(_csprojBox);
        _csprojRow.Controls.Add(_browseCsprojButton);
        _csprojRow.Dock = System.Windows.Forms.DockStyle.Fill;
        _csprojRow.Name = "_csprojRow";

        // _editorFolderLabel
        _editorFolderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _editorFolderLabel.AutoSize = true;
        _editorFolderLabel.Name = "_editorFolderLabel";
        _editorFolderLabel.Text = "Editor folder:";

        // _editorFolderBox
        _editorFolderBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _editorFolderBox.Name = "_editorFolderBox";
        _editorFolderBox.ReadOnly = true;
        _editorFolderBox.Size = new System.Drawing.Size(400, 23);

        // _rootNamespaceLabel
        _rootNamespaceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _rootNamespaceLabel.AutoSize = true;
        _rootNamespaceLabel.Name = "_rootNamespaceLabel";
        _rootNamespaceLabel.Text = "Root namespace:";

        // _rootNamespaceBox
        _rootNamespaceBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _rootNamespaceBox.Name = "_rootNamespaceBox";
        _rootNamespaceBox.Size = new System.Drawing.Size(400, 23);

        // _virtualResLabel
        _virtualResLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _virtualResLabel.AutoSize = true;
        _virtualResLabel.Name = "_virtualResLabel";
        _virtualResLabel.Text = "Virtual resolution:";

        // _virtualWidthInput
        _virtualWidthInput.Dock = System.Windows.Forms.DockStyle.Left;
        _virtualWidthInput.Minimum = 1;
        _virtualWidthInput.Maximum = 7680;
        _virtualWidthInput.Value = 1920;
        _virtualWidthInput.Name = "_virtualWidthInput";
        _virtualWidthInput.Width = 70;

        // _virtualResXLabel
        _virtualResXLabel.Dock = System.Windows.Forms.DockStyle.Left;
        _virtualResXLabel.AutoSize = false;
        _virtualResXLabel.Width = 14;
        _virtualResXLabel.Name = "_virtualResXLabel";
        _virtualResXLabel.Text = " x";
        _virtualResXLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        // _virtualHeightInput
        _virtualHeightInput.Dock = System.Windows.Forms.DockStyle.Fill;
        _virtualHeightInput.Minimum = 1;
        _virtualHeightInput.Maximum = 4320;
        _virtualHeightInput.Value = 1080;
        _virtualHeightInput.Name = "_virtualHeightInput";

        // _virtualResRow
        _virtualResRow.Controls.Add(_virtualHeightInput);
        _virtualResRow.Controls.Add(_virtualResXLabel);
        _virtualResRow.Controls.Add(_virtualWidthInput);
        _virtualResRow.Dock = System.Windows.Forms.DockStyle.Fill;
        _virtualResRow.Name = "_virtualResRow";

        // _generalLayout
        _generalLayout.ColumnCount = 2;
        _generalLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
        _generalLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _generalLayout.Controls.Add(_projectNameLabel, 0, 0);
        _generalLayout.Controls.Add(_projectNameBox, 1, 0);
        _generalLayout.Controls.Add(_versionLabel, 0, 1);
        _generalLayout.Controls.Add(_versionBox, 1, 1);
        _generalLayout.Controls.Add(_csprojLabel, 0, 2);
        _generalLayout.Controls.Add(_csprojRow, 1, 2);
        _generalLayout.Controls.Add(_editorFolderLabel, 0, 3);
        _generalLayout.Controls.Add(_editorFolderBox, 1, 3);
        _generalLayout.Controls.Add(_rootNamespaceLabel, 0, 4);
        _generalLayout.Controls.Add(_rootNamespaceBox, 1, 4);
        _generalLayout.Controls.Add(_virtualResLabel, 0, 5);
        _generalLayout.Controls.Add(_virtualResRow, 1, 5);
        _generalLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        _generalLayout.Location = new System.Drawing.Point(3, 3);
        _generalLayout.Name = "_generalLayout";
        _generalLayout.Padding = new System.Windows.Forms.Padding(4);
        _generalLayout.RowCount = 6;
        _generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _generalLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _generalLayout.Size = new System.Drawing.Size(556, 420);
        _generalLayout.TabIndex = 0;

        // _generalTab
        _generalTab.Controls.Add(_generalLayout);
        _generalTab.Location = new System.Drawing.Point(4, 24);
        _generalTab.Name = "_generalTab";
        _generalTab.Padding = new System.Windows.Forms.Padding(3);
        _generalTab.Size = new System.Drawing.Size(556, 442);
        _generalTab.TabIndex = 0;
        _generalTab.Text = "General";

        // ── Content tab ───────────────────────────────────────────────────

        // _contentFolderLabel
        _contentFolderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _contentFolderLabel.AutoSize = true;
        _contentFolderLabel.Name = "_contentFolderLabel";
        _contentFolderLabel.Text = "Content folder:";

        // _contentFolderBox
        _contentFolderBox.Dock = System.Windows.Forms.DockStyle.Fill;
        _contentFolderBox.Name = "_contentFolderBox";

        // _browseContentFolderButton
        _browseContentFolderButton.Dock = System.Windows.Forms.DockStyle.Right;
        _browseContentFolderButton.Name = "_browseContentFolderButton";
        _browseContentFolderButton.Size = new System.Drawing.Size(30, 23);
        _browseContentFolderButton.Text = "...";
        _browseContentFolderButton.Click += OnBrowseContentFolderClick;

        // _contentFolderRow
        _contentFolderRow.Controls.Add(_contentFolderBox);
        _contentFolderRow.Controls.Add(_browseContentFolderButton);
        _contentFolderRow.Dock = System.Windows.Forms.DockStyle.Fill;
        _contentFolderRow.Name = "_contentFolderRow";

        // _mgcbFileLabel
        _mgcbFileLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _mgcbFileLabel.AutoSize = true;
        _mgcbFileLabel.Name = "_mgcbFileLabel";
        _mgcbFileLabel.Text = "MGCB file:";

        // _mgcbFileBox
        _mgcbFileBox.Dock = System.Windows.Forms.DockStyle.Fill;
        _mgcbFileBox.Name = "_mgcbFileBox";
        _mgcbFileBox.ReadOnly = true;

        // _browseMgcbButton
        _browseMgcbButton.Dock = System.Windows.Forms.DockStyle.Right;
        _browseMgcbButton.Name = "_browseMgcbButton";
        _browseMgcbButton.Size = new System.Drawing.Size(30, 23);
        _browseMgcbButton.Text = "...";
        _browseMgcbButton.Click += OnBrowseMgcbClick;

        // _mgcbFileRow
        _mgcbFileRow.Controls.Add(_mgcbFileBox);
        _mgcbFileRow.Controls.Add(_browseMgcbButton);
        _mgcbFileRow.Dock = System.Windows.Forms.DockStyle.Fill;
        _mgcbFileRow.Name = "_mgcbFileRow";

        // _buildConfigLabel
        _buildConfigLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _buildConfigLabel.AutoSize = true;
        _buildConfigLabel.Name = "_buildConfigLabel";
        _buildConfigLabel.Text = "Build config:";

        // _buildConfigCombo
        _buildConfigCombo.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _buildConfigCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _buildConfigCombo.Items.Add("Debug");
        _buildConfigCombo.Items.Add("Release");
        _buildConfigCombo.Name = "_buildConfigCombo";
        _buildConfigCombo.Size = new System.Drawing.Size(160, 23);

        // _autoBuildCheckBox
        _autoBuildCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _autoBuildCheckBox.AutoSize = true;
        _autoBuildCheckBox.Name = "_autoBuildCheckBox";
        _autoBuildCheckBox.Text = "Auto-build on Play";

        // _contentLayout
        _contentLayout.ColumnCount = 2;
        _contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
        _contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _contentLayout.Controls.Add(_contentFolderLabel, 0, 0);
        _contentLayout.Controls.Add(_contentFolderRow, 1, 0);
        _contentLayout.Controls.Add(_mgcbFileLabel, 0, 1);
        _contentLayout.Controls.Add(_mgcbFileRow, 1, 1);
        _contentLayout.Controls.Add(_buildConfigLabel, 0, 2);
        _contentLayout.Controls.Add(_buildConfigCombo, 1, 2);
        _contentLayout.Controls.Add(_autoBuildCheckBox, 0, 3);
        _contentLayout.SetColumnSpan(_autoBuildCheckBox, 2);
        _contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        _contentLayout.Location = new System.Drawing.Point(3, 3);
        _contentLayout.Name = "_contentLayout";
        _contentLayout.Padding = new System.Windows.Forms.Padding(4);
        _contentLayout.RowCount = 4;
        _contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _contentLayout.Size = new System.Drawing.Size(556, 420);
        _contentLayout.TabIndex = 0;

        // _contentTab
        _contentTab.Controls.Add(_contentLayout);
        _contentTab.Location = new System.Drawing.Point(4, 24);
        _contentTab.Name = "_contentTab";
        _contentTab.Padding = new System.Windows.Forms.Padding(3);
        _contentTab.Size = new System.Drawing.Size(556, 442);
        _contentTab.TabIndex = 1;
        _contentTab.Text = "Content";

        // ── Localization tab ──────────────────────────────────────────────

        // _locFolderLabel
        _locFolderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _locFolderLabel.AutoSize = true;
        _locFolderLabel.Name = "_locFolderLabel";
        _locFolderLabel.Text = "Localization folder:";

        // _locFolderBox
        _locFolderBox.Dock = System.Windows.Forms.DockStyle.Fill;
        _locFolderBox.Name = "_locFolderBox";

        // _browseLocFolderButton
        _browseLocFolderButton.Dock = System.Windows.Forms.DockStyle.Right;
        _browseLocFolderButton.Name = "_browseLocFolderButton";
        _browseLocFolderButton.Size = new System.Drawing.Size(30, 23);
        _browseLocFolderButton.Text = "...";
        _browseLocFolderButton.Click += OnBrowseLocFolderClick;

        // _locFolderRow
        _locFolderRow.Controls.Add(_locFolderBox);
        _locFolderRow.Controls.Add(_browseLocFolderButton);
        _locFolderRow.Dock = System.Windows.Forms.DockStyle.Fill;
        _locFolderRow.Name = "_locFolderRow";

        // _defaultLocaleLabel
        _defaultLocaleLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _defaultLocaleLabel.AutoSize = true;
        _defaultLocaleLabel.Name = "_defaultLocaleLabel";
        _defaultLocaleLabel.Text = "Default locale:";

        // _defaultLocaleCombo
        _defaultLocaleCombo.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _defaultLocaleCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
        _defaultLocaleCombo.Name = "_defaultLocaleCombo";
        _defaultLocaleCombo.Size = new System.Drawing.Size(160, 23);

        // _supportedLocalesLabel
        _supportedLocalesLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        _supportedLocalesLabel.AutoSize = true;
        _supportedLocalesLabel.Name = "_supportedLocalesLabel";
        _supportedLocalesLabel.Text = "Supported locales:";

        // _localeColumn
        _localeColumn.HeaderText = "Locale";
        _localeColumn.Name = "_localeColumn";

        // _supportedLocalesGrid
        _supportedLocalesGrid.AllowUserToAddRows = true;
        _supportedLocalesGrid.AllowUserToDeleteRows = true;
        _supportedLocalesGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        _supportedLocalesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _supportedLocalesGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _localeColumn });
        _supportedLocalesGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        _supportedLocalesGrid.Name = "_supportedLocalesGrid";
        _supportedLocalesGrid.TabIndex = 0;

        // _localizationLayout
        _localizationLayout.ColumnCount = 2;
        _localizationLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
        _localizationLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _localizationLayout.Controls.Add(_locFolderLabel, 0, 0);
        _localizationLayout.Controls.Add(_locFolderRow, 1, 0);
        _localizationLayout.Controls.Add(_defaultLocaleLabel, 0, 1);
        _localizationLayout.Controls.Add(_defaultLocaleCombo, 1, 1);
        _localizationLayout.Controls.Add(_supportedLocalesLabel, 0, 2);
        _localizationLayout.Controls.Add(_supportedLocalesGrid, 1, 2);
        _localizationLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        _localizationLayout.Location = new System.Drawing.Point(3, 3);
        _localizationLayout.Name = "_localizationLayout";
        _localizationLayout.Padding = new System.Windows.Forms.Padding(4);
        _localizationLayout.RowCount = 3;
        _localizationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _localizationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _localizationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _localizationLayout.Size = new System.Drawing.Size(556, 420);
        _localizationLayout.TabIndex = 0;

        // _localizationTab
        _localizationTab.Controls.Add(_localizationLayout);
        _localizationTab.Location = new System.Drawing.Point(4, 24);
        _localizationTab.Name = "_localizationTab";
        _localizationTab.Padding = new System.Windows.Forms.Padding(3);
        _localizationTab.Size = new System.Drawing.Size(556, 442);
        _localizationTab.TabIndex = 2;
        _localizationTab.Text = "Localization";

        // ── Code Generation tab ───────────────────────────────────────────

        // _outputFolderLabel
        _outputFolderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _outputFolderLabel.AutoSize = true;
        _outputFolderLabel.Name = "_outputFolderLabel";
        _outputFolderLabel.Text = "Output folder:";

        // _outputFolderBox
        _outputFolderBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _outputFolderBox.Name = "_outputFolderBox";
        _outputFolderBox.Size = new System.Drawing.Size(400, 23);
        _outputFolderBox.TextChanged += OnOutputFolderChanged;

        // _generateOnSaveCheckBox
        _generateOnSaveCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _generateOnSaveCheckBox.AutoSize = true;
        _generateOnSaveCheckBox.Name = "_generateOnSaveCheckBox";
        _generateOnSaveCheckBox.Text = "Generate code on Scene Save";

        // _previewPathLabel
        _previewPathLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _previewPathLabel.AutoSize = true;
        _previewPathLabel.Name = "_previewPathLabel";
        _previewPathLabel.Text = "Preview output path:";

        // _previewOutputLabel
        _previewOutputLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _previewOutputLabel.AutoSize = true;
        _previewOutputLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        _previewOutputLabel.Name = "_previewOutputLabel";
        _previewOutputLabel.Text = "(no project loaded)";

        // _generateAllScenesButton
        _generateAllScenesButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _generateAllScenesButton.AutoSize = true;
        _generateAllScenesButton.Enabled = false;
        _generateAllScenesButton.Name = "_generateAllScenesButton";
        _generateAllScenesButton.Text = "Generate All Scenes Now";

        // _codeGenLayout
        _codeGenLayout.ColumnCount = 2;
        _codeGenLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
        _codeGenLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _codeGenLayout.Controls.Add(_outputFolderLabel, 0, 0);
        _codeGenLayout.Controls.Add(_outputFolderBox, 1, 0);
        _codeGenLayout.Controls.Add(_generateOnSaveCheckBox, 0, 1);
        _codeGenLayout.SetColumnSpan(_generateOnSaveCheckBox, 2);
        _codeGenLayout.Controls.Add(_previewPathLabel, 0, 2);
        _codeGenLayout.Controls.Add(_previewOutputLabel, 1, 2);
        _codeGenLayout.Controls.Add(_generateAllScenesButton, 0, 3);
        _codeGenLayout.SetColumnSpan(_generateAllScenesButton, 2);
        _codeGenLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        _codeGenLayout.Location = new System.Drawing.Point(3, 3);
        _codeGenLayout.Name = "_codeGenLayout";
        _codeGenLayout.Padding = new System.Windows.Forms.Padding(4);
        _codeGenLayout.RowCount = 4;
        _codeGenLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _codeGenLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _codeGenLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _codeGenLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
        _codeGenLayout.Size = new System.Drawing.Size(556, 420);
        _codeGenLayout.TabIndex = 0;

        // _codeGenTab
        _codeGenTab.Controls.Add(_codeGenLayout);
        _codeGenTab.Location = new System.Drawing.Point(4, 24);
        _codeGenTab.Name = "_codeGenTab";
        _codeGenTab.Padding = new System.Windows.Forms.Padding(3);
        _codeGenTab.Size = new System.Drawing.Size(556, 442);
        _codeGenTab.TabIndex = 3;
        _codeGenTab.Text = "Code Generation";

        // ── Button panel ──────────────────────────────────────────────────

        // _cancelButton
        _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new System.Drawing.Size(80, 26);
        _cancelButton.Text = "Cancel";

        // _okButton
        _okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        _okButton.Name = "_okButton";
        _okButton.Size = new System.Drawing.Size(80, 26);
        _okButton.Text = "OK";
        _okButton.Click += OnOkClick;

        // _buttonPanel
        _buttonPanel.AutoSize = true;
        _buttonPanel.Controls.Add(_okButton);
        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
        _buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        _buttonPanel.Name = "_buttonPanel";
        _buttonPanel.Padding = new System.Windows.Forms.Padding(4);
        _buttonPanel.Size = new System.Drawing.Size(580, 40);
        _buttonPanel.TabIndex = 1;

        // ── Form ──────────────────────────────────────────────────────────
        AcceptButton = _okButton;
        CancelButton = _cancelButton;
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(580, 520);
        Controls.Add(_tabControl);
        Controls.Add(_buttonPanel);
        Font = new System.Drawing.Font("Segoe UI", 9F);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ProjectSettingsDialog";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "Project Settings";

        ((System.ComponentModel.ISupportInitialize)_supportedLocalesGrid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_virtualWidthInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_virtualHeightInput).EndInit();
        _generalTab.ResumeLayout(false);
        _contentTab.ResumeLayout(false);
        _localizationTab.ResumeLayout(false);
        _codeGenTab.ResumeLayout(false);
        _tabControl.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
