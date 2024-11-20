using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using MySql.Data.MySqlClient;

//ST10215637

namespace ContractMonthlyClaimSystem
{
    public partial class ReviewClaimsWindow : Window
    {
        private string connectionString = "Server=localhost;Database=cmcs_db;User ID=root;Password=meanbowl789;SslMode=None;";
        private int lecturerId;

        public ReviewClaimsWindow(int lecturerId)
        {
            InitializeComponent();
            this.lecturerId = lecturerId;
            LoadClaims();
        }

        private void LoadClaims()
        {
            List<Claim> claims = new List<Claim>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            Claims.Id, 
                            Claims.LecturerId, 
                            Claims.TotalHours, 
                            Claims.HourlyRate, 
                            Claims.Description, 
                            Claims.Status, 
                            SupportingDocuments.DocumentPath
                        FROM Claims
                        LEFT JOIN SupportingDocuments 
                        ON Claims.Id = SupportingDocuments.ClaimId
                        WHERE Claims.LecturerId = @LecturerId";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@LecturerId", lecturerId);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                claims.Add(new Claim
                                {
                                    Id = reader.GetInt32("Id"),
                                    LecturerId = reader.GetInt32("LecturerId"),
                                    TotalHours = reader.GetDecimal("TotalHours"),
                                    HourlyRate = reader.GetDecimal("HourlyRate"),
                                    Description = reader.GetString("Description"),
                                    Status = reader.GetString("Status"),
                                    DocumentPath = reader.IsDBNull(reader.GetOrdinal("DocumentPath"))
                                        ? null
                                        : reader.GetString("DocumentPath")
                                });
                            }
                        }
                    }
                }
                claimsDataGrid.ItemsSource = claims;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading claims: " + ex.Message);
            }
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            UpdateClaimStatus("Approved");
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            UpdateClaimStatus("Rejected");
        }

        private void UpdateClaimStatus(string status)
        {
            if (claimsDataGrid.SelectedItem is Claim selectedClaim)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "UPDATE Claims SET Status = @Status WHERE Id = @Id";

                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Status", status);
                            cmd.Parameters.AddWithValue("@Id", selectedClaim.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show($"Claim {status.ToLower()} successfully.");
                    LoadClaims(); // Refresh claims list
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating claim status: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please select a claim to approve or reject.");
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (claimsDataGrid.SelectedItem is Claim selectedClaim)
            {
                if (!string.IsNullOrEmpty(selectedClaim.DocumentPath) && System.IO.File.Exists(selectedClaim.DocumentPath))
                {
                    Process.Start(new ProcessStartInfo(selectedClaim.DocumentPath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("The associated document could not be found.");
                }
            }
            else
            {
                MessageBox.Show("Please select a claim to view its associated document.");
            }
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
        public string DocumentPath { get; set; } // Added DocumentPath
    }
}
