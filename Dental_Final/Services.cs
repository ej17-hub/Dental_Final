using System;
using System.Data;
using System.Data.SqlClient;
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
            dataGridViewServices.AllowUserToAddRows = false;
            dataGridViewServices.CellContentClick += dataGridViewServices_CellContentClick;
            dataGridViewServices.ContextMenuStrip = contextMenuStrip1;
        }

        private void LoadServicesData()
        {
            string connectionString = @"Server=DESKTOP-O65C6K9\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
            string query = "SELECT service_id, name, price, description, duration_minutes FROM services";

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
                // Add Actions column if not already added
                if (!dataGridViewServices.Columns.Contains("Actions"))
                {
                    DataGridViewButtonColumn actionsColumn = new DataGridViewButtonColumn();
                    actionsColumn.Name = "Actions";
                    actionsColumn.HeaderText = "Actions";
                    actionsColumn.Text = "⋮";
                    actionsColumn.UseColumnTextForButtonValue = true;
                    actionsColumn.Width = 60;
                    dataGridViewServices.Columns.Add(actionsColumn);
                }

                // Hide service_id column
                if (dataGridViewServices.Columns.Contains("service_id"))
                {
                    dataGridViewServices.Columns["service_id"].Visible = false;
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Add_Service add_Service = new Add_Service();
            add_Service.Show();
            this.Hide();
        }

        // Handle click on Actions button
        private void dataGridViewServices_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridViewServices.Columns[e.ColumnIndex].Name == "Actions")
            {
                dataGridViewServices.ClearSelection();
                dataGridViewServices.Rows[e.RowIndex].Selected = true;
                selectedServiceId = Convert.ToInt32(dataGridViewServices.Rows[e.RowIndex].Cells["service_id"].Value);

                // Show context menu at cell position
                var cellRect = dataGridViewServices.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                var cellLocation = dataGridViewServices.PointToScreen(cellRect.Location);
                contextMenuStrip1.Show(cellLocation.X, cellLocation.Y + cellRect.Height);
            }
        }

        private void editToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (selectedServiceId != -1)
            {
                Edit_Services editForm = new Edit_Services(selectedServiceId);
                editForm.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Please select a service to edit.");
            }
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (selectedServiceId == -1)
            {
                MessageBox.Show("Please select a service to delete.");
                return;
            }

            // Show confirmation dialog
            var result = MessageBox.Show(
                "Are you sure you want to delete this service?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                string connectionString = @"Server=DESKTOP-O65C6K9\SQLEXPRESS;Database=dental_final_clinic;Trusted_Connection=True;";
                string query = "DELETE FROM services WHERE service_id = @ServiceId";

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ServiceId", selectedServiceId);

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
        }
    }
}


