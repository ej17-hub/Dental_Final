using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Edit_Patient : Form
    {
        private int _patientId; // Store the patient ID for update

        public Edit_Patient(int patientId)
        {
            InitializeComponent();
            _patientId = patientId;
            LoadPatientData();
        }

        private void LoadPatientData()
        {
            string connectionString = "Server=DESKTOP-O65C6K9\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
            string query = "SELECT * FROM patients WHERE patient_id = @PatientId";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@PatientId", _patientId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        textBoxFirstName.Text = reader["first_name"].ToString();
                        textBoxLastName.Text = reader["last_name"].ToString();
                        txtMiddleInitial.Text = reader["middle_initial"].ToString();
                        txtSuffix.Text = reader["suffix"].ToString();
                        textBoxEmail.Text = reader["email"].ToString();
                        textBoxPhone.Text = reader["phone"].ToString();
                        cmbGender.Text = reader["gender"].ToString();
                        txtAddress.Text = reader["address"].ToString();
                        if (reader["birth_date"] != DBNull.Value)
                            dateTimePickerBirthDate.Value = Convert.ToDateTime(reader["birth_date"]);
                        else
                            dateTimePickerBirthDate.Value = DateTime.Now;
                    }
                }
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            string connectionString = "Server=DESKTOP-O65C6K9\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
            string query = @"UPDATE patients SET
                first_name = @FirstName,
                last_name = @LastName,
                middle_initial = @MiddleInitial,
                suffix = @Suffix,
                email = @Email,
                phone = @Phone,
                gender = @Gender,
                birth_date = @BirthDate,
                address = @Address
                WHERE patient_id = @PatientId";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FirstName", textBoxFirstName.Text.Trim());
                cmd.Parameters.AddWithValue("@LastName", textBoxLastName.Text.Trim());
                cmd.Parameters.AddWithValue("@MiddleInitial", (object)txtMiddleInitial.Text.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Suffix", (object)txtSuffix.Text.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", textBoxEmail.Text.Trim());
                cmd.Parameters.AddWithValue("@Phone", (object)textBoxPhone.Text.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Gender", (object)cmbGender.Text ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BirthDate", dateTimePickerBirthDate.Value);
                cmd.Parameters.AddWithValue("@Address", (object)txtAddress.Text.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PatientId", _patientId);

                conn.Open();
                cmd.ExecuteNonQuery();
                MessageBox.Show("Patient information updated successfully.");
                this.Hide();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}