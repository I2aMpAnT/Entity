namespace entity.MetaEditor2
{
    partial class MetaEditorControlPage
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            // Before closing the Panel, make sure the user has the option to save any changes
            this.checkSave();

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MetaEditorControlPage));
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnReset = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.cbHideUnused = new System.Windows.Forms.CheckBox();
            this.btnSaveValues = new System.Windows.Forms.Button();
            this.btnRestoreValues = new System.Windows.Forms.Button();
            this.cbSortByName = new System.Windows.Forms.CheckBox();
            this.tmr_MEControlPage = new System.Windows.Forms.Timer(this.components);
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeViewTagReflexives = new System.Windows.Forms.TreeView();
            this.cmTreeReflexives = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsExternalReferenceAdd = new System.Windows.Forms.ToolStripMenuItem();
            this.tsExternalReferencePoint = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panelMetaEditor = new System.Windows.Forms.Panel();
            this.panelInfoPane = new System.Windows.Forms.TextBox();
            this.toolStripPanel1 = new System.Windows.Forms.ToolStripPanel();
            this.tsNavigation = new System.Windows.Forms.ToolStrip();
            this.tsMetaMassEdit = new System.Windows.Forms.ToolStrip();
            this.tsBtnResetValue = new System.Windows.Forms.ToolStripButton();
            this.tsBtnResetReflexiveChunk = new System.Windows.Forms.ToolStripButton();
            this.tsBtnResetReflexiveAll = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsBtnCopyToAll = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsBtnAddChunk = new System.Windows.Forms.ToolStripButton();
            this.tsBtnDeleteChunk = new System.Windows.Forms.ToolStripButton();
            this.tsBtnDuplicateChunk = new System.Windows.Forms.ToolStripButton();
            this.tsDebugCommands = new System.Windows.Forms.ToolStrip();
            this.tsbtnPeek = new System.Windows.Forms.ToolStripButton();
            this.tsbtnPoke = new System.Windows.Forms.ToolStripButton();
            this.tscbApplyTo = new System.Windows.Forms.ToolStripComboBox();
            this.btnTreeViewOpen = new System.Windows.Forms.RadioButton();
            this.cmIdent = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cmMassField = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsMassFieldOpenMassFieldEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.jumpToTagToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsIdentOpenMassFieldEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.cmTreeReflexives.SuspendLayout();
            this.panel1.SuspendLayout();
            this.toolStripPanel1.SuspendLayout();
            this.tsMetaMassEdit.SuspendLayout();
            this.tsDebugCommands.SuspendLayout();
            this.cmIdent.SuspendLayout();
            this.cmMassField.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(89, 6);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(80, 23);
            this.btnReset.TabIndex = 2;
            this.btnReset.Text = "&Reset Tag";
            this.toolTip1.SetToolTip(this.btnReset, "Resets all the tags values back to last saved values");
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(3, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(80, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "&Save";
            this.toolTip1.SetToolTip(this.btnSave, "Saves all changes made within the current tag");
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // cbHideUnused
            // 
            this.cbHideUnused.AutoSize = true;
            this.cbHideUnused.Checked = true;
            this.cbHideUnused.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbHideUnused.Location = new System.Drawing.Point(17, 65);
            this.cbHideUnused.Name = "cbHideUnused";
            this.cbHideUnused.Size = new System.Drawing.Size(140, 17);
            this.cbHideUnused.TabIndex = 0;
            this.cbHideUnused.Text = "Hide Unused Reflexives";
            this.toolTip1.SetToolTip(this.cbHideUnused, "Removes reflexives with a count of zero from the tree listing");
            this.cbHideUnused.UseVisualStyleBackColor = true;
            this.cbHideUnused.CheckedChanged += new System.EventHandler(this.cbHideUnused_CheckedChanged);
            // 
            // btnSaveValues
            // 
            this.btnSaveValues.Location = new System.Drawing.Point(3, 35);
            this.btnSaveValues.Name = "btnSaveValues";
            this.btnSaveValues.Size = new System.Drawing.Size(80, 23);
            this.btnSaveValues.TabIndex = 3;
            this.btnSaveValues.Text = "Export Values";
            this.toolTip1.SetToolTip(this.btnSaveValues, "Backs up all tag values, allowing for later restoration");
            this.btnSaveValues.UseVisualStyleBackColor = true;
            this.btnSaveValues.Click += new System.EventHandler(this.btnSaveValues_Click);
            // 
            // btnRestoreValues
            // 
            this.btnRestoreValues.Enabled = false;
            this.btnRestoreValues.Location = new System.Drawing.Point(89, 35);
            this.btnRestoreValues.Name = "btnRestoreValues";
            this.btnRestoreValues.Size = new System.Drawing.Size(80, 23);
            this.btnRestoreValues.TabIndex = 4;
            this.btnRestoreValues.Text = "Import Values";
            this.toolTip1.SetToolTip(this.btnRestoreValues, "Restores chosen values within a tag");
            this.btnRestoreValues.UseVisualStyleBackColor = true;
            this.btnRestoreValues.Click += new System.EventHandler(this.btnRestoreValues_Click);
            // 
            // cbSortByName
            // 
            this.cbSortByName.AutoSize = true;
            this.cbSortByName.Checked = true;
            this.cbSortByName.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSortByName.Location = new System.Drawing.Point(17, 81);
            this.cbSortByName.Name = "cbSortByName";
            this.cbSortByName.Size = new System.Drawing.Size(91, 17);
            this.cbSortByName.TabIndex = 5;
            this.cbSortByName.Text = "Sort By Name";
            this.toolTip1.SetToolTip(this.cbSortByName, "Sorts either by offset or name");
            this.cbSortByName.UseVisualStyleBackColor = true;
            this.cbSortByName.CheckedChanged += new System.EventHandler(this.cbSortByName_CheckedChanged);
            // 
            // tmr_MEControlPage
            // 
            this.tmr_MEControlPage.Interval = 500;
            this.tmr_MEControlPage.Tick += new System.EventHandler(this.tmr_MEControlPage_Tick);
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(20, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeViewTagReflexives);
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panelMetaEditor);
            this.splitContainer1.Panel2.Controls.Add(this.panelInfoPane);
            this.splitContainer1.Panel2.Controls.Add(this.toolStripPanel1);
            this.splitContainer1.Size = new System.Drawing.Size(931, 374);
            this.splitContainer1.SplitterDistance = 195;
            this.splitContainer1.SplitterWidth = 3;
            this.splitContainer1.TabIndex = 3;
            // 
            // treeViewTagReflexives
            // 
            this.treeViewTagReflexives.ContextMenuStrip = this.cmTreeReflexives;
            this.treeViewTagReflexives.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewTagReflexives.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
            this.treeViewTagReflexives.HideSelection = false;
            this.treeViewTagReflexives.Location = new System.Drawing.Point(0, 104);
            this.treeViewTagReflexives.Name = "treeViewTagReflexives";
            this.treeViewTagReflexives.ShowNodeToolTips = true;
            this.treeViewTagReflexives.Size = new System.Drawing.Size(193, 268);
            this.treeViewTagReflexives.TabIndex = 0;
            this.treeViewTagReflexives.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.treeViewTagReflexives_DrawNode);
            this.treeViewTagReflexives.DoubleClick += new System.EventHandler(this.treeViewTagReflexives_DoubleClick);
            this.treeViewTagReflexives.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewTagReflexives_AfterSelect);
            this.treeViewTagReflexives.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewTagReflexives_BeforeSelect);
            this.treeViewTagReflexives.MouseLeave += new System.EventHandler(this.treeViewTagReflexives_MouseLeave);
            this.treeViewTagReflexives.Click += new System.EventHandler(this.treeViewTagReflexives_Click);
            // 
            // cmTreeReflexives
            // 
            this.cmTreeReflexives.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsExternalReferenceAdd,
            this.tsExternalReferencePoint});
            this.cmTreeReflexives.Name = "cmTreeReflexives";
            this.cmTreeReflexives.Size = new System.Drawing.Size(265, 48);
            // 
            // tsExternalReferenceAdd
            // 
            this.tsExternalReferenceAdd.Name = "tsExternalReferenceAdd";
            this.tsExternalReferenceAdd.Size = new System.Drawing.Size(264, 22);
            this.tsExternalReferenceAdd.Text = "Add to external references list";
            this.tsExternalReferenceAdd.Click += new System.EventHandler(this.tsExternalReferenceAdd_Click);
            // 
            // tsExternalReferencePoint
            // 
            this.tsExternalReferencePoint.Name = "tsExternalReferencePoint";
            this.tsExternalReferencePoint.Size = new System.Drawing.Size(264, 22);
            this.tsExternalReferencePoint.Text = "Reference external reflexive from list";
            this.tsExternalReferencePoint.Click += new System.EventHandler(this.tsExternalReferencePoint_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.cbSortByName);
            this.panel1.Controls.Add(this.btnRestoreValues);
            this.panel1.Controls.Add(this.btnSaveValues);
            this.panel1.Controls.Add(this.btnReset);
            this.panel1.Controls.Add(this.btnSave);
            this.panel1.Controls.Add(this.cbHideUnused);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(193, 104);
            this.panel1.TabIndex = 1;
            this.panel1.MouseLeave += new System.EventHandler(this.treeViewTagReflexives_MouseLeave);
            // 
            // panelMetaEditor
            // 
            this.panelMetaEditor.AutoScroll = true;
            this.panelMetaEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMetaEditor.Location = new System.Drawing.Point(0, 50);
            this.panelMetaEditor.Name = "panelMetaEditor";
            this.panelMetaEditor.Size = new System.Drawing.Size(731, 250);
            this.panelMetaEditor.TabIndex = 1;
            // 
            // panelInfoPane
            // 
            this.panelInfoPane.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panelInfoPane.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelInfoPane.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelInfoPane.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panelInfoPane.Location = new System.Drawing.Point(0, 300);
            this.panelInfoPane.Multiline = true;
            this.panelInfoPane.Name = "panelInfoPane";
            this.panelInfoPane.Size = new System.Drawing.Size(731, 72);
            this.panelInfoPane.TabIndex = 0;
            this.panelInfoPane.Text = "Info Pane";
            this.panelInfoPane.Visible = false;
            // 
            // toolStripPanel1
            // 
            this.toolStripPanel1.Controls.Add(this.tsNavigation);
            this.toolStripPanel1.Controls.Add(this.tsMetaMassEdit);
            this.toolStripPanel1.Controls.Add(this.tsDebugCommands);
            this.toolStripPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolStripPanel1.Location = new System.Drawing.Point(0, 0);
            this.toolStripPanel1.Name = "toolStripPanel1";
            this.toolStripPanel1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.toolStripPanel1.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.toolStripPanel1.Size = new System.Drawing.Size(731, 50);
            // 
            // tsNavigation
            // 
            this.tsNavigation.Dock = System.Windows.Forms.DockStyle.None;
            this.tsNavigation.Location = new System.Drawing.Point(0, 0);
            this.tsNavigation.Name = "tsNavigation";
            this.tsNavigation.Size = new System.Drawing.Size(731, 25);
            this.tsNavigation.Stretch = true;
            this.tsNavigation.TabIndex = 0;
            this.tsNavigation.Text = "toolStrip1";
            // 
            // tsMetaMassEdit
            // 
            this.tsMetaMassEdit.Dock = System.Windows.Forms.DockStyle.None;
            this.tsMetaMassEdit.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsBtnResetValue,
            this.tsBtnResetReflexiveChunk,
            this.tsBtnResetReflexiveAll,
            this.toolStripSeparator1,
            this.tsBtnCopyToAll,
            this.toolStripSeparator2,
            this.tsBtnAddChunk,
            this.tsBtnDeleteChunk,
            this.tsBtnDuplicateChunk});
            this.tsMetaMassEdit.Location = new System.Drawing.Point(3, 25);
            this.tsMetaMassEdit.Name = "tsMetaMassEdit";
            this.tsMetaMassEdit.Size = new System.Drawing.Size(600, 25);
            this.tsMetaMassEdit.TabIndex = 0;
            this.tsMetaMassEdit.Text = "toolStrip1";
            // 
            // tsBtnResetValue
            // 
            this.tsBtnResetValue.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsBtnResetValue.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnResetValue.Image")));
            this.tsBtnResetValue.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnResetValue.Name = "tsBtnResetValue";
            this.tsBtnResetValue.Size = new System.Drawing.Size(68, 22);
            this.tsBtnResetValue.Text = "Reset Value";
            this.tsBtnResetValue.Click += new System.EventHandler(this.tsBtnResetValue_Click);
            // 
            // tsBtnResetReflexiveChunk
            // 
            this.tsBtnResetReflexiveChunk.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsBtnResetReflexiveChunk.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnResetReflexiveChunk.Image")));
            this.tsBtnResetReflexiveChunk.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnResetReflexiveChunk.Name = "tsBtnResetReflexiveChunk";
            this.tsBtnResetReflexiveChunk.Size = new System.Drawing.Size(72, 22);
            this.tsBtnResetReflexiveChunk.Text = "Reset Chunk";
            this.tsBtnResetReflexiveChunk.Click += new System.EventHandler(this.tsBtnResetReflexive_Click);
            // 
            // tsBtnResetReflexiveAll
            // 
            this.tsBtnResetReflexiveAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsBtnResetReflexiveAll.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnResetReflexiveAll.Image")));
            this.tsBtnResetReflexiveAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnResetReflexiveAll.Name = "tsBtnResetReflexiveAll";
            this.tsBtnResetReflexiveAll.Size = new System.Drawing.Size(99, 22);
            this.tsBtnResetReflexiveAll.Text = "Reset Chunks (All)";
            this.tsBtnResetReflexiveAll.Click += new System.EventHandler(this.tsBtnResetReflexiveAll_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsBtnCopyToAll
            // 
            this.tsBtnCopyToAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsBtnCopyToAll.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnCopyToAll.Image")));
            this.tsBtnCopyToAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnCopyToAll.Name = "tsBtnCopyToAll";
            this.tsBtnCopyToAll.Size = new System.Drawing.Size(98, 22);
            this.tsBtnCopyToAll.Text = "Copy to all chunks";
            this.tsBtnCopyToAll.ToolTipText = "Copies the current selected value to all reflexive chunks";
            this.tsBtnCopyToAll.Click += new System.EventHandler(this.tsBtnCopyToAll_Click);
            //
            // toolStripSeparator2
            //
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            //
            // tsBtnAddChunk
            //
            this.tsBtnAddChunk.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsBtnAddChunk.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnAddChunk.Name = "tsBtnAddChunk";
            this.tsBtnAddChunk.Size = new System.Drawing.Size(65, 22);
            this.tsBtnAddChunk.Text = "Add Chunk";
            this.tsBtnAddChunk.ToolTipText = "Adds a new blank chunk to the current reflexive";
            this.tsBtnAddChunk.Click += new System.EventHandler(this.tsBtnAddChunk_Click);
            //
            // tsBtnDeleteChunk
            //
            this.tsBtnDeleteChunk.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsBtnDeleteChunk.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnDeleteChunk.Name = "tsBtnDeleteChunk";
            this.tsBtnDeleteChunk.Size = new System.Drawing.Size(78, 22);
            this.tsBtnDeleteChunk.Text = "Delete Chunk";
            this.tsBtnDeleteChunk.ToolTipText = "Deletes the currently selected chunk from the reflexive";
            this.tsBtnDeleteChunk.Click += new System.EventHandler(this.tsBtnDeleteChunk_Click);
            //
            // tsBtnDuplicateChunk
            //
            this.tsBtnDuplicateChunk.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsBtnDuplicateChunk.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnDuplicateChunk.Name = "tsBtnDuplicateChunk";
            this.tsBtnDuplicateChunk.Size = new System.Drawing.Size(95, 22);
            this.tsBtnDuplicateChunk.Text = "Duplicate Chunk";
            this.tsBtnDuplicateChunk.ToolTipText = "Duplicates the currently selected chunk";
            this.tsBtnDuplicateChunk.Click += new System.EventHandler(this.tsBtnDuplicateChunk_Click);
            //
            // tsDebugCommands
            // 
            this.tsDebugCommands.Dock = System.Windows.Forms.DockStyle.None;
            this.tsDebugCommands.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbtnPeek,
            this.tsbtnPoke,
            this.tscbApplyTo});
            this.tsDebugCommands.Location = new System.Drawing.Point(402, 25);
            this.tsDebugCommands.Name = "tsDebugCommands";
            this.tsDebugCommands.Size = new System.Drawing.Size(172, 25);
            this.tsDebugCommands.TabIndex = 1;
            // 
            // tsbtnPeek
            // 
            this.tsbtnPeek.CheckOnClick = true;
            this.tsbtnPeek.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbtnPeek.Image = ((System.Drawing.Image)(resources.GetObject("tsbtnPeek.Image")));
            this.tsbtnPeek.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbtnPeek.Name = "tsbtnPeek";
            this.tsbtnPeek.Size = new System.Drawing.Size(34, 22);
            this.tsbtnPeek.Text = "Peek";
            this.tsbtnPeek.ToolTipText = "Displays retrieved values from the debug box";
            this.tsbtnPeek.CheckedChanged += new System.EventHandler(this.tsbtnPeek_CheckedChanged);
            // 
            // tsbtnPoke
            // 
            this.tsbtnPoke.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbtnPoke.Image = ((System.Drawing.Image)(resources.GetObject("tsbtnPoke.Image")));
            this.tsbtnPoke.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbtnPoke.Name = "tsbtnPoke";
            this.tsbtnPoke.Size = new System.Drawing.Size(34, 22);
            this.tsbtnPoke.Text = "Poke";
            this.tsbtnPoke.ToolTipText = "Sends value(s) to debug box";
            this.tsbtnPoke.Click += new System.EventHandler(this.tsbtnPoke_Click);
            // 
            // tscbApplyTo
            // 
            this.tscbApplyTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscbApplyTo.Items.AddRange(new object[] {
            "Value",
            "Reflexive",
            "All Reflexives"});
            this.tscbApplyTo.Name = "tscbApplyTo";
            this.tscbApplyTo.Size = new System.Drawing.Size(90, 25);
            // 
            // btnTreeViewOpen
            // 
            this.btnTreeViewOpen.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnTreeViewOpen.AutoCheck = false;
            this.btnTreeViewOpen.Checked = true;
            this.btnTreeViewOpen.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnTreeViewOpen.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnTreeViewOpen.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ControlLight;
            this.btnTreeViewOpen.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnTreeViewOpen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTreeViewOpen.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTreeViewOpen.Location = new System.Drawing.Point(0, 0);
            this.btnTreeViewOpen.Margin = new System.Windows.Forms.Padding(1);
            this.btnTreeViewOpen.Name = "btnTreeViewOpen";
            this.btnTreeViewOpen.Size = new System.Drawing.Size(20, 374);
            this.btnTreeViewOpen.TabIndex = 4;
            this.btnTreeViewOpen.TabStop = true;
            this.btnTreeViewOpen.Text = ">>";
            this.btnTreeViewOpen.UseVisualStyleBackColor = true;
            this.btnTreeViewOpen.MouseLeave += new System.EventHandler(this.treeViewTagReflexives_MouseLeave);
            this.btnTreeViewOpen.MouseEnter += new System.EventHandler(this.btnTreeViewOpen_MouseEnter);
            this.btnTreeViewOpen.Click += new System.EventHandler(this.btnTreeViewOpen_Click);
            // 
            // cmIdent
            // 
            this.cmIdent.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.jumpToTagToolStripMenuItem,
            this.tsIdentOpenMassFieldEdit});
            this.cmIdent.Name = "cmIdent";
            this.cmIdent.Size = new System.Drawing.Size(195, 70);
            // 
            // cmMassField
            // 
            this.cmMassField.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMassFieldOpenMassFieldEdit});
            this.cmMassField.Name = "cmMassField";
            this.cmMassField.Size = new System.Drawing.Size(195, 26);
            // 
            // tsOpenMassFieldEdit
            // 
            this.tsMassFieldOpenMassFieldEdit.Name = "tsMassFieldOpenMassFieldEdit";
            this.tsMassFieldOpenMassFieldEdit.Size = new System.Drawing.Size(194, 22);
            this.tsMassFieldOpenMassFieldEdit.Text = "Open Mass Field Editor";
            this.tsMassFieldOpenMassFieldEdit.Click += new System.EventHandler(this.tsOpenMassFieldEdit_Click);
            // 
            // jumpToTagToolStripMenuItem
            // 
            this.jumpToTagToolStripMenuItem.Name = "jumpToTagToolStripMenuItem";
            this.jumpToTagToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
            this.jumpToTagToolStripMenuItem.Text = "Jump To Tag";
            this.jumpToTagToolStripMenuItem.Click += new System.EventHandler(jumpToTagToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.tsIdentOpenMassFieldEdit.Name = "tsIdentOpenMassFieldEdit";
            this.tsIdentOpenMassFieldEdit.Size = new System.Drawing.Size(194, 22);
            this.tsIdentOpenMassFieldEdit.Text = "Open Mass Field Editor";
            this.tsIdentOpenMassFieldEdit.Click += new System.EventHandler(this.tsOpenMassFieldEdit_Click);
            // 
            // MetaEditorControlPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.btnTreeViewOpen);
            this.Name = "MetaEditorControlPage";
            this.Size = new System.Drawing.Size(951, 374);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MetaEditorControlPage_KeyUp);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MetaEditorControlPage_KeyDown);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.cmTreeReflexives.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.toolStripPanel1.ResumeLayout(false);
            this.toolStripPanel1.PerformLayout();
            this.tsMetaMassEdit.ResumeLayout(false);
            this.tsMetaMassEdit.PerformLayout();
            this.tsDebugCommands.ResumeLayout(false);
            this.tsDebugCommands.PerformLayout();
            this.cmIdent.ResumeLayout(false);
            this.cmMassField.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Timer tmr_MEControlPage;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeViewTagReflexives;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.CheckBox cbHideUnused;
        private System.Windows.Forms.TextBox panelInfoPane;
        private System.Windows.Forms.Panel panelMetaEditor;
        private System.Windows.Forms.ToolStrip tsNavigation;
        private System.Windows.Forms.RadioButton btnTreeViewOpen;
        #endregion
        private System.Windows.Forms.ToolStrip tsMetaMassEdit;
        private System.Windows.Forms.ToolStripButton tsBtnResetValue;
        private System.Windows.Forms.ToolStripButton tsBtnResetReflexiveChunk;
        private System.Windows.Forms.ToolStripButton tsBtnResetReflexiveAll;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tsBtnCopyToAll;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton tsBtnAddChunk;
        private System.Windows.Forms.ToolStripButton tsBtnDeleteChunk;
        private System.Windows.Forms.ToolStripButton tsBtnDuplicateChunk;
        private System.Windows.Forms.ContextMenuStrip cmIdent;
        private System.Windows.Forms.ToolStripPanel toolStripPanel1;
        private System.Windows.Forms.ToolStrip tsDebugCommands;
        private System.Windows.Forms.ToolStripButton tsbtnPeek;
        private System.Windows.Forms.ToolStripButton tsbtnPoke;
        private System.Windows.Forms.ToolStripComboBox tscbApplyTo;
        private System.Windows.Forms.Button btnRestoreValues;
        private System.Windows.Forms.Button btnSaveValues;
        private System.Windows.Forms.CheckBox cbSortByName;
        private System.Windows.Forms.ContextMenuStrip cmTreeReflexives;
        private System.Windows.Forms.ToolStripMenuItem tsExternalReferenceAdd;
        private System.Windows.Forms.ToolStripMenuItem tsExternalReferencePoint;
        private System.Windows.Forms.ContextMenuStrip cmMassField;
        private System.Windows.Forms.ToolStripMenuItem tsMassFieldOpenMassFieldEdit;
        private System.Windows.Forms.ToolStripMenuItem jumpToTagToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsIdentOpenMassFieldEdit;

    }
}
