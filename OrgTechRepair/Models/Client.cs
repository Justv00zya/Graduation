namespace OrgTechRepair.Models;

public class Client
{
    public int Id { get; set; }
    /// <summary>Связь с учётной записью (роль Client) для личного кабинета.</summary>
    public string? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
