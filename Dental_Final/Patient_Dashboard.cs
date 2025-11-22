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
            InitializeComponent();
        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Patient_Appointments pa = new Patient_Appointments();
            pa.Show();
            this.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Patient_Services patient_Services = new Patient_Services();
            patient_Services.Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Patient_Settings ps = new Patient_Settings();
            ps.Show();  
            this.Hide();
        }
    }
}
