using Godot;
using System;
using static Godot.Input;

public partial class PlayerLook : Marker3D
{
    [Export] Movement movement;
    [Export] Camera3D cam;
    [Export] public bool PlayerRotationEnabled = true;
    [Export] public float mouseSens = 0.2f;
    [Export] float cam_accel = 40f;
    [Export] public Vector2 _rotation;
    const float SENS_M = 0.2f;
    public override void _Ready()
    {
        Position = movement.Head.GlobalPosition;

        MouseMode = MouseModeEnum.Captured; //Sets the mouse to captured
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!PlayerRotationEnabled) return;
        if (@event.IsActionPressed("shoot")) //Mouse0
        {
            MouseMode = MouseModeEnum.Captured;
        }
        if (@event.IsActionPressed("ui_cancel")) // ESC
        {
            MouseMode = MouseModeEnum.Visible;
        }
        if (@event is InputEventMouseMotion mouseMotion)
        {
            // modify accumulated mouse rotation
            _rotation += (-mouseMotion.Relative * mouseSens * SENS_M);

            _rotation.y = Mathf.Clamp(_rotation.y, -89f, 89f);
            
            Rotation = new Vector3(0, Mathf.DegToRad(_rotation.x), 0);
            cam.Rotation = new Vector3(Mathf.DegToRad(_rotation.y), 0, 0);

        }
    }
    public override void _Process(double delta)
    {
        //Camera Interpolation to fix jittering
        if (Engine.GetFramesPerSecond() > Engine.PhysicsTicksPerSecond)
        {
            Position = Position.Lerp(movement.Head.GlobalPosition, cam_accel * (float)delta);
        }
        else
        {
            Position = movement.Head.GlobalPosition;
        }
    }
}
