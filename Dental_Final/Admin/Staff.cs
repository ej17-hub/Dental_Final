
using Dental_Final.Admin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Staff : Form
    {
        string connectionString = "Server=DESKTOP-O65C6K9\\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        private readonly Dictionary<string, string> dayAbbreviations = new Dictionary<string, string>
        {
            { "Monday", "M" },
            { "Tuesday", "T" },
            { "Wednesday", "W" },
            { "Thursday", "Th" },
            { "Friday", "F" },
            { "Saturday", "Sat" },
            { "Sunday", "Sun" }
        };

        public Staff()
        {
            InitializeComponent();
            LoadDentists();
            LoadStaff();
            this.WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// Checks if a dentist can be deleted by verifying they have no upcoming appointments
        /// </summary>
        private bool CanDeleteDentist(int dentistId, out string errorMessage, out int conflictCount)
        {
            errorMessage = string.Empty;
            conflictCount = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT COUNT(*) as conflict_count
                            FROM appointments
                            WHERE dentist_id = @DentistId
                            AND CAST(appointment_date AS DATE) >= CAST(GETDATE() AS DATE)
                            ";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DentistId", dentistId);
                        conn.Open();

                        int count = (int)cmd.ExecuteScalar();
                        conflictCount = count;

                        if (count > 0)
                        {
                            errorMessage = $"Cannot delete this dentist! This dentist has {count} upcoming appointment(s). Please reassign or cancel these appointments before deleting.";
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Error checking dentist appointments: " + ex.Message;
                conflictCount = 0;
                return false;
            }
        }

        /// <summary>
        /// Checks if staff can be deleted by verifying they are not assigned to upcoming appointments
        /// </summary>
        private bool CanDeleteStaff(int staffId, out string errorMessage, out int conflictCount)
        {
            errorMessage = string.Empty;
            conflictCount = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT COUNT(*) as conflict_count
                    FROM appointments
                    WHERE (staff_assign_1 = @StaffId OR staff_assign_2 = @StaffId)
                    AND CAST(appointment_date AS DATE) >= CAST(GETDATE() AS DATE)
                    ";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StaffId", staffId);
                        conn.Open();

                        int count = (int)cmd.ExecuteScalar();
                        conflictCount = count;

                        if (count > 0)
                        {
                            errorMessage = $"Cannot delete this staff member! This staff is assigned to {count} upcoming appointment(s). Please reassign or cancel these appointments before deleting.";
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Error checking staff appointments: " + ex.Message;
                conflictCount = 0;
                return false;
            }
        }

        private void btnNewService_Click(object sender, EventArgs e)
        {
            Add_Dentist addDentistForm = new Add_Dentist();
            addDentistForm.ShowDialog();
            LoadDentists();
        }

        private void dataGridViewDentists_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataRowView rowView = dataGridViewDentists.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (rowView == null)
                return;

            int dentistId = Convert.ToInt32(rowView["dentist_id"]);
            string dentistName = rowView["Dentist"].ToString();

            if (e.ColumnIndex == dataGridViewDentists.Columns["Edit"].Index)
            {
                Edit_Dentist editForm = new Edit_Dentist(dentistId);
                editForm.ShowDialog();
                LoadDentists();
            }
            else if (e.ColumnIndex == dataGridViewDentists.Columns["Delete"].Index)
            {
                if (!CanDeleteDentist(dentistId, out string errorMessage, out int conflictCount))
                {
                    MessageBox.Show(errorMessage, "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirmResult = MessageBox.Show(
                    $"Are you sure you want to delete dentist '{dentistName}'?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM dentists WHERE dentist_id = @DentistId", conn))
                        {
                            cmd.Parameters.AddWithValue("@DentistId", dentistId);
                            conn.Open();
                            cmd.ExecuteNonQuery();

                            MessageBox.Show($"Dentist '{dentistName}' has been deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadDentists();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting dentist: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public void LoadDentists()
        {
            const string query = @"
                SELECT
                    dentist_id,
                    first_name + ' ' + COALESCE(' ' + NULLIF(middle_initial, ' '), '') + '. '
                        + last_name
                        + CASE WHEN suffix IS NOT NULL AND LTRIM(RTRIM(suffix)) <> '' THEN ' ' + suffix ELSE '' END
                        AS Dentist,
                    gender,
                    specialization,
                    email,
                    available_days
                FROM dbo.dentists
                ORDER BY last_name, first_name;";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var da = new SqlDataAdapter(query, conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["available_days"] != DBNull.Value)
                        {
                            string availableDays = row["available_days"].ToString();
                            string[] days = availableDays.Split(',');
                            string[] abbreviatedDays = days.Select(day => dayAbbreviations.ContainsKey(day.Trim())
                                ? dayAbbreviations[day.Trim()]
                                : day.Trim()).ToArray();
                            row["available_days"] = string.Join(", ", abbreviatedDays);
                        }
                    }

                    if (!dt.Columns.Contains("dentist_display_id"))
                        dt.Columns.Add("dentist_display_id", typeof(string));

                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["dentist_id"] != DBNull.Value)
                        {
                            int id = Convert.ToInt32(row["dentist_id"]);
                            row["dentist_display_id"] = "#DT" + id.ToString("000");
                        }
                        else
                        {
                            row["dentist_display_id"] = string.Empty;
                        }
                    }

                    dataGridViewDentists.AutoGenerateColumns = false;
                    dataGridViewDentists.DataSource = dt;
                    dataGridViewDentists.AllowUserToAddRows = false;
                    dataGridViewDentists.Columns.Clear();

                    var dentistDisplayCol = new DataGridViewTextBoxColumn
                    {
                        Name = "dentist_display_id",
                        HeaderText = "Dentist ID",
                        DataPropertyName = "dentist_display_id",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                    };
                    dataGridViewDentists.Columns.Add(dentistDisplayCol);

                    dataGridViewDentists.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "Dentist",
                        HeaderText = "Name",
                        DataPropertyName = "Dentist",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                    });

                    dataGridViewDentists.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "gender",
                        HeaderText = "Gender",
                        DataPropertyName = "gender",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                    });

                    dataGridViewDentists.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "specialization",
                        HeaderText = "Specialization",
                        DataPropertyName = "specialization",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                    });

                    dataGridViewDentists.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "email",
                        HeaderText = "Email",
                        DataPropertyName = "email",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                    });

                    dataGridViewDentists.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "available_days",
                        HeaderText = "Available Days",
                        DataPropertyName = "available_days",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                    });

                    DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn
                    {
                        Name = "Edit",
                        HeaderText = "",
                        Text = "Edit",
                        UseColumnTextForButtonValue = true,
                        Width = 60
                    };
                    dataGridViewDentists.Columns.Add(editColumn);

                    DataGridViewButtonColumn deleteColumn = new DataGridViewButtonColumn
                    {
                        Name = "Delete",
                        HeaderText = "",
                        Text = "Delete",
                        UseColumnTextForButtonValue = true,
                        Width = 60
                    };
                    dataGridViewDentists.Columns.Add(deleteColumn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dentists: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {
        }

        public void LoadStaff()
        {
            const string query = @"
                SELECT 
                    staff_id,
                    first_name + ' ' +
                    COALESCE(NULLIF(middle_initial, ' ') + '. ', '')
                    + last_name
                    + CASE WHEN suffix IS NOT NULL AND LTRIM(RTRIM(suffix)) <> '' THEN ' ' + suffix ELSE '' END
                    AS Name,
                    gender,
                    email
                FROM dbo.staff
                ORDER BY last_name, first_name;";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var da = new SqlDataAdapter(query, conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (!dt.Columns.Contains("staff_display_id"))
                        dt.Columns.Add("staff_display_id", typeof(string));

                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["staff_id"] != DBNull.Value)
                        {
                            int id = Convert.ToInt32(row["staff_id"]);
                            row["staff_display_id"] = "#ST" + id.ToString("000");
                        }
                        else
                        {
                            row["staff_display_id"] = string.Empty;
                        }
                    }

                    dataGridViewStaff.AutoGenerateColumns = false;
                    dataGridViewStaff.DataSource = dt;
                    dataGridViewStaff.AllowUserToAddRows = false;
                    dataGridViewStaff.Columns.Clear();

                    var staffDisplayCol = new DataGridViewTextBoxColumn
                    {
                        Name = "staff_display_id",
                        HeaderText = "Staff ID",
                        DataPropertyName = "staff_display_id",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                    };
                    dataGridViewStaff.Columns.Add(staffDisplayCol);

                    dataGridViewStaff.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "Name",
                        HeaderText = "Name",
                        DataPropertyName = "Name",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                    });

                    dataGridViewStaff.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "gender",
                        HeaderText = "Gender",
                        DataPropertyName = "gender",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                    });

                    dataGridViewStaff.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "email",
                        HeaderText = "Email",
                        DataPropertyName = "email",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                    });

                    DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn
                    {
                        Name = "Edit",
                        HeaderText = "",
                        Text = "Edit",
                        UseColumnTextForButtonValue = true,
                        Width = 60
                    };
                    dataGridViewStaff.Columns.Add(editColumn);

                    DataGridViewButtonColumn deleteColumn = new DataGridViewButtonColumn
                    {
                        Name = "Delete",
                        HeaderText = "",
                        Text = "Delete",
                        UseColumnTextForButtonValue = true,
                        Width = 60
                    };
                    dataGridViewStaff.Columns.Add(deleteColumn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading staff: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Add_Staff addStaffForm = new Add_Staff();
            addStaffForm.ShowDialog();
            LoadStaff();
        }

        private void dataGridViewStaff_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataRowView rowView = dataGridViewStaff.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (rowView == null)
                return;

            int staffId = Convert.ToInt32(rowView["staff_id"]);
            string staffName = rowView["Name"].ToString();

            if (e.ColumnIndex == dataGridViewStaff.Columns["Edit"].Index)
            {
                Edit_Staff editForm = new Edit_Staff(staffId);
                editForm.ShowDialog();
                LoadStaff();
            }
            else if (e.ColumnIndex == dataGridViewStaff.Columns["Delete"].Index)
            {
                if (!CanDeleteStaff(staffId, out string errorMessage, out int conflictCount))
                {
                    MessageBox.Show(errorMessage, "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirmResult = MessageBox.Show(
                    $"Are you sure you want to delete staff member '{staffName}'?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM staff WHERE staff_id = @StaffId", conn))
                        {
                            cmd.Parameters.AddWithValue("@StaffId", staffId);
                            conn.Open();
                            cmd.ExecuteNonQuery();

                            MessageBox.Show($"Staff member '{staffName}' has been deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadStaff();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting staff member: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void button3_Click(object sender, EventArgs e)
        {
            Patients patients = new Patients();
            patients.Show();
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

        private void button10_Click(object sender, EventArgs e)
        {
            var dr = MessageBox.Show("Are you sure you want to log out?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            try
            {
                var login = new Log_in();
                login.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open login screen: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}