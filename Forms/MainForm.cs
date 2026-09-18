using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DemoExam.Api;
using DemoExam.Data;

namespace DemoExam.Forms
{
    public sealed class MainForm : Form
    {
        private readonly TabControl _tabs = new TabControl();
        private readonly DataGridView _customersGrid = CreateGrid();
        private readonly DataGridView _catalogGrid = CreateGrid();
        private readonly DataGridView _ordersGrid = CreateGrid();
        private readonly DataGridView _costGrid = CreateGrid();

        private readonly TextBox _name = new TextBox();
        private readonly TextBox _inn = new TextBox();
        private readonly TextBox _address = new TextBox();
        private readonly TextBox _phone = new TextBox();
        private readonly ComboBox _type = new ComboBox();
        private readonly TextBox _orderId = new TextBox();
        private readonly Label _costLabel = new Label();
        private readonly Label _apiStatus = new Label();
        private readonly ApiServer _api = new ApiServer();

        private int? _selectedCustomerId;

        public MainForm()
        {
            Text = "Демоэкзамен";
            Width = 1180;
            Height = 760;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F);

            _tabs.Dock = DockStyle.Fill;
            _tabs.TabPages.Add(BuildCustomersTab());
            _tabs.TabPages.Add(BuildCatalogTab());
            _tabs.TabPages.Add(BuildOrdersTab());
            _tabs.TabPages.Add(BuildCostTab());
            _tabs.TabPages.Add(BuildApiTab());
            Controls.Add(_tabs);

            Shown += delegate
            {
                try
                {
                    LoadCustomers();
                    LoadCatalog();
                    LoadOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Не удалось подключиться к БД.\n\n" + ex.Message +
                        "\n\nПроверь App.config и сначала выполни SQL-скрипты из папки Database.",
                        "Подключение к БД", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            FormClosing += delegate { _api.Dispose(); };
        }

        private TabPage BuildCustomersTab()
        {
            var page = new TabPage("Заказчики");
            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 760 };
            split.Panel1.Controls.Add(_customersGrid);

            var form = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(12)
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddField(form, 0, "Название", _name);
            AddField(form, 1, "ИНН", _inn);
            AddField(form, 2, "Адрес", _address);
            AddField(form, 3, "Телефон", _phone);

            _type.DropDownStyle = ComboBoxStyle.DropDownList;
            _type.Items.AddRange(new object[] { "Покупатель", "Поставщик" });
            _type.SelectedIndex = 0;
            AddField(form, 4, "Тип", _type);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12) };
            buttons.Controls.Add(MakeButton("Добавить", AddCustomer));
            buttons.Controls.Add(MakeButton("Изменить", UpdateCustomer));
            buttons.Controls.Add(MakeButton("Удалить", DeleteCustomer));
            buttons.Controls.Add(MakeButton("Очистить", delegate { ClearCustomerEditor(); }));
            buttons.Controls.Add(MakeButton("Импорт JSON", ImportJson));

            var right = new Panel { Dock = DockStyle.Fill };
            right.Controls.Add(buttons);
            right.Controls.Add(form);
            buttons.Top = form.Bottom + 5;
            split.Panel2.Controls.Add(right);

