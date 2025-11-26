using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Patient_Dashboard : Form
    {
        public Patient_Dashboard()
        {
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();
        }

        // In each navigation handler, forward this.Tag (patient id) to the next form.
        private void OpenPatientServices()
        {
            var ps = new Patient_Services();
            if (this.Tag != null)
                ps.Tag = this.Tag;
            ps.Show();
            this.Hide();
        }

        // Example: call from the button that opens Services:
        private void button3_Click(object sender, EventArgs e)
        {
            var ps = new Patient_Services();
            if (this.Tag != null)
                ps.Tag = this.Tag;
            ps.Show();
            this.Hide();
        }

        // Open Settings (Patient_Settings expects PatientId)
        private void button4_Click(object sender, EventArgs e)
        {
            var settings = new Patient_Settings();
            if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pid))
                settings.PatientId = pid;
            settings.Show();
            this.Hide();
        }

        // Open Appointments
        private void button1_Click(object sender, EventArgs e)
        {
            var appts = new Patient_Appointments();
            if (this.Tag != null)
                appts.Tag = this.Tag;
            appts.Show();
            this.Hide();
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
    }
}