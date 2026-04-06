namespace OrgTechRepair.Models.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string TabNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public int PositionId { get; set; }
    public string? PositionName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? INN { get; set; }
    public string? Address { get; set; }
    public DateTime HireDate { get; set; }
}
