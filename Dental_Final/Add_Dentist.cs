using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Add_Dentist : Form
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;


        public Add_Dentist()
        {
            InitializeComponent();
            // wire submit button in case it's not wired in the designer
            this.button1.Click += button1_Click;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!ValidateForm())
                return;

            SaveDentist();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Please enter First Name and Last Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (dtpBirthDate.Value.Date >= DateTime.Now.Date)
            {
                MessageBox.Show("Please enter a valid birth date.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void SaveDentist()
        {
            // Collect availability days
            var days = checkedListBox1.CheckedItems.Cast<string>().ToArray();
            string availableDays = days.Length > 0 ? string.Join(",", days) : null;

            // Normalize phone: prefix +63 if not present and not empty
            string phoneRaw = txtPhone.Text.Trim();
            string phone = null;
            if (!string.IsNullOrWhiteSpace(phoneRaw))
            {
                phone = phoneRaw.StartsWith("+") ? phoneRaw : "+63" + phoneRaw;
            }

            // Middle initial: take first char if provided
            string middleInitial = null;
            if (!string.IsNullOrWhiteSpace(txtMiddleInitial.Text))
                middleInitial = txtMiddleInitial.Text.Trim().Substring(0, 1);

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO dbo.dentists
                        (first_name, last_name, middle_initial, suffix, gender, specialization, email, phone, bio, available_days, birthdate)
                    OUTPUT INSERTED.dentist_id
                    VALUES
                        (@FirstName, @LastName, @MiddleInitial, @Suffix, @Gender, @Specialization, @Email, @Phone, @Bio, @AvailableDays, @Birthdate)";

                cmd.Parameters.AddWithValue("@FirstName", txtFirstName.Text.Trim());
                cmd.Parameters.AddWithValue("@LastName", txtLastName.Text.Trim());
                cmd.Parameters.AddWithValue("@MiddleInitial", (object)middleInitial ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Suffix", string.IsNullOrWhiteSpace(txtSuffix.Text) ? (object)DBNull.Value : txtSuffix.Text.Trim());
                cmd.Parameters.AddWithValue("@Gender", string.IsNullOrWhiteSpace(cmbGender.Text) ? (object)DBNull.Value : cmbGender.Text.Trim());
                cmd.Parameters.AddWithValue("@Specialization", string.IsNullOrWhiteSpace(textBox1.Text) ? (object)DBNull.Value : textBox1.Text.Trim());
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text.Trim());
                cmd.Parameters.AddWithValue("@Phone", (object)phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Bio", string.IsNullOrWhiteSpace(txtAddress.Text) ? (object)DBNull.Value : txtAddress.Text.Trim());
                cmd.Parameters.AddWithValue("@AvailableDays", string.IsNullOrWhiteSpace(availableDays) ? (object)DBNull.Value : availableDays);
                cmd.Parameters.AddWithValue("@Birthdate", dtpBirthDate.Value.Date);

                try
                {
                    conn.Open();
                    // existing behavior used ExecuteNonQuery; keep insert and still return success message
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Dentist saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Log activity (swallow logging errors)
                    try
                    {
                        var fullName = (txtFirstName.Text.Trim() + " " + txtLastName.Text.Trim()).Trim();
                        ActivityLogger.Log($"Admin added dentist '{fullName}'");
                    }
                    catch { }

                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving dentist: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}