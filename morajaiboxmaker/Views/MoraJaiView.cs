using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MoraJaiView : Control
{
    [Export]
    public Label SolvedLabel;

    [Export]
    public CheckBox EditCheckBox;

    [Export]
    public TextEdit DataTextEdit;

    [Export]
    public Button ExportButton;

    [Export]
    public Button LoadButton;

    [Export]
    public Button ClearButton;

    [Export]
    public Button PremadeLevelButtonTemplate;

    [Export]
    AudioStreamPlayer SfxHover;

    [Export]
    AudioStreamPlayer SfxClick;

    [Export]
    AudioStreamPlayer SfxSolved;

    [Export]
    public Array<MoraJaiButton> MoraJaiButtons;

    [Export]
    public Array<CornerButton> CornerButtons;

    private Grid<MoraJaiButton> Grid { get; set; }
    private bool IsEditing => EditCheckBox.ButtonPressed;


    private List<string> PremadeLevels = new()
    {
        "000000-000001-000000-01010101",
        "000002-000602-000202-02020202"
    };

    public override void _Ready()
    {
        base._Ready();
        InitializeGrid();
        InitializeCorners();
        InitializeEdit();
        InitializeOtherButtons();
        InitializePremadeLevelButtons();
        Clear();
    }

    private void InitializeOtherButtons()
    {
        ExportButton.Pressed += PressedExport;
        LoadButton.Pressed += PressedLoad;
        ClearButton.Pressed += Clear;

        InitializeButtonSounds(ExportButton);
        InitializeButtonSounds(LoadButton);
        InitializeButtonSounds(ClearButton);
    }

    private void InitializeEdit()
    {
        EditCheckBox.Toggled += EditToggled;
    }

    private void InitializeGrid()
    {
        Grid = new Grid<MoraJaiButton>();

        var rows = 3;
        var columns = 3;
        for (int y = 0; y < columns; y++)
        {
            for (int x = 0; x < rows; x++)
            {
                var i = x + y * columns;
                var button = MoraJaiButtons[i];
                button.Coords = new Vector2I(x, y);
                button.Pressed += () => PressMoraJaiButton(button);
                InitializeButtonSounds(button);
                Grid.Set(x, y, button);
            }
        }
    }

    private void InitializeCorners()
    {
        CornerButtons[0].Target = new Vector2I(0, 0);
        CornerButtons[1].Target = new Vector2I(2, 0);
        CornerButtons[2].Target = new Vector2I(2, 2);
        CornerButtons[3].Target = new Vector2I(0, 2);

        foreach (var button in CornerButtons)
        {
            button.Pressed += () => PressCornerButton(button);
            InitializeButtonSounds(button);
        }
    }

    private void InitializePremadeLevelButtons()
    {
        var parent = PremadeLevelButtonTemplate.GetParent();

        for (int i = 0; i < PremadeLevels.Count; i++)
        {
            var level = PremadeLevels[i];
            var button = PremadeLevelButtonTemplate.Duplicate() as Button;
            parent.AddChild(button);
            button.Text = "Premade " + (i + 1);
            button.Pressed += () => Load(level);
            InitializeButtonSounds(button);
        }

        PremadeLevelButtonTemplate.Hide();
    }

    private void InitializeButtonSounds(Button button)
    {
        button.Pressed += () => PlayClickSfx();
        button.MouseEntered += () => PlayHoverSfx();
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventKey key_event)
        {
            if (key_event.Keycode == Key.Escape)
            {
                GetTree().Quit();
            }
        }
    }

    private void Clear()
    {
        Load("000000-000000-000000-00000000");
    }

    private void Load(string data)
    {
        data = data.Replace(" ", "").Replace("-", "");

        if (data.Length < 13 * 2) return;

        SetButtonColor(0, 0, GetColorFromData(0));
        SetButtonColor(1, 0, GetColorFromData(1));
        SetButtonColor(2, 0, GetColorFromData(2));
        SetButtonColor(0, 1, GetColorFromData(3));
        SetButtonColor(1, 1, GetColorFromData(4));
        SetButtonColor(2, 1, GetColorFromData(5));
        SetButtonColor(0, 2, GetColorFromData(6));
        SetButtonColor(1, 2, GetColorFromData(7));
        SetButtonColor(2, 2, GetColorFromData(8));

        SetTargetColors(
            GetColorFromData(9),
            GetColorFromData(10),
            GetColorFromData(11),
            GetColorFromData(12)
            );

        ResetSolved();

        int GetColorFromData(int i)
        {
            var s = data.Substring(i * 2, 2);
            var c = int.TryParse(s, out var _c) ? _c : 0;
            return c;
        }
    }

    private void SetTargetColors(MoraJaiColor nw, MoraJaiColor ne, MoraJaiColor se, MoraJaiColor sw) => SetTargetColors((int)nw, (int)ne, (int)se, (int)sw);
    private void SetTargetColors(int nw, int ne, int se, int sw)
    {
        CornerButtons[0].SetExpectedColor(nw);
        CornerButtons[1].SetExpectedColor(ne);
        CornerButtons[2].SetExpectedColor(se);
        CornerButtons[3].SetExpectedColor(sw);
    }

    private void PressCornerButton(CornerButton button)
    {
        if (IsEditing)
        {
            PressCornerEdit(button);
        }
        else
        {
            UpdateCornerButton(button, true);
        }

        ValidateSolution();
    }

    private void UpdateCornerButton(CornerButton button, bool allow_fill = false)
    {
        var target = GetButton(button.Target);
        var filled = target.IsColor(button.ExpectedColor) && (allow_fill || button.Filled) && !IsEditing;
        button.SetFilled(filled);
    }

    private void UpdateCornerButtons(bool allow_fill = false)
    {
        CornerButtons.ForEach(button => UpdateCornerButton(button, allow_fill));
    }

    private void SetColor(int x, int y, MoraJaiColor color) => SetButtonColor(x, y, (int)color);
    private void SetButtonColor(int x, int y, int color)
    {
        if (Grid.TryGet(x, y, out var button))
        {
            button.SetColor(color);
        }
    }

    private MoraJaiButton GetButton(Vector2I coord, bool wrap = false)
    {
        if (wrap)
        {
            coord.X = (coord.X + 3) % 3;
            coord.Y = (coord.Y + 3) % 3;
        }

        return Grid.TryGet(coord, out var button) ? button : null;
    }

    private void Swap(Vector2I coord1, Vector2I coord2, bool wrap = false)
    {
        var b1 = GetButton(coord1);
        var b2 = GetButton(coord2);
        if (b1 == null || b2 == null) return;
        if (b1 == b2) return;

        var temp = b2.CurrentColor;
        b2.SetColor(b1.CurrentColor);
        b1.SetColor(temp);
    }

    private void PressMoraJaiButton(MoraJaiButton button, int? color_override = null)
    {
        if (IsEditing)
        {
            PressButtonEdit(button);
        }
        else
        {
            switch (color_override ?? button.CurrentColor)
            {
                case 0: PressGrey(button); break;
                case 1: PressWhite(button); break;
                case 2: PressYellow(button); break;
                case 3: PressPurple(button); break;
                case 4: PressBlack(button); break;
                case 5: PressRed(button); break;
                case 6: PressPink(button); break;
                case 7: PressBlue(button); break;
                case 8: PressOrange(button); break;
                case 9: PressGreen(button); break;
                default: return;
            }
        }

        UpdateCornerButtons();
    }

    private List<MoraJaiButton> GetAdjacentNeighbours(MoraJaiButton button)
    {
        var neis = new List<MoraJaiButton>
        {
            GetButton(button.Coords + new Vector2I(0, -1)),
            GetButton(button.Coords + new Vector2I(1, 0)),
            GetButton(button.Coords + new Vector2I(0, 1)),
            GetButton(button.Coords + new Vector2I(-1, 0)),
        };

        return neis.Where(x => x != null).ToList();
    }

    private List<MoraJaiButton> GetSurroundingNeighbours(MoraJaiButton button)
    {
        var neis = new List<MoraJaiButton>
        {
            GetButton(button.Coords + new Vector2I(0, -1)),
            GetButton(button.Coords + new Vector2I(1, -1)),
            GetButton(button.Coords + new Vector2I(1, 0)),
            GetButton(button.Coords + new Vector2I(1, 1)),
            GetButton(button.Coords + new Vector2I(0, 1)),
            GetButton(button.Coords + new Vector2I(-1, 1)),
            GetButton(button.Coords + new Vector2I(-1, 0)),
            GetButton(button.Coords + new Vector2I(-1, -1)),
        };

        return neis.Where(x => x != null).ToList();
    }

    private void PressGrey(MoraJaiButton button)
    {
        // Do nothing
    }

    private void PressWhite(MoraJaiButton button)
    {
        GetAdjacentNeighbours(button).ForEach(x =>
        {
            if (x.IsColor(MoraJaiColor.Grey))
            {
                x.SetColor(button.CurrentColor);
            }
            else if (x.IsColor(button.CurrentColor))
            {
                x.SetColor(MoraJaiColor.Grey);
            }
        });

        button.SetColor(MoraJaiColor.Grey);
    }

    private void PressYellow(MoraJaiButton button)
    {
        Swap(button.Coords, new Vector2I(button.Coords.X, button.Coords.Y - 1));
    }

    private void PressPurple(MoraJaiButton button)
    {
        Swap(button.Coords, new Vector2I(button.Coords.X, button.Coords.Y + 1));
    }

    private void PressBlack(MoraJaiButton button)
    {
        var b1 = GetButton(new Vector2I(0, button.Coords.Y));
        var b2 = GetButton(new Vector2I(1, button.Coords.Y));
        var b3 = GetButton(new Vector2I(2, button.Coords.Y));
        var temp = b3.CurrentColor;
        b3.SetColor(b2.CurrentColor);
        b2.SetColor(b1.CurrentColor);
        b1.SetColor(temp);
    }

    private void PressRed(MoraJaiButton button)
    {
        MoraJaiButtons.ForEach(button =>
        {
            if (button.IsColor(MoraJaiColor.White))
            {
                button.SetColor(MoraJaiColor.Black);
            }
            else if (button.IsColor(MoraJaiColor.Black))
            {
                button.SetColor(MoraJaiColor.Red);
            }
        });
    }

    private void PressPink(MoraJaiButton button)
    {
        var neis = GetSurroundingNeighbours(button);
        var prev = neis.Last().CurrentColor;
        neis.ForEach(button =>
        {
            var current = button.CurrentColor;
            button.SetColor(prev);
            prev = current;
        });
    }

    private void PressBlue(MoraJaiButton button)
    {
        var middle = GetButton(new Vector2I(1, 1));
        PressMoraJaiButton(button, middle.CurrentColor);
    }

    private void PressOrange(MoraJaiButton button)
    {
        var neis = GetAdjacentNeighbours(button);
        var colors = neis.Select(nei => nei.CurrentColor);
        var no_majority = colors.Distinct().Count() == colors.Count();
        var majority = colors.GroupBy(i => i)
            .OrderByDescending(grp => grp.Count())
            .Select(grp => grp.Key).First();

        if (!no_majority)
        {
            button.SetColor(majority);
        }
    }

    private void PressGreen(MoraJaiButton button)
    {
        var x = button.Coords.X;
        x = x == 0 ? 2 : x == 2 ? 0 : 1;

        var y = button.Coords.Y;
        y = y == 0 ? 2 : y == 2 ? 0 : 1;

        Swap(button.Coords, new Vector2I(x, y));
    }

    private void PressButtonEdit(MoraJaiButton button)
    {
        var colors = Enum.GetValues(typeof(MoraJaiColor)).Cast<MoraJaiColor>().ToList();
        var next = (button.CurrentColor + 1) % colors.Count();
        button.SetColor(next);
    }

    private void PressCornerEdit(CornerButton button)
    {
        var colors = Enum.GetValues(typeof(MoraJaiColor)).Cast<MoraJaiColor>().ToList();
        var next = (button.ExpectedColor + 1) % colors.Count();
        button.SetExpectedColor(next);
    }

    private void ValidateSolution()
    {
        var valid = CornerButtons.All(button => button.Filled) && !IsEditing;

        if (valid)
        {
            SolvedLabel.Show();
            SfxSolved.Play();
        }
    }

    private void ResetSolved()
    {
        SolvedLabel.Hide();
    }

    private void EditToggled(bool toggled)
    {
        ResetSolved();
        UpdateCornerButtons();
    }

    private void PressedExport()
    {
        var data = string.Empty;
        data += GetDataString(GetButton(new Vector2I(0, 0)).CurrentColor);
        data += GetDataString(GetButton(new Vector2I(1, 0)).CurrentColor);
        data += GetDataString(GetButton(new Vector2I(2, 0)).CurrentColor);
        data += "-";
        data += GetDataString(GetButton(new Vector2I(0, 1)).CurrentColor);
        data += GetDataString(GetButton(new Vector2I(1, 1)).CurrentColor);
        data += GetDataString(GetButton(new Vector2I(2, 1)).CurrentColor);
        data += "-";
        data += GetDataString(GetButton(new Vector2I(0, 2)).CurrentColor);
        data += GetDataString(GetButton(new Vector2I(1, 2)).CurrentColor);
        data += GetDataString(GetButton(new Vector2I(2, 2)).CurrentColor);
        data += "-";
        data += GetDataString(CornerButtons[0].ExpectedColor);
        data += GetDataString(CornerButtons[1].ExpectedColor);
        data += GetDataString(CornerButtons[2].ExpectedColor);
        data += GetDataString(CornerButtons[3].ExpectedColor);

        DataTextEdit.Text = data;

        string GetDataString(int i)
        {
            return i.ToString("00");
        }
    }

    private void PressedLoad()
    {
        Load(DataTextEdit.Text);
    }

    private void PlayHoverSfx()
    {
        SfxHover.Play();
    }

    private void PlayClickSfx()
    {
        SfxClick.Play();
    }
}
