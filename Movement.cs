using Godot;
using System;
using static Cmd;
using static Godot.Input;

public partial struct Cmd
{
    public static Vector2 move_input;
    public static bool wishJump;
}

// *** IMPORTANT ***
// Make sure the set your inputs (WASD keys) accordingly to ui keys (ui_left, ui_right, ui_up, ui_down) in the Editor
//This is my first released script so please don't expect a fully fledged FPS Controller, still working on it

public partial class Movement : CharacterBody3D
{

    Action onLand, onAir, onJump;
    [Export] public PlayerLook playerLook;
    [Export] public bool MovementEnabled = true;
    // Movement Varabiles
    [Export] float friction = 6f;
    [Export] float moveSpeed = 7.0f;   // Ground move speed
    [Export] float groundAccel = 14f;    // Ground accel
    [Export] float groundDeaccel = 10f;    // Deacceleration that occurs when running on the ground
    [Export] float airAccel = 2.0f;   // Air accel
    [Export] float airDeaccel = 2.0f;   // Deacceleration experienced when opposite strafing
    [Export] float jumpSpeed = 10f;   // The speed at which the characters up axis gains when hitting jump
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle(); // should be around 28 personally
    [Export] bool holdJumpToBhop = true;  // When enabled allows player to just hold jump button to keep on bhopping perfectly
    [Export] public bool Grounded = true;
    // All Vector Variables
    Vector3 playerVelocity, wishDir = Vector3.Zero;
    float playerTopVelocity = 0.0f;

    [Export] AudioStreamPlayer jumpSound, landSound;

    [Export] public Marker3D Head;
    [Export] Label speedLabel, wishDirLabel, TopSpeedLabel, FrameRateLabel;
    public override void _EnterTree()
    {
        onLand += doLanding;
        onLand += QueueJump;
        onAir += doOnAir;
    }
    public override void _ExitTree()
    {
        onLand -= doLanding;
        onLand -= QueueJump;
        onAir -= doOnAir;
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        WishJumpLogic(@event);
        setDir();
    }
    void HasLandedLogic()
    {
        if (Grounded != IsOnFloor())
        {
            if (!Grounded)
            {
                onLand?.Invoke();
                Grounded = true;
            }
            else
            {
                onAir?.Invoke();
                Grounded = false;
            }
        }
    }
    void doLanding()
    {
        landSound.Play();
    }
    void doOnAir()
    {
        //Do stuff the moment you start jumping
    }
    void WishJumpLogic(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept") && !wishJump)
            wishJump = true;
        if (@event.IsActionReleased("ui_accept"))
            wishJump = false;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!MovementEnabled) return;
        HasLandedLogic();

        if (Grounded) GroundMove();
        else AirMove();

        // This will move the player
        Velocity = playerVelocity;
        MoveAndSlide();
        ShowMovementStats();
        ShowFrameRateStats();
    }
    public void setDir()
    {
        move_input = GetVector("ui_left", "ui_right", "ui_up", "ui_down"); // Y is inverted, don't ask why it's Godot
        wishDir = new Vector3(move_input.x, 0, move_input.y).Rotated(Vector3.Up, playerLook.Rotation.y).Normalized();
    }
    void QueueJump()
    {
        if (holdJumpToBhop)
            wishJump = IsActionPressed("ui_accept");
    }
    float wishSpeed() => wishDir.LengthSquared() * moveSpeed;

    //Allows for movement to slightly increase as you move through the air
    void AirMove()
    {
        //snap = Vector3.Down;
        Accelerate(wishDir, wishSpeed(), airAccel); // accel
        playerVelocity.y += -gravity * (float)GetPhysicsProcessDeltaTime();
    }
    //[Export] Vector3 snap;
    private void GroundMove()
    {
        
        //snap = -GetFloorNormal();
        // Do not apply friction if the player is queueing up the next jump
        ApplyFriction(!wishJump);

        Accelerate(wishDir, wishSpeed(), groundAccel);
        if (wishJump)
        {
            jumpSound.Play();
            wishJump = false;
            //snap = Vector3.Zero;
            playerVelocity.y = jumpSpeed;
            onJump?.Invoke();
        }
    }
    private void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        
        float addspeed, accelspeed, currentspeed;
        currentspeed = playerVelocity.Dot(wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0) return;
        accelspeed = accel * (float)GetPhysicsProcessDeltaTime() * wishspeed;
        if (accelspeed > addspeed) { accelspeed = addspeed; }

        playerVelocity.x += accelspeed * wishdir.x;
        playerVelocity.z += accelspeed * wishdir.z;
    }


    //Applies friction to the player, called in both the air and on the ground
    private void ApplyFriction(bool enabled)
    {
        if (!enabled) return;
        Vector3 vec = playerVelocity; // Equivalent to: VectorCopy();
        float lastSpeed, newspeed, control, drop;
        drop = 0f; vec.y = 0f;
        lastSpeed = vec.Length();
        /* Only if the player is on the ground then apply friction */
        if (Grounded)
        {
            control = lastSpeed < groundDeaccel ? groundDeaccel : lastSpeed;
            drop = control * friction * (float)GetPhysicsProcessDeltaTime();
        }
        newspeed = lastSpeed - drop;
        //playerFriction = newspeed;
        if (newspeed < 0) { newspeed = 0; }
        if (lastSpeed > 0) { newspeed /= lastSpeed; }

        playerVelocity.x *= newspeed;
        playerVelocity.z *= newspeed;
    }

    //Some UI Stuff
    void ShowFrameRateStats()
    {
        FrameRateLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
    }
    void ShowMovementStats()
    {
        Vector3 labelVel = playerVelocity;
        labelVel.y = 0;

        // Show the player's current speed
        speedLabel.Text = $"Speed: {labelVel.Length()}";
        // Show the player's current wishDir
        wishDirLabel.Text = $"WishDir:\n X = {wishDir.x}\nZ = {wishDir.z}";

        if (labelVel.LengthSquared() > playerTopVelocity * playerTopVelocity) //avoiding sqrt for performance
        {
            playerTopVelocity = labelVel.Length();
        }
        TopSpeedLabel.Text = $"TopSpeed: {playerTopVelocity}";
    }
}
