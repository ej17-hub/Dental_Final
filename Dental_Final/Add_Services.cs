using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        // Nullable int to hold the service ID when editing an existing service
        private int? editingServiceId;

        public Add_Services()
        {
            InitializeComponent();
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
        }

       

        private void button1_Click_1(object sender, EventArgs e)
        {
            string connectionString = "Server=DESKTOP-PB8NME4\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";

            string serviceName = txtServiceName.Text.Trim();
            decimal servicePrice;
            if (!decimal.TryParse(txtServicePrice.Text.Trim(), out servicePrice))
            {
                MessageBox.Show("Please enter a valid price.");
                return;
            }
            string description = txtDescription.Text.Trim();
            int duration;
            if (!int.TryParse(txtDuration.Text.Trim(), out duration))
            {
                MessageBox.Show("Please enter a valid duration (in minutes).");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                if (editingServiceId.HasValue)
                {
                    // UPDATE existing service
                    string updateQuery = @"UPDATE services SET name = @ServiceName, price = @ServicePrice, description = @Description, duration_minutes = @Duration WHERE service_id = @ServiceId";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                        cmd.Parameters.AddWithValue("@ServicePrice", servicePrice);
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Duration", duration);
                        cmd.Parameters.AddWithValue("@ServiceId", editingServiceId.Value);

                        try
                        {
                            int affected = cmd.ExecuteNonQuery();
                            if (affected > 0)
                                MessageBox.Show("Service updated successfully.");
                            else
                                MessageBox.Show("No service was updated. Please check the data.");
                            this.Close();

                            Services services = new Services();
                            services.Show();
                            this.Hide();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error: " + ex.Message);
                        }
                    }
                }
                else
                {
                    // INSERT new service
                    string insertQuery = @"INSERT INTO services (name, price, description, duration_minutes) VALUES (@ServiceName, @ServicePrice, @Description, @Duration)";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                        cmd.Parameters.AddWithValue("@ServicePrice", servicePrice);
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@Duration", duration);

                        try
                        {
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Service added successfully.");
                            this.Close();
                            Services services = new Services();
                            services.Show();
                            this.Hide();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error: " + ex.Message);
                        }
                    }
                }
            }
        }
    }
}
