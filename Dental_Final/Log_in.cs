using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Log_in : Form
    {
        // Keep using the same stable connection string used elsewhere
        private readonly string connectionString = @"Server=FANGON\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        public Log_in()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            // preserve admin / user backdoors
            if (email == "admin" && password == "admin123")
            {
                Dashboard dashboard = new Dashboard();
                dashboard.Show();
                this.Hide();
                return;
            }
            if (email == "user" && password == "user123")
            {
                Patient_Dashboard patient_Dashboard = new Patient_Dashboard();
                patient_Dashboard.Show();
                this.Hide();
                return;
            }

            // validate input
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter email and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("SELECT patient_id, ISNULL(password_hash,'') AS password_hash FROM patients WHERE LOWER(ISNULL(email,'')) = LOWER(@email)", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (!rdr.Read())
                        {
                            MessageBox.Show("Invalid email or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        var dbPasswordHash = rdr["password_hash"] != DBNull.Value ? rdr["password_hash"].ToString() : string.Empty;
                        var patientIdObj = rdr["patient_id"];
                        int patientId = (patientIdObj != DBNull.Value) ? Convert.ToInt32(patientIdObj) : -1;

                        // compute hash of entered password and compare
                        string enteredHash = ComputeSha256Hash(password);

                        if (string.Equals(dbPasswordHash, enteredHash, StringComparison.OrdinalIgnoreCase))
                        {
                            // successful patient login
                            var patientDashboard = new Patient_Dashboard();
                            // pass patient id through Tag (or implement a proper property on Patient_Dashboard)
                            patientDashboard.Tag = patientId;
                            patientDashboard.Show();
                            this.Hide();
                            return;
                        }
                        else
                        {
                            MessageBox.Show("Invalid email or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtEmail_Enter(object sender, EventArgs e)
        {
            if (txtEmail.Text == "Email")
            {
                txtEmail.Text = "";
                txtEmail.ForeColor = Color.Black;
            }
        }
        private void txtEmail_Leave(object sender, EventArgs e)
        {
            if (txtEmail.Text == "")
            {
                txtEmail.Text = "Email";
                txtEmail.ForeColor = Color.LightGray;
            }

        }

        private void txtPassword_Enter(object sender, EventArgs e)
        {
            if (txtPassword.Text == "Password")
            {
                txtPassword.Text = "";
                txtPassword.ForeColor = Color.Black;
                txtPassword.UseSystemPasswordChar = true;
            }
        }

        private void txtPassword_Leave(object sender, EventArgs e)
        {
            if (txtPassword.Text == "")
            {
                txtPassword.Text = "Password";
                txtPassword.ForeColor = Color.LightGray;
                txtPassword.UseSystemPasswordChar = false;
            }
        }

        private void Log_in_Load(object sender, EventArgs e)
        {
            txtEmail.Text = "Email";
            txtEmail.ForeColor = Color.LightGray;
            txtPassword.Text = "Password";
            txtPassword.ForeColor = Color.LightGray;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Patient_Login patientLogin = new Patient_Login();
            patientLogin.Show();
            this.Hide();
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