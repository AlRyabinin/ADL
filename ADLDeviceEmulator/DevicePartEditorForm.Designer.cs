using System.Drawing;
using System.Windows.Forms;

namespace ADLDeviceEmulator;

public sealed partial class DevicePartEditorForm
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel rootLayoutPanel = null!;
    private Label _titleLabel = null!;
    private DataGridView _grid = null!;
    private PropertyGrid _propertyGrid = null!;
    private Label _hintLabel = null!;
    private FlowLayoutPanel buttonsPanel = null!;
    private Button _addButton = null!;
    private Button _removeButton = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    protected override void Dispose(bool disposing)
    {
        if(disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        rootLayoutPanel = new TableLayoutPanel();
        _titleLabel = new Label();
        _grid = new DataGridView();
        _propertyGrid = new PropertyGrid();
        _hintLabel = new Label();
        buttonsPanel = new FlowLayoutPanel();
        _addButton = new Button();
        _removeButton = new Button();
        _saveButton = new Button();
        _cancelButton = new Button();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
        rootLayoutPanel.SuspendLayout();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        //
        // rootLayoutPanel
        //
        rootLayoutPanel.ColumnCount = 1;
        rootLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayoutPanel.Controls.Add(_titleLabel, 0, 0);
        rootLayoutPanel.Controls.Add(_grid, 0, 1);
        rootLayoutPanel.Controls.Add(_propertyGrid, 0, 1);
        rootLayoutPanel.Controls.Add(_hintLabel, 0, 2);
        rootLayoutPanel.Controls.Add(buttonsPanel, 0, 3);
        rootLayoutPanel.Dock = DockStyle.Fill;
        rootLayoutPanel.Padding = new Padding(10);
        rootLayoutPanel.RowCount = 4;
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayoutPanel.Name = "rootLayoutPanel";
        //
        // _titleLabel
        //
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        _titleLabel.Text = "Состав устройства";
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.Name = "_titleLabel";
        //
        // _grid
        //
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = SystemColors.Window;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.Dock = DockStyle.Fill;
        _grid.MultiSelect = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Name = "_grid";
        //
        // _propertyGrid
        //
        _propertyGrid.Dock = DockStyle.Fill;
        _propertyGrid.HelpVisible = true;
        _propertyGrid.PropertySort = PropertySort.Categorized;
        _propertyGrid.ToolbarVisible = true;
        _propertyGrid.Name = "_propertyGrid";
        _propertyGrid.Visible = false;
        //
        // _hintLabel
        //
        _hintLabel.Dock = DockStyle.Fill;
        _hintLabel.ForeColor = SystemColors.GrayText;
        _hintLabel.Text = "Изменения применяются после нажатия Сохранить.";
        _hintLabel.TextAlign = ContentAlignment.MiddleLeft;
        _hintLabel.Name = "_hintLabel";
        //
        // buttonsPanel
        //
        buttonsPanel.AutoSize = true;
        buttonsPanel.Controls.Add(_addButton);
        buttonsPanel.Controls.Add(_removeButton);
        buttonsPanel.Controls.Add(_saveButton);
        buttonsPanel.Controls.Add(_cancelButton);
        buttonsPanel.Dock = DockStyle.Fill;
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.WrapContents = false;
        buttonsPanel.Name = "buttonsPanel";
        //
        // _addButton
        //
        _addButton.AutoSize = true;
        _addButton.Text = "Добавить";
        _addButton.UseVisualStyleBackColor = true;
        _addButton.Click += addButton_Click;
        //
        // _removeButton
        //
        _removeButton.AutoSize = true;
        _removeButton.Text = "Удалить";
        _removeButton.UseVisualStyleBackColor = true;
        _removeButton.Click += removeButton_Click;
        //
        // _saveButton
        //
        _saveButton.AutoSize = true;
        _saveButton.Text = "Сохранить";
        _saveButton.DialogResult = DialogResult.None;
        _saveButton.UseVisualStyleBackColor = true;
        _saveButton.Click += saveButton_Click;
        //
        // _cancelButton
        //
        _cancelButton.AutoSize = true;
        _cancelButton.Text = "Отмена";
        _cancelButton.UseVisualStyleBackColor = true;
        _cancelButton.Click += cancelButton_Click;
        //
        // DevicePartEditorForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1050, 650);
        Controls.Add(rootLayoutPanel);
        MinimumSize = new Size(780, 480);
        Name = "DevicePartEditorForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Редактор части устройства";
        ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
        rootLayoutPanel.ResumeLayout(false);
        buttonsPanel.ResumeLayout(false);
        buttonsPanel.PerformLayout();
        ResumeLayout(false);
    }
}
