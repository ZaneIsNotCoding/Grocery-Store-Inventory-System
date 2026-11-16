Imports System.IO
Imports System.Drawing
Imports System.Data
Imports System.Security.Cryptography
Imports System.Text.Json
Imports System.Windows.Forms
Imports MySql.Data.MySqlClient

Friend Module Program
    <STAThread>
    Public Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Try
            Db.Initialize()
            Application.Run(New LoginForm())
        Catch ex As Exception
            MessageBox.Show("Could not connect to MySQL. Check appsettings.local.json and confirm XAMPP MySQL is running." & Environment.NewLine & ex.Message, "Database connection", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Module

Friend Module Db
    Private _connectionString As String
    Public Sub Initialize()
        Dim configPath = System.IO.Path.Combine(AppContext.BaseDirectory, "appsettings.local.json")
        If Not File.Exists(configPath) Then Throw New FileNotFoundException("Create appsettings.local.json from the project copy and enter your MySQL credentials.", configPath)
        Using doc = JsonDocument.Parse(File.ReadAllText(configPath))
            _connectionString = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("Inventory").GetString()
        End Using
        Using c = OpenConnection()
        End Using
    End Sub
    Public Function OpenConnection() As MySqlConnection
        Dim c As New MySqlConnection(_connectionString)
        c.Open()
        Return c
    End Function
    Public Function Query(sql As String, ParamArray args As Object()) As DataTable
        Using c = OpenConnection(), cmd As New MySqlCommand(sql, c)
            Bind(cmd, args)
            Using reader = cmd.ExecuteReader()
                Dim t As New DataTable()
                t.Load(reader)
                Return t
            End Using
        End Using
    End Function
    Public Function Execute(sql As String, ParamArray args As Object()) As Integer
        Using c = OpenConnection(), cmd As New MySqlCommand(sql, c)
            Bind(cmd, args)
            Return cmd.ExecuteNonQuery()
        End Using
    End Function
    Public Function Scalar(sql As String, ParamArray args As Object()) As Object
        Using c = OpenConnection(), cmd As New MySqlCommand(sql, c)
            Bind(cmd, args)
            Return cmd.ExecuteScalar()
        End Using
    End Function
    Private Sub Bind(cmd As MySqlCommand, args As Object())
        For i = 0 To args.Length - 1
            cmd.Parameters.AddWithValue("@p" & i, If(args(i), DBNull.Value))
        Next
    End Sub
End Module

Friend Module Passwords
    Public Function Hash(password As String) As String
        Dim salt(15) As Byte
        RandomNumberGenerator.Fill(salt)
        Dim hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210000, HashAlgorithmName.SHA256, 32)
        Return "pbkdf2-sha256$210000$" & Convert.ToBase64String(salt) & "$" & Convert.ToBase64String(hashBytes)
    End Function
    Public Function Verify(password As String, stored As String) As Boolean
        Try
            Dim bits = stored.Split("$"c)
            If bits.Length <> 4 OrElse bits(0) <> "pbkdf2-sha256" Then Return False
            Dim salt = Convert.FromBase64String(bits(2)), expected = Convert.FromBase64String(bits(3))
            Dim actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Integer.Parse(bits(1)), HashAlgorithmName.SHA256, expected.Length)
            Return CryptographicOperations.FixedTimeEquals(actual, expected)
        Catch
            Return False
        End Try
    End Function
End Module

