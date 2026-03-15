using Microsoft.AspNetCore.Identity;

Console.Error.Write("Password: ");
string password = ReadPasswordFromConsole();

if (string.IsNullOrEmpty(password))
{
    Console.Error.WriteLine("Password cannot be empty.");
    return 1;
}

var hasher = new PasswordHasher<object>();
Console.WriteLine(hasher.HashPassword(null, password));
return 0;

static string ReadPasswordFromConsole()
{
    var chars = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.Error.WriteLine();
            break;
        }
        if (key.Key == ConsoleKey.Backspace)
        {
            if (chars.Count > 0)
                chars.RemoveAt(chars.Count - 1);
        }
        else if (!char.IsControl(key.KeyChar))
        {
            chars.Add(key.KeyChar);
        }
    }
    return new string(chars.ToArray());
}
