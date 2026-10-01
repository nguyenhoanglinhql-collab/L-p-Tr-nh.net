using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace phần3
{
    public partial class Form1 : Form
    {
        private readonly BindingList<Product> _products = [];
        private readonly BindingList<Product> _visibleProducts = [];
        private readonly BindingList<ProductCategory> _categories =
        [
            new() { Id = "phone", Name = "Điện thoại" },
            new() { Id = "laptop", Name = "Laptop" },
            new() { Id = "accessory", Name = "Phụ kiện" }
        ];
        private BindingSource _productBindingSource = null!;
        private ErrorProvider _errorProvider = null!;
        private TextBox txtProductId = null!;
        private TextBox txtProductName = null!;
        private TextBox txtUnitPrice = null!;
        private TextBox txtQuantity = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboCategory = null!;
        private PictureBox picAvatar = null!;
        private Button btnChooseImage = null!;
        private Button btnAdd = null!;
        private Button btnUpdate = null!;
        private Button btnDelete = null!;
        private DataGridView dgvProducts = null!;
        private ToolStripStatusLabel statusLabel = null!;
        private string? _selectedImagePath;
        private Product? _selectedProduct;
        private bool _isLoadingProduct;

        public Form1()
        {
            InitializeComponent();
            InitializeProductManager();
        }

        private void InitializeProductManager()
        {
            Text = "TechMart Product Manager";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 480);
            ClientSize = new Size(1000, 520);
            Font = new Font("Segoe UI", 9F);

            _errorProvider = new ErrorProvider(components)
            {
                ContainerControl = this,
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };
            _productBindingSource = new BindingSource(components)
            {
                DataSource = _visibleProducts
            };

            BuildInterface();
            ConfigureProductGrid();
            WireProductEvents();
            RefreshVisibleProducts();
        }

        private void WireProductEvents()
        {
            btnAdd.Click += (_, _) => AddProduct();
            btnUpdate.Click += (_, _) => UpdateProduct();
            btnDelete.Click += (_, _) => DeleteProduct();
            btnChooseImage.Click += (_, _) => ChooseImage();
            txtSearch.TextChanged += (_, _) => RefreshVisibleProducts();
            dgvProducts.SelectionChanged += (_, _) => LoadSelectedProduct();
        }

        private void AddProduct()
        {
            if (!TryReadEditor(out var name, out var categoryId, out var unitPrice, out var quantity))
            {
                return;
            }

            var product = new Product
            {
                ProductId = _products.Count == 0 ? 1 : _products.Max(item => item.ProductId) + 1,
                ProductName = name,
                Category = categoryId,
                UnitPrice = unitPrice,
                Quantity = quantity,
                ImagePath = _selectedImagePath
            };
            _products.Add(product);
            RefreshVisibleProducts();
            ClearEditor();
        }

        private void UpdateProduct()
        {
            if (_selectedProduct is null)
            {
                MessageBox.Show(this, "Vui lòng chọn sản phẩm cần cập nhật.", "TechMart", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryReadEditor(out var name, out var categoryId, out var unitPrice, out var quantity))
            {
                return;
            }

            _selectedProduct.ProductName = name;
            _selectedProduct.Category = categoryId;
            _selectedProduct.UnitPrice = unitPrice;
            _selectedProduct.Quantity = quantity;
            _selectedProduct.ImagePath = _selectedImagePath;
            RefreshVisibleProducts();
        }

        private void DeleteProduct()
        {
            if (_selectedProduct is null)
            {
                MessageBox.Show(this, "Vui lòng chọn sản phẩm cần xóa.", "TechMart", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                this,
                $"Bạn có chắc muốn xóa sản phẩm '{_selectedProduct.ProductName}' không?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            _products.Remove(_selectedProduct);
            _selectedProduct = null;
            RefreshVisibleProducts();
            ClearEditor();
        }

        private bool TryReadEditor(out string name, out string categoryId, out decimal unitPrice, out int quantity)
        {
            name = txtProductName.Text.Trim();
            categoryId = cboCategory.SelectedValue as string ?? string.Empty;
            unitPrice = 0;
            quantity = 0;
            _errorProvider.Clear();
            var isValid = true;

            if (name.Length == 0)
            {
                _errorProvider.SetError(txtProductName, "Vui lòng nhập tên sản phẩm.");
                isValid = false;
            }

            if (!decimal.TryParse(txtUnitPrice.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out unitPrice) || unitPrice <= 0)
            {
                _errorProvider.SetError(txtUnitPrice, "Đơn giá phải là số lớn hơn 0.");
                isValid = false;
            }

            if (!int.TryParse(txtQuantity.Text.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out quantity) || quantity < 0)
            {
                _errorProvider.SetError(txtQuantity, "Số lượng phải là số nguyên lớn hơn hoặc bằng 0.");
                isValid = false;
            }

            if (categoryId.Length == 0)
            {
                _errorProvider.SetError(cboCategory, "Vui lòng chọn danh mục.");
                isValid = false;
            }

            return isValid;
        }

        private void RefreshVisibleProducts()
        {
            var searchText = txtSearch?.Text.Trim() ?? string.Empty;
            var selectedProduct = _selectedProduct;
            var matches = _products
                .Where(product => product.ProductName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            _isLoadingProduct = true;
            _visibleProducts.RaiseListChangedEvents = false;
            _visibleProducts.Clear();
            foreach (var product in matches)
            {
                _visibleProducts.Add(product);
            }
            _visibleProducts.RaiseListChangedEvents = true;
            _productBindingSource.ResetBindings(false);

            var selectedIndex = selectedProduct is null ? -1 : _visibleProducts.IndexOf(selectedProduct);
            if (selectedIndex >= 0)
            {
                dgvProducts.ClearSelection();
                dgvProducts.Rows[selectedIndex].Selected = true;
                dgvProducts.CurrentCell = dgvProducts.Rows[selectedIndex].Cells[0];
            }
            else
            {
                dgvProducts.ClearSelection();
                dgvProducts.CurrentCell = null;
                if (selectedProduct is not null)
                {
                    _selectedProduct = null;
                    ClearEditor();
                }
            }

            _isLoadingProduct = false;
            statusLabel.Text = $"Tổng số sản phẩm: {_products.Count}";
        }

        private void LoadSelectedProduct()
        {
            if (_isLoadingProduct || dgvProducts.CurrentRow?.DataBoundItem is not Product product)
            {
                return;
            }

            _selectedProduct = product;
            _isLoadingProduct = true;
            txtProductId.Text = product.ProductId.ToString(CultureInfo.CurrentCulture);
            txtProductName.Text = product.ProductName;
            cboCategory.SelectedValue = product.Category;
            txtUnitPrice.Text = product.UnitPrice.ToString("N0", CultureInfo.CurrentCulture);
            txtQuantity.Text = product.Quantity.ToString(CultureInfo.CurrentCulture);
            _selectedImagePath = product.ImagePath;
            DisplayProductImage(product.ImagePath);
            _errorProvider.Clear();
            _isLoadingProduct = false;
        }

        private void ClearEditor()
        {
            _selectedProduct = null;
            _isLoadingProduct = true;
            txtProductId.Clear();
            txtProductName.Clear();
            if (cboCategory.Items.Count > 0)
            {
                cboCategory.SelectedIndex = 0;
            }
            txtUnitPrice.Clear();
            txtQuantity.Clear();
            _selectedImagePath = null;
            ReplaceAvatarImage(null);
            _errorProvider.Clear();
            dgvProducts.ClearSelection();
            _isLoadingProduct = false;
        }

        private void ChooseImage()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Chọn ảnh sản phẩm",
                Filter = "Tệp ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Tất cả tệp|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                ReplaceAvatarImage(ReadImageCopy(dialog.FileName));
                _selectedImagePath = dialog.FileName;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException)
            {
                MessageBox.Show(this, "Không thể mở tệp ảnh đã chọn.", "Lỗi ảnh", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayProductImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                ReplaceAvatarImage(null);
                return;
            }

            try
            {
                ReplaceAvatarImage(ReadImageCopy(imagePath));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException)
            {
                ReplaceAvatarImage(null);
            }
        }

        private static Image ReadImageCopy(string imagePath)
        {
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }

        private void ReplaceAvatarImage(Image? image)
        {
            var previousImage = picAvatar.Image;
            picAvatar.Image = image;
            previousImage?.Dispose();
        }

        private void ExportToCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Xuất danh sách sản phẩm",
                Filter = "CSV (*.csv)|*.csv",
                DefaultExt = "csv",
                AddExtension = true,
                FileName = "TechMart_Products.csv",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var rows = new List<string>
                {
                    CsvRow("Mã SP", "Tên SP", "Danh mục", "Đơn giá", "Số lượng")
                };
                rows.AddRange(_products.Select(product => CsvRow(
                    product.ProductId.ToString(CultureInfo.InvariantCulture),
                    product.ProductName,
                    _categories.FirstOrDefault(category => category.Id == product.Category)?.Name ?? product.Category,
                    product.UnitPrice.ToString(CultureInfo.InvariantCulture),
                    product.Quantity.ToString(CultureInfo.InvariantCulture))));
                File.WriteAllLines(dialog.FileName, rows, new UTF8Encoding(true));
                MessageBox.Show(this, "Đã xuất danh sách sản phẩm thành công.", "TechMart", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                MessageBox.Show(this, $"Không thể xuất tệp CSV: {exception.Message}", "Lỗi xuất tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string CsvRow(params string[] values) =>
            string.Join(",", values.Select(value => $"\"{value.Replace("\"", "\"\"")}\""));

        private void BuildInterface()
        {
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var exportMenuItem = new ToolStripMenuItem("Export CSV") { ShortcutKeys = Keys.Control | Keys.E };
            var exitMenuItem = new ToolStripMenuItem("Exit") { ShortcutKeys = Keys.Control | Keys.X };
            exportMenuItem.Click += (_, _) => ExportToCsv();
            exitMenuItem.Click += (_, _) => Close();
            fileMenu.DropDownItems.AddRange([exportMenuItem, new ToolStripSeparator(), exitMenuItem]);
            menuStrip.Items.Add(fileMenu);
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);

            var statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Tổng số sản phẩm: 0") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            statusStrip.Items.Add(statusLabel);
            Controls.Add(statusStrip);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8, 6, 8, 6),
                BackColor = SystemColors.Control
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(mainLayout);
            mainLayout.BringToFront();
            menuStrip.BringToFront();
            statusStrip.BringToFront();

            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0, 8, 8, 4)
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(leftLayout, 0, 0);

            var inputLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(0)
            };
            inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29F));
            inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 71F));
            for (var row = 0; row < 5; row++)
            {
                inputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            }
            leftLayout.Controls.Add(inputLayout, 0, 0);

            txtProductId = CreateTextBox();
            txtProductId.ReadOnly = true;
            txtProductName = CreateTextBox();
            cboCategory = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = _categories,
                DisplayMember = nameof(ProductCategory.Name),
                ValueMember = nameof(ProductCategory.Id)
            };
            txtUnitPrice = CreateTextBox();
            txtUnitPrice.TextAlign = HorizontalAlignment.Right;
            txtQuantity = CreateTextBox();
            txtQuantity.TextAlign = HorizontalAlignment.Right;

            AddInputRow(inputLayout, 0, "Mã SP:", txtProductId);
            AddInputRow(inputLayout, 1, "Tên SP:", txtProductName);
            AddInputRow(inputLayout, 2, "Danh mục:", cboCategory);
            AddInputRow(inputLayout, 3, "Đơn giá:", txtUnitPrice);
            AddInputRow(inputLayout, 4, "Số lượng:", txtQuantity);

            var lowerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 6, 0, 0)
            };
            lowerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            lowerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            lowerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftLayout.Controls.Add(lowerLayout, 0, 1);

            picAvatar = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 8, 0)
            };
            lowerLayout.Controls.Add(picAvatar, 0, 0);

            var actionPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0, 0, 0, 0)
            };
            for (var row = 0; row < 4; row++)
            {
                actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            }
            btnChooseImage = new Button { Text = "Chọn ảnh", Dock = DockStyle.Fill, Margin = new Padding(4) };
            btnAdd = new Button { Text = "Add", Dock = DockStyle.Fill, Margin = new Padding(4) };
            btnDelete = new Button { Text = "Delete", Dock = DockStyle.Fill, Margin = new Padding(4) };
            btnUpdate = new Button { Text = "Update", Dock = DockStyle.Fill, Margin = new Padding(4) };
            actionPanel.Controls.Add(btnChooseImage, 0, 0);
            actionPanel.Controls.Add(btnAdd, 0, 1);
            actionPanel.Controls.Add(btnDelete, 0, 2);
            actionPanel.Controls.Add(btnUpdate, 0, 3);
            lowerLayout.Controls.Add(actionPanel, 1, 0);

            var dataLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10, 8, 0, 4)
            };
            dataLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            dataLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(dataLayout, 1, 0);

            txtSearch = CreateTextBox();
            txtSearch.PlaceholderText = "Tìm kiếm";
            dataLayout.Controls.Add(txtSearch, 0, 0);

            dgvProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            dataLayout.Controls.Add(dgvProducts, 0, 1);
        }

        private void ConfigureProductGrid()
        {
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colProductId",
                HeaderText = "Mã SP",
                DataPropertyName = nameof(Product.ProductId),
                FillWeight = 55
            });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colProductName",
                HeaderText = "Tên SP",
                DataPropertyName = nameof(Product.ProductName),
                FillWeight = 145
            });
            dgvProducts.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "colCategory",
                HeaderText = "Danh mục",
                DataPropertyName = nameof(Product.Category),
                DataSource = _categories,
                DisplayMember = nameof(ProductCategory.Name),
                ValueMember = nameof(ProductCategory.Id),
                DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing,
                FillWeight = 90
            });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colUnitPrice",
                HeaderText = "Đơn giá",
                DataPropertyName = nameof(Product.UnitPrice),
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight },
                FillWeight = 85
            });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colQuantity",
                HeaderText = "Số lượng",
                DataPropertyName = nameof(Product.Quantity),
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
                FillWeight = 70
            });
            dgvProducts.DataSource = _productBindingSource;
        }

        private static TextBox CreateTextBox() => new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 6, 3, 6)
        };

        private static void AddInputRow(TableLayoutPanel layout, int row, string labelText, Control input)
        {
            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(3)
            };
            input.Margin = new Padding(3, 6, 3, 6);
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(input, 1, row);
        }
    }
}
