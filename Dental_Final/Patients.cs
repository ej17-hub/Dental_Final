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
            string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
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
                if (dataGridViewPatients.Columns.Contains("Edit"))
                    dataGridViewPatients.Columns.Remove("Edit");
                if (dataGridViewPatients.Columns.Contains("Delete"))
                    dataGridViewPatients.Columns.Remove("Delete");
                if (dataGridViewPatients.Columns.Contains("Actions"))
                    dataGridViewPatients.Columns.Remove("Actions");

                // Add Edit button column
                DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn();
                editColumn.Name = "Edit";
                editColumn.HeaderText = "";
                editColumn.Text = "Edit";
                editColumn.UseColumnTextForButtonValue = true;
                editColumn.Width = 60;
                dataGridViewPatients.Columns.Add(editColumn);

                // Add Delete button column
                DataGridViewButtonColumn deleteColumn = new DataGridViewButtonColumn();
                deleteColumn.Name = "Delete";
                deleteColumn.HeaderText = "";
                deleteColumn.Text = "Delete";
                deleteColumn.UseColumnTextForButtonValue = true;
                deleteColumn.Width = 60;
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

            // Get patient_id for the selected row
            int patientId = Convert.ToInt32(dataGridViewPatients.Rows[e.RowIndex].Cells["patient_id"].Value);

            // Edit button logic
            if (e.ColumnIndex == dataGridViewPatients.Columns["Edit"].Index)
            {
                Edit_Patient editP = new Edit_Patient(patientId);
                editP.Show();
            }
            // Delete button logic
            else if (e.ColumnIndex == dataGridViewPatients.Columns["Delete"].Index)
            {
                var confirmResult = MessageBox.Show(
                    "Are you sure you want to delete this patient?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.Yes)
                {
                    string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
                    string query = "DELETE FROM patients WHERE patient_id = @PatientId";

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", patientId);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Patient deleted successfully.");
                    LoadPatients();
                }
            }
        }
    }
}