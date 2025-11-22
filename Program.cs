using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        // Wczytanie wiadomości z pliku wejściowego
        string message = File.ReadAllText("message.txt");
        Console.WriteLine("Wiadomość wczytana z pliku message.txt: " + message);

        // Konwersja tekstu na ciąg bitów (kod ASCII 8-bit)
        string messageBits = TextToBits(message);

        // Generowanie dwóch dużych liczb pierwszych spełniających wymóg p ≡ q ≡ 3 (mod 4)
        BigInteger p = GenerateLargePrime(512);
        BigInteger q = GenerateLargePrime(512);
        BigInteger n = p * q; // Moduł używany w generatorze BBS

        Console.WriteLine("Wygenerowano losowe p, q spełniające p ≡ q ≡ 3 (mod 4)");

        // Generowanie klucza o tej samej długości co wiadomość z użyciem BBS
        string key = GenerateBBSKey(messageBits.Length, n);
        File.WriteAllText("key.bin", key); // Zapis klucza do pliku

        // Szyfrowanie wiadomości metodą One-Time Pad (XOR bit po bicie)
        string encryptedBits = XOR(messageBits, key);
        File.WriteAllText("encrypted.bin", encryptedBits); // Zapis szyfrogramu do pliku

        // Deszyfrowanie (ponowne XOR z tym samym kluczem)
        string decryptedBits = XOR(encryptedBits, key);
        string decryptedText = BitsToText(decryptedBits);
        File.WriteAllText("decrypted.txt", decryptedText); // Zapis odszyfrowanego tekstu

        Console.WriteLine("\nPliki zapisano:");
        Console.WriteLine(" - key.bin");
        Console.WriteLine(" - encrypted.bin");
        Console.WriteLine(" - decrypted.txt");

        // Testy jakości losowości klucza
        Console.WriteLine("\n--- Test monobit ---");
        MonobitTest(key);

        Console.WriteLine("\n--- Test runs ---");
        RunsTest(key);
    }

    // Funkcja generująca dużą liczbę pierwszą (512-bit) spełniającą warunek p ≡ 3 (mod 4)
    static BigInteger GenerateLargePrime(int bits)
    {
        using RNGCryptoServiceProvider rng = new();

        while (true)
        {
            byte[] bytes = new byte[bits / 8];
            rng.GetBytes(bytes); // losowe bajty

            bytes[bytes.Length - 1] |= 0x80;  // wymuszenie największego bitu, aby liczba miała pełną długość

            BigInteger prime = new BigInteger(bytes, isUnsigned: true);
            
            // Warunek p % 4 == 3 oraz test pierwszości
            if (prime % 4 == 3 && IsProbablePrime(prime))
                return prime;
        }
    }

    // Bardzo prosty test pierwszości (Fermata) – wystarczający dla projektu edukacyjnego
    static bool IsProbablePrime(BigInteger value, int k = 20)
    {
        return BigInteger.ModPow(2, value - 1, value) == 1;
    }

    // Generowanie klucza metodą Blum–Blum–Shub
    static string GenerateBBSKey(int length, BigInteger n)
    {
        using RNGCryptoServiceProvider rng = new();
        byte[] seedBytes = new byte[n.GetByteCount()];
        rng.GetBytes(seedBytes);

        // Losowa wartość początkowa seed
        BigInteger seed = new BigInteger(seedBytes, isUnsigned: true) % n;
        BigInteger x = (seed * seed) % n;

        StringBuilder bits = new();

        // Iteracyjne generowanie bitów:
        // x = x^2 mod n, a najmłodszy bit x zapisujemy jako wynik
        for (int i = 0; i < length; i++)
        {
            x = (x * x) % n;
            bits.Append((int)(x % 2)); // pobranie najmłodszego bitu
        }

        return bits.ToString();
    }

    // Operacja XOR na dwóch łańcuchach bitowych (implementacja OTP)
    static string XOR(string a, string b)
    {
        StringBuilder result = new();
        for (int i = 0; i < a.Length; i++)
            result.Append(a[i] == b[i] ? '0' : '1');
        return result.ToString();
    }

    // Zamiana tekstu na reprezentację bitową 8-bitową
    static string TextToBits(string text)
    {
        StringBuilder sb = new();
        foreach (char c in text)
            sb.Append(Convert.ToString(c, 2).PadLeft(8, '0')); // ASCII 8bit
        return sb.ToString();
    }

    // Zamiana bitów na tekst ASCII
    static string BitsToText(string bits)
    {
        StringBuilder sb = new();
        for (int i = 0; i < bits.Length; i += 8)
            sb.Append((char)Convert.ToInt32(bits.Substring(i, 8), 2));
        return sb.ToString();
    }

    // Test monobit – sprawdza proporcję 1 i 0
    static void MonobitTest(string bits)
    {
        int n = bits.Length;
        int sum = 0;

        // Zamiana każdego bitu na +1 lub -1 i sumowanie
        foreach (char c in bits)
            sum += (c == '1') ? 1 : -1;

        // Obliczenie statystyki NIST
        double sobs = Math.Abs(sum) / Math.Sqrt(n);
        double pValue = SpecialFunctions.Erfc(sobs / Math.Sqrt(2));

        Console.WriteLine($"Długość ciągu: {n}");
        Console.WriteLine($"Suma bitów (S): {sum}");
        Console.WriteLine($"S_obs: {sobs}");
        Console.WriteLine($"p-value: {pValue}");

        if (pValue > 0.01)
            Console.WriteLine("Wynik: TEST ZALICZONY (ciąg wygląda na losowy)");
        else
            Console.WriteLine("Wynik: TEST NIEZALICZONY (ciąg nie wygląda na losowy)");
    }
