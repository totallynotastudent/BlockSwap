using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;
using static FunctionalBlockSwap.ToolType;
using Hook = On.Terraria;
using Terraria.ID;

namespace FunctionalBlockSwap {
    public class FunctionalBlockSwap : Mod {
        public static ushort wallID = 0;

        public override void Load() {
            Hook.Player.PlaceThing+=PlaceThing;
        }
        public override void PostAddRecipes() {
            wallID = (ushort)ModContent.WallType<DummyWall>();
        }

        private void PlaceThing(Hook.Player.orig_PlaceThing orig, Player self) {
            Tile tile = Main.tile[Player.tileTargetX,Player.tileTargetY];
            Tile tile2 = Main.tile[Player.tileTargetX, Player.tileTargetY+1];
            ushort wall = tile2.wall;
            if(PlaceThingChecks(self)&&self.HeldItem.createTile>-1&&self.HeldItem.createTile!=tile.type&&tile.active()) {
                int selected = self.selectedItem;
                if(Main.tileHammer[tile.type]) {
                    bool breakTile = Main.tileNoFail[tile.type];
                    if(!breakTile&&WorldGen.CanKillTile(Player.tileTargetX, Player.tileTargetY)) {
                        self.selectedItem = GetBestToolSlot(self, out int power, toolType: Hammer);
                        if(tile.type == TileID.DemonAltar && (power < 80 || !Main.hardMode)) {
                            self.Hurt(PlayerDeathReason.ByOther(4), self.statLife / 2, -self.direction);
                        }
                    }
                    if(breakTile) {
                        WorldGen.KillTile(Player.tileTargetX, Player.tileTargetY);
                        SetWall(tile2);
                        if(Main.netMode == NetmodeID.MultiplayerClient) {
                            NetMessage.SendData(MessageID.TileChange, -1, -1, null, 0, Player.tileTargetX, Player.tileTargetY);
                        }
                    }
                }else if(!(Main.tileAxe[tile.type] || Main.tileHammer[tile.type]) && !Main.tileContainer[tile.type]) {
                    self.selectedItem = GetBestToolSlot(self, out int power, toolType: Pickaxe);
                    self.PickTile(Player.tileTargetX, Player.tileTargetY, power);
                    if(self.hitTile.data[tile.type].damage>0) {
		                AchievementsHelper.CurrentlyMining = true;
		                self.hitTile.Clear(tile.type);
			            WorldGen.KillTile(Player.tileTargetX, Player.tileTargetY);
                        SetWall(tile2);
				        AchievementsHelper.HandleMining();
			            if (Main.netMode == NetmodeID.MultiplayerClient){
				            NetMessage.SendData(MessageID.TileChange, -1, -1, null, 0, Player.tileTargetX, Player.tileTargetY);
			            }
		                AchievementsHelper.CurrentlyMining = false;
                    }else if(!tile.active()) {
                        tile.ResetToType(0);
                        tile.active(false);
                        SetWall(tile2);
                    }
                }
                self.selectedItem = selected;
            }
            if(Main.netMode == NetmodeID.MultiplayerClient) {
                ushort t = tile.type;
                orig(self);
				if(tile.type!=t)NetMessage.SendData(MessageID.TileChange, -1, -1, null, tile.type, Player.tileTargetX, Player.tileTargetY);
            } else {
                orig(self);
            }

            tile2.wall = wall;
        }
        static void SetWall(Tile tile) {
            tile.wall = wallID;
        }
        public static bool PlaceThingChecks(Player player) {
            int tileBoost = player.HeldItem.tileBoost;
            return !player.noBuilding&&
                (player.itemTime == 0 && player.itemAnimation > 0 && player.controlUseItem)&&
                (player.Left.X / 16f - Player.tileRangeX - tileBoost - player.blockRange <= Player.tileTargetX &&
                (player.Right.X) / 16f + Player.tileRangeX + tileBoost - 1f + player.blockRange >= Player.tileTargetX &&
                player.Top.Y / 16f - Player.tileRangeY - tileBoost - player.blockRange <= Player.tileTargetY &&
                (player.Bottom.Y) / 16f + Player.tileRangeY + tileBoost - 2f + player.blockRange >= Player.tileTargetY);
        }
        public static int GetBestToolSlot(Player player, out int power, ToolType toolType = Pickaxe) {
            int slot = player.selectedItem;
            power = 0;
            int i;
            int length = player.inventory.Length;
            switch(toolType) {
                case Pickaxe:
                for(i = 0; i < length; i++)
                    if(player.inventory[i].pick>power) {
                        power = player.inventory[i].pick;
                        slot = i;
                    }
                break;
                case Axe:
                for(i = 0; i < length; i++)
                    if(player.inventory[i].axe>power) {
                        power = player.inventory[i].axe;
                        slot = i;
                    }
                break;
                case Hammer:
                for(i = 0; i < length; i++)
                    if(player.inventory[i].hammer>power) {
                        power = player.inventory[i].hammer;
                        slot = i;
                    }
                break;
            }
            return slot;
        }
	}
    public class DummyWall : ModWall {
        public override bool Autoload(ref string name, ref string texture) {
            texture = "Terraria/Wall_1";
            return true;
        }
    }
    public enum ToolType {
        Pickaxe,
        Axe,
        Hammer
    }
}