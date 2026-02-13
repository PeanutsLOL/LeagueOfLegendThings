using System.IO;
using Terraria.ModLoader;
using Terraria;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings
{
	public enum LeaguePacketType : byte
	{
		SyncPlayerVitals = 1,
		ElectrocuteFx = 2,
		DarkHarvestProcSfx = 3,
		DarkHarvestGainSfx = 4,
		DarkHarvestFinalSfx = 5
	}

	public class LeagueOfLegendThings : Mod
	{
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			LeaguePacketType packetType = (LeaguePacketType)reader.ReadByte();

			switch (packetType)
			{
				case LeaguePacketType.SyncPlayerVitals:
				{
					int playerId = reader.ReadInt32();
					int life = reader.ReadInt32();
					int mana = reader.ReadInt32();

					if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient && playerId >= 0 && playerId < Main.maxPlayers)
					{
						Player player = Main.player[playerId];
						if (player.active)
						{
							player.statLife = life;
							player.statMana = mana;
						}
					}
					break;
				}
					case LeaguePacketType.ElectrocuteFx:
					{
						float startX = reader.ReadSingle();
						float startY = reader.ReadSingle();
						float endX = reader.ReadSingle();
						float endY = reader.ReadSingle();

						if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
						{
							Vector2 startPos = new Vector2(startX, startY);
							Vector2 endPos = new Vector2(endX, endY);
							LightningBoltSystem.SpawnBolt(startPos, endPos, Color.Red, duration: 60, width: 7.5f, segments: 14);
							var sfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Electrocute_SFX")
							{
								Volume = 0.75f,
								PitchVariance = 0.5f
							};
							SoundEngine.PlaySound(sfx, endPos);
						}
						break;
					}
					case LeaguePacketType.DarkHarvestProcSfx:
					case LeaguePacketType.DarkHarvestGainSfx:
					case LeaguePacketType.DarkHarvestFinalSfx:
					{
						float x = reader.ReadSingle();
						float y = reader.ReadSingle();
						if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
						{
							SoundStyle style = packetType switch
							{
								LeaguePacketType.DarkHarvestProcSfx => new SoundStyle("LeagueOfLegendThings/Content/Buffs/Dark_Harvest_SFX_2") { Volume = 0.8f, PitchVariance = 0f },
								LeaguePacketType.DarkHarvestGainSfx => new SoundStyle("LeagueOfLegendThings/Content/Buffs/Dark_Harvest_SFX") { Volume = 0.8f, PitchVariance = 0f },
								_ => new SoundStyle("LeagueOfLegendThings/Content/Buffs/Dark_Harvest_SFX_4") { Volume = 0.8f, PitchVariance = 0f }
							};
							SoundEngine.PlaySound(style, new Vector2(x, y));
						}
						break;
					}
			}
		}

	}
}
