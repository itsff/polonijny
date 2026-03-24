using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace SlownikPolonijny.Web.Services;

public class StoredUser
{
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class FileUserService
{
    readonly List<StoredUser> _users;
    readonly PasswordHasher<StoredUser> _hasher = new();

    public FileUserService(string filePath)
    {
        var json = File.ReadAllText(filePath);
        _users = JsonSerializer.Deserialize<List<StoredUser>>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
    }

    public StoredUser ValidateCredentials(string username, string password)
    {
        var user = _users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user == null) return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success
            || result == PasswordVerificationResult.SuccessRehashNeeded
            ? user : null;
    }
}
