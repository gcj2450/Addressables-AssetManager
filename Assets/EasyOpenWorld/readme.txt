Go to EasyOpenWorld/Tiles/ and drag all of the scenes to the Scenes in Build list (File > Build settings or Ctrl+Shift+B)

Open the base scene at EasyOpenWorld/Base
Find _EasyOpenWorld gameobject and attached EasyOpenWorld script, expand the script to draw the grid.

Click the Map button on screen to toggle the tile map. Left click a tile to load/unload it, right click to zoom into it. Loaded scenes are colored dark green, currently active scene (scene where drag&dropped prefabs will go to) is colored bright green.

When you start the game, no other scenes except the base scene should be loaded. You can automatically unload all tiles before playing by clicking the on-screen Play button.

The base scene should always be open, other tile scenes in EasyOpenWorld/Tiles/ are automatically loaded based on player position. Player object can be defined in the base scene, EasyOpenWorld script

World size and tile size can be changed in the base scene, EasyOpenWorld script

How to add new tiles:
	A. Add an empty scene to EasyOpenWorld/Tiles/, name it with its coordinates "x_y" and drag it to the Scenes in Build list (File > Build settings or Ctrl+Shift+B)
	or
	B. Open Map and click the "Fill" button, this will automatically create empty scenes to fill the map. You can select all tiles in the Scenes folder (Ctrl+A), then drag all the scenes to the Scenes in Build list (File > Build settings or Ctrl+Shift+B)

Tutorial video: https://www.youtube.com/watch?v=wVf7FJ_O_0c

Need help?
Discord: Lukebox#8482
Email: lukebox@hailgames.net



-- Troubleshooting -- 

	Tiles are not loading?
		- Make sure there's EasyOpenWorld script in your base scene. If not, drag the _EasyOpenWorld prefab to the base scene
		- Go to EasyOpenWorld script in the base scene and check that the Player is set to your player gameobject https://i.gyazo.com/2fc8036cefbabac81c5603c9949c343f.png
		- Add all tile scenes in EasyOpenWorld/Tiles/ to the Scenes in Build list https://i.gyazo.com/8a38bc95b3be88fb8dc30351cbc73c86.mp4
		
	Duplicate gameobjects, script errors?
		- Make sure no other tiles are loaded when the game starts, you can use the on-screen Play button to unload all tiles automatically before starting the game

	Maps are not loading correctly? Missing gameobjects?
		- When building maps, be careful not to place gameobjects in the wrong scene (otherwise they won't be loaded correctly). Make sure the tile of the map you're working on is active. You can drag & drop gameobjects from a loaded scene to another if you've made this mistake.
		