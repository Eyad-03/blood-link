namespace BloodLink.Models.Entities
{
    // All enums are stored in the database as text (see BloodLinkDbContext).

    public enum UserRole { Donor, Recipient, Hospital, Admin }

    public enum AccountStatus { Active, Suspended }

    public enum BloodType { APositive, ANegative, BPositive, BNegative, ABPositive, ABNegative, OPositive, ONegative }

    public enum OrganizationType { Hospital, BloodDonationCenter, MedicalCenter }

    public enum OrganizationStatus { Pending, Active, Rejected }

    public enum RequestPriority { Normal, High, Urgent }

    public enum BloodRequestStatus { Pending, Accepted, Declined, Completed, Cancelled }

    public enum ChatSender { System, Donor, Recipient }

    public enum AppointmentStatus { Pending, Approved, Declined, Completed, Cancelled }

    public enum EligibilityState { Idle, Ok, Review, Blocked }

    public enum DonationRecordStatus { PendingVerification, Verified }

    public enum HallBookingStatus { PendingApproval, Approved, Paid, Rejected, Completed }

    public enum AdPlan { Basic, Featured, Premium }

    public enum ReportPriority { Low, Medium, High }

    public enum ReportStatus { New, InReview, Resolved }
}
