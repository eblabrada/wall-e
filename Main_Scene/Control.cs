using Godot;
using System;
using Geometry;

//    <TargetFramework>net472</TargetFramework>


public partial class Control : Godot.Control
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}


	public void _on_Button_pressed() 
	{
		Update_GeoExpr_Window(GetNode<MarginContainer>("Draw_Area_Marg"));

		var code = GetNode<TextEdit>("Code_Edit_Marg/Code_Edit");
		var draw_area = GetNode<Node2D>("Draw_Area_Marg/Viewport_Container/SubViewport/Background/Node2D");

		// var txt = code.Text;
		// var data = txt.Split('\n');
		// var v1 = double.Parse(data[0]);
		// var v2 = -double.Parse(data[1]);
		// var rad = double.Parse(data[2]);

		// draw_area.Clear();
		draw_area.AddDrawable(new Circle(), new Color(1000));
		draw_area.ReDraw();
		// GD.Print(txt);
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
	// public override void _Process(float delta)
	// {
	// 	// var draw_area_margin = GetNode<MarginContainer>("Draw_Area_Marg");
	// 	// // var top_left = draw_area_margin.RectPosition;	
	// 	// // var top_right = new Vector2(top_left.x + draw_area_margin.RectSize.x, top_left.y);
	// 	// // var botton_left = new Vector2(top_left.x, top_left.y + draw_area_margin.RectSize.y);
	// 	// // var botton_right = draw_area_margin.RectSize + top_left;
	// 	// var x = draw_area_margin.RectSize.x/2;
	// 	// var y = draw_area_margin.RectSize.y/2;

	// 	// var draw_area = GetNode<Node2D>("Draw_Area_Marg/Viewport_Container/Viewport/Background/Node2D");
	// 	// draw_area.Transform = new Transform2D(new Vector2(1, 0), new Vector2(0, 1), new Vector2(x, y));
	// 	// draw_area.Update();
	// }

	private void Update_GeoExpr_Window(MarginContainer draw_area_container)
	{
		GeoExpr.UpdateWindow(
			0, draw_area_container.Size.X,
			0, draw_area_container.Size.Y
		);
	}
}

