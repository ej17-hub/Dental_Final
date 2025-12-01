using Dental_Final.Admin;
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
    public partial class Services : Form
    {
        private int selectedServiceId = -1; // Store selected service ID

        public Services()
        {
            InitializeComponent();
            LoadServicesData();

            this.WindowState = FormWindowState.Maximized;

            dataGridViewServices.AllowUserToAddRows = false;

            // Make only the column headers bold and keep cell text regular
            dataGridViewServices.EnableHeadersVisualStyles = false;
            dataGridViewServices.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridViewServices.Font, FontStyle.Bold);
            dataGridViewServices.DefaultCellStyle.Font = new Font(dataGridViewServices.Font, FontStyle.Regular);

            // Ensure single subscription to prevent the handler running multiple times
            dataGridViewServices.CellContentClick -= dataGridViewServices_CellContentClick_1;
            dataGridViewServices.CellContentClick += dataGridViewServices_CellContentClick_1;

            // Ensure single subscription for formatting the Price column to show peso sign
            dataGridViewServices.CellFormatting -= DataGridViewServices_CellFormatting;
            dataGridViewServices.CellFormatting += DataGridViewServices_CellFormatting;
        }

        private void LoadServicesData()
        {
            string connectionString = "Server=DESKTOP-O65C6K9\\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

            string query = "SELECT service_id, name, category, price, description, duration_minutes FROM services";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
            {
                DataTable dt = new DataTable();
                try
                {
                    adapter.Fill(dt);
                    dataGridViewServices.DataSource = dt;
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleDatabaseError(ex, "Loading services data");
                    return;
                }

                // Remove old action/edit/delete columns if they exist to avoid duplicates
                if (dataGridViewServices.Columns.Contains("Edit"))
                    dataGridViewServices.Columns.Remove("Edit");
                if (dataGridViewServices.Columns.Contains("Delete"))
                    dataGridViewServices.Columns.Remove("Delete");

                // Add Edit button column
                DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn();
                editColumn.Name = "Edit";
                editColumn.HeaderText = "";
                editColumn.Text = "Edit";
                editColumn.UseColumnTextForButtonValue = true;
                editColumn.Width = 60;
                dataGridViewServices.Columns.Add(editColumn);

                // Add Delete button column
                DataGridViewButtonColumn deleteColumn = new DataGridViewButtonColumn();
                deleteColumn.Name = "Delete";
                deleteColumn.HeaderText = "";
                deleteColumn.Text = "Delete";
                deleteColumn.UseColumnTextForButtonValue = true;
                deleteColumn.Width = 60;
                dataGridViewServices.Columns.Add(deleteColumn);

                // Hide service_id column if present
                if (dataGridViewServices.Columns.Contains("service_id"))
                {
                    dataGridViewServices.Columns["service_id"].Visible = false;
                }

                // Rename headers
                if (dataGridViewServices.Columns.Contains("name"))
                    dataGridViewServices.Columns["name"].HeaderText = "Service Name";
                if (dataGridViewServices.Columns.Contains("category"))
                    dataGridViewServices.Columns["category"].HeaderText = "Category";
                if (dataGridViewServices.Columns.Contains("price"))
                {
                    dataGridViewServices.Columns["price"].HeaderText = "Price";
                    dataGridViewServices.Columns["price"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
                if (dataGridViewServices.Columns.Contains("description"))
                    dataGridViewServices.Columns["description"].HeaderText = "Description";
                if (dataGridViewServices.Columns.Contains("duration_minutes"))
                    dataGridViewServices.Columns["duration_minutes"].HeaderText = "Duration (min)";
            }
        }

        // Formats price cells to include the peso sign while keeping underlying value numeric
        private void DataGridViewServices_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = sender as DataGridView;
            if (grid == null) return;
            var col = grid.Columns[e.ColumnIndex];
            if (col == null) return;

            if (string.Equals(col.Name, "price", StringComparison.OrdinalIgnoreCase))
            {
                if (e.Value != null && e.Value != DBNull.Value)
                {
                    decimal val;
                    if (decimal.TryParse(e.Value.ToString(), out val))
                    {
                        e.Value = "₱" + val.ToString("N2");
                        e.FormattingApplied = true;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a service can be safely deleted by verifying:
        /// 1. No dentists have this category as their specialization
        /// 2. No appointments are using this service
        /// 3. No appointment_requests_services links exist (optional check)
        /// </summary>
        private bool CanDeleteService(int serviceId, string categoryOrSpecialization, out string errorMessage)
        {
            errorMessage = "";
            string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Check 1: If any dentist has this category as their specialization
                    string checkDentistSql = @"
                        SELECT COUNT(*) FROM dentists
                        WHERE specialization = @spec
                    ";

                    using (var cmd = new SqlCommand(checkDentistSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@spec", categoryOrSpecialization ?? "");
                        int dentistCount = (int)cmd.ExecuteScalar();

                        if (dentistCount > 0)
                        {
                            errorMessage = $"Cannot delete this service!\n\n" +
                                         $"There {(dentistCount == 1 ? "is" : "are")} {dentistCount} dentist(s) " +
                                         $"specialized in \"{categoryOrSpecialization}\".\n\n" +
                                         $"Please reassign the dentist(s) to a different specialization before deleting this service.";
                            return false;
                        }
                    }

                    // Check 2: If this service is referenced in any appointment
                    string checkAppointmentSql = @"
                        SELECT COUNT(*) FROM appointments 
                        WHERE service_id = @serviceId
                    ";

                    using (var cmd = new SqlCommand(checkAppointmentSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        int appointmentCount = (int)cmd.ExecuteScalar();

                        if (appointmentCount > 0)
                        {
                            errorMessage = $"Cannot delete this service!\n\n" +
                                         $"This service is currently used in {appointmentCount} appointment(s).\n\n" +
                                         $"Please remove or update these appointments before deleting this service.";
                            return false;
                        }
                    }

                    // Check 3: If this service is in appointments_services linking table
                    string checkAppointmentServicesSql = @"
                        SELECT COUNT(*) FROM appointments_services 
                        WHERE service_id = @serviceId
                    ";

                    using (var cmd = new SqlCommand(checkAppointmentServicesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        int linkCount = (int)cmd.ExecuteScalar();

                        if (linkCount > 0)
                        {
                            errorMessage = $"Cannot delete this service!\n\n" +
                                         $"This service is linked to {linkCount} appointment(s).\n\n" +
                                         $"Please remove these links before deleting this service.";
                            return false;
                        }
                    }

                    // Check 4: If this service is in appointment_requests_services linking table
                    string checkRequestServicesSql = @"
                        SELECT COUNT(*) FROM appointment_requests_services 
                        WHERE service_id = @serviceId
                    ";

                    using (var cmd = new SqlCommand(checkRequestServicesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        int requestLinkCount = (int)cmd.ExecuteScalar();

                        if (requestLinkCount > 0)
                        {
                            errorMessage = $"Cannot delete this service!\n\n" +
                                         $"This service is linked to {requestLinkCount} pending appointment request(s).\n\n" +
                                         $"Please process these requests before deleting this service.";
                            return false;
                        }
                    }
                }

                return true; // Safe to delete
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Checking service constraints before deletion");
                errorMessage = "Error checking if service can be deleted. Please check the error log.";
                return false;
            }
        }

        private void btnNewService_Click(object sender, EventArgs e)
        {
            Add_Services add_Service = new Add_Services();
            add_Service.Show();

            this.Close();
        }

        private void dataGridViewServices_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var clickedColumn = dataGridViewServices.Columns[e.ColumnIndex].Name;

                // Edit button clicked
                if (clickedColumn == "Edit")
                {
                    int serviceId = Convert.ToInt32(dataGridViewServices.Rows[e.RowIndex].Cells["service_id"].Value);
                    Edit_Services editForm = new Edit_Services(serviceId);
                    editForm.Show();
                    this.Close();
                    return;
                }

                // Delete button clicked
                if (clickedColumn == "Delete")
                {
                    int serviceId = Convert.ToInt32(dataGridViewServices.Rows[e.RowIndex].Cells["service_id"].Value);
                    string serviceName = dataGridViewServices.Rows[e.RowIndex].Cells["name"].Value?.ToString() ?? "Unknown Service";
                    string categoryOrSpec = dataGridViewServices.Rows[e.RowIndex].Cells["category"].Value?.ToString() ?? "";

                    // CHECK CONSTRAINTS BEFORE ALLOWING DELETE
                    if (!CanDeleteService(serviceId, categoryOrSpec, out string errMsg))
                    {
                        ErrorHandler.HandleValidationError(errMsg, "Cannot Delete Service");
                        return;
                    }

                    // Show confirmation dialog
                    var result = ErrorHandler.ShowConfirmation(
                        $"Are you sure you want to delete the service:\n\n\"{serviceName}\"?\n\n" +
                        "This action cannot be undone.",
                        "Confirm Delete");

                    if (result)
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;
                        string query = "DELETE FROM services WHERE service_id = @ServiceId";

                        using (SqlConnection conn = new SqlConnection(connectionString))
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@ServiceId", serviceId);

                            try
                            {
                                conn.Open();
                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // Log the deletion
                                    try
                                    {
                                        ActivityLogger.Log($"Admin deleted service: {serviceName}");
                                    }
                                    catch { /* Ignore logging errors */ }

                                    ErrorHandler.ShowSuccess($"Service \"{serviceName}\" deleted successfully.");
                                    LoadServicesData(); // Refresh the grid
                                }
                                else
                                {
                                    ErrorHandler.HandleValidationError(
                                        "Service could not be deleted. It may have already been removed.",
                                        "Delete Service");
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorHandler.HandleDatabaseError(ex, "Deleting service");
                            }
                        }
                    }
                    return;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Appointments appointments = new Appointments();
            appointments.Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Patients patients = new Patients();
            patients.Show();
            this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Staff staff = new Staff();
            staff.Show();
            this.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Adding_Patient adding_Patient = new Adding_Patient();
            adding_Patient.Show();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Add_Appointment add_Appointment = new Add_Appointment();
            add_Appointment.Show();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Pending_Appointments pending_Appointments = new Pending_Appointments();
            pending_Appointments.Show();
            this.Hide();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            var dr = ErrorHandler.ShowConfirmation(
                "Are you sure you want to log out?",
                "Confirm Logout");

            if (!dr) return;

            try
            {
                var login = new Log_in();
                login.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Opening login screen");
            }
        }
    }
}