            _customersGrid.SelectionChanged += delegate { FillCustomerEditorFromGrid(); };
            page.Controls.Add(split);
            return page;
        }

        private TabPage BuildCatalogTab()
        {
            var page = new TabPage("Номенклатура и цены");
            page.Controls.Add(_catalogGrid);
            return page;
        }

        private TabPage BuildOrdersTab()
        {
            var page = new TabPage("Заказы покупателей");
            page.Controls.Add(_ordersGrid);
            return page;
        }

        private TabPage BuildCostTab()
        {
            var page = new TabPage("Расчет себестоимости");
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(10) };
            top.Controls.Add(new Label { Text = "ID заказа:", AutoSize = true, Margin = new Padding(3, 9, 3, 3) });
            _orderId.Width = 80;
            _orderId.Text = "1";
            top.Controls.Add(_orderId);
            top.Controls.Add(MakeButton("Рассчитать", CalculateCost));
            _costLabel.AutoSize = true;
            _costLabel.Font = new Font(Font, FontStyle.Bold);
            _costLabel.Margin = new Padding(20, 9, 3, 3);
            top.Controls.Add(_costLabel);

            page.Controls.Add(_costGrid);
            page.Controls.Add(top);
            _costGrid.Dock = DockStyle.Fill;
            top.BringToFront();
            return page;
        }

        private TabPage BuildApiTab()
        {
            var page = new TabPage("API");
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(18)
            };

            var title = new Label
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, 14F, FontStyle.Bold),
                Text = "Встроенный HTTP API на HttpListener (.NET 8)"
            };
            panel.Controls.Add(title);
            panel.Controls.Add(new Label { AutoSize = true, Text = "Базовый адрес: http://localhost:8080/" });
            panel.Controls.Add(new Label { AutoSize = true, Text = "GET /api/health" });
            panel.Controls.Add(new Label { AutoSize = true, Text = "GET /api/customers" });
            panel.Controls.Add(new Label { AutoSize = true, Text = "GET /api/order-cost?orderId=1" });

            var row = new FlowLayoutPanel { AutoSize = true };
            row.Controls.Add(MakeButton("Запустить API", StartApi));
            row.Controls.Add(MakeButton("Остановить API", StopApi));
            _apiStatus.AutoSize = true;
            _apiStatus.Margin = new Padding(15, 9, 3, 3);
            _apiStatus.Text = "Остановлен";
            row.Controls.Add(_apiStatus);
            panel.Controls.Add(row);

            panel.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1000, 0),
                Text = "Важно: правило обработки данных в задании 5 должно быть взято из Приложения 2. " +
                       "В имеющихся материалах его нет, поэтому сейчас /api/customers демонстрационно скрывает ИНН, адрес и телефон."
            });

            page.Controls.Add(panel);
            return page;
        }

        private static DataGridView CreateGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White
            };
        }

        private static Button MakeButton(string text, EventHandler click)
        {
            var button = new Button { Text = text, AutoSize = true, Height = 32 };
            button.Click += click;
            return button;
        }

        private static void AddField(TableLayoutPanel panel, int row, string caption, Control control)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) }, 0, row);
            control.Dock = DockStyle.Top;
            panel.Controls.Add(control, 1, row);
        }

        private void LoadCustomers()
        {
            _customersGrid.DataSource = Database.Query(@"
SELECT CounterpartyId AS [ID], ExternalId AS [Внешний код], Name AS [Название],
       Inn AS [ИНН], Address AS [Адрес], Phone AS [Телефон], CounterpartyType AS [Тип]
FROM Counterparties
ORDER BY Name;");
        }

        private void LoadCatalog()
        {
            _catalogGrid.DataSource = Database.Query(@"
SELECT n.NomenclatureId AS [ID], n.Code AS [Код], n.Name AS [Номенклатура],
       n.NomenclatureType AS [Тип], u.Symbol AS [Ед. изм.],
       p.Price AS [Текущая цена]
FROM Nomenclature n
JOIN Units u ON u.UnitId = n.UnitId
OUTER APPLY
(
    SELECT TOP 1 Price
    FROM Prices p0
    WHERE p0.NomenclatureId = n.NomenclatureId
    ORDER BY p0.EffectiveFrom DESC, p0.PriceId DESC
) p
ORDER BY n.NomenclatureType, n.Name;");
        }

        private void LoadOrders()
        {
            _ordersGrid.DataSource = Database.Query(@"
SELECT o.OrderId AS [ID], o.OrderNumber AS [Номер], o.OrderDate AS [Дата],
       c.Name AS [Заказчик],
       SUM(ol.Quantity * ol.UnitSalePrice - ol.DiscountAmount) AS [Сумма продажи]
FROM SalesOrders o
JOIN Counterparties c ON c.CounterpartyId = o.CustomerId
JOIN SalesOrderLines ol ON ol.OrderId = o.OrderId
GROUP BY o.OrderId, o.OrderNumber, o.OrderDate, c.Name
ORDER BY o.OrderDate DESC;");
        }

        private void AddCustomer(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                MessageBox.Show("Введите название.");
                return;
            }

            Database.Execute(@"
INSERT INTO Counterparties(Name, Inn, Address, Phone, CounterpartyType)
VALUES(@Name, NULLIF(@Inn, ''), @Address, @Phone, @Type);",
                new SqlParameter("@Name", _name.Text.Trim()),
                new SqlParameter("@Inn", _inn.Text.Trim()),
                new SqlParameter("@Address", _address.Text.Trim()),
                new SqlParameter("@Phone", _phone.Text.Trim()),
                new SqlParameter("@Type", Convert.ToString(_type.SelectedItem)));

            LoadCustomers();
            ClearCustomerEditor();
        }

        private void UpdateCustomer(object sender, EventArgs e)
        {
            if (!_selectedCustomerId.HasValue)
            {
                MessageBox.Show("Выберите строку.");
                return;
            }

            Database.Execute(@"
UPDATE Counterparties
SET Name=@Name, Inn=NULLIF(@Inn,''), Address=@Address, Phone=@Phone, CounterpartyType=@Type
WHERE CounterpartyId=@Id;",
                new SqlParameter("@Name", _name.Text.Trim()),
                new SqlParameter("@Inn", _inn.Text.Trim()),
                new SqlParameter("@Address", _address.Text.Trim()),
                new SqlParameter("@Phone", _phone.Text.Trim()),
                new SqlParameter("@Type", Convert.ToString(_type.SelectedItem)),
                new SqlParameter("@Id", _selectedCustomerId.Value));

            LoadCustomers();
        }

        private void DeleteCustomer(object sender, EventArgs e)
        {
            if (!_selectedCustomerId.HasValue)
            {
                MessageBox.Show("Выберите строку.");
                return;
            }

            if (MessageBox.Show("Удалить выбранного контрагента?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Database.Execute("DELETE FROM Counterparties WHERE CounterpartyId=@Id;",
                    new SqlParameter("@Id", _selectedCustomerId.Value));
                LoadCustomers();
                ClearCustomerEditor();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Удаление запрещено, если контрагент уже используется в заказе.\n\n" + ex.Message);
            }
        }

        private void FillCustomerEditorFromGrid()
        {
            if (_customersGrid.CurrentRow == null) return;
            var row = _customersGrid.CurrentRow;
            if (row.Cells["ID"].Value == null) return;

            _selectedCustomerId = Convert.ToInt32(row.Cells["ID"].Value);
            _name.Text = Convert.ToString(row.Cells["Название"].Value);
            _inn.Text = Convert.ToString(row.Cells["ИНН"].Value);
            _address.Text = Convert.ToString(row.Cells["Адрес"].Value);
            _phone.Text = Convert.ToString(row.Cells["Телефон"].Value);
            var type = Convert.ToString(row.Cells["Тип"].Value);
            if (_type.Items.Contains(type)) _type.SelectedItem = type;
        }

        private void ClearCustomerEditor()
        {
            _selectedCustomerId = null;
            _name.Clear();
            _inn.Clear();
            _address.Clear();
            _phone.Clear();
            _type.SelectedIndex = 0;
            _customersGrid.ClearSelection();
        }

        private void ImportJson(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog { Filter = "JSON (*.json)|*.json|Все файлы|*.*" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var count = JsonImporter.ImportCustomers(dialog.FileName);
                    LoadCustomers();
                    MessageBox.Show("Импортировано/обновлено записей: " + count);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка импорта:\n" + ex.Message);
                }
            }
        }

        private void CalculateCost(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(_orderId.Text, out id))
            {
                MessageBox.Show("Введите числовой ID заказа.");
                return;
            }

            _costGrid.DataSource = Database.Query(@"
SELECT OrderId AS [Заказ], CustomerName AS [Заказчик],
       MaterialCost AS [Материалы], OperationCost AS [Операции], TotalCost AS [Полная себестоимость]
FROM dbo.v_OrderProductionCost
WHERE OrderId=@Id;", new SqlParameter("@Id", id));

            if (_costGrid.Rows.Count > 0 && _costGrid.Rows[0].Cells["Полная себестоимость"].Value != null)
                _costLabel.Text = "Итого: " + Convert.ToDecimal(_costGrid.Rows[0].Cells["Полная себестоимость"].Value).ToString("N2") + " руб.";
            else
                _costLabel.Text = "Заказ не найден";
        }

        private void StartApi(object sender, EventArgs e)
        {
            try
            {
                _api.Start("http://localhost:8080/");
                _apiStatus.Text = "Работает: http://localhost:8080/";
                _apiStatus.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось запустить API.\n" + ex.Message +
                                "\n\nЕсли Access denied: запусти Visual Studio от администратора или выполни команду netsh из README.");
            }
        }

        private void StopApi(object sender, EventArgs e)
        {
            _api.Stop();
            _apiStatus.Text = "Остановлен";
            _apiStatus.ForeColor = SystemColors.ControlText;
        }
    }
}
