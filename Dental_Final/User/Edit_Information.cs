using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dental_Final.User
{
    public partial class Edit_Information : Form
    {
        // same connection string used across the project
        private readonly string connectionString = @"Server=DESKTOP-O65C6K9\SQLEXPRESS;Database=dental_final_clinic;Integrated Security=True;MultipleActiveResultSets=True";

        // Caller should set this before showing the form (current logged-in patient id).
        public int? PatientId { get; set; }

        public Edit_Information()
        {
            InitializeComponent();

            // wire load to populate fields if PatientId provided (or Tag)
            this.Load -= Edit_Information_Load;
            this.Load += Edit_Information_Load;
        }

        private void Edit_Information_Load(object sender, EventArgs e)
        {
            // try to populate fields
            int? pid = ResolvePatientId();
            if (pid.HasValue)
                LoadPatientToControls(pid.Value);
        }

        private int? ResolvePatientId()
        {
            if (PatientId.HasValue) return PatientId.Value;

            if (this.Tag != null)
            {
                if (this.Tag is int) return (int)this.Tag;
                int parsed;
                if (int.TryParse(this.Tag.ToString(), out parsed)) return parsed;
            }

            // try to find Patient_Settings form and read its PatientId property
            var psForm = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Name == "Patient_Settings");
            if (psForm != null)
            {
                try
                {
                    var prop = psForm.GetType().GetProperty("PatientId", BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                    {
                        var val = prop.GetValue(psForm);
                        if (val is int) return (int)val;
                        int parsed;
                        if (int.TryParse(val?.ToString(), out parsed)) return parsed;
                    }
                }
                catch { }
            }

            return null;
        }

        private void LoadPatientToControls(int patientId)
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
                        if (!rdr.Read()) return;

                        Func<string, string> getString = col =>
                        {
                            try
                            {
                                int idx = rdr.GetOrdinal(col);
                                if (rdr.IsDBNull(idx)) return string.Empty;
                                return rdr.GetString(idx).Trim();
                            }
                            catch { return string.Empty; }
                        };

                        Func<string, DateTime?> getDate = col =>
                        {
                            try
                            {
                                int idx = rdr.GetOrdinal(col);
                                if (rdr.IsDBNull(idx)) return null;
                                return Convert.ToDateTime(rdr.GetValue(idx));
                            }
                            catch { return null; }
                        };

                        textBoxFirstName.Text = getString("first_name");
                        txtMiddleInitial.Text = getString("middle_initial");
                        textBoxLastName.Text = getString("last_name");
                        txtSuffix.Text = getString("suffix");
                        textBoxEmail.Text = getString("email");

                        var bd = getDate("birth_date");
                        if (!bd.HasValue) bd = getDate("dob");
                        if (bd.HasValue) dateTimePickerBirthDate.Value = bd.Value;
                        // else leave picker default

                        txtAddress.Text = getString("address");

                        var phone = getString("phone");
                        if (string.IsNullOrEmpty(phone)) phone = getString("mobile");
                        textBoxPhone.Text = phone;

                        var gender = getString("gender");
                        if (string.IsNullOrEmpty(gender)) gender = getString("sex");
                        if (!string.IsNullOrEmpty(gender))
                        {
                            // try to set combo if matches items
                            var idx = cmbGender.FindStringExact(gender);
                            if (idx >= 0) cmbGender.SelectedIndex = idx;
                            else cmbGender.Text = gender;
                        }

                        // NOTE: password field intentionally left empty for security
                        txtPassword.Text = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load patient data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // checks whether a column exists in dbo schema
        private bool ColumnExists(string tableName, string columnName)
        {
            const string sql = "SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(@table) AND name = @col";
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@table", "dbo." + tableName);
                    cmd.Parameters.AddWithValue("@col", columnName);
                    conn.Open();
                    var r = cmd.ExecuteScalar();
                    return r != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var dr = MessageBox.Show("Are you sure you want to return? All unsaved changes will be lost.", "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr == DialogResult.Yes)
            {
                this.Close();
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show("Save changes to your profile?", "Confirm Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            int? pid = ResolvePatientId();
            if (!pid.HasValue)
            {
                MessageBox.Show("Unable to determine patient id to save. Please re-open the settings from your dashboard.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // gather values
            var first = textBoxFirstName.Text.Trim();
            var middle = txtMiddleInitial.Text.Trim();
            var last = textBoxLastName.Text.Trim();
            var suffix = txtSuffix.Text.Trim();
            var email = textBoxEmail.Text.Trim();
            var address = txtAddress.Text.Trim();
            var phone = textBoxPhone.Text.Trim();
            var gender = cmbGender.Text.Trim();
            DateTime? birth = null;
            try { birth = dateTimePickerBirthDate.Value.Date; } catch { birth = null; }
            var passwordPlain = txtPassword.Text; // will hash if password_hash column exists

            // build dynamic update depending on column availability
            var setClauses = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (ColumnExists("patients", "first_name")) { setClauses.Add("first_name = @first"); parameters["@first"] = (object)first ?? DBNull.Value; }
            if (ColumnExists("patients", "middle_initial")) { setClauses.Add("middle_initial = @middle"); parameters["@middle"] = (object)middle ?? DBNull.Value; }
            if (ColumnExists("patients", "last_name")) { setClauses.Add("last_name = @last"); parameters["@last"] = (object)last ?? DBNull.Value; }
            if (ColumnExists("patients", "suffix")) { setClauses.Add("suffix = @suffix"); parameters["@suffix"] = (object)suffix ?? DBNull.Value; }
            if (ColumnExists("patients", "email")) { setClauses.Add("email = @email"); parameters["@email"] = (object)email ?? DBNull.Value; }
            if (ColumnExists("patients", "address")) { setClauses.Add("address = @address"); parameters["@address"] = (object)address ?? DBNull.Value; }

            // phone vs mobile
            if (ColumnExists("patients", "phone")) { setClauses.Add("phone = @phone"); parameters["@phone"] = (object)phone ?? DBNull.Value; }
            else if (ColumnExists("patients", "mobile")) { setClauses.Add("mobile = @phone"); parameters["@phone"] = (object)phone ?? DBNull.Value; }

            // gender/sex
            if (ColumnExists("patients", "gender")) { setClauses.Add("gender = @gender"); parameters["@gender"] = (object)gender ?? DBNull.Value; }
            else if (ColumnExists("patients", "sex")) { setClauses.Add("sex = @gender"); parameters["@gender"] = (object)gender ?? DBNull.Value; }

            // birth date naming
            if (birth.HasValue)
            {
                if (ColumnExists("patients", "birth_date")) { setClauses.Add("birth_date = @birth"); parameters["@birth"] = birth.Value.Date; }
                else if (ColumnExists("patients", "dob")) { setClauses.Add("dob = @birth"); parameters["@birth"] = birth.Value.Date; }
            }

            // password: prefer password_hash column and compute SHA256 hex; fallback to password column if exists
            if (!string.IsNullOrEmpty(passwordPlain))
            {
                if (ColumnExists("patients", "password_hash"))
                {
                    setClauses.Add("password_hash = @pwd");
                    parameters["@pwd"] = ComputeSha256Hash(passwordPlain);
                }
                else if (ColumnExists("patients", "password"))
                {
                    setClauses.Add("password = @pwd");
                    parameters["@pwd"] = passwordPlain;
                }
            }

            if (!setClauses.Any())
            {
                MessageBox.Show("No writable patient columns detected in database to update.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sql = $"UPDATE patients SET {string.Join(", ", setClauses)} WHERE patient_id = @id";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    foreach (var kv in parameters)
                        cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@id", pid.Value);

                    conn.Open();
                    var rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        MessageBox.Show("Changes saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // refresh Patient_Settings if open
                        var psForm = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Name == "Patient_Settings");
                        if (psForm != null)
                        {
                            try
                            {
                                // attempt to call private LoadPatient(int) method via reflection
                                var mi = psForm.GetType().GetMethod("LoadPatient", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (mi != null)
                                {
                                    mi.Invoke(psForm, new object[] { pid.Value });
                                }
                                else
                                {
                                    // fallback: set PatientId property (if exists) and try to call a public refresh method
                                    var prop = psForm.GetType().GetProperty("PatientId", BindingFlags.Public | BindingFlags.Instance);
                                    if (prop != null && prop.CanWrite)
                                    {
                                        prop.SetValue(psForm, pid.Value);
                                        var refreshMi = psForm.GetType().GetMethod("RefreshFields", BindingFlags.Public | BindingFlags.Instance);
                                        if (refreshMi != null) refreshMi.Invoke(psForm, null);
                                    }
                                }
                            }
                            catch
                            {
                                // swallow refresh errors
                            }
                        }

                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("No rows updated. Please verify patient id and try again.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save changes: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ComputeSha256Hash(string raw)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(raw);
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder();
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}