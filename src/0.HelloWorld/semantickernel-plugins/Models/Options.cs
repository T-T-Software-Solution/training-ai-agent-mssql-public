using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticKernelPlugins;

public class JwtOptions
{
    public required string Key { get; set; }
}

public class AuthOptions
{
    public required string AdminUser { get; set; }
    public required string AdminPassword { get; set; }
}

public class SoftwareOptions
{
    public required string Version { get; set; }
}

public class AzureOpenAIOptions
{
    public required string ChatModel { get; set; }
    public required string Endpoint { get; set; }
    public required string ApiKey { get; set; }
}