Friend Module UiTheme
    Public ReadOnly Forest As Color = Color.FromArgb(48, 111, 78)
    Public ReadOnly ForestDark As Color = Color.FromArgb(35, 82, 58)
    Public ReadOnly Canvas As Color = Color.FromArgb(245, 247, 244)
    Public ReadOnly Ink As Color = Color.FromArgb(39, 49, 43)
    Public ReadOnly Muted As Color = Color.FromArgb(111, 122, 114)
    Public ReadOnly Line As Color = Color.FromArgb(224, 230, 225)
    Public Sub StyleGrid(grid As DataGridView)
        grid.BackgroundColor = Color.White
        grid.BorderStyle = BorderStyle.None
        grid.EnableHeadersVisualStyles = False
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(239, 244, 239)
        grid.ColumnHeadersDefaultCellStyle.ForeColor = ForestDark
        grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        grid.ColumnHeadersHeight = 38
        grid.DefaultCellStyle.BackColor = Color.White
        grid.DefaultCellStyle.ForeColor = Ink
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(222, 239, 226)
        grid.DefaultCellStyle.SelectionForeColor = Ink
        grid.DefaultCellStyle.Padding = New Padding(6, 3, 6, 3)
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 249)
        grid.RowTemplate.Height = 34
        grid.GridColor = Line
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        grid.RowHeadersVisible = False
    End Sub
    Public Sub StyleButton(button As Button, Optional primary As Boolean = False)
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = If(primary, 0, 1)
        button.FlatAppearance.BorderColor = Line
        button.BackColor = If(primary, Forest, Color.White)
        button.ForeColor = If(primary, Color.White, Ink)
        button.Font = New Font("Segoe UI", 9, If(primary, FontStyle.Bold, FontStyle.Regular))
        button.Padding = New Padding(10, 5, 10, 5)
        button.Cursor = Cursors.Hand
        button.Margin = New Padding(4, 3, 4, 3)
    End Sub
End Module

