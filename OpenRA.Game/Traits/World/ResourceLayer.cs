#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class ResourceLayerInfo : TraitInfo<ResourceLayer> { }

	public class ResourceLayer: IRenderOverlay, IWorldLoaded
	{
		World world;

		public ResourceType[] resourceTypes;
		CellContents[,] content;
		
		public void Render( WorldRenderer wr )
		{
			foreach( var rt in world.WorldActor.TraitsImplementing<ResourceType>() )
				rt.info.PaletteIndex = wr.GetPaletteIndex(rt.info.Palette);

			var clip = Game.viewport.WorldBounds(world);
			for (int x = clip.Left; x < clip.Right; x++)
				for (int y = clip.Top; y < clip.Bottom; y++)
				{
					if (!world.LocalShroud.IsExplored(new int2(x, y)))
						continue;

					var c = content[x, y];
					if (c.image != null)
						c.image[c.density].DrawAt(
							Game.CellSize * new int2(x, y),
							c.type.info.PaletteIndex);
				}
		}

		public void WorldLoaded(World w)
		{
			this.world = w;
			content = new CellContents[w.Map.MapSize.X, w.Map.MapSize.Y];

			resourceTypes = w.WorldActor.TraitsImplementing<ResourceType>().ToArray();
			foreach (var rt in resourceTypes)
				rt.info.Sprites = rt.info.SpriteNames.Select(a => SpriteSheetBuilder.LoadAllSprites(a)).ToArray();

			var map = w.Map;

			for (int x = map.Bounds.Left; x < map.Bounds.Right; x++)
				for (int y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
				{
                    var type = resourceTypes.FirstOrDefault(
						r => r.info.ResourceType == w.Map.MapResources[x, y].type);

                    if (type == null)
                        continue;

					if (!AllowResourceAt(type, new int2(x,y)))
						continue;

                    content[x, y].type = type;
					content[x, y].image = ChooseContent(type);
				}

			for (int x = map.Bounds.Left; x < map.Bounds.Right; x++)
				for (int y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
					if (content[x, y].type != null)
					{
						content[x, y].density = GetIdealDensity(x, y);
						w.Map.CustomTerrain[x, y] = content[x, y].type.info.TerrainType;
					}
		}

        public bool AllowResourceAt(ResourceType rt, int2 a)
        {
            if (!world.Map.IsInMap(a.X, a.Y)) return false;
            if (!rt.info.AllowedTerrainTypes.Contains(world.GetTerrainInfo(a).Type)) return false;
            if (!rt.info.AllowUnderActors && world.WorldActor.Trait<UnitInfluence>().AnyUnitsAt(a)) return false;
            return true;
        }

		Sprite[] ChooseContent(ResourceType t)
		{
			return t.info.Sprites[world.SharedRandom.Next(t.info.Sprites.Length)];
		}

		int GetAdjacentCellsWith(ResourceType t, int i, int j)
		{
			int sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					if (content[i+u, j+v].type == t)
						++sum;
			return sum;
		}

		int GetIdealDensity(int x, int y)
		{
			return (GetAdjacentCellsWith(content[x, y].type, x, y) *
				(content[x, y].image.Length - 1)) / 9;
		}

		public void AddResource(ResourceType t, int i, int j, int n)
		{
			if (content[i, j].type == null)
			{
				content[i, j].type = t;
				content[i, j].image = ChooseContent(t);
				content[i, j].density = -1;
			}

			if (content[i, j].type != t)
				return;

			content[i, j].density = Math.Min(
				content[i, j].image.Length - 1, 
				content[i, j].density + n);
			
			world.Map.CustomTerrain[i,j] = t.info.TerrainType;
		}

		public bool IsFull(int i, int j) { return content[i, j].density == content[i, j].image.Length - 1; }

		public ResourceType Harvest(int2 p)
		{
			var type = content[p.X,p.Y].type;
			if (type == null) return null;

			if (--content[p.X, p.Y].density < 0)
			{
				content[p.X, p.Y].type = null;
				content[p.X, p.Y].image = null;
				world.Map.CustomTerrain[p.X, p.Y] = null;
			}
			return type;
		}

		public void Destroy(int2 p)
		{
			content[p.X, p.Y].type = null;
			content[p.X, p.Y].image = null;
			content[p.X, p.Y].density = 0;
			world.Map.CustomTerrain[p.X, p.Y] = null;
		}

		public ResourceType GetResource(int2 p) { return content[p.X, p.Y].type; }

		public struct CellContents
		{
			public ResourceType type;
			public Sprite[] image;
			public int density;
		}
	}
}
