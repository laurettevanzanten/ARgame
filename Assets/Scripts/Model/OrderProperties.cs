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
        label = tokens[0];

        var boxIdTextTokens = tokens[0].Substring(1).Split('.');
        x = int.Parse(boxIdTextTokens[0]) - 1;
        y = int.Parse(boxIdTextTokens[1]) - 1;
    }

    public string ToString(string itemName)
    {
        return label + ": " + itemName + ", " + count;
    }
}

