
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
        string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

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

        private void btnNewService_Click(object sender, EventArgs e)
        {
            Add_Dentist addDentistForm = new Add_Dentist();
            addDentistForm.ShowDialog();

            // Refresh the grid after the dialog is closed (new dentist may have been added)
            LoadDentists();
        }

        private void dataGridViewDentists_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            // Get dentist_id from the underlying DataTable row
            DataRowView rowView = dataGridViewDentists.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (rowView == null)
                return;

            int dentistId = Convert.ToInt32(rowView["dentist_id"]);

            // Edit button clicked
            if (e.ColumnIndex == dataGridViewDentists.Columns["Edit"].Index)
            {
                Edit_Dentist editForm = new Edit_Dentist(dentistId);
                editForm.ShowDialog();
                LoadDentists(); // Refresh after edit
            }
            // Delete button clicked
            else if (e.ColumnIndex == dataGridViewDentists.Columns["Delete"].Index)
            {
                var confirmResult = MessageBox.Show(
                    "Are you sure you want to delete this dentist?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.Yes)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM dentists WHERE dentist_id = @DentistId", conn))
                    {
                        cmd.Parameters.AddWithValue("@DentistId", dentistId);
                        try
                        {
                            conn.Open();
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Dentist deleted successfully.");
                            LoadDentists(); // Refresh the grid
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error deleting dentist: " + ex.Message);
                        }
                    }
                }
            }
        }

        // Loads dentists and displays only: formatted display id, Dentist (first last [mi] [suffix]), gender, specialization, email, available_days
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

                    // Convert available_days to abbreviated form
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

                    // Add a display-only column that preserves the original integer dentist_id
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

                    // Clear existing columns
                    dataGridViewDentists.Columns.Clear();

                    // Visible formatted display column
                    var dentistDisplayCol = new DataGridViewTextBoxColumn
                    {
                        Name = "dentist_display_id",
                        HeaderText = "Dentist ID",
                        DataPropertyName = "dentist_display_id",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                    };
                    dataGridViewDentists.Columns.Add(dentistDisplayCol);

                    // Add other visible data columns
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

                    // Add Edit button column
                    DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn
                    {
                        Name = "Edit",
                        HeaderText = "",
                        Text = "Edit",
                        UseColumnTextForButtonValue = true,
                        Width = 60
                    };
                    dataGridViewDentists.Columns.Add(editColumn);

                    // Add Delete button column
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

        // Load staff with formatted display id (#STxxx) while keeping underlying staff_id integer
        // Load staff with formatted display id (#STxxx) while keeping underlying staff_id integer
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

                    // Add display-only staff_display_id column
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

                    // Clear all columns first
                    dataGridViewStaff.Columns.Clear();

                    // Visible formatted staff display column
                    var staffDisplayCol = new DataGridViewTextBoxColumn
                    {
                        Name = "staff_display_id",
                        HeaderText = "Staff ID",
                        DataPropertyName = "staff_display_id",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                    };
                    dataGridViewStaff.Columns.Add(staffDisplayCol);

                    // Name
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

                    // Add Edit button column
                    DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn
                    {
                        Name = "Edit",
                        HeaderText = "",
                        Text = "Edit",
                        UseColumnTextForButtonValue = true,
                        Width = 60
                    };
                    dataGridViewStaff.Columns.Add(editColumn);

                    // Add Delete button column
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

            // Get underlying integer staff_id from the DataTable row
            DataRowView rowView = dataGridViewStaff.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (rowView == null)
                return;

            int staffId = Convert.ToInt32(rowView["staff_id"]);

            // Edit button clicked
            if (e.ColumnIndex == dataGridViewStaff.Columns["Edit"].Index)
            {
                Edit_Staff editForm = new Edit_Staff(staffId);
                editForm.ShowDialog();
                LoadStaff(); // Refresh after edit
            }
            // Delete button clicked
            else if (e.ColumnIndex == dataGridViewStaff.Columns["Delete"].Index)
            {
                var confirmResult = MessageBox.Show(
                    "Are you sure you want to delete this staff member?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.Yes)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM staff WHERE staff_id = @StaffId", conn))
                    {
                        cmd.Parameters.AddWithValue("@StaffId", staffId);
                        try
                        {
                            conn.Open();
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Staff member deleted successfully.");
                            LoadStaff(); // Refresh the grid
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error deleting staff member: " + ex.Message);
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
            Appointment_Status appointment_Status = new Appointment_Status();
            appointment_Status.Show();
            this.Hide();
        }
    }
}