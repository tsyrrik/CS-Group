using FileDatabaseTask.Data;
using FileDatabaseTask.Models;
using FileDatabaseTask.Services;

namespace FileDatabaseTask;

public sealed class MainForm : Form
{
    private readonly ScanRepository repository;
    private readonly FileScanner scanner = new();
    private readonly ScanComparer comparer = new();

    private readonly TextBox pathTextBox = new();
    private readonly Button browseButton = new();
    private readonly Button scanButton = new();
    private readonly Button cancelButton = new();
    private readonly Button saveButton = new();
    private readonly ComboBox scansComboBox = new();
    private readonly Button refreshScansButton = new();
    private readonly Button loadScanButton = new();
    private readonly Button compareButton = new();
    private readonly DataGridView grid = new();
    private readonly Label statusLabel = new();
    private readonly BindingSource bindingSource = new();

    private List<FileSystemEntry> currentEntries = [];
    private string? currentRootPath;
    private CancellationTokenSource? operationCancellationTokenSource;

    public MainForm(ScanRepository repository)
    {
        this.repository = repository;

        Text = "База данных файлов";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(980, 600);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();

        Load += MainFormLoad;
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        layout.Controls.Add(BuildPathPanel(), 0, 0);
        layout.Controls.Add(BuildScanPanel(), 0, 1);
        layout.Controls.Add(BuildGrid(), 0, 2);
        layout.Controls.Add(BuildStatusPanel(), 0, 3);

        Controls.Add(layout);
    }

