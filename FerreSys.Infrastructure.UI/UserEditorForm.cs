using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

internal sealed class UserEditorForm : Form
{
    private readonly User? _existing;
    private readonly TextBox _firstName = new() { Dock = DockStyle.Fill };
    private readonly TextBox _lastName = new() { Dock = DockStyle.Fill };
    private readonly DateTimePicker _birth = new() { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
    private readonly TextBox _username = new() { Dock = DockStyle.Fill };
    private readonly TextBox _password = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly TextBox _confirm = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly TextBox _job = new() { Dock = DockStyle.Fill };
    private readonly TextBox _phone = new() { Dock = DockStyle.Fill };
    private readonly TextBox _email = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _hasHire = new() { Text = "Fecha de ingreso", AutoSize = true };
    private readonly DateTimePicker _hire = new() { Format = DateTimePickerFormat.Short, Width = 120, Enabled = false };
    private readonly TextBox _notes = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _active = new() { Text = "Activo", AutoSize = true, Checked = true };
    private readonly PictureBox _photo = new() { Size = new Size(96, 96), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
    private readonly Dictionary<UserPermission, CheckBox> _permissions = new();
    private byte[]? _photoBytes;

    public UserEditorForm(User? existing)
    {
        _existing = existing;
        bool admin = existing?.IsBuiltInAdmin == true;

        Text = existing == null ? "Nuevo empleado" : "Editar empleado";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 620);
        Font = UiTheme.Ui;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        if (existing != null)
        {
            _firstName.Text = existing.FirstName;
            _lastName.Text = existing.LastName;
            _birth.Value = existing.BirthDate.ToDateTime(TimeOnly.MinValue);
            _username.Text = existing.Username;
            _job.Text = existing.JobTitle;
            _phone.Text = existing.Phone;
            _email.Text = existing.Email;
            _notes.Text = existing.Notes;
            _active.Checked = existing.IsActive;
            _photoBytes = existing.Photo;
            if (existing.HireDate is DateOnly hiredOn)
            {
                _hasHire.Checked = true;
                _hire.Value = hiredOn.ToDateTime(TimeOnly.MinValue);
            }
        }
        else
        {
            _birth.Value = DateTime.Today.AddYears(-18);
        }

        _photo.Image = ProductImages.ForGrid(_photoBytes);
        _hasHire.CheckedChanged += (_, _) => _hire.Enabled = _hasHire.Checked;

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(12),
            AutoScroll = true
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;
        Add(grid, ref row, "Nombre", _firstName);
        Add(grid, ref row, "Apellidos", _lastName);
        Add(grid, ref row, "Nacimiento", _birth);
        Add(grid, ref row, "Usuario", _username);
        Add(grid, ref row, existing == null ? "Contraseña" : "Nueva contraseña", _password);
        Add(grid, ref row, "Confirmar", _confirm);
        Add(grid, ref row, "Puesto", _job);
        Add(grid, ref row, "Teléfono", _phone);
        Add(grid, ref row, "Correo", _email);

        var hire = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        hire.Controls.Add(_hasHire);
        hire.Controls.Add(_hire);
        Add(grid, ref row, "Ingreso", hire);
        Add(grid, ref row, "Notas", _notes, 72);
        Add(grid, ref row, "Estado", _active);

        var photoPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        photoPanel.Controls.Add(_photo);
        var pick = new Button { Text = "Foto...", Width = 80, Height = 28 };
        var clear = new Button { Text = "Quitar", Width = 80, Height = 28 };
        pick.Click += (_, _) => PickPhoto();
        clear.Click += (_, _) =>
        {
            _photoBytes = null;
            _photo.Image = ProductImages.Placeholder;
        };
        photoPanel.Controls.Add(pick);
        photoPanel.Controls.Add(clear);
        Add(grid, ref row, "Foto", photoPanel, 100);

        var flags = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };
        AddFlag(flags, UserPermission.Sell, "Vender (cobrar y carrito)");
        AddFlag(flags, UserPermission.ViewSales, "Ver ventas y devoluciones");
        AddFlag(flags, UserPermission.Products, "Productos, inventario y kárdex");
        AddFlag(flags, UserPermission.ImportProducts, "Importar CSV");
        AddFlag(flags, UserPermission.Cash, "Caja (movimientos, corte X y Z)");
        AddFlag(flags, UserPermission.Reports, "Reportes");
        AddFlag(flags, UserPermission.Settings, "Configuración (negocio y periféricos)");
        AddFlag(flags, UserPermission.ManageUsers, "Administrar usuarios");
        Add(grid, ref row, "Permisos", flags, 180);

        if (admin)
        {
            _username.ReadOnly = true;
            _password.Enabled = false;
            _confirm.Enabled = false;
            _active.Enabled = false;
            foreach (CheckBox box in _permissions.Values)
            {
                box.Checked = true;
                box.Enabled = false;
            }

            var note = new Label
            {
                Text = "El administrador conserva todos los permisos. La contraseña Admin123! se restaura al iniciar.",
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 36
            };
            Add(grid, ref row, "", note, 40);
        }
        else if (existing != null)
        {
            foreach ((UserPermission permission, CheckBox box) in _permissions)
                box.Checked = existing.Has(permission);
        }
        else
        {
            _permissions[UserPermission.Sell].Checked = true;
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44,
            Padding = new Padding(8)
        };
        var cancel = new Button { Text = "Cancelar", Width = 100, DialogResult = DialogResult.Cancel };
        var save = new Button { Text = "Guardar", Width = 100 };
        save.Click += (_, _) => SaveUser();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);

        Controls.Add(grid);
        Controls.Add(buttons);
        CancelButton = cancel;
        AcceptButton = save;
    }

    private void SaveUser()
    {
        if (!string.IsNullOrEmpty(_password.Text) && _password.Text != _confirm.Text)
        {
            MessageBox.Show("La confirmación de la contraseña no coincide.", "Empleado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        UserPermission permissions = UserPermission.None;
        foreach ((UserPermission permission, CheckBox box) in _permissions)
        {
            if (box.Checked)
                permissions |= permission;
        }

        var draft = new UserDraft
        {
            Id = _existing?.Id,
            Username = _username.Text,
            Password = _password.Text,
            FirstName = _firstName.Text,
            LastName = _lastName.Text,
            BirthDate = DateOnly.FromDateTime(_birth.Value),
            JobTitle = _job.Text,
            Phone = _phone.Text,
            Email = _email.Text,
            HireDate = _hasHire.Checked ? DateOnly.FromDateTime(_hire.Value) : null,
            Notes = _notes.Text,
            Photo = _photoBytes,
            IsActive = _active.Checked,
            Permissions = permissions
        };

        try
        {
            var database = new FerreSys.Infrastructure.Persistence.SqliteDbContext();
            var repository = new FerreSys.Infrastructure.Persistence.SqliteUserRepository(database);
            new SaveUserUseCase(repository).Execute(draft);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Empleado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PickPhoto()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "Foto del empleado"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _photoBytes = ProductImages.EncodeThumbnail(dialog.FileName);
            _photo.Image = ProductImages.ForGrid(_photoBytes);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Foto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AddFlag(FlowLayoutPanel panel, UserPermission permission, string text)
    {
        var box = new CheckBox { Text = text, AutoSize = true, Margin = new Padding(0, 2, 0, 2) };
        _permissions[permission] = box;
        panel.Controls.Add(box);
    }

    private static void Add(TableLayoutPanel grid, ref int row, string label, Control editor, int height = 32)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        grid.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        grid.Controls.Add(editor, 1, row);
        row++;
    }
}
