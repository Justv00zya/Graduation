namespace OrgTechRepair.Models;

public class Employee
{
    public int Id { get; set; }
    public string TabNumber { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public Position? Position { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? INN { get; set; }
    public string? Address { get; set; }
    public DateTime HireDate { get; set; }
}
