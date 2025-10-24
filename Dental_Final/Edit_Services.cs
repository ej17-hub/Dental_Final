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
    public partial class Edit_Services : Form
    {

        Services s = new Services();

        private int serviceId;
        private string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";

        public Edit_Services(int id)
        {
            InitializeComponent();

            s.Show();
            s.WindowState = FormWindowState.Maximized;
            serviceId = id;
            LoadServiceData();
        }

        private void LoadServiceData()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT name, price, description, duration_minutes FROM services WHERE service_id = @ServiceId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ServiceId", serviceId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtServiceName.Text = reader["name"].ToString();
                            txtServicePrice.Text = reader["price"].ToString();
                            txtDescription.Text = reader["description"].ToString();
                            txtDuration.Text = reader["duration_minutes"].ToString();
                        }
                    }
                }
            }
        }
        private void button1_Click_1(object sender, EventArgs e)
        {
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
                string query = @"UPDATE services 
                                 SET name = @ServiceName, 
                                     price = @ServicePrice, 
                                     description = @Description, 
                                     duration_minutes = @Duration 
                                 WHERE service_id = @ServiceId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ServiceName", serviceName);
                    cmd.Parameters.AddWithValue("@ServicePrice", servicePrice);
                    cmd.Parameters.AddWithValue("@Description", description);
                    cmd.Parameters.AddWithValue("@Duration", duration);
                    cmd.Parameters.AddWithValue("@ServiceId", serviceId);

                    try
                    {
                        conn.Open();
                        int result = cmd.ExecuteNonQuery();
                        if (result > 0)
                        {
                            MessageBox.Show("Service updated successfully!");

                            // Log activity (safe swallow)
                            try
                            {
                                ActivityLogger.Log($"Admin updated service '{serviceName}'");
                            }
                            catch { }

                            s.Close();
                            Services servicesForm = new Services();
                            servicesForm.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Update failed. Please try again.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error updating service: " + ex.Message);
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}