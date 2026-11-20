namespace BloodLink.Models.Dtos
{
    // Booking form on /Donor/Appointments (donorAppointments.js).
    public class CreateAppointmentDto
    {
        public string FacilityId { get; set; } = "";
        public AppointmentDonorDto Donor { get; set; } = new();
        public string RequestedDate { get; set; } = "";   // "2026-11-25"
        public string RequestedTime { get; set; } = "";   // "09:30"
        public string? Notes { get; set; }
        public EligibilityDto Eligibility { get; set; } = new();
    }

    public class AppointmentDonorDto
    {
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Email { get; set; }
        public string Type { get; set; } = "";
    }

    // Answers of the eligibility check. Yes/No answers come as "yes" / "no".
    public class EligibilityDto
    {
        public int Age { get; set; }
        public decimal Weight { get; set; }
        public string? LastDonation { get; set; }
        public string? Well { get; set; }
        public string? Meds { get; set; }
        public string? MedsDetail { get; set; }
        public string? Travel { get; set; }
        public string? TravelDetail { get; set; }
        public string? Procedure { get; set; }
        public string? State { get; set; }               // ok | review | blocked
        public List<string> Flags { get; set; } = new();
    }

    // Hospital decision on an appointment (/Hospital → Appointments).
    public class AppointmentDecisionDto
    {
        public string Action { get; set; } = "";         // approve | decline | cancel | complete
        public string? Date { get; set; }
        public string? Time { get; set; }
        public string? Note { get; set; }
        public string? Reason { get; set; }
        public bool AddStock { get; set; }
    }
}
