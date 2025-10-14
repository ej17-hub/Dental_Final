using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Patients : Form
    {
        private int selectedPatientId;

        public Patients()
        {
            InitializeComponent();
        }

        private int CalculateAge(DateTime birthDate)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            if (DateTime.Now.DayOfYear < birthDate.DayOfYear)
                age--;
            return age;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Adding_Patient adding = new Adding_Patient();
            adding.Show();
            this.Hide();
        }
    }
}
