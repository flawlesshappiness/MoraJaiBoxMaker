using Godot;

public partial class CornerButton : Button
{
    [Export]
    public ColorRect ColorRect;

    public Vector2I Target { get; set; }

    public int ExpectedColor { get; private set; }
    public bool Filled { get; private set; }

    public void SetExpectedColor(int color)
    {
        ExpectedColor = color;
        UpdateColor();
    }

    public void UpdateColor()
    {
        ColorRect.Color = new Color(MoraJaiButton.GetColorHex(ExpectedColor));
        ColorRect.Modulate = Colors.White.SetA(Filled ? 1.0f : 0.5f);
    }

    public void SetFilled(bool filled)
    {
        Filled = filled;
        UpdateColor();
    }
}
