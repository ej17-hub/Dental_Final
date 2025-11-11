using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Add_Services : Form
    {
        Services s = new Services();
        string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;
        // Nullable int to hold the service ID when editing an existing service
        private int? editingServiceId;

        public Add_Services()
        {
            InitializeComponent();

            s.Show();
            s.WindowState = FormWindowState.Maximized;
            
            // Load categories into combo box
            LoadCategoriesFromDatabase();
        }

        // Load available categories from dentist specializations in database
        private void LoadCategoriesFromDatabase()
        {
            try
            {
                // Find the cmbCategory control if it exists
                ComboBox categoryCombo = this.Controls.Find("cmbCategory", true).FirstOrDefault() as ComboBox;
                
                if (categoryCombo == null)
                {
                    // Control doesn't exist in designer yet - will be added
                    return;
                }

                categoryCombo.Items.Clear();
                
                // Add "None" option for services without specific category
                categoryCombo.Items.Add("(None)");

                // Query to get distinct specializations from dentists table
                string query = @"SELECT DISTINCT specialization 
                                FROM dentists 
                                WHERE specialization IS NOT NULL 
                                  AND LTRIM(RTRIM(specialization)) <> ''
                                ORDER BY specialization";

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string specialization = reader["specialization"].ToString().Trim();
                            if (!string.IsNullOrEmpty(specialization))
                            {
                                categoryCombo.Items.Add(specialization);
                            }
                        }
                    }
                }

                // If no specializations found, add default categories
                if (categoryCombo.Items.Count <= 1) // Only "(None)" exists
                {
                    categoryCombo.Items.Add("General Dentistry");
                    categoryCombo.Items.Add("Restorative Dentistry");
                    categoryCombo.Items.Add("Pediatric Dentistry");
                    categoryCombo.Items.Add("Orthodontics");
                    categoryCombo.Items.Add("Prosthodontics");
                    categoryCombo.Items.Add("Cosmetic Dentistry");
                    categoryCombo.Items.Add("Oral Surgery");
                    categoryCombo.Items.Add("Endodontics");
                }

                // Set default selection
                categoryCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                // If there's an error, populate with default categories
                ComboBox categoryCombo = this.Controls.Find("cmbCategory", true).FirstOrDefault() as ComboBox;
                if (categoryCombo != null)
                {
                    categoryCombo.Items.Clear();
                    categoryCombo.Items.Add("(None)");
                    categoryCombo.Items.Add("General Dentistry");
                    categoryCombo.Items.Add("Restorative Dentistry");
                    categoryCombo.Items.Add("Pediatric Dentistry");
                    categoryCombo.Items.Add("Orthodontics");
                    categoryCombo.Items.Add("Prosthodontics");
                    categoryCombo.Items.Add("Cosmetic Dentistry");
                    categoryCombo.Items.Add("Oral Surgery");
                    categoryCombo.Items.Add("Endodontics");
                    categoryCombo.SelectedIndex = 0;
                }
            }
        }

        // Method to set the service ID for editing
        public void SetEditingServiceId(int serviceId)
        {
            editingServiceId = serviceId;
        }

        // Populate controls with service data from the reader
        public void SetServiceData(SqlDataReader reader)
        {
            // Set editingServiceId from the reader
            if (reader["service_id"] != DBNull.Value)
                editingServiceId = Convert.ToInt32(reader["service_id"]);
            
            txtServiceName.Text = reader["name"].ToString();
            txtServicePrice.Text = reader["price"].ToString();
            txtDescription.Text = reader["description"].ToString();
            txtDuration.Text = reader["duration_minutes"].ToString();
            
            // Load category if exists
            ComboBox categoryCombo = this.Controls.Find("cmbCategory", true).FirstOrDefault() as ComboBox;
            if (categoryCombo != null && reader["category"] != DBNull.Value)
            {
                string category = reader["category"].ToString().Trim();
                
                // Try to find and select the category
                int index = categoryCombo.FindStringExact(category);
                if (index >= 0)
                {
                    categoryCombo.SelectedIndex = index;
                }
                else
                {
                    // Category not found, select "(None)"
                    categoryCombo.SelectedIndex = 0;
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // Validate service name
            string serviceName = txtServiceName.Text.Trim();
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                MessageBox.Show("Please enter a service name.", "Validation", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtServiceName.Focus();
                return;
            }

            // Validate price
            decimal servicePrice;
            if (!decimal.TryParse(txtServicePrice.Text.Trim(), out servicePrice) || servicePrice <= 0)
            {
                MessageBox.Show("Please enter a valid price greater than zero.", "Validation", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtServicePrice.Focus();
                return;
            }

            // Validate description (optional but get it)
            string description = txtDescription.Text.Trim();

            // Validate duration
            int duration;
            if (!int.TryParse(txtDuration.Text.Trim(), out duration) || duration <= 0)
            {
                MessageBox.Show("Please enter a valid duration (in minutes) greater than zero.", "Validation", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDuration.Focus();
                return;
            }
            
            // Get category - if "(None)" is selected, store as NULL
            string category = null;
            ComboBox categoryCombo = this.Controls.Find("cmbCategory", true).FirstOrDefault() as ComboBox;
            
            if (categoryCombo != null && categoryCombo.SelectedIndex > 0)
            {
                category = categoryCombo.SelectedItem?.ToString()?.Trim();
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    
                    if (editingServiceId.HasValue)
                    {
                        // UPDATE existing service
                        string updateQuery = @"UPDATE services 
                                              SET name = @ServiceName, 
                                                  price = @ServicePrice, 
                                                  description = @Description, 
                                                  duration_minutes = @Duration,
                                                  category = @Category
                                              WHERE service_id = @ServiceId";
                        
                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                            cmd.Parameters.AddWithValue("@ServicePrice", servicePrice);
                            cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                            cmd.Parameters.AddWithValue("@Duration", duration);
                            cmd.Parameters.AddWithValue("@Category", string.IsNullOrEmpty(category) ? (object)DBNull.Value : category);
                            cmd.Parameters.AddWithValue("@ServiceId", editingServiceId.Value);

                            int affected = cmd.ExecuteNonQuery();
                            if (affected > 0)
                            {
                                MessageBox.Show("Service updated successfully.", "Success", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // Log activity (safe swallow)
                                try
                                {
                                    string categoryText = string.IsNullOrEmpty(category) ? "no category" : $"category '{category}'";
                                    ActivityLogger.Log($"Admin updated service '{serviceName}' in {categoryText}");
                                }
                                catch { }

                                this.Close();
                                s.Close();

                                Services services = new Services();
                                services.Show();
                            }
                            else
                            {
                                MessageBox.Show("No service was updated. Please check the data.", "Warning", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                    else
                    {
                        // INSERT new service
                        string insertQuery = @"INSERT INTO services (name, price, description, duration_minutes, category) 
                                              VALUES (@ServiceName, @ServicePrice, @Description, @Duration, @Category)";
                        
                        using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                            cmd.Parameters.AddWithValue("@ServicePrice", servicePrice);
                            cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                            cmd.Parameters.AddWithValue("@Duration", duration);
                            cmd.Parameters.AddWithValue("@Category", string.IsNullOrEmpty(category) ? (object)DBNull.Value : category);

                            cmd.ExecuteNonQuery();
                            
                            MessageBox.Show("Service added successfully.", "Success", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Log activity (safe swallow)
                            try
                            {
                                string categoryText = string.IsNullOrEmpty(category) ? "no category" : $"category '{category}'";
                                ActivityLogger.Log($"Admin added service '{serviceName}' in {categoryText} priced ₱{servicePrice:N2}");
                            }
                            catch { }

                            this.Close();
                            s.Close();

                            Services services = new Services();
                            services.Show();
                            this.Hide();
                        }
                    }
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Number == 207) // Invalid column name
                    {
                        MessageBox.Show("Database Error: The 'category' column does not exist in the services table.\n\n" +
                            "Please run this SQL script:\n\n" +
                            "ALTER TABLE services ADD category NVARCHAR(100) NULL;", 
                            "Database Schema Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (sqlEx.Number == 2627 || sqlEx.Number == 2601) // Duplicate key
                    {
                        MessageBox.Show("A service with this name already exists. Please use a different name.", 
                            "Duplicate Service", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show($"Database error: {sqlEx.Message}\n\nError Number: {sqlEx.Number}", 
                            "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while saving the service:\n\n{ex.Message}", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void categoryCombo_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}