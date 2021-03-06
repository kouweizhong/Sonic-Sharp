﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NTiled;
using System.IO;

namespace SonicSharp
{
    public static class Level
    {
        public static List<Tileset> tilesets = new List<Tileset>();
        public static List<Tile> tiles = new List<Tile>();
        public static List<gameObject> objects = new List<gameObject>();
        public static Vector2[] playerstarts = new Vector2[3], camerastarts = new Vector2[3];
        public static int onscreentilecount = 0;

        public static void Load(string leveldir, string filename)
        {
            string fullname = Main.startdir + "\\Levels\\" + leveldir + "\\" + filename;
            if (File.Exists(fullname))
            {
                //Load the map using NTiled
                XDocument document = XDocument.Load(fullname);
                TiledMap map = new TiledReader().Read(document);

                //Tilesets
                foreach (TiledTileset tileset in map.Tilesets)
                {
                    if (tileset.Image != null && File.Exists(Main.startdir + "\\Levels\\" + leveldir + "\\" + tileset.Image.Source))
                    {
                        Program.game.Content.RootDirectory = "Levels";
                        Tileset ts = new Tileset(Program.game.Content.Load<Texture2D>(leveldir + "\\" + new FileInfo(tileset.Image.Source).Name));

                        //Generate all the tile rectangles within the tileset.
                        int i = 0;
                        for (int y = 0; y < tileset.Image.Height; y += 16)
                        {
                            for (int x = 0; x < tileset.Image.Width; x += 16)
                            {
                                ts.tilesetparts.Add(new Rectangle(x, y, 16, 16));
                                AssignProperties(tileset, i, ts);
                                i++;
                            }
                        }

                        tilesets.Add(ts);
                    }
                }

                //Layers
                foreach (TiledLayer layer in map.Layers)
                {
                    TiledTileLayer tlayer = layer as TiledTileLayer;
                    TiledObjectGroup olayer = layer as TiledObjectGroup;

                    if (tlayer != null)
                    {
                        int i = 0, tilesetid = 0;
                        for (int y = 0; y < layer.Height * 16; y += 16)
                        {
                            for (int x = 0; x < layer.Width * 16; x += 16)
                            {
                                if (tlayer.Tiles[i] != 0)
                                {
                                    //TODO: Find the correct tileset for each tile.
                                    //OLD CODE:

                                    //if (!tilesets[tilesetid].tilesetparts.Count > !tilesets[tilesetid].tileids.Contains(tlayer.Tiles[i]))
                                    //{
                                    //    Console.WriteLine(tlayer.Tiles[i]);
                                    //    //tilesetid = -1;
                                    //    for (int tsi = 0; tsi < tilesets.Count; tsi++)
                                    //    {
                                    //        if (tilesets[tsi].tileids.Contains(tlayer.Tiles[i])) { tilesetid = tsi; break; }
                                    //    }
                                    //}
                                    //else { Console.WriteLine(tlayer.Tiles[i]); }

                                    //Spawn all the tiles
                                    if (tilesetid != -1)
                                    {
                                        tiles.Add(new Tile(tilesetid, tlayer.Tiles[i] - 1, tilesets[tilesetid].tilesetparts[tlayer.Tiles[i] - 1], new Vector2(x, y)));
                                    }
                                }
                                i++;
                            }
                        }
                    }

                    if (olayer != null)
                    {
                        foreach (TiledObject obj in olayer.Objects)
                        {
                            switch (obj.Type)
                            {
                                case "Camera H Border Lock": objects.Add(new CameraHBorder(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height), obj.Properties.ContainsKey("Stops Player") ? (obj.Properties["Stops Player"] == "1" || obj.Properties["Stops Player"].ToUpper() == "TRUE") : false)); break;
                                case "Camera V Border Lock": objects.Add(new CameraVBorder(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height), obj.Properties.ContainsKey("Stops Player") ? (obj.Properties["Stops Player"] == "1" || obj.Properties["Stops Player"].ToUpper() == "TRUE") : false)); break;
                                case "Death Trigger": objects.Add(new DeathTrigger(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height))); break;
                                case "Ring": objects.Add(new Ring((float)obj.X, (float)obj.Y)); break;
                            }
                        }
                    }
                }

