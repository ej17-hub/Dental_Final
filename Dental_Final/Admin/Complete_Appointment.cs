using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Complete_Appointment : Form
    {
        string connectionString = "Server=FANGON\\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        private int _appointmentId;

        public Complete_Appointment()
        {
            InitializeComponent();
        }

        // New constructor to populate fields when opened from Appointments grid
        // NOTE: appointmentId is required so this form can mark the appointment completed.
        public Complete_Appointment(
            int appointmentId,
            string patient,
            string dentist,
            string staff1,
            string staff2,
            DateTime appointmentDate,
            string services,
            decimal? totalPrice)
        {
            InitializeComponent();

            _appointmentId = appointmentId;

            label6.Text = patient ?? string.Empty;                 // Patient -> label6
            label7.Text = dentist ?? string.Empty;                 // Dentist -> label7
            label9.Text = staff1 ?? string.Empty;                  // Staff 1 -> label9
            label10.Text = staff2 ?? string.Empty;                 // Staff 2 -> label10
            label11.Text = appointmentDate != DateTime.MinValue    // Date -> label11
                ? appointmentDate.ToString("yyyy-MM-dd")
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(services))
            {
                var lines = services
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();

                // allow wrapping / multiline: limit width and let height grow
                label13.AutoSize = true;
                label13.MaximumSize = new Size(380, 0); // adjust width as needed
                label13.Text = string.Join(Environment.NewLine, lines);
            }
            else
            {
                label13.Text = string.Empty;
            }

            label14.Text = totalPrice.HasValue                     // Total Price -> label14
                ? totalPrice.Value.ToString("C2")
                : string.Empty;
        }

        // Confirm and mark appointment completed, refresh owner grid, keep Appointments open
        private void button1_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to mark this appointment as completed?",
                "Confirm Complete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            const string updateSql = "UPDATE appointments SET notes = ISNULL(notes,'') + @marker WHERE appointment_id = @id";
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@marker", " [Completed]");
                    cmd.Parameters.AddWithValue("@id", _appointmentId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                // if the form was opened with Show(this) and owner is Appointments, refresh its grids
                if (this.Owner is Appointments ownerForm)
                {
                    ownerForm.RefreshGridsPublic();
                }

                // close this completion form only
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to mark appointment as completed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel?",
                "Confirm Cancel",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.Close();
            }
        }
    }
}