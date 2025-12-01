using System;
using System.Configuration;
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
            string connectionString = "Server=DESKTOP-O65C6K9\\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

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
            string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

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

                // Log activity (swallow logging errors)
                try
                {
                    var fullName = (textBoxFirstName.Text.Trim() + " " + textBoxLastName.Text.Trim()).Trim();
                    ActivityLogger.Log($"Admin updated patient '{fullName}'");
                }
                catch { }

                this.Hide();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void cmbGender_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void textBoxPhone_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtSuffix_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtMiddleInitial_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxLastName_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxFirstName_TextChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void textBoxEmail_TextChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void dateTimePickerBirthDate_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}