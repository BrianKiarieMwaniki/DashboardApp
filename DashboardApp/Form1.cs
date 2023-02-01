using DashboardApp.Models;

namespace DashboardApp
{
    public partial class Form1 : Form
    {
        private Dashboard model;

        public Form1()
        {
            InitializeComponent();

            //Default - last 7 days
            dtpStartDate.Value = DateTime.Today.AddDays(-7);
            dtpEndDate.Value = DateTime.Today;
            btnLast7Days.Select();

            model = new Dashboard();
            LoadData();
        }

        private void LoadData()
        {
            var refreshData = model.LoadData(dtpStartDate.Value, dtpEndDate.Value);
            if (refreshData)
            {
                lblNumOrders.Text = model.NumOrders.ToString();
                lblTotalRevenue.Text = $"${model.TotalRevenue.ToString()}";
                lblTotalProfit.Text = $"${model.TotalProfit.ToString()}";

                lblNumCustomers.Text = model.NumCustomers.ToString();
                lblNumSuppliers.Text = model.NumSuppliers.ToString();
                lblNumProducts.Text = model.NumProducts.ToString();


                chartGrossRevenue.DataSource = model.GrossRevenueList;
                chartGrossRevenue.Series[0].XValueMember = "Date";
                chartGrossRevenue.Series[0].YValueMembers = "Total Amount";
                chartGrossRevenue.DataBind();

                chartTopProducts.DataSource = model.TopProductsList;
                chartTopProducts.Series[0].XValueMember = "Key";
                chartTopProducts.Series[0].YValueMembers = "Value";
                chartTopProducts.DataBind();

                dgvUnderstock.DataSource = model.UnderstockList;
                dgvUnderstock.Columns[0].HeaderText = "Item";
                dgvUnderstock.Columns[1].HeaderText = "Units";

                Console.WriteLine("Loaded View :)");

            }
            else Console.WriteLine("View not Loaded, same query");
        }

        private void EnableCustomDates(bool isEnabled)
        {
            dtpStartDate.Enabled = isEnabled;
            dtpEndDate.Enabled = isEnabled;
            btnOkCustomDate.Visible = isEnabled;
        }

        #region event methods

        private void btnToday_Click(object sender, EventArgs e)
        {
            dtpStartDate.Value = DateTime.Today;
            dtpEndDate.Value = DateTime.Now;
            LoadData();
            EnableCustomDates(false);
        }

        private void btnLast7Days_Click(object sender, EventArgs e)
        {
            dtpStartDate.Value = DateTime.Today.AddDays(-7);
            dtpEndDate.Value = DateTime.Now;
            LoadData();
            EnableCustomDates(false);
        }

        private void btnLast30Days_Click(object sender, EventArgs e)
        {
            dtpStartDate.Value = DateTime.Today.AddDays(-30);
            dtpEndDate.Value = DateTime.Now;
            LoadData();
            EnableCustomDates(false);
        }
        
        private void btnThisMonth_Click(object sender, EventArgs e)
        {
            dtpStartDate.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dtpEndDate.Value = DateTime.Now;
            LoadData();
            EnableCustomDates(false);
        }
       
        private void btnCustomDate_Click(object sender, EventArgs e)
        {
            EnableCustomDates(true);
        }

        private void btnOkCustomDate_Click(object sender, EventArgs e)
        {
            LoadData();
        }
        #endregion

    }
}