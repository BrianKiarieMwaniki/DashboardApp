using DashboardApp.Db;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashboardApp.Models
{
    public struct RevenueByDate
    {
        public string Date { get; set; }
        public decimal TotalAmount { get; set; }
    }
    public class Dashboard : DbConnection
    {
        private DateTime startDate;
        private DateTime endDate;
        private int numberDays;

        public int NumCustomers { get; private set; }

        public int NumSuppliers { get; private set; }
        public int NumProducts { get; private set; }
        public List<KeyValuePair<string, int>>? TopProductsList { get; private set; }
        public List<KeyValuePair<string, int>>? UnderstockList { get; private set; }
        public List<RevenueByDate> GrossRevenueList { get; private set; }
        public int NumOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }

        public Dashboard()
        {

        }

        private void GetNumberItems()
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "select count(id) from Customer";
                    NumCustomers = (int)command.ExecuteScalar();

                    command.CommandText = "select count(id) from Supplier";
                    NumSuppliers = (int)command.ExecuteScalar();

                    command.CommandText = "select count(id) from Product";
                    NumProducts = (int)command.ExecuteScalar();

                    command.CommandText = @"select count(id) from [Order] where OrderDate between @fromDate and @toDate ";

                    command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = endDate;
                    NumProducts = (int)command.ExecuteScalar();
                }
            }
        }

        private void GetOrderAnalysis()
        {
            GrossRevenueList = new List<RevenueByDate>();
            TotalProfit = 0;
            TotalRevenue = 0;
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = @"select OrderDate,sum(TotalAmount) as TotalAmount 
                                            from [Order]
                                            where OrderDate between @fromdate and @todate
                                            group by OrderDate";

                    command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = endDate;

                    var reader = command.ExecuteReader();
                    var resultTable = new List<KeyValuePair<DateTime, decimal>>();
                    while (reader.Read())
                    {
                        resultTable.Add(new KeyValuePair<DateTime, decimal>((DateTime)reader[0], (decimal)reader[1]));

                        TotalRevenue += (decimal)reader[1];
                    }
                    TotalProfit = TotalRevenue * 0.2m;
                    reader.Close();

                    //Group by hours
                    if (numberDays <= 1)
                    {
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by orderList.Key.ToString("hhtt")
                                           into order
                                            select new RevenueByDate
                                            {
                                                Date = order.Key,
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }
                    //Group by days
                    else if (numberDays <= 30)
                    {
                        foreach (var item in resultTable)
                        {
                            GrossRevenueList.Add(new RevenueByDate()
                            {
                                Date = item.Key.ToString("dd MMM"),
                                TotalAmount = (decimal)item.Value
                            });
                        }
                    }
                    //Group by weeks
                    else if (numberDays <= 92)
                    {
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(orderList.Key, CalendarWeekRule.FirstDay, DayOfWeek.Monday)
                                            into order
                                            select new RevenueByDate
                                            {
                                                Date = "Week " + order.Key.ToString(),
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();


                    }

                    //Group by Months
                    else if (numberDays <= (365 * 2))
                    {
                        bool isYear = numberDays <= 365 ? true : false;
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by orderList.Key.ToString("MMM yyyy")
                                            into order
                                            select new RevenueByDate
                                            {
                                                Date = isYear ? order.Key.Substring(0, order.Key.IndexOf(" ")) : order.Key,
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }

                    //Group by Years
                    else
                    {
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by orderList.Key.ToString("yyyy")
                                            into order
                                            select new RevenueByDate
                                            {
                                                Date = "Week " + order.Key.ToString(),
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();


                    }
                }
            }
        }

        private void GetProductAnalysis()
        {
            TopProductsList = new List<KeyValuePair<string, int>>();
            UnderstockList = new List<KeyValuePair<string, int>>();

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    SqlDataReader reader;
                    command.Connection = connection;
                    command.CommandText = @"select top 5 p.ProductName, sum(OrderItem.Quantity) as Q
                                            from [OrderItem]
                                            inner join [Product] P on P.Id = OrderItem.ProductId
                                            inner join [Order] O on O.Id = OrderItem.OrderId
                                            where OrderDate between @fromdate and @todate
                                            group by P.ProductName
                                            order by Q desc";
                    command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = endDate;

                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        TopProductsList.Add(new KeyValuePair<string, int>(reader[0].ToString(), (int)reader[1]));
                    }
                    reader.Close();

                    //Get Understock
                    command.CommandText = @"select ProductName,Stock
                                            from [Product]
                                            where stock <= 6 and IsDiscontinued = 0";

                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        UnderstockList.Add(new KeyValuePair<string, int>(reader[0].ToString(), (int)reader[1]));
                    }
                    reader.Close();
                }
            }
        }

        public bool LoadData(DateTime startDate, DateTime endDate)
        {
            endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, endDate.Hour, endDate.Month, 59);
            if (startDate != this.startDate || endDate != this.endDate)
            {
                this.startDate = startDate;
                this.endDate = endDate;
                this.numberDays = (endDate - startDate).Days;

                GetNumberItems();
                GetProductAnalysis();
                GetOrderAnalysis();

                Console.WriteLine("Data not refreshed, same query: {0}", startDate.ToString(), endDate.ToString());
                return true;
            }
            else
            {
                Console.WriteLine("Data not refreshed, same query: {0}", startDate.ToString(), endDate.ToString());
                return false;
            }
        }

    }
}
