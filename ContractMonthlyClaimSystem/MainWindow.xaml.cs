using System;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MySql.Data.MySqlClient;

//ST10215637

namespace ContractMonthlyClaimSystem
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=localhost;Database=cmcs_db;User ID=root;Password=meanbowl789;SslMode=None;";
        private string selectedFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Lecturer Login
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            int lecturerId;
            string password = txtPassword.Password;

            if (int.TryParse(txtLecturerId.Text, out lecturerId) && !string.IsNullOrEmpty(password))
            {
                bool loginSuccessful = ValidateLogin(lecturerId, password);

                if (loginSuccessful)
                {
                    MessageBox.Show("Login successful!");
                    ReviewClaimsButton.IsEnabled = true;
                }
                else
                {
                    MessageBox.Show("Invalid login credentials.");
                }
            }
            else
            {
                MessageBox.Show("Please enter valid Lecturer ID and Password.");
            }
        }

        private bool ValidateLogin(int lecturerId, string password)
        {
            // Simulate login check (you can replace with actual DB validation)
            return true; // Assume success for now
        }

        // Submit Claim with Auto Calculation
        private void SubmitClaim_Click(object sender, RoutedEventArgs e)
        {
            CalculateTotalPayment();

            if (string.IsNullOrWhiteSpace(txtTotalHours.Text) ||
                string.IsNullOrWhiteSpace(txtHourlyRate.Text) ||
                string.IsNullOrWhiteSpace(txtClaimDescription.Text))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            decimal totalHours, hourlyRate;
            if (decimal.TryParse(txtTotalHours.Text, out totalHours) && decimal.TryParse(txtHourlyRate.Text, out hourlyRate))
            {
                if (!ValidateClaim(totalHours, hourlyRate))
                {
                    MessageBox.Show("The claim contains invalid data. Please check hours and rate.");
                    return;
                }

                SubmitClaimToDatabase(1, totalHours, hourlyRate, txtClaimDescription.Text);
            }
            else
            {
                MessageBox.Show("Please enter valid numbers for Total Hours and Hourly Rate.");
            }
        }

        private void CalculateTotalPayment()
        {
            if (decimal.TryParse(txtTotalHours.Text, out decimal totalHours) &&
                decimal.TryParse(txtHourlyRate.Text, out decimal hourlyRate))
            {
                decimal totalPayment = totalHours * hourlyRate;
                lblTotalPayment.Content = $"R{totalPayment:F2}";
            }
            else
            {
                lblTotalPayment.Content = "Invalid Input";
            }
        }

        private void OpenHRDashboard_Click(object sender, RoutedEventArgs e)
        {
            HRDashboardWindow hrWindow = new HRDashboardWindow();
            hrWindow.Show();
        }

        private bool ValidateClaim(decimal totalHours, decimal hourlyRate)
        {
            const decimal maxHours = 160;
            const decimal maxRate = 1000;
            return totalHours > 0 && hourlyRate > 0 && totalHours <= maxHours && hourlyRate <= maxRate;
        }

        private void SubmitClaimToDatabase(int lecturerId, decimal totalHours, decimal hourlyRate, string description)
        {
            try
            {
                string status = "Pending";
                string rejectionReason = null;

                // Validate claim
                if (totalHours > 200)
                {
                    status = "Rejected";
                    rejectionReason = "Total hours exceed 200.";
                }
                else if (hourlyRate < 50 || hourlyRate > 500)
                {
                    status = "Rejected";
                    rejectionReason = "Hourly rate must be between R50 and R500.";
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO Claims (LecturerId, TotalHours, HourlyRate, Description, Status, RejectionReason) " +
                                   "VALUES (@LecturerId, @TotalHours, @HourlyRate, @Description, @Status, @RejectionReason)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@LecturerId", lecturerId);
                        cmd.Parameters.AddWithValue("@TotalHours", totalHours);
                        cmd.Parameters.AddWithValue("@HourlyRate", hourlyRate);
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@RejectionReason", rejectionReason);
                        cmd.ExecuteNonQuery();
                    }
                }

                if (status == "Rejected")
                {
                    MessageBox.Show($"Claim rejected: {rejectionReason}");
                }
                else
                {
                    MessageBox.Show("Claim submitted successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error submitting claim: " + ex.Message);
            }
        }


        // Review Claims
        private void ReviewClaimsButton_Click(object sender, RoutedEventArgs e)
        {
            int lecturerId = 1;

            ReviewClaimsWindow reviewWindow = new ReviewClaimsWindow(lecturerId);
            reviewWindow.Show();
        }

        // Upload Supporting File
        private void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files|*.pdf|Word Documents|*.doc;*.docx|Images|*.jpg;*.png|All Files|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                const long maxSize = 5 * 1024 * 1024; // 5 MB
                FileInfo fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > maxSize)
                {
                    MessageBox.Show("File size exceeds the 5MB limit. Please select a smaller file.");
                    return;
                }

                selectedFilePath = filePath;
                txtUploadedFile.Text = fileInfo.Name; // Display file name
            }
        }



        // Generate Report
        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Fetch approved claims from the database
                List<dynamic> approvedClaims = GetApprovedClaims();

                if (approvedClaims.Count == 0)
                {
                    MessageBox.Show("No approved claims found to generate a report.");
                    return;
                }

                // Use SaveFileDialog to select save location
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "ApprovedClaimsReport",
                    DefaultExt = ".csv",
                    Filter = "CSV files (*.csv)|*.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    ExportClaimsToCsv(approvedClaims, filePath);
                    MessageBox.Show("Report generated successfully at " + filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating report: " + ex.Message);
            }
        }

        private List<dynamic> GetApprovedClaims()
        {
            var approvedClaims = new List<dynamic>();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, lecturer_id, total_hours, hourly_rate, description FROM Claims WHERE status = 'Approved'";


                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            approvedClaims.Add(new
                            {
                                Id = reader.GetInt32("Id"),
                                LecturerId = reader.GetInt32("LecturerId"),
                                TotalHours = reader.GetInt32("TotalHours"),
                                HourlyRate = reader.GetDecimal("HourlyRate"),
                                Description = reader.GetString("Description")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching approved claims: " + ex.Message);
            }

            return approvedClaims;
        }

        private void ExportClaimsToCsv(List<dynamic> claims, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Write header row
                    writer.WriteLine("Id, LecturerId, TotalHours, HourlyRate, Description");

                    // Write claim data
                    foreach (var claim in claims)
                    {
                        writer.WriteLine($"{claim.ClaimId}, {claim.LecturerId}, {claim.TotalHours}, {claim.HourlyRate}, {claim.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error writing to CSV file: " + ex.Message);
            }
        }
    }
}
    