using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Dental_Final
{
    public partial class Adding_Patient : Form
    {
        public Adding_Patient()
        {
            InitializeComponent();
            txtPassword.PasswordChar = '●';
        }


        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";


            // Collect data from form controls
            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();
            string middleInitial = txtMiddleInitial.Text.Trim();
            string suffix = txtSuffix.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string password = txtPassword.Text;
            string gender = cmbGender.Text;
            DateTime birthDate = dtpBirthDate.Value;
            string address = txtAddress.Text.Trim();

            // Hash the password before saving
            string passwordHash = HashPassword(password);

            string query = @"INSERT INTO patients 
                (first_name, last_name, middle_initial, suffix, email, phone, password_hash, gender, birth_date, address)
                VALUES (@FirstName, @LastName, @MiddleInitial, @Suffix, @Email, @Phone, @PasswordHash, @Gender, @BirthDate, @Address)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@MiddleInitial", (object)middleInitial ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Suffix", (object)suffix ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Phone", (object)phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@Gender", (object)gender ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BirthDate", birthDate);
                cmd.Parameters.AddWithValue("@Address", (object)address ?? DBNull.Value);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Patient information saved successfully.");

                    // Log activity (swallow logging errors)
                    try
                    {
                        var fullName = (firstName + " " + lastName).Trim();
                        ActivityLogger.Log($"Admin added patient '{fullName}'");
                    }
                    catch { }

                    Patients pats = new Patients();
                    pats.Show();
                    this.Hide();


                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        public void SetPatientData(SqlDataReader reader)
        {
            // Example: set each field from the reader
            txtFirstName.Text = reader["first_name"].ToString();
            txtLastName.Text = reader["last_name"].ToString();
            txtMiddleInitial.Text = reader["middle_initial"].ToString();
            txtSuffix.Text = reader["suffix"].ToString();
            txtEmail.Text = reader["email"].ToString();
            txtPhone.Text = reader["phone"].ToString();
            cmbGender.Text = reader["gender"].ToString();
            txtAddress.Text = reader["address"].ToString();

            // Set birth date if not null
            if (reader["birth_date"] != DBNull.Value)
                dtpBirthDate.Value = Convert.ToDateTime(reader["birth_date"]);
            else
                dtpBirthDate.Value = DateTime.Now;

            // Password is usually not shown for editing, but if you want:
            // txtPassword.Text = reader["password_hash"].ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}