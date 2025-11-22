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
    public partial class Patient_Appointments : Form
    {
        public Patient_Appointments()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Patient_Dashboard pd = new Patient_Dashboard();
            pd.Show();
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
