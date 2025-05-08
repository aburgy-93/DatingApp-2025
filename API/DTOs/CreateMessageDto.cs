using System;

namespace API.DTOs;

public class CreateMessageDto
{
    public string RecipientUsername { get; set; }
    public required string Content { get; set; }
}