Friend Class LoginForm
    Inherits Form
    Private ReadOnly userBox As New TextBox() With {.PlaceholderText = "Username", .Width = 240}
    Private ReadOnly passBox As New TextBox() With {.PlaceholderText = "Password", .UseSystemPasswordChar = True, .Width = 240}
    Private ReadOnly createButton As New Button() With {.Text = "Create first account", .AutoSize = True}
    Public Sub New()
        BackColor = UiTheme.Canvas
        Font = New Font("Segoe UI", 9)
        Text = "Grocery Inventory · Sign in"
        StartPosition = FormStartPosition.CenterScreen
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        ClientSize = New Size(420, 300)
        Dim panel As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .Padding = New Padding(55, 34, 20, 10), .WrapContents = False, .BackColor = Color.White}
        panel.Controls.Add(New Label() With {.Text = "Grocery Store Inventory", .Font = New Font("Segoe UI", 18, FontStyle.Bold), .ForeColor = UiTheme.ForestDark, .AutoSize = True, .Margin = New Padding(0, 0, 0, 6)})
        panel.Controls.Add(New Label() With {.Text = "Sign in to manage your store stock", .Font = New Font("Segoe UI", 9), .ForeColor = UiTheme.Muted, .AutoSize = True, .Margin = New Padding(0, 0, 0, 18)})
        panel.Controls.Add(userBox) : panel.Controls.Add(passBox)
        Dim buttons As New FlowLayoutPanel() With {.AutoSize = True}
        Dim login As New Button() With {.Text = "Sign in", .AutoSize = True}
        UiTheme.StyleButton(login, True) : UiTheme.StyleButton(createButton)
        buttons.Controls.Add(login) : buttons.Controls.Add(createButton)
        panel.Controls.Add(buttons) : Controls.Add(panel)
        AddHandler login.Click, AddressOf SignIn
        AddHandler createButton.Click, AddressOf CreateFirstAccount
        createButton.Visible = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM app_users")) = 0
        AcceptButton = login
    End Sub
    Private Sub SignIn(sender As Object, e As EventArgs)
        Dim t = Db.Query("SELECT user_id,password_hash FROM app_users WHERE username=@p0", userBox.Text.Trim())
        If t.Rows.Count = 1 AndAlso Passwords.Verify(passBox.Text, CStr(t.Rows(0)("password_hash"))) Then
            Hide()
            Using f As New MainForm(CInt(t.Rows(0)("user_id")), userBox.Text.Trim())
                f.ShowDialog()
            End Using
            Close()
        Else
            MessageBox.Show("Username or password is incorrect.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub
    Private Sub CreateFirstAccount(sender As Object, e As EventArgs)
        If Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM app_users")) > 0 Then Return
        If userBox.Text.Trim().Length < 3 OrElse passBox.Text.Length < 10 Then
            MessageBox.Show("Choose a username of at least 3 characters and a password of at least 10 characters.") : Return
        End If
        Db.Execute("INSERT INTO app_users(username,password_hash) VALUES(@p0,@p1)", userBox.Text.Trim(), Passwords.Hash(passBox.Text))
        createButton.Visible = False
        MessageBox.Show("Account created. Select Sign in to continue.")
    End Sub
End Class

Friend Class MainForm
    Inherits Form
    Private ReadOnly _userId As Integer
    Private ReadOnly _username As String
    Private ReadOnly tabs As New TabControl() With {.Dock = DockStyle.Fill}
    Private ReadOnly dashboard As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(22), .ColumnCount = 4, .RowCount = 2, .BackColor = UiTheme.Canvas}
    Private ReadOnly products As New DataGridView()
    Private ReadOnly history As New DataGridView()
    Private ReadOnly reports As New DataGridView()
    Private ReadOnly search As New TextBox() With {.PlaceholderText = "Search SKU, product, or category", .Width = 250}
    Private ReadOnly welcome As New Label()
    Public Sub New(userId As Integer, username As String)
        _userId = userId : _username = username
        Text = "Grocery Store Inventory" : WindowState = FormWindowState.Maximized
        BackColor = UiTheme.Canvas : Font = New Font("Segoe UI", 9)
        Dim header As New Panel() With {.Dock = DockStyle.Top, .Height = 82, .BackColor = Color.White, .Padding = New Padding(24, 13, 24, 10)}
        Dim title As New Label() With {.Text = "Grocery Store Inventory", .Font = New Font("Segoe UI", 17, FontStyle.Bold), .ForeColor = UiTheme.ForestDark, .AutoSize = True, .Location = New Point(24, 10)}
        Dim subtitle As New Label() With {.Text = "Keep products and stock movements organized", .Font = New Font("Segoe UI", 9), .ForeColor = UiTheme.Muted, .AutoSize = True, .Location = New Point(26, 43)}
        welcome.Text = "Signed in as " & _username : welcome.AutoSize = True : welcome.ForeColor = UiTheme.Muted : welcome.Anchor = AnchorStyles.Top Or AnchorStyles.Right : welcome.Location = New Point(Width - 220, 31)
        header.Controls.Add(title) : header.Controls.Add(subtitle) : header.Controls.Add(welcome)
        dashboard.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25)) : dashboard.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25)) : dashboard.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25)) : dashboard.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
        dashboard.RowStyles.Add(New RowStyle(SizeType.Absolute, 145)) : dashboard.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        Dim dashboardPage As New TabPage("Dashboard") With {.BackColor = UiTheme.Canvas}
        dashboardPage.Controls.Add(dashboard) : tabs.TabPages.Add(dashboardPage)
        tabs.Dock = DockStyle.Fill : tabs.Font = New Font("Segoe UI", 9, FontStyle.Bold) : tabs.Padding = New Point(14, 7)
        Controls.Add(tabs)
        BuildProducts()
        BuildStock()
        BuildReports()
        Controls.Add(header)
        AddHandler tabs.SelectedIndexChanged, Sub() RefreshCurrent()
        RefreshCurrent()
    End Sub
    Private Sub MakePage(title As String, grid As DataGridView)
        grid.Dock = DockStyle.Fill : grid.ReadOnly = True : grid.AllowUserToAddRows = False : grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        UiTheme.StyleGrid(grid)
        Dim page As New TabPage(title) With {.BackColor = UiTheme.Canvas, .Padding = New Padding(14)} : page.Controls.Add(grid) : tabs.TabPages.Add(page)
    End Sub
    Private Sub BuildProducts()
        Dim page As New TabPage("Products")
        Dim bar As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 54, .Padding = New Padding(10, 8, 6, 4), .BackColor = UiTheme.Canvas, .WrapContents = False}
        Dim add = Button("Add"), edit = Button("Edit selected"), deactivate = Button("Deactivate"), refresh = Button("Refresh")
        UiTheme.StyleButton(add, True) : UiTheme.StyleButton(edit) : UiTheme.StyleButton(deactivate) : UiTheme.StyleButton(refresh)
        bar.Controls.Add(search) : bar.Controls.Add(add) : bar.Controls.Add(edit) : bar.Controls.Add(deactivate) : bar.Controls.Add(refresh)
        products.Dock = DockStyle.Fill : products.ReadOnly = True : products.AllowUserToAddRows = False : products.SelectionMode = DataGridViewSelectionMode.FullRowSelect : products.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        page.Controls.Add(products) : page.Controls.Add(bar) : page.BackColor = UiTheme.Canvas : page.Padding = New Padding(14) : tabs.TabPages.Add(page) : UiTheme.StyleGrid(products)
        AddHandler search.TextChanged, Sub() LoadProducts()
        AddHandler refresh.Click, Sub() LoadProducts()
        AddHandler add.Click, Sub() EditProduct(Nothing)
        AddHandler edit.Click, Sub() EditProduct(SelectedId(products))
        AddHandler deactivate.Click, Sub()
                                         Dim id = SelectedId(products)
                                         If id IsNot Nothing AndAlso MessageBox.Show("Deactivate selected product? Its history will be kept.", "Confirm", MessageBoxButtons.YesNo) = DialogResult.Yes Then
                                             Db.Execute("UPDATE products SET is_active=0 WHERE product_id=@p0", id) : LoadProducts()
                                         End If
                                     End Sub
    End Sub
    Private Sub BuildStock()
        Dim page As New TabPage("Stock transactions")
        Dim bar As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 54, .Padding = New Padding(10, 8, 6, 4), .BackColor = UiTheme.Canvas}
        Dim incoming = Button("Stock in"), outgoing = Button("Stock out"), refresh = Button("Refresh")
        UiTheme.StyleButton(incoming, True) : UiTheme.StyleButton(outgoing) : UiTheme.StyleButton(refresh)
        bar.Controls.Add(incoming) : bar.Controls.Add(outgoing) : bar.Controls.Add(refresh)
        history.Dock = DockStyle.Fill : history.ReadOnly = True : history.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        page.Controls.Add(history) : page.Controls.Add(bar) : page.BackColor = UiTheme.Canvas : page.Padding = New Padding(14) : tabs.TabPages.Add(page) : UiTheme.StyleGrid(history)
        AddHandler incoming.Click, Sub() Stock("IN")
        AddHandler outgoing.Click, Sub() Stock("OUT")
        AddHandler refresh.Click, Sub() LoadHistory()
    End Sub
    Private Sub BuildReports()
        Dim page As New TabPage("Reports")
        Dim bar As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 54, .Padding = New Padding(10, 8, 6, 4), .BackColor = UiTheme.Canvas}
        Dim inv = Button("Inventory"), low = Button("Low stock"), tx = Button("Transactions")
        UiTheme.StyleButton(inv, True) : UiTheme.StyleButton(low) : UiTheme.StyleButton(tx)
        bar.Controls.Add(inv) : bar.Controls.Add(low) : bar.Controls.Add(tx)
        reports.Dock = DockStyle.Fill : reports.ReadOnly = True : reports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        page.Controls.Add(reports) : page.Controls.Add(bar) : page.BackColor = UiTheme.Canvas : page.Padding = New Padding(14) : tabs.TabPages.Add(page) : UiTheme.StyleGrid(reports)
        AddHandler inv.Click, Sub()
                                   reports.DataSource = Db.Query("SELECT sku,product_name,category,quantity,unit,unit_price,quantity*unit_price AS stock_value FROM products WHERE is_active=1 ORDER BY product_name")
                                   SetMoneyFormat(reports, "unit_price", "stock_value")
                               End Sub
        AddHandler low.Click, Sub() reports.DataSource = Db.Query("SELECT sku,product_name,quantity,reorder_level,unit FROM products WHERE is_active=1 AND quantity<=reorder_level ORDER BY quantity")
        AddHandler tx.Click, Sub() reports.DataSource = Db.Query("SELECT t.transaction_id,p.sku,p.product_name,t.transaction_type,t.quantity,t.note,u.username,t.created_at FROM stock_transactions t JOIN products p ON p.product_id=t.product_id JOIN app_users u ON u.user_id=t.user_id ORDER BY t.created_at DESC LIMIT 1000")
    End Sub
    Private Shared Function Button(caption As String) As Button
        Return New Button() With {.Text = caption, .AutoSize = True}
    End Function
    Private Shared Sub SetMoneyFormat(grid As DataGridView, ParamArray columns As String())
        For Each columnName In columns
            If grid.Columns.Contains(columnName) Then grid.Columns(columnName).DefaultCellStyle.Format = "₱ #,##0.00"
        Next
    End Sub
    Private Shared Function SelectedId(grid As DataGridView) As Integer?
        If grid.CurrentRow Is Nothing OrElse Not grid.Columns.Contains("product_id") Then Return Nothing
        Return CInt(grid.CurrentRow.Cells("product_id").Value)
    End Function
    Private Sub RefreshCurrent()
        Select Case tabs.SelectedIndex
            Case 0 : LoadDashboard()
            Case 1 : LoadProducts()
            Case 2 : LoadHistory()
        End Select
    End Sub
    Private Sub LoadDashboard()
        Dim row = Db.Query("SELECT (SELECT COUNT(*) FROM products WHERE is_active=1) AS active_products,(SELECT COALESCE(SUM(quantity),0) FROM products WHERE is_active=1) AS units_in_stock,(SELECT COUNT(*) FROM products WHERE is_active=1 AND quantity<=reorder_level) AS low_stock_products,(SELECT COALESCE(SUM(quantity*unit_price),0) FROM products WHERE is_active=1) AS stock_value").Rows(0)
        dashboard.Controls.Clear()
        dashboard.Controls.Add(MetricCard("Active products", CStr(row("active_products")), UiTheme.Forest), 0, 0)
        dashboard.Controls.Add(MetricCard("Units in stock", CStr(row("units_in_stock")), Color.FromArgb(80, 137, 123)), 1, 0)
        dashboard.Controls.Add(MetricCard("Low stock", CStr(row("low_stock_products")), Color.FromArgb(190, 137, 58)), 2, 0)
        dashboard.Controls.Add(MetricCard("Estimated stock value", "₱ " & Convert.ToDecimal(row("stock_value")).ToString("N2"), Color.FromArgb(92, 113, 151)), 3, 0)
        dashboard.Controls.Add(New Label() With {.Text = "A quick view of your active inventory", .AutoSize = True, .ForeColor = UiTheme.Muted, .Font = New Font("Segoe UI", 10), .Margin = New Padding(2, 22, 0, 0)}, 0, 1)
        dashboard.SetColumnSpan(dashboard.GetControlFromPosition(0, 1), 4)
    End Sub
    Private Function MetricCard(caption As String, value As String, accent As Color) As Control
        Dim card As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Margin = New Padding(0, 0, 14, 0), .Padding = New Padding(16)}
        Dim stripe As New Panel() With {.Dock = DockStyle.Left, .Width = 4, .BackColor = accent}
        Dim label As New Label() With {.Text = caption, .AutoSize = True, .ForeColor = UiTheme.Muted, .Font = New Font("Segoe UI", 9), .Location = New Point(18, 17)}
        Dim number As New Label() With {.Text = value, .AutoSize = True, .ForeColor = UiTheme.Ink, .Font = New Font("Segoe UI", 23, FontStyle.Bold), .Location = New Point(16, 49)}
        card.Controls.Add(stripe) : card.Controls.Add(label) : card.Controls.Add(number)
        Return card
    End Function
    Private Sub LoadProducts()
        products.DataSource = Db.Query("SELECT product_id,sku,product_name,category,unit,unit_price,quantity,reorder_level FROM products WHERE is_active=1 AND (sku LIKE @p0 OR product_name LIKE @p1 OR category LIKE @p2) ORDER BY product_name", "%" & search.Text.Trim() & "%", "%" & search.Text.Trim() & "%", "%" & search.Text.Trim() & "%")
        If products.Columns.Contains("product_id") Then products.Columns("product_id").Visible = False
        SetMoneyFormat(products, "unit_price")
    End Sub
    Private Sub LoadHistory()
        history.DataSource = Db.Query("SELECT t.transaction_id,p.sku,p.product_name,t.transaction_type,t.quantity,t.note,u.username,t.created_at FROM stock_transactions t JOIN products p ON p.product_id=t.product_id JOIN app_users u ON u.user_id=t.user_id ORDER BY t.created_at DESC LIMIT 500")
    End Sub
    Private Sub EditProduct(id As Integer?)
        Dim current As DataRow = Nothing
        If id.HasValue Then
            Dim t = Db.Query("SELECT * FROM products WHERE product_id=@p0", id.Value)
            If t.Rows.Count = 0 Then Return
            current = t.Rows(0)
        End If
        Using f As New ProductDialog(current)
            If f.ShowDialog() = DialogResult.OK Then
                If id.HasValue Then
                    Db.Execute("UPDATE products SET sku=@p0,product_name=@p1,category=@p2,unit=@p3,unit_price=@p4,reorder_level=@p5 WHERE product_id=@p6", f.Sku, f.ProductTitle, f.Category, f.Unit, f.Price, f.Reorder, id.Value)
                Else
                    Db.Execute("INSERT INTO products(sku,product_name,category,unit,unit_price,reorder_level) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)", f.Sku, f.ProductTitle, f.Category, f.Unit, f.Price, f.Reorder)
                End If
                LoadProducts()
            End If
        End Using
    End Sub
    Private Sub Stock(kind As String)
        Dim active = Db.Query("SELECT product_id,CONCAT(sku,' · ',product_name) AS label FROM products WHERE is_active=1 ORDER BY product_name")
        If active.Rows.Count = 0 Then MessageBox.Show("Add a product first.") : Return
        Using f As New StockDialog(active, kind)
            If f.ShowDialog() <> DialogResult.OK Then Return
            Using c = Db.OpenConnection(), tx = c.BeginTransaction()
                Try
                    Using cmd As New MySqlCommand("SELECT quantity FROM products WHERE product_id=@id FOR UPDATE", c, tx)
                        cmd.Parameters.AddWithValue("@id", f.ProductId)
                        Dim current = Convert.ToInt32(cmd.ExecuteScalar())
                        If kind = "OUT" AndAlso f.Quantity > current Then Throw New InvalidOperationException("Stock out quantity exceeds current stock (" & current & ").")
                    End Using
                    Using cmd As New MySqlCommand("UPDATE products SET quantity=quantity+@change WHERE product_id=@id", c, tx)
                        cmd.Parameters.AddWithValue("@change", If(kind = "IN", f.Quantity, -f.Quantity)) : cmd.Parameters.AddWithValue("@id", f.ProductId) : cmd.ExecuteNonQuery()
                    End Using
                    Using cmd As New MySqlCommand("INSERT INTO stock_transactions(product_id,transaction_type,quantity,note,user_id) VALUES(@product,@kind,@qty,@note,@user)", c, tx)
                        cmd.Parameters.AddWithValue("@product", f.ProductId) : cmd.Parameters.AddWithValue("@kind", kind) : cmd.Parameters.AddWithValue("@qty", f.Quantity) : cmd.Parameters.AddWithValue("@note", f.Note) : cmd.Parameters.AddWithValue("@user", _userId) : cmd.ExecuteNonQuery()
                    End Using
                    tx.Commit()
                Catch
                    tx.Rollback() : Throw
                End Try
            End Using
            LoadHistory()
        End Using
    End Sub
