Read the main README.md in the project root for an overall flavour of the game/coding structure. This is C#.
I prefer comments above functions/methods. along with a commented line of ========================================================= so i can easily see when editing code in godot
I prefer some comment at top of each script to explain what the script is doing.
I prefer a .md file inside each major folder to explain whats inside this folder.
I prefer a masterconfig file inside each major capslock folder to group easily frequent tweakable settings
I prefer you organizing functions/methods into regions when appropriate.
I prefer to organize my scripts for ease of finding in future etc.
I do like you to vertically compress code where possible, so it's a little easier to read on screen.
I also prefer my code to be not overly complex, more like simplicity, while still achieving the gameplay I desire + performance friendly
I like re-using code when possible, I.E we use a helper function for staggered checks for enemies and things, similar to my Macros i had in my previous gamemaker vector game etc.
Please keep in mind I like optimization where possible too. Performance oriented.
I like keeping things containted and modular for easy expansion and easy tweaking of things without having to rewrite other things.
I like Node helpers as well so that the scene trees to not become cluttered. we should group common things in a shared plain node.
I like grouping similar variables in structs / export groups for inspector so it shows sub categories etc.
Check out my isometric 3d and isometric 2d github repositories for code/folder structure references.
If an .md file is available in the project for a section/feature/mechanic we are editing, refer to the .md as sometimes it will have extra context. Some might be outdated. Update md files appropriately when completing work.
Only do one commit for github per job instead of multiple commits.


Refer to https://github.com/ben0kay/godot-tower-defense for folder / notes structure
Refer to https://github.com/ben0kay/isometric-new-2d for similar game mechanics
Refer to https://github.com/ben0kay/Ben_MonsterExpansion for Grim Dawns spawn mechanics just for ideas