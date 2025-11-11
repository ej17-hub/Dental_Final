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
    public partial class Add_Staff : Form
    {
        private string connectionString = "Server=DESKTOP-PB8NME4\\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";


        public Add_Staff()
        {
            InitializeComponent();
        }

        private void SaveStaffToDatabase()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO dbo.staff (first_name, last_name, middle_initial, suffix, gender, email, phone , birth_date) VALUES (@FirstName, @LastName, @MiddleInitial, @Suffix, @Gender, @Email, @phone, @BirthDate)", conn))
                    {
                        // Assuming you have TextBox controls with these names
                        cmd.Parameters.AddWithValue("@FirstName", txtFirstName.Text);
                        cmd.Parameters.AddWithValue("@LastName", txtLastName.Text);
                        cmd.Parameters.AddWithValue("@MiddleInitial", string.IsNullOrEmpty(txtMiddleInitial.Text) ? (object)DBNull.Value : txtMiddleInitial.Text);
                        cmd.Parameters.AddWithValue("@Suffix", string.IsNullOrEmpty(txtSuffix.Text) ? (object)DBNull.Value : txtSuffix.Text);
                        cmd.Parameters.AddWithValue("@Gender", string.IsNullOrEmpty(cboGender.Text) ? (object)DBNull.Value : cboGender.Text);
                        cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text);
                        cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(txtPhone.Text) ? (object)DBNull.Value : txtPhone.Text);
                        cmd.Parameters.AddWithValue("@BirthDate", dtpBirthDate.Value);

                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Staff information saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Log activity (swallow logging errors)
                        try
                        {
                            var fullName = (txtFirstName.Text.Trim() + " " + txtLastName.Text.Trim()).Trim();
                            ActivityLogger.Log($"Admin added staff '{fullName}'");
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving staff information: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveStaffToDatabase();
            this.Close();
        }
    }
}