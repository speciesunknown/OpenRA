#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using TUtil = OpenRA.Traits.Util;

namespace OpenRA.Mods.RA
{
	class IronCurtainPowerInfo : SupportPowerInfo
	{
		public readonly int Duration = 10; // Seconds
		public readonly int Range = 1; // Range in cells

		public override object Create(ActorInitializer init) { return new IronCurtainPower(init.self, this); }
	}

	class IronCurtainPower : SupportPower
	{
		public IronCurtainPower(Actor self, IronCurtainPowerInfo info) : base(self, info) { }
		public override IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectTarget(order, manager, this);
		}
		
		public override void Activate(Actor self, Order order)
		{
			self.Trait<RenderBuilding>().PlayCustomAnim(self, "active");

			Sound.Play("ironcur9.aud", Game.CellSize * order.TargetLocation);
			foreach (var target in UnitsInRange(order.TargetLocation))
				target.Trait<IronCurtainable>().Activate(target, (Info as IronCurtainPowerInfo).Duration * 25);
		}

		public IEnumerable<Actor> UnitsInRange(int2 xy)
		{
			int range = (Info as IronCurtainPowerInfo).Range;
			var uim = self.World.WorldActor.Trait<UnitInfluence>();
			var tiles = self.World.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(uim.GetUnitsAt(t));
			
			return units.Distinct().Where(a => a.HasTrait<IronCurtainable>());
		}
		
		class SelectTarget : IOrderGenerator
		{
			readonly IronCurtainPower power;
			readonly int range;
			readonly Sprite tile;
			readonly SupportPowerManager manager;
			readonly string order;
			
			public SelectTarget(string order, SupportPowerManager manager, IronCurtainPower power)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.range = (power.Info as IronCurtainPowerInfo).Range;
				tile = UiOverlay.SynthesizeTile(0x04);
			}

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				world.CancelInputMode();				
				if (mi.Button == MouseButton.Left && power.UnitsInRange(xy).Any())
					yield return new Order(order, manager.self, false) { TargetLocation = xy };
			}
			
			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				var xy = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
				foreach (var unit in power.UnitsInRange(xy))
					wr.DrawSelectionBox(unit, Color.Red);
			}

			public void RenderBeforeWorld(WorldRenderer wr, World world)
			{
				var xy = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
				foreach (var t in world.FindTilesInCircle(xy, range))
					tile.DrawAt( wr, Game.CellSize * t, "terrain" );
			}

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				return power.UnitsInRange(xy).Any()	? "ability" : "move-blocked";
			}
		}
	}
}