                //Assign Playerstarts
                if (map.Properties["Sonic Player Start"] != null) { playerstarts[0] = new Vector2(Convert.ToSingle(map.Properties["Sonic Player Start"].Split(',')[0]), Convert.ToSingle(map.Properties["Sonic Player Start"].Split(',')[1])); }
                if (map.Properties["Tails Player Start"] != null) { playerstarts[1] = new Vector2(Convert.ToSingle(map.Properties["Tails Player Start"].Split(',')[0]), Convert.ToSingle(map.Properties["Tails Player Start"].Split(',')[1])); }
                if (map.Properties["Knuckles Player Start"] != null) { playerstarts[2] = new Vector2(Convert.ToSingle(map.Properties["Knuckles Player Start"].Split(',')[0]), Convert.ToSingle(map.Properties["Knuckles Player Start"].Split(',')[1])); }

                //Assign Camerastarts
                if (map.Properties["Sonic Camera Start"] != null) { camerastarts[0] = new Vector2(Convert.ToSingle(map.Properties["Sonic Camera Start"].Split(',')[0]), Convert.ToSingle(map.Properties["Sonic Camera Start"].Split(',')[1])); }
                if (map.Properties["Tails Camera Start"] != null) { camerastarts[1] = new Vector2(Convert.ToSingle(map.Properties["Tails Camera Start"].Split(',')[0]), Convert.ToSingle(map.Properties["Tails Camera Start"].Split(',')[1])); }
                if (map.Properties["Knuckles Camera Start"] != null) { camerastarts[2] = new Vector2(Convert.ToSingle(map.Properties["Knuckles Camera Start"].Split(',')[0]), Convert.ToSingle(map.Properties["Knuckles Camera Start"].Split(',')[1])); }

                //TODO: Fix the camerastart assignments.

                //Move players to their correct start positions
                foreach (Player plr in Main.players)
                {
                    if (plr.GetType() == typeof(Sonic)) { plr.pos = playerstarts[0]; }
                    else if (plr.GetType() == typeof(Tails)) { plr.pos = playerstarts[1]; }
                    else if (plr.GetType() == typeof(Knuckles)) { plr.pos = playerstarts[2]; }

                    if (plr.GetType() == typeof(Sonic)) { Camera.pos = camerastarts[0]*Main.scalemodifier; }
                    else if (plr.GetType() == typeof(Tails)) { Camera.pos = camerastarts[1] * Main.scalemodifier; }
                    else if (plr.GetType() == typeof(Knuckles)) { Camera.pos = camerastarts[2] * Main.scalemodifier; }

                    plr.active = true;
                }

                Program.game.Content.RootDirectory = "Content";
            }
            else
            {
                MessageBox.Show("ERROR: The given level (\"" + filename + "\") does not exist!","SoniC#",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        private static void AssignProperties(TiledTileset tileset, int i, Tileset ts)
        {
            foreach (TiledTile tile in tileset.Tiles)
            {
                if (tile.Id == i) { ts.tileproperties.Add((tile.Properties.Count > 0) ? tile.Properties : null); return; }
            }
            ts.tileproperties.Add(null);
        }

        public static void UnLoad()
        {
            //De-activate all the players
            for (int i = 0; i < Main.players.Count; i++)
            {
                Main.players[i].active = false;
            }
        }

        public static void Update()
        {
            for (int i = 0; i < Main.players.Count; i++)
            {
                Main.players[i].Update();
            }

            Camera.Update();
            Ring.ringsprite.Animate(Ring.ringsprite.framerate);

            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].Update();
            }
        }

        public static void Draw()
        {
            if (Main.debugmode) { onscreentilecount = 0; }

            //Draw the tiles...
            foreach (Tile tile in tiles)
            {
                if (tile.pos.X + 32 >= Camera.pos.X / Main.scalemodifier && tile.pos.X - 32 <= Camera.pos.X/Main.scalemodifier + Program.game.Window.ClientBounds.Width / Main.scalemodifier && tile.pos.Y + 32 >= Camera.pos.Y / Main.scalemodifier && tile.pos.Y - 32 <= Camera.pos.Y / Main.scalemodifier + Program.game.Window.ClientBounds.Height / Main.scalemodifier)
                {
                    tile.Draw();
                    if (Main.debugmode) { onscreentilecount++; }
                }
            }

            //Then the players...
            for (int i = 0; i < Main.players.Count; i++)
            {
                Main.players[i].Draw();
            }
            
            //And, lastly, the objects..
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].Draw();
            }
        }
    }
}
