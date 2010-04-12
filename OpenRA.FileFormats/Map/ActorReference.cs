#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

namespace OpenRA.FileFormats
{
	public struct ActorReference
	{
		public readonly int2 Location;
		public readonly string Name;
		public readonly string Owner;
		public ActorReference( string name, int2 location, string owner )
		{
			Name = name;
			Location = location;
			Owner = owner;
		}
		
		public override string ToString ()
		{
			return string.Format("{0} {1} {2},{3}", Name, Owner, Location.X,Location.Y);
		}

	}
}