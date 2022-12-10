namespace WebAPI.Models.Security;

public class RecoveryCode
{
    // This method creates a random code of 12 characters.
    public static List<string> Generate(int amount)
    {
        var codes = new List<string>();
        for (var i = 0; i < amount; i++) codes.Add(RandomString(12));
        return codes;
    }

    // This method creates a random string of a given length.
    private static string RandomString(int length)
    {
        const string upperLowerNumbers = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var chars = Enumerable.Range(0, length)
            .Select(_ => upperLowerNumbers[random.Next(0, upperLowerNumbers.Length)]);
        return new string(chars.ToArray());
    }
}