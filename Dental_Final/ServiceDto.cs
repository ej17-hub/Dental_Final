using System;

namespace Dental_Final
{
    // Small DTO used to move service info between forms
    public class ServiceDto
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public int? DurationMinutes { get; set; }
        public string Specialization { get; set; }

        public override string ToString()
        {
            // CheckedListBox will display the service name
            return Name ?? base.ToString();
        }
    }
}