using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace IINACT.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;

    public MainWindow(Plugin plugin) : base(
        "My Amazing Window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        Plugin = plugin;
    }

    public override void Draw()
    {
        ImGui.Text($"The random config bool is {Plugin.Configuration.DisableDamageShield}");

        if (ImGui.Button("Show Settings"))
        {
            Plugin.DrawConfigUI();
        }

        ImGui.Spacing();

        ImGui.Text("Have a goat:");
        ImGui.Indent(55);
        ImGui.Unindent(55);
    }

    public void Dispose()
    {
    }
}
