namespace SemanticKernelPlugins;

public class LoginModel
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class MessageRequest
{
    public required string Question { get; set; }
}