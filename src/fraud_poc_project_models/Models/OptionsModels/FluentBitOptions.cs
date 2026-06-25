using Serilog.Events;
using System.ComponentModel.DataAnnotations;

namespace OptionsModels;

public record FluentBitOptions
{
    public const string Section = "FluentBit";

    [Required]
    public required string Host { get; init; }

    [Required]
    public required int Port { get; init; }

    [Required]
    public required string Tag { get; init; }

    [Required]
    public LogEventLevel RestrictedToMinimumLevel { get; set; }
}