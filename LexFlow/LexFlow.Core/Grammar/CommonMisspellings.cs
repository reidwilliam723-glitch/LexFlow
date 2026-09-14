namespace LexFlow.Core.Grammar;

/// <summary>
/// Common mechanical misspellings with a single well-defined correction.
/// Used by the rule-based checker and by inline typing suggestions.
/// </summary>
public static class CommonMisspellings
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["teh"] = "the",
        ["adn"] = "and",
        ["taht"] = "that",
        ["thier"] = "their",
        ["recieve"] = "receive",
        ["recieved"] = "received",
        ["beleive"] = "believe",
        ["beleived"] = "believed",
        ["acheive"] = "achieve",
        ["seperate"] = "separate",
        ["seperated"] = "separated",
        ["definately"] = "definitely",
        ["occassion"] = "occasion",
        ["occured"] = "occurred",
        ["untill"] = "until",
        ["wierd"] = "weird",
        ["truely"] = "truly",
        ["adress"] = "address",
        ["accomodate"] = "accommodate",
        ["goverment"] = "government",
        ["independant"] = "independent",
        ["neccessary"] = "necessary",
        ["publically"] = "publicly",
        ["enviroment"] = "environment",
        ["existance"] = "existence",
        ["foriegn"] = "foreign",
        ["freind"] = "friend",
        ["goverment"] = "government",
        ["harrass"] = "harass",
        ["knowlege"] = "knowledge",
        ["liason"] = "liaison",
        ["maintainance"] = "maintenance",
        ["millenium"] = "millennium",
        ["mispell"] = "misspell",
        ["noticable"] = "noticeable",
        ["persue"] = "pursue",
        ["priviledge"] = "privilege",
        ["probaly"] = "probably",
        ["reccomend"] = "recommend",
        ["refered"] = "referred",
        ["relevent"] = "relevant",
        ["religous"] = "religious",
        ["resistence"] = "resistance",
        ["succesful"] = "successful",
        ["tommorrow"] = "tomorrow",
        ["tounge"] = "tongue",
        ["usefull"] = "useful",
        ["visable"] = "visible",
        ["wether"] = "whether",
        ["wich"] = "which",
        ["alot"] = "a lot",
        ["irregardless"] = "regardless",
        ["supposably"] = "supposedly",
        ["undoubtably"] = "undoubtedly"
    };

    public static bool TryCorrect(string word, out string correction)
    {
        if (string.IsNullOrEmpty(word))
        {
            correction = string.Empty;
            return false;
        }

        return Map.TryGetValue(word, out correction!);
    }

    public static IEnumerable<KeyValuePair<string, string>> All => Map;
}
