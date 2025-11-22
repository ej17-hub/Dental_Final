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
    public partial class Patient_Settings : Form
    {
        public Patient_Settings()
        {
            InitializeComponent();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Patient_Services patient_Services = new Patient_Services();
            patient_Services.Show();
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Patient_Appointments patient_Appointments = new Patient_Appointments(); 
            patient_Appointments.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Patient_Dashboard pd = new Patient_Dashboard(); 
            pd.Show();
            this.Hide();
        }
    }
}
