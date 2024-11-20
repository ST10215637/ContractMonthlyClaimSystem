using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using IOPath = System.IO.Path;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ContractMonthlyClaimSystem
{
    public partial class HRDashboardWindow : Window
    {
        private string connectionString = "Server=localhost;Database=cmcs_db;User ID=root;Password=meanbowl789;SslMode=None;";

        public HRDashboardWindow()
        {
            InitializeComponent();
            LoadClaims();
        }

        private void LoadClaims()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Id, LecturerId, TotalHours, HourlyRate, Description, Status, RejectionReason FROM Claims";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        var claims = new List<dynamic>();
                        while (reader.Read())
                        {
                            claims.Add(new
                            {
                                ClaimId = reader["Id"],
                                LecturerId = reader["LecturerId"],
                                TotalHours = reader["TotalHours"],
                                HourlyRate = reader["HourlyRate"],
                                Description = reader["Description"],
                                Status = reader["Status"],
                                RejectionReason = reader["RejectionReason"]
                            });
                        }
                        claimsDataGrid.ItemsSource = claims;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading claims: " + ex.Message);
            }
        }


        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var claims = claimsDataGrid.ItemsSource as List<Claim>;
                if (claims == null || claims.Count == 0)
                {
                    MessageBox.Show("No claims available to generate a report.");
                    return;
                }

                string filePath = "ApprovedClaimsReport.csv";
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("ID,Lecturer ID,Total Hours,Hourly Rate,Description,Status,Total Payment");
                    foreach (var claim in claims)
                    {
                        var totalPayment = claim.TotalHours * claim.HourlyRate;
                        writer.WriteLine($"{claim.Id},{claim.LecturerId},{claim.TotalHours},{claim.HourlyRate}," +
                                         $"{claim.Description},{claim.Status},{totalPayment}");
                    }
                }

                MessageBox.Show($"Report generated successfully at: {System.IO.Path.GetFullPath(filePath)}");

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating report: " + ex.Message);
            }
        }

        public class Claim
        {
            public int Id { get; set; }
            public int LecturerId { get; set; }
            public decimal TotalHours { get; set; }
            public decimal HourlyRate { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public decimal TotalPayment { get; set; }
        }
    }
}