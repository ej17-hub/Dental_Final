using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Dental_Final
{
    /// <summary>
    /// Centralized error handler for the Dental Clinic Management System
    /// </summary>
    public static class ErrorHandler
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DentalClinic",
            "Logs",
            $"ErrorLog_{DateTime.Now:yyyyMMdd}.txt"
        );

        private static readonly object LogLock = new object();
        private static readonly TimeSpan AppointmentStartTime = new TimeSpan(8, 0, 0);  // 8:00 AM
        private static readonly TimeSpan AppointmentEndTime = new TimeSpan(17, 0, 0);    // 5:00 PM

        static ErrorHandler()
        {
            string logDirectory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDirectory))
            {
                try
                {
                    Directory.CreateDirectory(logDirectory);
                }
                catch { }
            }
        }

        #region Appointment Validation Methods

        public static bool ValidateAppointmentTime(TimeSpan appointmentTime, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (appointmentTime < AppointmentStartTime || appointmentTime >= AppointmentEndTime)
            {
                errorMessage = $"Appointments can only be booked between 8:00 AM and 5:00 PM.\n\n" +
                              $"Selected time: {DateTime.Today.Add(appointmentTime):h:mm tt}";

                HandleValidationError(errorMessage, "Invalid Appointment Time");
                LogError($"Appointment time validation failed: {appointmentTime:hh\\:mm}");
                return false;
            }

            return true;
        }

        public static bool ValidateAppointmentTime(DateTime appointmentDateTime, out string errorMessage)
        {
            return ValidateAppointmentTime(appointmentDateTime.TimeOfDay, out errorMessage);
        }

        public static bool CheckDuplicateAppointment(
            string connectionString,
            int dentistId,
            DateTime appointmentDate,
            TimeSpan appointmentTime,
            out string errorMessage,
            int? excludeAppointmentId = null)
        {
            errorMessage = string.Empty;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    // Query adapted for your schema - appointment_time is VARCHAR(50), appointment_date is DATETIME
                    string query = @"
                        SELECT 
                            a.appointment_id,
                            a.appointment_time,
                            a.appointment_date,
                            COALESCE(p.first_name + ' ' + p.last_name, CAST(p.patient_id AS VARCHAR)) AS patient_name
                        FROM appointments a
                        LEFT JOIN patients p ON a.patient_id = p.patient_id
                        WHERE a.dentist_id = @dentistId
                        AND CAST(a.appointment_date AS DATE) = @appointmentDate
                        AND a.appointment_time = @appointmentTime";

                    if (excludeAppointmentId.HasValue)
                    {
                        query += " AND a.appointment_id <> @excludeAppointmentId";
                    }

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@dentistId", dentistId);
                        cmd.Parameters.Add("@appointmentDate", SqlDbType.Date).Value = appointmentDate;
                        cmd.Parameters.AddWithValue("@appointmentTime", appointmentTime.ToString(@"hh\:mm"));

                        if (excludeAppointmentId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@excludeAppointmentId", excludeAppointmentId.Value);
                        }

                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string patientName = reader["patient_name"]?.ToString() ?? "Unknown Patient";
                                string existingTimeStr = reader["appointment_time"]?.ToString() ?? "";

                                errorMessage = $"This time slot is already booked!\n\n" +
                                             $"Date: {appointmentDate:MMMM d, yyyy}\n" +
                                             $"Time: {existingTimeStr}\n" +
                                             $"Patient: {patientName}\n\n" +
                                             $"Please select a different time.";

                                HandleValidationError(errorMessage, "Duplicate Appointment");
                                LogError($"Duplicate appointment attempt - Dentist ID: {dentistId}, Date: {appointmentDate:yyyy-MM-dd}, Time: {appointmentTime:hh\\:mm}");
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Error checking for duplicate appointments.";
                HandleDatabaseError(ex, "Checking duplicate appointments");
                return false;
            }
        }

        public static bool CheckAppointmentConflict(
            string connectionString,
            int dentistId,
            DateTime appointmentDate,
            TimeSpan appointmentTime,
            out string errorMessage,
            int bufferMinutes = 30,
            int? excludeAppointmentId = null)
        {
            errorMessage = string.Empty;

            try
            {
                // For VARCHAR time comparison, we'll check for appointments on the same day
                // and manually filter in C# since time arithmetic is complex with VARCHAR
                using (var conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT 
                            a.appointment_id,
                            a.appointment_time,
                            COALESCE(p.first_name + ' ' + p.last_name, CAST(p.patient_id AS VARCHAR)) AS patient_name
                        FROM appointments a
                        LEFT JOIN patients p ON a.patient_id = p.patient_id
                        WHERE a.dentist_id = @dentistId
                        AND CAST(a.appointment_date AS DATE) = @appointmentDate";

                    if (excludeAppointmentId.HasValue)
                    {
                        query += " AND a.appointment_id <> @excludeAppointmentId";
                    }

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@dentistId", dentistId);
                        cmd.Parameters.Add("@appointmentDate", SqlDbType.Date).Value = appointmentDate;

                        if (excludeAppointmentId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@excludeAppointmentId", excludeAppointmentId.Value);
                        }

                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string existingTimeStr = reader["appointment_time"]?.ToString() ?? "";

                                // Try to parse the existing time
                                if (TimeSpan.TryParse(existingTimeStr, out TimeSpan existingTime))
                                {
                                    // Calculate time difference in minutes
                                    double minutesDiff = Math.Abs((appointmentTime - existingTime).TotalMinutes);

                                    if (minutesDiff < bufferMinutes)
                                    {
                                        string patientName = reader["patient_name"]?.ToString() ?? "Unknown Patient";

                                        errorMessage = $"This time slot conflicts with an existing appointment!\n\n" +
                                                     $"Existing Appointment:\n" +
                                                     $"Date: {appointmentDate:MMMM d, yyyy}\n" +
                                                     $"Time: {existingTimeStr}\n" +
                                                     $"Patient: {patientName}\n\n" +
                                                     $"Please select a time at least {bufferMinutes} minutes apart.";

                                        HandleValidationError(errorMessage, "Appointment Conflict");
                                        LogError($"Appointment conflict - Dentist ID: {dentistId}, Date: {appointmentDate:yyyy-MM-dd}, Time: {appointmentTime:hh\\:mm}");
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Error checking for appointment conflicts.";
                HandleDatabaseError(ex, "Checking appointment conflicts");
                return false;
            }
        }

        public static bool ValidateAppointmentBooking(
            string connectionString,
            int dentistId,
            DateTime appointmentDate,
            TimeSpan appointmentTime,
            int? excludeAppointmentId = null,
            bool checkConflicts = true,
            int bufferMinutes = 30)
        {
            if (!ValidateAppointmentTime(appointmentTime, out string timeError))
            {
                return false;
            }

            if (!CheckDuplicateAppointment(connectionString, dentistId, appointmentDate, appointmentTime, out string duplicateError, excludeAppointmentId))
            {
                return false;
            }

            if (checkConflicts)
            {
                if (!CheckAppointmentConflict(connectionString, dentistId, appointmentDate, appointmentTime, out string conflictError, bufferMinutes, excludeAppointmentId))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckPatientAppointmentLimit(
            string connectionString,
            int patientId,
            out string errorMessage,
            int maxPendingAppointments = 5)
        {
            errorMessage = string.Empty;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    // Count all future appointments for this patient
                    string query = @"
                        SELECT COUNT(*) 
                        FROM appointments 
                        WHERE patient_id = @patientId 
                        AND CAST(appointment_date AS DATE) >= @today";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@patientId", patientId);
                        cmd.Parameters.Add("@today", SqlDbType.Date).Value = DateTime.Today;

                        conn.Open();
                        int appointmentCount = (int)cmd.ExecuteScalar();

                        if (appointmentCount >= maxPendingAppointments)
                        {
                            errorMessage = $"This patient has reached the maximum number of appointments ({maxPendingAppointments}).\n\n" +
                                         $"Please complete or cancel existing appointments before booking new ones.";

                            HandleValidationError(errorMessage, "Appointment Limit Reached");
                            LogError($"Patient {patientId} exceeded appointment limit: {appointmentCount} appointments");
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Error checking patient appointment limit.";
                HandleDatabaseError(ex, "Checking patient appointment limit");
                return false;
            }
        }

        #endregion

        #region Public Methods

        public static void Handle(Exception ex, string context = "", bool showMessage = true)
        {
            if (ex == null) return;

            string errorMessage = GetUserFriendlyMessage(ex, context);
            string technicalDetails = GetTechnicalDetails(ex, context);

            LogError(technicalDetails);

            if (showMessage)
            {
                ShowErrorMessage(errorMessage, ex);
            }
        }

        public static void HandleDatabaseError(Exception ex, string operation = "")
        {
            string context = string.IsNullOrEmpty(operation) ? "Database operation" : operation;

            if (ex is SqlException sqlEx)
            {
                HandleSqlException(sqlEx, context);
            }
            else
            {
                Handle(ex, context);
            }
        }

        public static void HandleValidationError(string message, string title = "Validation Error")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LogError($"Validation Error: {message}");
        }

        public static void HandleFileError(Exception ex, string fileName = "")
        {
            string context = string.IsNullOrEmpty(fileName) ? "File operation" : $"File operation: {fileName}";
            Handle(ex, context);
        }

        public static void HandleAuthenticationError(string message = "")
        {
            string errorMessage = string.IsNullOrEmpty(message)
                ? "Authentication failed. Please check your credentials and try again."
                : message;

            MessageBox.Show(errorMessage, "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            LogError($"Authentication Error: {errorMessage}");
        }

        public static void ShowSuccess(string message, string title = "Success")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static bool ShowConfirmation(string message, string title = "Confirm")
        {
            DialogResult result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return result == DialogResult.Yes;
        }

        #endregion

        #region Private Helper Methods

        private static string GetUserFriendlyMessage(Exception ex, string context)
        {
            StringBuilder message = new StringBuilder();

            if (!string.IsNullOrEmpty(context))
            {
                message.AppendLine($"An error occurred during: {context}");
                message.AppendLine();
            }

            switch (ex)
            {
                case SqlException sqlEx:
                    message.Append(GetSqlExceptionMessage(sqlEx));
                    break;
                case UnauthorizedAccessException _:
                    message.Append("Access denied. You don't have permission to perform this operation.");
                    break;
                case FileNotFoundException fnfEx:
                    message.Append($"File not found: {fnfEx.FileName}");
                    break;
                case IOException ioEx:
                    message.Append($"File operation failed: {ioEx.Message}");
                    break;
                case WebException webEx:
                    message.Append($"Network error: {webEx.Message}");
                    break;
                case InvalidOperationException _:
                    message.Append("The operation is not valid for the current state.");
                    break;
                case FormatException _:
                    message.Append("Invalid data format. Please check your input.");
                    break;
                case ArgumentNullException argEx:
                    message.Append($"Required value is missing: {argEx.ParamName}");
                    break;
                case ArgumentException argEx:
                    message.Append($"Invalid argument: {argEx.Message}");
                    break;
                case NullReferenceException _:
                    message.Append("A required object reference is not set.");
                    break;
                case DivideByZeroException _:
                    message.Append("Cannot divide by zero.");
                    break;
                case OverflowException _:
                    message.Append("The value is too large or too small.");
                    break;
                case TimeoutException _:
                    message.Append("The operation has timed out. Please try again.");
                    break;
                default:
                    message.Append($"An unexpected error occurred: {ex.Message}");
                    break;
            }

            return message.ToString();
        }

        private static string GetSqlExceptionMessage(SqlException sqlEx)
        {
            switch (sqlEx.Number)
            {
                case -1: return "Database connection timeout. Please check your network connection.";
                case -2: return "The operation took too long to complete. Please try again.";
                case 2:
                case 53: return "Cannot connect to the database server. Please check if the server is running.";
                case 18456: return "Database login failed. Please check your credentials.";
                case 2627:
                case 2601: return "A record with this information already exists.";
                case 547: return "Cannot delete this record because it is referenced by other records.";
                case 515: return "Required field cannot be empty.";
                case 8152: return "The data you entered is too long for the field.";
                case 1205: return "Database is busy. Please try again.";
                case 50000: return sqlEx.Message;
                default: return $"Database error occurred: {sqlEx.Message}";
            }
        }

        private static void HandleSqlException(SqlException sqlEx, string context)
        {
            string message = GetSqlExceptionMessage(sqlEx);
            string technicalDetails = GetTechnicalDetails(sqlEx, context);

            LogError(technicalDetails);
            MessageBox.Show(message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static string GetTechnicalDetails(Exception ex, string context)
        {
            StringBuilder details = new StringBuilder();
            details.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
            details.AppendLine($"Context: {context}");
            details.AppendLine($"Exception Type: {ex.GetType().FullName}");
            details.AppendLine($"Message: {ex.Message}");
            details.AppendLine($"Source: {ex.Source}");
            details.AppendLine($"Stack Trace: {ex.StackTrace}");

            if (ex is SqlException sqlEx)
            {
                details.AppendLine($"SQL Error Number: {sqlEx.Number}");
                details.AppendLine($"SQL State: {sqlEx.State}");
                details.AppendLine($"SQL Server: {sqlEx.Server}");
                details.AppendLine($"SQL Procedure: {sqlEx.Procedure}");
                details.AppendLine($"SQL Line Number: {sqlEx.LineNumber}");
            }

            if (ex.InnerException != null)
            {
                details.AppendLine("\nInner Exception:");
                details.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                details.AppendLine($"Message: {ex.InnerException.Message}");
                details.AppendLine($"Stack Trace: {ex.InnerException.StackTrace}");
            }

            details.AppendLine(new string('-', 80));
            return details.ToString();
        }

        private static void LogError(string errorDetails)
        {
            try
            {
                lock (LogLock)
                {
                    File.AppendAllText(LogFilePath, errorDetails + Environment.NewLine);
                }
            }
            catch { }
        }

        private static void ShowErrorMessage(string message, Exception ex)
        {
            string title = ex is SqlException ? "Database Error" :
                          ex is IOException ? "File Error" :
                          ex is WebException ? "Network Error" :
                          "Error";

            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool ValidateInput(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                HandleValidationError($"{fieldName} cannot be empty.", "Validation Error");
                return false;
            }
            return true;
        }

        public static bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                HandleValidationError("Email address is required.", "Validation Error");
                return false;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email)
                {
                    HandleValidationError("Invalid email format.", "Validation Error");
                    return false;
                }
                return true;
            }
            catch
            {
                HandleValidationError("Invalid email format.", "Validation Error");
                return false;
            }
        }

        public static bool ValidateDate(DateTime date, string fieldName, bool futureOnly = false)
        {
            if (futureOnly && date.Date < DateTime.Today)
            {
                HandleValidationError($"{fieldName} must be today or a future date.", "Validation Error");
                return false;
            }

            if (date.Year < 1900 || date.Year > 2100)
            {
                HandleValidationError($"{fieldName} is not a valid date.", "Validation Error");
                return false;
            }

            return true;
        }

        #endregion
    }
}