End Class

Friend Class ProductDialog
    Inherits Form
    Private ReadOnly skuBox As New TextBox(), nameBox As New TextBox(), categoryBox As New TextBox(), unitBox As New TextBox()
    Private ReadOnly priceBox As New NumericUpDown() With {.DecimalPlaces = 2, .Maximum = 100000000, .Minimum = 0}
    Private ReadOnly reorderBox As New NumericUpDown() With {.Maximum = 1000000, .Minimum = 0}
    Public ReadOnly Property Sku As String
        Get
            Return skuBox.Text.Trim()
        End Get
    End Property
    Public ReadOnly Property ProductTitle As String
        Get
            Return nameBox.Text.Trim()
        End Get
    End Property
    Public ReadOnly Property Category As String
        Get
            Return categoryBox.Text.Trim()
        End Get
    End Property
    Public ReadOnly Property Unit As String
        Get
            Return unitBox.Text.Trim()
        End Get
    End Property
    Public ReadOnly Property Price As Decimal
        Get
            Return priceBox.Value
        End Get
    End Property
    Public ReadOnly Property Reorder As Integer
        Get
            Return CInt(reorderBox.Value)
        End Get
    End Property
    Public Sub New(row As DataRow)
        Text = If(row Is Nothing, "Add product", "Edit product") : FormBorderStyle = FormBorderStyle.FixedDialog : StartPosition = FormStartPosition.CenterParent : ClientSize = New Size(370, 300)
        Dim fields As (String, Control)() = {("SKU", skuBox), ("Name", nameBox), ("Category", categoryBox), ("Unit", unitBox), ("Unit price", priceBox), ("Reorder level", reorderBox)}
        Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 7, .Padding = New Padding(12)}
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 32)) : layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 68))
        For Each item In fields
            layout.Controls.Add(New Label() With {.Text = item.Item1, .AutoSize = True, .Anchor = AnchorStyles.Left}, 0, layout.RowCount - 7 + layout.Controls.Count \ 2)
            item.Item2.Dock = DockStyle.Fill : layout.Controls.Add(item.Item2, 1, layout.Controls.Count \ 2)
        Next
        Dim save As New Button() With {.Text = "Save", .DialogResult = DialogResult.None, .AutoSize = True}
        layout.Controls.Add(save, 1, 6) : Controls.Add(layout)
        If row IsNot Nothing Then
            skuBox.Text = CStr(row("sku")) : nameBox.Text = CStr(row("product_name")) : categoryBox.Text = CStr(row("category")) : unitBox.Text = CStr(row("unit")) : priceBox.Value = CDec(row("unit_price")) : reorderBox.Value = CDec(row("reorder_level"))
        Else
            unitBox.Text = "piece"
        End If
        AddHandler save.Click, Sub()
                                   If Sku = "" OrElse ProductTitle = "" OrElse Unit = "" Then MessageBox.Show("SKU, name, and unit are required.") : Return
                                   DialogResult = DialogResult.OK : Close()
                               End Sub
    End Sub
