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
    public partial class Patients : Form
    {
        private int selectedPatientId;

        public Patients()
        {
            InitializeComponent();
            LoadPatients();
            dataGridViewPatients.AllowUserToAddRows = false;
        }

        private void LoadPatients()
        {
            string connectionString = @"Server=DESKTOP-PB8NME4\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
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

                // Add Actions column if not already added
                if (!dataGridViewPatients.Columns.Contains("Actions"))
                {
                    DataGridViewButtonColumn actionsColumn = new DataGridViewButtonColumn();
                    actionsColumn.Name = "Actions";
                    actionsColumn.HeaderText = "Actions";
                    actionsColumn.Text = "⋮"; // Unicode for vertical ellipsis
                    actionsColumn.UseColumnTextForButtonValue = true;
                    actionsColumn.Width = 60;
                    dataGridViewPatients.Columns.Add(actionsColumn);
                }
            }
        }

        private int CalculateAge(DateTime birthDate)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            if (DateTime.Now.DayOfYear < birthDate.DayOfYear)
                age--;
            return age;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Adding_Patient adding = new Adding_Patient();
            adding.Show();
            this.Hide();
        }

        private void searchTerm_Click(object sender, EventArgs e)
        {
            LoadPatients();
        }

        private void dataGridViewPatients_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridViewPatients.Columns["Actions"].Index && e.RowIndex >= 0)
            {
                // Get the selected patient_id (not PatientID, which is formatted)
                var patientIdRaw = dataGridViewPatients.Rows[e.RowIndex].Cells["patient_id"].Value;
                if (patientIdRaw != null && int.TryParse(patientIdRaw.ToString(), out int pid))
                {
                    selectedPatientId = pid;
                    contextMenuActions.Show(Cursor.Position);
                }
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Fetch patient data from the database
            string connectionString = @"Server=DESKTOP-PB8NME4\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
            string query = @"SELECT * FROM patients WHERE patient_id = @PatientId";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@PatientId", selectedPatientId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Pass data to Adding_Patient form (assume you have a constructor or properties for this)
                        Adding_Patient editForm = new Adding_Patient();
                        // Example: set properties or call a method to populate fields
                        editForm.SetPatientData(reader); // You need to implement this method in Adding_Patient
                        editForm.Show();
                        this.Hide();
                    }
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Permanently delete data?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                string connectionString = @"Server=DESKTOP-PB8NME4\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
                string query = @"DELETE FROM patients WHERE patient_id = @PatientId";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PatientId", selectedPatientId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                LoadPatients(); // Refresh the grid
            }
        }
    }
}
