using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class CreateResultDto
{
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, 100000)]
    public int Position { get; set; }
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}
