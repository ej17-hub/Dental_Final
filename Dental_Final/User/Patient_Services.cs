using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Patient_Services : Form
    {
        // Use the same fixed connection string as other forms for stability
        private readonly string connectionString = @"Server=DESKTOP-O65C6K9\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        public Patient_Services()
        {
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();

            // wire events
            comboBox1.SelectedIndexChanged -= ComboBox1_SelectedIndexChanged;
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;

            // load categories and initial services
            LoadCategories();
            LoadServicesForSelectedCategory();
        }


        private void button3_Click(object sender, EventArgs e)
        {
            // Open Settings and pass PatientId if available (Patient_Settings expects PatientId property)
            Patient_Settings patient_Settings = new Patient_Settings();
            if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pid))
                patient_Settings.PatientId = pid;
            patient_Settings.Show();
            this.Hide();
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadServicesForSelectedCategory();
        }

        private void LoadCategories()
        {
            try
            {
                comboBox1.Items.Clear();

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("SELECT DISTINCT ISNULL(category,'') AS category FROM services ORDER BY ISNULL(category,'')", conn))
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        // add "All" first
                        comboBox1.Items.Add("All");

                        while (rdr.Read())
                        {
                            var cat = rdr["category"] != DBNull.Value ? rdr["category"].ToString() : string.Empty;
                            if (string.IsNullOrWhiteSpace(cat))
                                continue;
                            comboBox1.Items.Add(cat);
                        }
                    }
                }

                // default selection
                if (comboBox1.Items.Count > 0)
                    comboBox1.SelectedItem = "All";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load categories: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadServicesForSelectedCategory()
        {
            var selected = comboBox1.SelectedItem != null ? comboBox1.SelectedItem.ToString() : "All";
            LoadServices(selected);
        }

        private void LoadServices(string category)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            // create grid columns
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "Service", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "description", HeaderText = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "price", HeaderText = "Price", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "duration", HeaderText = "Duration (min)", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            try
            {
                string sql;
                if (string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
                {
                    sql = @"SELECT name, description, price, duration_minutes FROM services ORDER BY name";
                }
                else
                {
                    sql = @"SELECT name, description, price, duration_minutes FROM services
                            WHERE ISNULL(category,'') = @cat ORDER BY name";
                }

                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
                        cmd.Parameters.AddWithValue("@cat", category);

                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var name = rdr["name"] != DBNull.Value ? rdr["name"].ToString() : string.Empty;
                            var desc = rdr["description"] != DBNull.Value ? rdr["description"].ToString() : string.Empty;
                            var priceText = rdr["price"] != DBNull.Value ? "₱" + Convert.ToDecimal(rdr["price"]).ToString("N2") : string.Empty;
                            var duration = rdr["duration_minutes"] != DBNull.Value ? rdr["duration_minutes"].ToString() : string.Empty;

                            dataGridView1.Rows.Add(name, desc, priceText, duration);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load services: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var dr = MessageBox.Show("Are you sure you want to log out?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            try
            {
                var login = new Log_in();
                login.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open login screen: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            // Button -> Dashboard (forward Tag)
            var pd = new Patient_Dashboard();
            if (this.Tag != null)
                pd.Tag = this.Tag;
            pd.Show();
            this.Hide();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // Opening Appointments from Services (already fixed to use this.Tag)
            var patient_Appointments = new Patient_Appointments();
            if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pid))
                patient_Appointments.Tag = pid;
            patient_Appointments.Show();
            this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Open Settings and pass PatientId via Patient_Settings.PatientId
            Patient_Settings patient_Settings = new Patient_Settings();
            if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pid))
                patient_Settings.PatientId = pid;
            patient_Settings.Show();
            this.Hide();
        }
    }
}