End Class

Friend Class StockDialog
    Inherits Form
    Private ReadOnly productBox As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 300, .DisplayMember = "label", .ValueMember = "product_id"}
    Private ReadOnly quantityBox As New NumericUpDown() With {.Minimum = 1, .Maximum = 1000000}
    Private ReadOnly noteBox As New TextBox() With {.Width = 300, .MaxLength = 255}
    Public ReadOnly Property ProductId As Integer
        Get
            Return CInt(productBox.SelectedValue)
        End Get
    End Property
    Public ReadOnly Property Quantity As Integer
        Get
            Return CInt(quantityBox.Value)
        End Get
    End Property
    Public ReadOnly Property Note As String
        Get
            Return noteBox.Text.Trim()
        End Get
    End Property
    Public Sub New(data As DataTable, kind As String)
        Text = If(kind = "IN", "Stock in", "Stock out") : StartPosition = FormStartPosition.CenterParent : FormBorderStyle = FormBorderStyle.FixedDialog : ClientSize = New Size(420, 210)
        productBox.DataSource = data
        Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .ColumnCount = 2}
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25)) : layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 75))
        layout.Controls.Add(New Label() With {.Text = "Product", .AutoSize = True}, 0, 0) : layout.Controls.Add(productBox, 1, 0)
        layout.Controls.Add(New Label() With {.Text = "Quantity", .AutoSize = True}, 0, 1) : layout.Controls.Add(quantityBox, 1, 1)
        layout.Controls.Add(New Label() With {.Text = "Note", .AutoSize = True}, 0, 2) : layout.Controls.Add(noteBox, 1, 2)
        Dim save As New Button() With {.Text = "Record transaction", .AutoSize = True}
        layout.Controls.Add(save, 1, 3) : Controls.Add(layout)
        AddHandler save.Click, Sub()
                                   DialogResult = DialogResult.OK
                                   Close()
                               End Sub
    End Sub
End Class
