using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
     
        string message = File.ReadAllText("message.txt");
        Console.WriteLine("Wiadomość wczytana z pliku message.txt: " + message);

        string messageBits = TextToBits(message);

    
        BigInteger p = GenerateLargePrime(512);
        BigInteger q = GenerateLargePrime(512);
        BigInteger n = p * q;

        Console.WriteLine("Wygenerowano losowe p, q spełniające p ≡ q ≡ 3 (mod 4)");

     
        string key = GenerateBBSKey(messageBits.Length, n);
        File.WriteAllText("key.bin", key);

       
        string encryptedBits = XOR(messageBits, key);
        File.WriteAllText("encrypted.bin", encryptedBits);

        
        string decryptedBits = XOR(encryptedBits, key);
        string decryptedText = BitsToText(decryptedBits);
        File.WriteAllText("decrypted.txt", decryptedText);

        Console.WriteLine("\nPliki zapisano:");
        Console.WriteLine(" - key.bin");
        Console.WriteLine(" - encrypted.bin");
        Console.WriteLine(" - decrypted.txt");

        
           Console.WriteLine("\n--- Test monobit ---");
        MonobitTest(key);
        Console.WriteLine("\n--- Test runs ---");
        RunsTest(key);
    }

  
    static BigInteger GenerateLargePrime(int bits)
    {
        using RNGCryptoServiceProvider rng = new();

        while (true)
        {
            byte[] bytes = new byte[bits / 8];
            rng.GetBytes(bytes);
            bytes[bytes.Length - 1] |= 0x80;  

            BigInteger prime = new BigInteger(bytes, isUnsigned: true);
            if (prime % 4 == 3 && IsProbablePrime(prime)) return prime;
        }
    }

    static bool IsProbablePrime(BigInteger value, int k = 20)
    {
        return BigInteger.ModPow(2, value - 1, value) == 1; 
    }

    static string GenerateBBSKey(int length, BigInteger n)
    {
        using RNGCryptoServiceProvider rng = new();
        byte[] seedBytes = new byte[n.GetByteCount()];
        rng.GetBytes(seedBytes);

        BigInteger seed = new BigInteger(seedBytes, isUnsigned: true) % n;
        BigInteger x = (seed * seed) % n;

        StringBuilder bits = new();

        for (int i = 0; i < length; i++)
        {
            x = (x * x) % n;
            bits.Append((int)(x % 2));
        }

        return bits.ToString();
    }

    static string XOR(string a, string b)
    {
        StringBuilder result = new();
        for (int i = 0; i < a.Length; i++)
            result.Append(a[i] == b[i] ? '0' : '1');
        return result.ToString();
    }

    static string TextToBits(string text)
    {
        StringBuilder sb = new();
        foreach (char c in text)
            sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
        return sb.ToString();
    }

    static string BitsToText(string bits)
    {
        StringBuilder sb = new();
        for (int i = 0; i < bits.Length; i += 8)
            sb.Append((char)Convert.ToInt32(bits.Substring(i, 8), 2));
        return sb.ToString();
    }
      static void MonobitTest(string bits)
    {
        int ones = 0, zeros = 0;
        foreach (char c in bits)
        {
            if (c == '1') ones++;
            else zeros++;
        }
        Console.WriteLine($"Liczba 1: {ones}, liczba 0: {zeros}");
    }


    static void RunsTest(string bits)
    {
        int runs = 1;
        for (int i = 1; i < bits.Length; i++)
            if (bits[i] != bits[i - 1])
                runs++;

        Console.WriteLine($"Liczba serii (runs): {runs}");
    }
}
