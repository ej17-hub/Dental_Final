using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Staff : Form
    {
        private readonly string connectionString = "Server=DESKTOP-O65C6K9\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";

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

            // Get dentist_id from the selected row
            int dentistId = Convert.ToInt32(dataGridViewDentists.Rows[e.RowIndex].Cells["dentist_id"].Value);

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

        // Loads dentists and displays only: dentist_id, Dentist (first last [mi] [suffix]), gender, specialization, email, available_days
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

                    dataGridViewDentists.AutoGenerateColumns = true;
                    dataGridViewDentists.DataSource = dt;
                    dataGridViewDentists.AllowUserToAddRows = false;

                    // Remove old action columns if they exist
                    if (dataGridViewDentists.Columns.Contains("Edit"))
                        dataGridViewDentists.Columns.Remove("Edit");
                    if (dataGridViewDentists.Columns.Contains("Delete"))
                        dataGridViewDentists.Columns.Remove("Delete");

                    // Add Edit button column
                    DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn();
                    editColumn.Name = "Edit";
                    editColumn.HeaderText = "";
                    editColumn.Text = "Edit";
                    editColumn.UseColumnTextForButtonValue = true;
                    editColumn.Width = 60;
                    dataGridViewDentists.Columns.Add(editColumn);

                    // Add Delete button column
                    DataGridViewButtonColumn deleteColumn = new DataGridViewButtonColumn();
                    deleteColumn.Name = "Delete";
                    deleteColumn.HeaderText = "";
                    deleteColumn.Text = "Delete";
                    deleteColumn.UseColumnTextForButtonValue = true;
                    deleteColumn.Width = 60;
                    dataGridViewDentists.Columns.Add(deleteColumn);

                    // Ensure desired columns exist and set headers/order
                    if (dataGridViewDentists.Columns.Contains("dentist_id"))
                    {
                        dataGridViewDentists.Columns["dentist_id"].HeaderText = "Dentist ID";
                        dataGridViewDentists.Columns["dentist_id"].DisplayIndex = 0;
                        dataGridViewDentists.Columns["dentist_id"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }

                    if (dataGridViewDentists.Columns.Contains("Dentist"))
                    {
                        dataGridViewDentists.Columns["Dentist"].HeaderText = "Name";
                        dataGridViewDentists.Columns["Dentist"].DisplayIndex = 1;
                        dataGridViewDentists.Columns["Dentist"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }

                    if (dataGridViewDentists.Columns.Contains("gender"))
                    {
                        dataGridViewDentists.Columns["gender"].HeaderText = "Gender";
                        dataGridViewDentists.Columns["gender"].DisplayIndex = 2;
                        dataGridViewDentists.Columns["gender"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }

                    if (dataGridViewDentists.Columns.Contains("specialization"))
                    {
                        dataGridViewDentists.Columns["specialization"].HeaderText = "Specialization";
                        dataGridViewDentists.Columns["specialization"].DisplayIndex = 3;
                        dataGridViewDentists.Columns["specialization"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }

                    if (dataGridViewDentists.Columns.Contains("email"))
                    {
                        dataGridViewDentists.Columns["email"].HeaderText = "Email";
                        dataGridViewDentists.Columns["email"].DisplayIndex = 4;
                        dataGridViewDentists.Columns["email"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }

                    if (dataGridViewDentists.Columns.Contains("available_days"))
                    {
                        dataGridViewDentists.Columns["available_days"].HeaderText = "Available Days";
                        dataGridViewDentists.Columns["available_days"].DisplayIndex = 5;
                        dataGridViewDentists.Columns["available_days"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }

                    if (dataGridViewDentists.Columns.Contains("Edit"))
                    {
                        dataGridViewDentists.Columns["Edit"].DisplayIndex = 6; // Or whatever position you prefer
                    }

                    if (dataGridViewDentists.Columns.Contains("Delete"))
                    {
                        dataGridViewDentists.Columns["Delete"].DisplayIndex = 7; // Or whatever position you prefer
                    }

                    // Remove any other auto-generated columns
                    var keep = new[] { "dentist_id", "Dentist", "gender", "specialization", "email", "available_days", "Edit", "Delete" };
                    foreach (DataGridViewColumn col in dataGridViewDentists.Columns.Cast<DataGridViewColumn>().ToList())
                    {
                        if (!keep.Contains(col.Name))
                            dataGridViewDentists.Columns.Remove(col.Name);
                    }
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

                    dataGridViewStaff.AutoGenerateColumns = false;
                    dataGridViewStaff.DataSource = dt;
                    dataGridViewStaff.AllowUserToAddRows = false;

                    // Clear all columns first
                    dataGridViewStaff.Columns.Clear();

                    // Add data columns
                    dataGridViewStaff.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = "staff_id",
                        HeaderText = "Staff ID",
                        DataPropertyName = "staff_id",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                    });

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

            int staffId = Convert.ToInt32(dataGridViewStaff.Rows[e.RowIndex].Cells["staff_id"].Value);

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
            //appointments
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Patients patients = new Patients();
            patients.Show();
            this.Hide();

        }

        private void button5_Click(object sender, EventArgs e)
        {
            Staff staff = new Staff();
            staff.Show();
            this.Hide();

        }
    }
}
