using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Services : Form
    {
        private int selectedServiceId = -1; // Store selected service ID

        public Services()
        {
            InitializeComponent();
            LoadServicesData();

            this.WindowState = FormWindowState.Maximized;

            dataGridViewServices.AllowUserToAddRows = false;

            // Make only the column headers bold and keep cell text regular
            dataGridViewServices.EnableHeadersVisualStyles = false;
            dataGridViewServices.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridViewServices.Font, FontStyle.Bold);
            dataGridViewServices.DefaultCellStyle.Font = new Font(dataGridViewServices.Font, FontStyle.Regular);

            // Ensure single subscription to prevent the handler running multiple times
            dataGridViewServices.CellContentClick -= dataGridViewServices_CellContentClick_1;
            dataGridViewServices.CellContentClick += dataGridViewServices_CellContentClick_1;

            // Ensure single subscription for formatting the Price column to show peso sign
            dataGridViewServices.CellFormatting -= DataGridViewServices_CellFormatting;
            dataGridViewServices.CellFormatting += DataGridViewServices_CellFormatting;
        }

        private void LoadServicesData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

            string query = "SELECT service_id, name, category, price, description, duration_minutes FROM services";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
            {
                DataTable dt = new DataTable();
                try
                {
                    adapter.Fill(dt);
                    dataGridViewServices.DataSource = dt;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading services: " + ex.Message);
                }

                // Remove old action/edit/delete columns if they exist to avoid duplicates
                if (dataGridViewServices.Columns.Contains("Edit"))
                    dataGridViewServices.Columns.Remove("Edit");
                if (dataGridViewServices.Columns.Contains("Delete"))
                    dataGridViewServices.Columns.Remove("Delete");

                // Add Edit button column
                DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn();
                editColumn.Name = "Edit";
                editColumn.HeaderText = "";
                editColumn.Text = "Edit";
                editColumn.UseColumnTextForButtonValue = true;
                editColumn.Width = 60;
                dataGridViewServices.Columns.Add(editColumn);

                // Add Delete button column
                DataGridViewButtonColumn deleteColumn = new DataGridViewButtonColumn();
                deleteColumn.Name = "Delete";
                deleteColumn.HeaderText = "";
                deleteColumn.Text = "Delete";
                deleteColumn.UseColumnTextForButtonValue = true;
                deleteColumn.Width = 60;
                dataGridViewServices.Columns.Add(deleteColumn);

                // Hide service_id column if present
                if (dataGridViewServices.Columns.Contains("service_id"))
                {
                    dataGridViewServices.Columns["service_id"].Visible = false;
                }

                // Rename headers
                if (dataGridViewServices.Columns.Contains("name"))
                    dataGridViewServices.Columns["name"].HeaderText = "Service Name";
                if (dataGridViewServices.Columns.Contains("category"))
                    dataGridViewServices.Columns["category"].HeaderText = "Category";
                if (dataGridViewServices.Columns.Contains("price"))
                {
                    dataGridViewServices.Columns["price"].HeaderText = "Price";
                    dataGridViewServices.Columns["price"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
                if (dataGridViewServices.Columns.Contains("description"))
                    dataGridViewServices.Columns["description"].HeaderText = "Description";
                if (dataGridViewServices.Columns.Contains("duration_minutes"))
                    dataGridViewServices.Columns["duration_minutes"].HeaderText = "Duration (min)";
            }
        }

        // Formats price cells to include the peso sign while keeping underlying value numeric
        private void DataGridViewServices_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = sender as DataGridView;
            if (grid == null) return;
            var col = grid.Columns[e.ColumnIndex];
            if (col == null) return;

            if (string.Equals(col.Name, "price", StringComparison.OrdinalIgnoreCase))
            {
                if (e.Value != null && e.Value != DBNull.Value)
                {
                    decimal val;
                    if (decimal.TryParse(e.Value.ToString(), out val))
                    {
                        e.Value = "₱" + val.ToString("N2");
                        e.FormattingApplied = true;
                    }
                }
            }
        }

        private void btnNewService_Click(object sender, EventArgs e)
        {
            Add_Services add_Service = new Add_Services();
            add_Service.Show();

            this.Close();

        }

        private void dataGridViewServices_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var clickedColumn = dataGridViewServices.Columns[e.ColumnIndex].Name;

                // Edit button clicked
                if (clickedColumn == "Edit")
                {
                    int serviceId = Convert.ToInt32(dataGridViewServices.Rows[e.RowIndex].Cells["service_id"].Value);
                    //Open your edit form(uncomment and adjust when Edit_Services implementation exists)
                    Edit_Services editForm = new Edit_Services(serviceId);
                    editForm.Show();
                    this.Close();
                    return;
                }


                // Delete button clicked
                if (clickedColumn == "Delete")
                {
                    int serviceId = Convert.ToInt32(dataGridViewServices.Rows[e.RowIndex].Cells["service_id"].Value);

                    var result = MessageBox.Show(
                        "Are you sure you want to delete this service?",
                        "Confirm Delete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings["DentalClinicConnection"].ConnectionString;

                        string query = "DELETE FROM services WHERE service_id = @ServiceId";

                        using (SqlConnection conn = new SqlConnection(connectionString))
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@ServiceId", serviceId);

                            try
                            {
                                conn.Open();
                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Service deleted successfully.");
                                    LoadServicesData(); // Refresh the grid
                                }
                                else
                                {
                                    MessageBox.Show("Service could not be deleted. It may have already been removed.");
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error deleting service: " + ex.Message);
                            }
                        }
                    }
                    return;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Appointments appointments = new Appointments();
            appointments.Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Patients patients = new Patients();
            patients.Show();
            this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Staff staff = new Staff();
            staff.Show();
            this.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Adding_Patient adding_Patient = new Adding_Patient();
            adding_Patient.Show();

        }

        private void button7_Click(object sender, EventArgs e)
        {
            Add_Appointment add_Appointment = new Add_Appointment();
            add_Appointment.Show();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Appointment_Status appointment_Status = new Appointment_Status();
            appointment_Status.Show();
            this.Hide();
        }
    }
}