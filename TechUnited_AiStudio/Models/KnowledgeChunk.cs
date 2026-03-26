using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechUnited_AiStudio.Models;

public class KnowledgeChunk
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Stores the 768-dimensional vector from nomic-embed-text as a JSON string.
    /// Example: "[0.123, -0.456, ...]"
    /// </summary>
    [Required]
    public string VectorJson { get; set; } = "[]";

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Helper property (Not Mapped to DB) to work with the actual float array in C#
    /// </summary>
    [NotMapped]
    public float[]? Vector => string.IsNullOrEmpty(VectorJson)
        ? null
        : System.Text.Json.JsonSerializer.Deserialize<float[]>(VectorJson);
}