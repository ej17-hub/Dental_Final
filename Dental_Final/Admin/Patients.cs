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
    public partial class Patients : Form
    {
        private int selectedPatientId;
        private int _patientId;

        public Patients()
        {
            InitializeComponent();
            LoadPatients();
            dataGridViewPatients.AllowUserToAddRows = false;

            this.WindowState = FormWindowState.Maximized;
        }

        private void LoadPatients()
        {
            string connectionString = "Server=DESKTOP-O65C6K9\\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

            string query = @"SELECT 
                                patient_id, 
                                first_name + ' ' + last_name AS Patient,
                                email,
                                birth_date,
                                phone
                             FROM patients";

            string searchTerm = textBox1.Text.Trim();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += @" WHERE 
                    CAST(patient_id AS VARCHAR) LIKE @SearchTerm
                    OR first_name + ' ' + last_name LIKE @SearchTerm";
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
            {
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                }

                DataTable dt = new DataTable();
                adapter.Fill(dt);

                dt.Columns.Add("PatientID", typeof(string));
                dt.Columns.Add("Age", typeof(int));
                dt.Columns.Add("ContactNo", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    row["PatientID"] = "#PT00" + row["patient_id"].ToString();
                    row["Age"] = CalculateAge(Convert.ToDateTime(row["birth_date"]));
                    row["ContactNo"] = row["phone"].ToString();
                }

                // Set DataGridView DataSource and display only needed columns
                dataGridViewPatients.DataSource = dt;
                dataGridViewPatients.Columns["Patient"].HeaderText = "Patient";
                dataGridViewPatients.Columns["PatientID"].HeaderText = "Patient ID";
                dataGridViewPatients.Columns["Age"].HeaderText = "Age";
                dataGridViewPatients.Columns["ContactNo"].HeaderText = "Contact No.";
                dataGridViewPatients.Columns["email"].Visible = false;
                dataGridViewPatients.Columns["birth_date"].Visible = false;
                dataGridViewPatients.Columns["phone"].Visible = false;
                dataGridViewPatients.Columns["patient_id"].Visible = false;

                // Remove old action columns if they exist
                if (dataGridViewPatients.Columns.Contains("View"))
                    dataGridViewPatients.Columns.Remove("View");
                if (dataGridViewPatients.Columns.Contains("Edit"))
                    dataGridViewPatients.Columns.Remove("Edit");
                if (dataGridViewPatients.Columns.Contains("Delete"))
                    dataGridViewPatients.Columns.Remove("Delete");
                if (dataGridViewPatients.Columns.Contains("Actions"))
                    dataGridViewPatients.Columns.Remove("Actions");

                // Add View button column
                DataGridViewButtonColumn viewColumn = new DataGridViewButtonColumn
                {
                    Name = "View",
                    HeaderText = "",
                    Text = "View",
                    UseColumnTextForButtonValue = true,
                    Width = 60
                };
                dataGridViewPatients.Columns.Add(viewColumn);

                // Add Edit button column
                DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn
                {
                    Name = "Edit",
                    HeaderText = "",
                    Text = "Edit",
                    UseColumnTextForButtonValue = true,
                    Width = 60
                };
                dataGridViewPatients.Columns.Add(editColumn);

                // Add Delete button column
                DataGridViewButtonColumn deleteColumn = new DataGridViewButtonColumn
                {
                    Name = "Delete",
                    HeaderText = "",
                    Text = "Delete",
                    UseColumnTextForButtonValue = true,
                    Width = 60
                };
                dataGridViewPatients.Columns.Add(deleteColumn);
            }
        }

        private int CalculateAge(DateTime birthDate)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            if (DateTime.Now.DayOfYear < birthDate.DayOfYear)
                age--;
            return age;
        }

        /// <summary>
        /// Checks if a patient can be safely deleted by verifying:
        /// 1. Patient has no upcoming appointments (based on appointment_date >= today)
        /// 2. Patient has no pending appointment requests
        /// </summary>
        private bool CanDeletePatient(int patientId, string patientName, out string errorMessage)
        {
            errorMessage = "";
            string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Check 1: If patient has any upcoming appointments (appointment_date >= today)
                    string checkUpcomingAppointmentsSql = @"
                        SELECT COUNT(*) FROM appointments 
                        WHERE patient_id = @patientId 
                        AND CAST(appointment_date AS DATE) >= CAST(GETDATE() AS DATE)
                    ";

                    using (var cmd = new SqlCommand(checkUpcomingAppointmentsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId);
                        int upcomingCount = (int)cmd.ExecuteScalar();

                        if (upcomingCount > 0)
                        {
                            errorMessage = $"Cannot delete patient \"{patientName}\"!\n\n" +
                                         $"This patient has {upcomingCount} upcoming appointment(s).\n\n" +
                                         $"Please cancel or complete these appointments before deleting this patient.";
                            return false;
                        }
                    }

                    // Check 2: If patient has any pending appointment requests
                    string checkPendingRequestsSql = @"
                        SELECT COUNT(*) FROM appointment_requests 
                        WHERE patient_id = @patientId 
                        AND status = 'Pending'
                    ";

                    using (var cmd = new SqlCommand(checkPendingRequestsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId);
                        int pendingCount = (int)cmd.ExecuteScalar();

                        if (pendingCount > 0)
                        {
                            errorMessage = $"Cannot delete patient \"{patientName}\"!\n\n" +
                                         $"This patient has {pendingCount} pending appointment request(s).\n\n" +
                                         $"Please process or cancel these requests before deleting this patient.";
                            return false;
                        }
                    }
                }

                return true; // Safe to delete
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleDatabaseError(ex, "Checking patient constraints before deletion");
                errorMessage = "Error checking if patient can be deleted. Please check the error log.";
                return false;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Adding_Patient adding = new Adding_Patient();
            adding.Show();
        }

        private void searchTerm_Click_1(object sender, EventArgs e)
        {
            LoadPatients();
        }

        private void dataGridViewPatients_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            // Get patient_id for the selected row (hidden column)
            object idObj = dataGridViewPatients.Rows[e.RowIndex].Cells["patient_id"].Value;
            if (idObj == null || idObj == DBNull.Value)
                return;

            int patientId = Convert.ToInt32(idObj);
            string patientName = dataGridViewPatients.Rows[e.RowIndex].Cells["Patient"].Value?.ToString() ?? "Unknown Patient";

            // View button logic
            if (dataGridViewPatients.Columns.Contains("View") && e.ColumnIndex == dataGridViewPatients.Columns["View"].Index)
            {
                try
                {
                    var viewForm = new ViewRecord(patientId);
                    viewForm.Show();
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleDatabaseError(ex, "Opening patient view record");
                }
                return;
            }

            // Edit button logic
            if (dataGridViewPatients.Columns.Contains("Edit") && e.ColumnIndex == dataGridViewPatients.Columns["Edit"].Index)
            {
                try
                {
                    Edit_Patient editP = new Edit_Patient(patientId);
                    editP.Show();
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleDatabaseError(ex, "Opening patient edit form");
                }
            }
            // Delete button logic
            else if (dataGridViewPatients.Columns.Contains("Delete") && e.ColumnIndex == dataGridViewPatients.Columns["Delete"].Index)
            {
                // CHECK CONSTRAINTS BEFORE ALLOWING DELETE
                if (!CanDeletePatient(patientId, patientName, out string errMsg))
                {
                    ErrorHandler.HandleValidationError(errMsg, "Cannot Delete Patient");
                    return;
                }

                // Show confirmation dialog
                var confirmResult = ErrorHandler.ShowConfirmation(
                    $"Are you sure you want to delete the patient:\n\n\"{patientName}\"?\n\n" +
                    "This action cannot be undone.",
                    "Confirm Delete");

                if (confirmResult)
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

                    string query = "DELETE FROM patients WHERE patient_id = @PatientId";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", patientId);
                        try
                        {
                            conn.Open();
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                // Log the deletion
                                try
                                {
                                    ActivityLogger.Log($"Admin deleted patient: {patientName}");
                                }
                                catch { /* Ignore logging errors */ }

                                ErrorHandler.ShowSuccess($"Patient \"{patientName}\" deleted successfully.");
                                LoadPatients();
                            }
                            else
                            {
                                ErrorHandler.HandleValidationError(
                                    "Patient could not be deleted. It may have already been removed.",
                                    "Delete Patient");
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandler.HandleDatabaseError(ex, "Deleting patient");
                        }
                    }
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

        private void button4_Click(object sender, EventArgs e)
        {
            Staff staff = new Staff();
            staff.Show();
            this.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Services services = new Services();
            services.Show();
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

        private void button9_Click(object sender, EventArgs e)
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

        private void Patients_Load(object sender, EventArgs e)
        {

        }

        private void dataGridViewPatients_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            // Get patient_id for the selected row (hidden column)
            object idObj = dataGridViewPatients.Rows[e.RowIndex].Cells["patient_id"].Value;
            if (idObj == null || idObj == DBNull.Value)
                return;

            int patientId = Convert.ToInt32(idObj);
            string patientName = dataGridViewPatients.Rows[e.RowIndex].Cells["Patient"].Value?.ToString() ?? "Unknown Patient";

            // View button logic
            if (dataGridViewPatients.Columns.Contains("View") && e.ColumnIndex == dataGridViewPatients.Columns["View"].Index)
            {
                try
                {
                    var viewForm = new ViewRecord(patientId);
                    viewForm.Show();
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleDatabaseError(ex, "Opening patient view record");
                }
                return;
            }

            // Edit button logic
            if (dataGridViewPatients.Columns.Contains("Edit") && e.ColumnIndex == dataGridViewPatients.Columns["Edit"].Index)
            {
                try
                {
                    Edit_Patient editP = new Edit_Patient(patientId);
                    editP.Show();
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleDatabaseError(ex, "Opening patient edit form");
                }
            }
            // Delete button logic
            else if (dataGridViewPatients.Columns.Contains("Delete") && e.ColumnIndex == dataGridViewPatients.Columns["Delete"].Index)
            {
                // CHECK CONSTRAINTS BEFORE ALLOWING DELETE
                if (!CanDeletePatient(patientId, patientName, out string errMsg))
                {
                    ErrorHandler.HandleValidationError(errMsg, "Cannot Delete Patient");
                    return;
                }

                // Show confirmation dialog
                var confirmResult = ErrorHandler.ShowConfirmation(
                    $"Are you sure you want to delete the patient:\n\n\"{patientName}\"?\n\n" +
                    "This action cannot be undone.",
                    "Confirm Delete");

                if (confirmResult)
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

                    string query = "DELETE FROM patients WHERE patient_id = @PatientId";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", patientId);
                        try
                        {
                            conn.Open();
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                // Log the deletion
                                try
                                {
                                    ActivityLogger.Log($"Admin deleted patient: {patientName}");
                                }
                                catch { /* Ignore logging errors */ }

                                ErrorHandler.ShowSuccess($"Patient \"{patientName}\" deleted successfully.");
                                LoadPatients();
                            }
                            else
                            {
                                ErrorHandler.HandleValidationError(
                                    "Patient could not be deleted. It may have already been removed.",
                                    "Delete Patient");
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandler.HandleDatabaseError(ex, "Deleting patient");
                        }
                    }
                }
            }
        }
    }
}
