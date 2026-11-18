namespace BloodLink.Models.Entities
{
    // Units in stock per blood type for one hospital (Hospital → Inventory).
    public class BloodInventory
    {
        public int Id { get; set; }

        public int OrganizationId { get; set; }
        public Organization Organization { get; set; } = null!;

        public BloodType BloodType { get; set; }

        public int Units { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