    private Control BuildPathPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));

        var pathLabel = new Label
        {
            Text = "Папка:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        pathTextBox.Dock = DockStyle.Fill;

        browseButton.Text = "Выбрать";
        browseButton.Dock = DockStyle.Fill;
        browseButton.Click += BrowseButtonClick;

        scanButton.Text = "Сканировать";
        scanButton.Dock = DockStyle.Fill;
        scanButton.Click += ScanButtonClick;

        cancelButton.Text = "Отмена";
        cancelButton.Dock = DockStyle.Fill;
        cancelButton.Enabled = false;
        cancelButton.Click += CancelButtonClick;

        saveButton.Text = "Сохранить";
        saveButton.Dock = DockStyle.Fill;
        saveButton.Click += SaveButtonClick;

        panel.Controls.Add(pathLabel, 0, 0);
        panel.Controls.Add(pathTextBox, 1, 0);
        panel.Controls.Add(browseButton, 2, 0);
        panel.Controls.Add(scanButton, 3, 0);
        panel.Controls.Add(cancelButton, 4, 0);
        panel.Controls.Add(saveButton, 5, 0);

        return panel;
    }

    private Control BuildScanPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));

        var scanLabel = new Label
        {
            Text = "Скан:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        scansComboBox.Dock = DockStyle.Fill;
        scansComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        scansComboBox.DisplayMember = nameof(ScanHeader.DisplayName);

        refreshScansButton.Text = "Обновить";
        refreshScansButton.Dock = DockStyle.Fill;
        refreshScansButton.Click += RefreshScansButtonClick;

        loadScanButton.Text = "Загрузить";
        loadScanButton.Dock = DockStyle.Fill;
        loadScanButton.Click += LoadScanButtonClick;

        compareButton.Text = "Сравнить";
        compareButton.Dock = DockStyle.Fill;
        compareButton.Click += CompareButtonClick;

        panel.Controls.Add(scanLabel, 0, 0);
        panel.Controls.Add(scansComboBox, 1, 0);
        panel.Controls.Add(refreshScansButton, 2, 0);
        panel.Controls.Add(loadScanButton, 3, 0);
        panel.Controls.Add(compareButton, 4, 0);

        return panel;
    }

    private Control BuildGrid()
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.DataSource = bindingSource;

        grid.Columns.Add(CreateTextColumn(nameof(FileGridRow.Status), "Статус", 140));
        grid.Columns.Add(CreateTextColumn(nameof(FileGridRow.Type), "Тип", 80));
        grid.Columns.Add(CreateTextColumn(nameof(FileGridRow.Name), "Имя", 220));
        grid.Columns.Add(CreateTextColumn(nameof(FileGridRow.RelativePath), "Относительный путь", 330));
        grid.Columns.Add(CreateTextColumn(nameof(FileGridRow.ParentRelativePath), "Родитель", 230));
        grid.Columns.Add(CreateTextColumn(nameof(FileGridRow.SavedSizeBytes), "Размер в БД", 120));
        grid.Columns.Add(CreateTextColumn(nameof(FileGridRow.CurrentSizeBytes), "Текущий размер", 120));
        grid.Columns.Add(CreateTextColumn(nameof(FileGridRow.FilesCount), "Файлов в папке", 120));

        return grid;
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string header, int width)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = header,
            Width = width,
            SortMode = DataGridViewColumnSortMode.Automatic
        };
    }

    private Control BuildStatusPanel()
    {
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.Text = "Готово";

        return statusLabel;
    }

    private async void MainFormLoad(object? sender, EventArgs eventArgs)
    {
        await RunUiActionAsync(async cancellationToken =>
        {
            await Task.Run(repository.EnsureCreated, cancellationToken);
            await LoadScanHeadersAsync(cancellationToken: cancellationToken);
            SetStatus("База данных готова.");
        });
    }

    private void BrowseButtonClick(object? sender, EventArgs eventArgs)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку для сканирования",
            UseDescriptionForTitle = true
        };

        if (Directory.Exists(pathTextBox.Text))
        {
            dialog.SelectedPath = pathTextBox.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            pathTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void ScanButtonClick(object? sender, EventArgs eventArgs)
    {
        await RunUiActionAsync(async cancellationToken =>
        {
            var rootPath = Path.GetFullPath(pathTextBox.Text.Trim());
            var progress = new Progress<int>(count => SetStatus($"Сканирование... найдено элементов: {count}."));
            var result = await Task.Run(() => scanner.Scan(rootPath, progress, cancellationToken), cancellationToken);

            currentEntries = result.Entries.ToList();
            currentRootPath = rootPath;

            DisplayEntries(currentEntries);
            SetScanStatus("Сканирование завершено", currentEntries.Count, result.Warnings);
        }, allowCancel: true);
    }

    private async void SaveButtonClick(object? sender, EventArgs eventArgs)
    {
        await RunUiActionAsync(async cancellationToken =>
        {
            if (currentEntries.Count == 0 || string.IsNullOrWhiteSpace(currentRootPath))
            {
                throw new InvalidOperationException("Сначала выполните сканирование папки.");
            }

            var scanId = await Task.Run(() => repository.SaveScan(currentRootPath, currentEntries), cancellationToken);
            await LoadScanHeadersAsync(scanId, cancellationToken);
            SetStatus($"Сканирование сохранено. ID: {scanId}.");
        });
    }

    private async void RefreshScansButtonClick(object? sender, EventArgs eventArgs)
    {
        await RunUiActionAsync(async cancellationToken =>
        {
            await LoadScanHeadersAsync(cancellationToken: cancellationToken);
            SetStatus("Список сохраненных сканирований обновлен.");
        });
    }

    private async void LoadScanButtonClick(object? sender, EventArgs eventArgs)
    {
        await RunUiActionAsync(async cancellationToken =>
        {
            var selectedScanId = GetSelectedScanId();
            var scan = await Task.Run(() => repository.LoadScan(selectedScanId), cancellationToken);

            pathTextBox.Text = scan.Header.RootPath;
            currentRootPath = scan.Header.RootPath;
            currentEntries = scan.Items.ToList();

            DisplayEntries(currentEntries);
            SetStatus($"Загружено сканирование ID {scan.Header.Id}. Элементов: {scan.Items.Count}.");
        });
    }

    private async void CompareButtonClick(object? sender, EventArgs eventArgs)
    {
        await RunUiActionAsync(async cancellationToken =>
        {
            var selectedScanId = GetSelectedScanId();
            var savedScan = await Task.Run(() => repository.LoadScan(selectedScanId), cancellationToken);

            if (!Directory.Exists(savedScan.Header.RootPath))
            {
                throw new DirectoryNotFoundException($"Папка не найдена: {savedScan.Header.RootPath}");
            }

            var progress = new Progress<int>(count => SetStatus($"Сравнение: сканирование текущего состояния... {count}."));
            var actualResult = await Task.Run(
                () => scanner.Scan(savedScan.Header.RootPath, progress, cancellationToken),
                cancellationToken);
            var rows = comparer.Compare(savedScan.Items, actualResult.Entries);

            bindingSource.DataSource = rows;

            var newCount = rows.Count(row => row.Status == CompareStatusLabels.New);
            var deletedCount = rows.Count(row => row.Status == CompareStatusLabels.Deleted);
            var changedCount = rows.Count(row =>
                row.Status is CompareStatusLabels.SizeChanged or CompareStatusLabels.DirectoryAggregateChanged);

            SetStatus(
                $"Сравнение завершено. Новых: {newCount}, удаленных: {deletedCount}, измененных: {changedCount}, предупреждений: {actualResult.Warnings.Count}.");
            ShowWarnings(actualResult.Warnings);
        }, allowCancel: true);
    }

    private void CancelButtonClick(object? sender, EventArgs eventArgs)
    {
        operationCancellationTokenSource?.Cancel();
        SetStatus("Операция отменяется...");
    }

    private void DisplayEntries(IEnumerable<FileSystemEntry> entries)
    {
        bindingSource.DataSource = entries
            .Select(entry => FileGridRow.FromEntry(entry))
            .ToList();
    }

    private async Task LoadScanHeadersAsync(int? selectedScanId = null, CancellationToken cancellationToken = default)
    {
        var scans = await Task.Run(repository.GetScans, cancellationToken);

        scansComboBox.DataSource = scans;
        if (selectedScanId is null)
        {
            return;
        }

        for (var index = 0; index < scans.Count; index++)
        {
            if (scans[index].Id == selectedScanId.Value)
            {
                scansComboBox.SelectedIndex = index;
                return;
            }
        }
    }

    private int GetSelectedScanId()
    {
        if (scansComboBox.SelectedItem is not ScanHeader selectedScan)
        {
            throw new InvalidOperationException("Выберите сохраненное сканирование.");
        }

        return selectedScan.Id;
    }

    private async Task RunUiActionAsync(Func<CancellationToken, Task> action, bool allowCancel = false)
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        try
        {
            operationCancellationTokenSource = cancellationTokenSource;
            ToggleButtons(false, allowCancel);
            Cursor = Cursors.WaitCursor;
            await action(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Операция отменена.");
        }
        catch (Exception exception)
        {
            SetStatus($"Ошибка: {exception.Message}");
            MessageBox.Show(this, exception.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            operationCancellationTokenSource = null;
            Cursor = Cursors.Default;
            ToggleButtons(true);
        }
    }

    private void ToggleButtons(bool enabled, bool allowCancel = false)
    {
        browseButton.Enabled = enabled;
        scanButton.Enabled = enabled;
        saveButton.Enabled = enabled;
        refreshScansButton.Enabled = enabled;
        loadScanButton.Enabled = enabled;
        compareButton.Enabled = enabled;
        cancelButton.Enabled = !enabled && allowCancel;
    }

    private void SetStatus(string message)
    {
        statusLabel.Text = message;
    }

    private void SetScanStatus(string prefix, int entriesCount, IReadOnlyList<string> warnings)
    {
        SetStatus($"{prefix}. Элементов: {entriesCount}, предупреждений: {warnings.Count}.");
        ShowWarnings(warnings);
    }

    private void ShowWarnings(IReadOnlyList<string> warnings)
    {
        if (warnings.Count == 0)
        {
            return;
        }

        var visibleWarnings = warnings.Take(10).ToList();
        var message = string.Join(Environment.NewLine, visibleWarnings);
        if (warnings.Count > visibleWarnings.Count)
        {
            message += $"{Environment.NewLine}...и еще {warnings.Count - visibleWarnings.Count}.";
        }

        MessageBox.Show(this, message, "Предупреждения сканирования", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
