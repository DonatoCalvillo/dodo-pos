using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;

namespace FerreSys.Infrastructure.UI;

internal sealed class UsersForm : Form
{
    private readonly ListUsersUseCase _list;
    private readonly SqliteAppSettingsStore _settings;
    private readonly DataGridView _grid;
    private IReadOnlyList<User> _users = [];

    public UsersForm(SqliteUserRepository users, SqliteAppSettingsStore settings)
    {
        _list = new ListUsersUseCase(users);
        _settings = settings;

        Text = "Empleados";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 480);
        Font = UiTheme.Ui;
        MinimizeBox = false;

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            Font = UiTheme.Ui
        };
        ProductImages.AddPhotoColumn(_grid);
        _grid.Columns.Add("Number", "Número");
        _grid.Columns.Add("Name", "Nombre");
        _grid.Columns.Add("Username", "Usuario");
        _grid.Columns.Add("Job", "Puesto");
        _grid.Columns.Add("Active", "Activo");
        _grid.Columns["Number"]!.FillWeight = 70;
        _grid.Columns["Username"]!.FillWeight = 70;
        _grid.Columns["Job"]!.FillWeight = 80;
        _grid.Columns["Active"]!.FillWeight = 40;
        _grid.CellDoubleClick += (_, _) => EditSelected();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(8),
            FlowDirection = FlowDirection.LeftToRight
        };
        buttons.Controls.Add(Action("Nuevo", (_, _) => Edit(null)));
        buttons.Controls.Add(Action("Editar", (_, _) => EditSelected()));
        buttons.Controls.Add(Action("Credencial PDF", (_, _) => PrintSelected()));
        buttons.Controls.Add(Action("Cerrar", (_, _) => Close()));

        Controls.Add(_grid);
        Controls.Add(buttons);
        Load += (_, _) => Reload();
        FormClosed += (_, _) => ProductImages.ClearRows(_grid);
    }

    private void Reload()
    {
        ProductImages.ClearRows(_grid);
        _users = _list.Execute();
        _grid.Rows.Clear();
        foreach (User user in _users)
        {
            int index = _grid.Rows.Add(
                ProductImages.ForGrid(user.Photo),
                user.EmployeeNumber,
                user.DisplayName,
                user.Username,
                user.JobTitle,
                user.IsActive ? "Sí" : "No");
            _grid.Rows[index].Tag = user.Id;
        }
    }

    private void EditSelected()
    {
        User? user = SelectedUser();
        if (user == null)
        {
            MessageBox.Show("Selecciona un empleado.", "Empleados", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Edit(user);
    }

    private void Edit(User? user)
    {
        using var editor = new UserEditorForm(user);
        if (editor.ShowDialog(this) == DialogResult.OK)
            Reload();
    }

    private void PrintSelected()
    {
        User? user = SelectedUser();
        if (user == null)
        {
            MessageBox.Show("Selecciona un empleado.", "Credencial", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            CredentialPdf.SaveAndOpen(this, user, _settings);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Credencial", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private User? SelectedUser()
    {
        if (_grid.CurrentRow?.Tag is not Guid id)
            return null;

        return _users.FirstOrDefault(user => user.Id == id);
    }

    private static Button Action(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(4) };
        button.Click += click;
        return button;
    }
}
