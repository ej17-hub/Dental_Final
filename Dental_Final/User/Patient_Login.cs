using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Patient_Login : Form
    {
        // Use the same stable connection string as other forms
        private readonly string connectionString = @"Server=FANGON\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        public Patient_Login()
        {
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();

            // wire the register button
            btnLogin.Click -= BtnLogin_Click;
            btnLogin.Click += BtnLogin_Click;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            // Map controls to logical fields (designer control names are a bit inconsistent)
            // label1 => First Name  -> txtEmail (designer naming mismatch)
            // label2 => Last Name   -> textBox1
            // label3 => Middle Initial -> textBox2
            // label4 => Email       -> textBox3
            // dateTimePicker1 => Birth date
            // textBox6 => Password
            // textBox7 => Confirm Password

            string firstName = txtEmail.Text.Trim();        // designer field name mismatch
            string lastName = textBox1.Text.Trim();
            string middleInitial = textBox2.Text.Trim();    // changed: now middle initial
            string email = textBox3.Text.Trim();
            string password = textBox6.Text;
            string confirm = textBox7.Text;
            DateTime? birthDate = null;

            if (dateTimePicker1 != null)
            {
                birthDate = dateTimePicker1.Value.Date;
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(firstName))
            {
                MessageBox.Show("Please enter First Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Please enter Last Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please enter Email.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter a password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (password != confirm)
            {
                MessageBox.Show("Password and Confirm Password do not match.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Check duplicate email
                    using (var dupCmd = new SqlCommand("SELECT TOP 1 patient_id FROM patients WHERE LOWER(ISNULL(email,'')) = LOWER(@email)", conn))
                    {
                        dupCmd.Parameters.AddWithValue("@email", email);
                        var existing = dupCmd.ExecuteScalar();
                        if (existing != null && existing != DBNull.Value)
                        {
                            MessageBox.Show("An account with that email already exists.", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    // Hash password (SHA-256)
                    string passwordHash = ComputeSha256Hash(password);

                    // Build INSERT - include middle_initial along with the active columns.
                    const string insertSql = @"
                        INSERT INTO patients
                            (first_name, last_name, middle_initial, email, password_hash, birth_date, created_at)
                        VALUES
                            (@first, @last, @middle, @email, @pwd, @birth, GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() AS int);";

                    using (var cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@first", string.IsNullOrWhiteSpace(firstName) ? (object)DBNull.Value : firstName);
                        cmd.Parameters.AddWithValue("@last", string.IsNullOrWhiteSpace(lastName) ? (object)DBNull.Value : lastName);
                        cmd.Parameters.AddWithValue("@middle", string.IsNullOrWhiteSpace(middleInitial) ? (object)DBNull.Value : middleInitial);
                        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
                        cmd.Parameters.AddWithValue("@pwd", string.IsNullOrWhiteSpace(passwordHash) ? (object)DBNull.Value : passwordHash);
                        if (birthDate.HasValue)
                            cmd.Parameters.Add("@birth", SqlDbType.Date).Value = birthDate.Value;
                        else
                            cmd.Parameters.AddWithValue("@birth", DBNull.Value);

                        var newIdObj = cmd.ExecuteScalar();
                        if (newIdObj != null && newIdObj != DBNull.Value)
                        {
                            int newId = Convert.ToInt32(newIdObj);
                            MessageBox.Show("Account created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            Log_in log_In = new Log_in();
                            log_In.Show();
                            this.Hide();

                            this.Close();
                            return;
                        }
                        else
                        {
                            MessageBox.Show("Failed to create account.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating account: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(rawData);
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}