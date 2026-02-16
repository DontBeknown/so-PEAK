using System;

[Serializable]
public struct SeedData
{
    public string seed1;
    public string seed2;
    public string seed3;
    
    // Combined full seed
    public string FullSeed => seed1 + seed2 + seed3;
    
    // Constructor from full seed
    public SeedData(string fullSeed, SeedConfig config)
    {
        if (string.IsNullOrEmpty(fullSeed))
        {
            fullSeed = GenerateRandomSeed(config);
        }
        
        // Pad if needed
        int totalLength = config.TotalDigitCount;
        fullSeed = fullSeed.PadRight(totalLength, '0');
        
        // Split into parts
        int pos = 0;
        seed1 = fullSeed.Substring(pos, config.seed1DigitCount);
        pos += config.seed1DigitCount;
        seed2 = fullSeed.Substring(pos, config.seed2DigitCount);
        pos += config.seed2DigitCount;
        seed3 = fullSeed.Substring(pos, config.seed3DigitCount);
    }
    
    // Constructor from parts
    public SeedData(string part1, string part2, string part3)
    {
        seed1 = part1;
        seed2 = part2;
        seed3 = part3;
    }
    
    // Generate random seed
    public static string GenerateRandomSeed(SeedConfig config)
    {
        string seed = "";
        System.Random random = new System.Random();
        for (int i = 0; i < config.TotalDigitCount; i++)
        {
            seed += random.Next(0, 10).ToString();
        }
        return seed;
    }
    
    // Validate seed parts
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(seed1) && 
               !string.IsNullOrEmpty(seed2) && 
               !string.IsNullOrEmpty(seed3);
    }
    
    public override string ToString()
    {
        return $"Seed1: {seed1} | Seed2: {seed2} | Seed3: {seed3}";
    }
}
