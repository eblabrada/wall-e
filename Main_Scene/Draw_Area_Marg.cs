using Godot;
using System;
using Geometry;

public partial class Draw_Area_Marg : MarginContainer
{
	private float axesVectorMultiplier = 1;
	private Node2D draw_area;
	public Vector2 Traslation;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Traslation = new(0, 0);
		Center_Transform();
		Update_GeoExpr_Window();
		draw_area = GetNode<Node2D>("Viewport_Container/SubViewport/Background/Node2D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void Update_GeoExpr_Window()
	{	
		draw_area = GetNode<Node2D>("Viewport_Container/SubViewport/Background/Node2D");
		var Center = draw_area.Transform.Origin;
		// Center/=axesVectorMultiplier;

		float x1 = Center.X - Size.X/2;
		float x2 = Center.X + Size.X/2;

		// GD.Print("x1: ", x1);
		// GD.Print("x2: ", x2);
		// GD.Print("ctr: ", Center);
		// GD.Print("x2 - x1 = ", x2 - x1);
		// GD.Print();


		float y1 = Center.Y - Size.Y/2;
		float y2 = Center.Y + Size.Y/2;
		
		// IDrawable.UpdateWindow(
		// 	-this.Size.X/2/axesVectorMultiplier, this.Size.X/2/axesVectorMultiplier,
		// 	-this.Size.Y/2/axesVectorMultiplier, this.Size.Y/2/axesVectorMultiplier
		// );

		IDrawable.UpdateWindow(
			x1, x2,
			y1, y2
		);

	}

	private void Center_Transform()
	{
		var draw_area = GetNode<Node2D>("Viewport_Container/SubViewport/Background/Node2D");

		var x = this.Size.X/2;
		var y = this.Size.Y/2;

		GD.Print("SIZE: " ,this.Size);
		draw_area.Transform = new Transform2D(
			new Vector2(axesVectorMultiplier, 0),
			new Vector2(0, -axesVectorMultiplier), 
			new(x, y)
		);

		GD.Print("ORIGIN: ", draw_area.Transform.Origin);
		GD.Print("TRANSLATION: ", Traslation);
	}

	void _on_item_rect_changed()
	{
		Center_Transform();
		Update_GeoExpr_Window();
	}

	private void ZoomIn()
	{
		axesVectorMultiplier*=1.1f;
		Vector2 curCenter = this.Size/2;

		var trasVector = -draw_area.Transform.Origin + curCenter;
		Traslation -= trasVector*0.1f;

		Center_Transform();
		Update_GeoExpr_Window();
		draw_area.QueueRedraw();
	}

	private void ZoomOut()
	{
		axesVectorMultiplier*=0.9f;

		Vector2 curCenter = this.Size/2;

		var trasVector = -draw_area.Transform.Origin + curCenter;
		Traslation += trasVector*0.1f;

		Center_Transform();
		Update_GeoExpr_Window();
		draw_area.QueueRedraw();
	}

	private void Move(Vector2 vector)
	{
		Traslation += vector;
		Center_Transform();
		Update_GeoExpr_Window();
		draw_area.QueueRedraw();
	}

    public override void _Input(InputEvent @event)
    {
		if (!HasFocus()) return;

		if (@event is InputEventKey key && key.Pressed)
		{
			if (key.ShiftPressed && key.Keycode == Key.Up)
				ZoomIn();
			else if (key.ShiftPressed && key.Keycode == Key.Down)
				ZoomOut();

			else if (key.Keycode == Key.Up)
				Move(new(0, this.Size.Y/50));

			else if (key.Keycode == Key.Down)
				Move(new(0, -this.Size.Y/50));

			else if (key.Keycode == Key.Left)
				Move(new(this.Size.Y/50, 0));

			else if (key.Keycode == Key.Right)
				Move(new(-this.Size.Y/50, 0));


		}
	}

}
