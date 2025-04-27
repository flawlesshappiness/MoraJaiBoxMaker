using Godot;

public partial class MoraJaiButton : Button
{
    [Export]
    public ColorRect ColorRect;

    public Vector2I Coords { get; set; }
    public int CurrentColor { get; private set; }

    public void SetColor(MoraJaiColor color) => SetColor((int)color);
    public void SetColor(int i)
    {
        CurrentColor = i;
        Text = ""; // GetColorName(i);
        ColorRect.Color = new Color(GetColorHex(i));
    }

    public bool IsColor(MoraJaiColor color) => IsColor((int)color);
    public bool IsColor(int i) => CurrentColor == i;

    public static string GetColorName(int i) => i switch
    {
        0 => "Grey", // Does nothing
        1 => "White", // Changes adjacent greys to white
        2 => "Yellow", // Swaps with button above
        3 => "Purple", // Swaps with button below
        4 => "Black", // Moves row to the right
        5 => "Red", // Turns all white to black, and black to red
        6 => "Pink", // Rotates adjacent buttons clockwise
        7 => "Blue", // Imitates center color
        8 => "Orange", // Becomes color of neighbour majority
        9 => "Green", // Swap with opposite side
        _ => "Unknown"
    };

    public static string GetColorHex(int i) => i switch
    {
        0 => "737b80",
        1 => "cccccc",
        2 => "ccb852",
        3 => "633d99",
        4 => "222222",
        5 => "b33636",
        6 => "e673e6",
        7 => "295fcc",
        8 => "e67a45",
        9 => "29cc36",
        _ => "00000000"
    };
}

public enum MoraJaiColor
{
    Grey,
    White,
    Yellow,
    Purple,
    Black,
    Red,
    Pink,
    Blue,
    Orange,
    Green
}
