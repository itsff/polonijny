using Microsoft.AspNetCore.Identity;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: HashPassword <password>");
    return 1;
}

var hasher = new PasswordHasher<object>();
string hash = hasher.HashPassword(null, args[0]);
Console.WriteLine(hash);
return 0;
