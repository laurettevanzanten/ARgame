public class OrderProperties
{
    public char rackLetter;
    public int x;
    public int y;
    public int count;
    public string label;

    public OrderProperties()
    {
    }

    public OrderProperties(string text)
    {
        var trimmedText = text.Trim();
        var tokens = trimmedText.Split(';');

        rackLetter = char.ToUpper(tokens[0][0]);
        count = int.Parse(tokens[1]);

        var positionString = tokens[0];
        var coordinateParts = positionString.Substring(1).Split('.');

        var shelfNumber = int.Parse(coordinateParts[1]);

        // xxx giant hack here to make the label correspond to the correct box. Magic constants and all
        // that. One day may be fixed (ie the data should be fixed not the label being adjusted in code).
        label = positionString[0].ToString() + (6-shelfNumber).ToString() + "." + coordinateParts[0];

        var boxIdTextTokens = tokens[0].Substring(1).Split('.');
        x = int.Parse(boxIdTextTokens[0]) - 1;
        y = int.Parse(boxIdTextTokens[1]) - 1;
    }

    public string ToString(string itemName)
    {
        return label + ": " + itemName + ", " + count;
    }
}

