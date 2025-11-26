using Dental_Final.User;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Dental_Final
{
    public partial class Patient_Settings : Form
    {
        // match other files' connection string
        private readonly string connectionString = @"Server=FANGON\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        // Caller should set this before showing the form (current logged-in patient id).
        public int? PatientId { get; set; }

        public Patient_Settings()
        {
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();

            // wire load so we can populate fields when the form shows
            this.Load -= Patient_Settings_Load;
            this.Load += Patient_Settings_Load;
        }

        private void Patient_Settings_Load(object sender, EventArgs e)
        {
            // ensure PatientId is obtained from any available source (PatientId property, this.Tag, or open dashboard)
            if (!PatientId.HasValue)
            {
                // 1) try form Tag if caller forwarded via Tag
                if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pidFromTag))
                {
                    PatientId = pidFromTag;
                }
                else
                {
                    // 2) try to find the Patient_Dashboard instance and read its Tag (common navigation flow)
                    var dash = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Name == "Patient_Dashboard");
                    if (dash != null && dash.Tag != null && int.TryParse(dash.Tag.ToString(), out int pidFromDash))
                    {
                        PatientId = pidFromDash;
                    }
                }
            }

            // load or clear based on resolved id
            if (PatientId.HasValue)
                LoadPatient(PatientId.Value);
            else
                ClearFields();
        }

        private void ClearFields()
        {
            textBoxFirstName.Text = string.Empty;
            txtMiddleInitial.Text = string.Empty;
            textBoxLastName.Text = string.Empty;
            txtSuffix.Text = string.Empty;
            textBoxEmail.Text = string.Empty;
            textBox2.Text = string.Empty; // birth date
            txtAddress.Text = string.Empty;
            textBoxPhone.Text = string.Empty;
            textBoxGender.Text = string.Empty;
        }

        private void LoadPatient(int patientId)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("SELECT * FROM patients WHERE patient_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", patientId);
                    conn.Open();

                    using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (!rdr.Read())
                        {
                            ClearFields();
                            return;
                        }

                        // helper local funcs
                        Func<string, string> getString = col =>
                        {
                            try
                            {
                                int idx = rdr.GetOrdinal(col);
                                if (rdr.IsDBNull(idx)) return string.Empty;
                                return rdr.GetString(idx).Trim();
                            }
                            catch (IndexOutOfRangeException)
                            {
                                return string.Empty;
                            }
                        };

                        Func<string, DateTime?> getDate = col =>
                        {
                            try
                            {
                                int idx = rdr.GetOrdinal(col);
                                if (rdr.IsDBNull(idx)) return null;
                                return Convert.ToDateTime(rdr.GetValue(idx));
                            }
                            catch (IndexOutOfRangeException)
                            {
                                return null;
                            }
                            catch
                            {
                                return null;
                            }
                        };

                        // Fill fields using database values; if column missing or value null -> blank
                        textBoxFirstName.Text = getString("first_name");
                        txtMiddleInitial.Text = getString("middle_initial");
                        textBoxLastName.Text = getString("last_name");
                        txtSuffix.Text = getString("suffix");
                        textBoxEmail.Text = getString("email");

                        // Birth date may be named birth_date or dob in some schemas; try common names
                        var bd = getDate("birth_date");
                        if (!bd.HasValue)
                            bd = getDate("dob");
                        textBox2.Text = bd.HasValue ? bd.Value.ToString("MMMM d, yyyy") : string.Empty;

                        txtAddress.Text = getString("address");
                        // phone may be stored as phone or mobile
                        var phone = getString("phone");
                        if (string.IsNullOrEmpty(phone))
                            phone = getString("mobile");
                        textBoxPhone.Text = phone;

                        // gender possibly stored as gender or sex
                        var gender = getString("gender");
                        if (string.IsNullOrEmpty(gender))
                            gender = getString("sex");
                        textBoxGender.Text = gender;
                    }
                }
            }
            catch (Exception ex)
            {
                // keep form usable; show blank fields on error
                ClearFields();
                MessageBox.Show("Failed to load patient data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // PUBLIC refresh helper for other forms to call after edits
        public void RefreshFields()
        {
            if (PatientId.HasValue)
            {
                LoadPatient(PatientId.Value);
            }
            else if (this.Tag != null && int.TryParse(this.Tag.ToString(), out int pidFromTag))
            {
                PatientId = pidFromTag;
                LoadPatient(pidFromTag);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Patient_Services patient_Services = new Patient_Services();
            // forward id via Tag so next form can read it
            if (PatientId.HasValue) patient_Services.Tag = PatientId.Value;
            patient_Services.Show();
            this.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Open ViewRecord with the current patient id so it can load fields
            if (PatientId.HasValue)
            {
                var viewRecord = new ViewRecord(PatientId.Value);
                viewRecord.Show();
            }
            else
            {
                // fallback: open an empty form
                var viewRecord = new ViewRecord();
                viewRecord.Show();
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // Open Edit_Information and pass PatientId so the form can populate controls
            var edit_Information = new Edit_Information();
            if (PatientId.HasValue)
                edit_Information.PatientId = PatientId;
            edit_Information.Show();
        }

        private void button5_Click_1(object sender, EventArgs e)
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

        private void button3_Click(object sender, EventArgs e)
        {
            Patient_Services patient_Services = new Patient_Services();
            if (PatientId.HasValue) patient_Services.Tag = PatientId.Value;
            patient_Services.Show();
            this.Hide();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // Open Patient_Appointments and set Tag to the patient id (Patient_Appointments reads Tag)
            Patient_Appointments patient_Appointments = new Patient_Appointments();
            if (PatientId.HasValue) patient_Appointments.Tag = PatientId.Value;
            patient_Appointments.Show();
            this.Hide();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Patient_Dashboard pd = new Patient_Dashboard();
            if (PatientId.HasValue) pd.Tag = PatientId.Value;
            pd.Show();
            this.Hide();
        }
    }
}