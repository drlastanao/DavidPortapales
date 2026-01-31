using System;
using System.Text;

namespace DavidPortapales.Services;

public class PasswordGeneratorService
{
    private const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    public string GeneratePassword(int length, bool useUpper, bool useLower, bool useSpecial)
    {
        if (length < 1) throw new ArgumentException("Length must be at least 1", nameof(length));

        var sbAlphaNum = new StringBuilder();
        if (useLower) sbAlphaNum.Append(LowerCase);
        if (useUpper) sbAlphaNum.Append(UpperCase);
        // Always include digits
        sbAlphaNum.Append(Digits); 
        var alphaNumPool = sbAlphaNum.ToString();

        var sbSpecial = new StringBuilder();
        if (useSpecial) sbSpecial.Append(SpecialChars);
        var specialPool = sbSpecial.ToString();

        // If no special chars selected, just use alphaNum
        if (string.IsNullOrEmpty(specialPool))
        {
            return GenerateFromPool(length, alphaNumPool);
        }

        // If alphaNum is somehow empty (impossible currently due to digits), fallback to special
        if (string.IsNullOrEmpty(alphaNumPool))
        {
             return GenerateFromPool(length, specialPool);
        }

        var sb = new StringBuilder();
        var rnd = new Random();

        for (int i = 0; i < length; i++)
        {
            // PROBABILITY LOGIC:
            // User requested that letters + digits have double the probability of symbols.
            // Ratio 2:1 for AlphaNum : Symbol.
            // P(AlphaNum) = 2/3, P(Symbol) = 1/3.
            
            // Random(3): 0, 1 -> AlphaNum. 2 -> Symbol.
            int choice = rnd.Next(3);
            
            if (choice < 2) 
            {
                sb.Append(alphaNumPool[rnd.Next(alphaNumPool.Length)]);
            }
            else
            {
                sb.Append(specialPool[rnd.Next(specialPool.Length)]);
            }
        }

        return sb.ToString();
    }

    private string GenerateFromPool(int length, string pool)
    {
         var sb = new StringBuilder();
         var rnd = new Random();
         for(int i = 0; i < length; i++)
         {
             sb.Append(pool[rnd.Next(pool.Length)]);
         }
         return sb.ToString(); 
    }

}
