using System;

// Represents one action in the context menu when looking at an interactable.
// Actions are auto-generated from components on the object (e.g. Interactable
// adds "Pick Up", a snapped MountPointOut adds "Enter Shooting Mode").
public enum HandSide { Left, Right }

public class InteractionAction
{
    public string Label { get; }
    public Action<HandSide> Execute { get; }

    public InteractionAction(string label, Action<HandSide> execute)
    {
        Label = label;
        Execute = execute;
    }
}