public static class SpecialFunctions
{
    // Przybliżenie funkcji komplementarnej błędu ERFC
    public static double Erfc(double x)
    {
        // Wzór Abramowitz and Stegun 7.1.26
        double z = Math.Abs(x);
        double t = 1.0 / (1.0 + 0.5 * z);

        double ans = t * Math.Exp(-z * z - 1.26551223 +
                    t * (1.00002368 +
                    t * (0.37409196 +
                    t * (0.09678418 +
                    t * (-0.18628806 +
                    t * (0.27886807 +
                    t * (-1.13520398 +
                    t * (1.48851587 +
                    t * (-0.82215223 +
                    t * 0.17087277)))))))));

        return (x >= 0.0) ? ans : 2.0 - ans;
    }
}

static void RunsTest(string bits)
{
    int n = bits.Length;
    int ones = 0;

    foreach (char c in bits)
        if (c == '1') ones++;

    double pi = (double)ones / n;

    Console.WriteLine($"pi (udział jedynek): {pi}");

    // Warunek wstępny NIST
    if (Math.Abs(pi - 0.5) >= (2.0 / Math.Sqrt(n)))
    {
        Console.WriteLine("Test RUNS nie może być wykonany – zbyt duża nierównowaga bitów.");
        Console.WriteLine("Wynik: TEST NIEZALICZONY");
        return;
    }

    // Liczenie liczby serii (runs)
    int runs = 1;
    for (int i = 1; i < n; i++)
        if (bits[i] != bits[i - 1])
            runs++;

    Console.WriteLine($"Liczba serii (runs): {runs}");

    // Obliczenie p-value
    double expectedRuns = 2 * n * pi * (1 - pi);
    double pValue = SpecialFunctions.Erfc(Math.Abs(runs - expectedRuns) / (2 * Math.Sqrt(2 * n) * pi * (1 - pi)));

    Console.WriteLine($"Oczekiwana liczba runów: {expectedRuns}");
    Console.WriteLine($"p-value: {pValue}");

    if (pValue > 0.01)
        Console.WriteLine("Wynik: TEST ZALICZONY (ciąg wygląda na losowy)");
    else
        Console.WriteLine("Wynik: TEST NIEZALICZONY (ciąg nie wygląda na losowy)");
}
}
