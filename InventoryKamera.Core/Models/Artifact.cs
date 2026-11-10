using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace InventoryKamera
{
	[Serializable]
	public class Artifact
	{
		[JsonProperty("setKey")]
		public string SetName { get; private set; }

		[JsonProperty("slotKey")]
		public string GearSlot { get; private set; }

		[JsonProperty("rarity")]
		public int Rarity { get; private set; }

		[JsonProperty("mainStatKey")]
		public string MainStat { get; private set; }

		[JsonProperty("level")]
		public int Level { get; private set; }

		[JsonProperty("substats")]
		public List<SubStat> SubStats { get; private set; }

		/// <summary>
		/// GOOD v3: a 5-star artifact below +4 can show a 4th substat in grey, marked "(Unactivated)".
		/// It gives no stats until the artifact reaches +4. Null (omitted from JSON) when there is none.
		/// </summary>
		[JsonProperty("unactivatedSubstats", NullValueHandling = NullValueHandling.Ignore)]
		public List<SubStat> UnactivatedSubStats { get; private set; }

		[JsonProperty("location")]
		public string EquippedCharacter { get; internal set; }

		[JsonProperty("lock")]
		public bool Lock { get; private set; }

		/// <summary>Crafted with a Sanctifying Elixir (GOOD v3 <c>elixirCrafted</c>).</summary>
		[JsonProperty("elixirCrafted")]
		public bool ElixirCrafted { get; internal set; }

        [JsonProperty("id")]
		public int Id { get; private set; }

		public Artifact()
		{
			Rarity = -1;
			GearSlot = null;
			MainStat = null;
			Level = -1;
			SubStats = new List<SubStat>(4);
			SetName = null;
			EquippedCharacter = null;
			Lock = false;
			Id = 0;
		}

		public Artifact(string _setName, int _rarity, int _level, string _gearSlot, string _mainStat, List<SubStat> _subStats, string _equippedCharacter = null, int _id = 0, bool _Lock = false)
		{
			GearSlot = string.IsNullOrWhiteSpace(_gearSlot) ? "" : _gearSlot;
			Rarity = _rarity;
			MainStat = string.IsNullOrWhiteSpace(_mainStat) ? "" : _mainStat;
			Level = _level;
			var subStats = _subStats.Where(e => e.value > 0).ToList();

			// Only 5-star artifacts below +4 can have an unactivated substat; at +4 it activates.
			// Ignore the flag anywhere else so a misread can't move an active stat out of "substats".
			bool canHaveUnactivated = _rarity == 5 && _level < 4;
			SubStats = subStats.Where(e => !(canHaveUnactivated && e.unactivated))
				.Select(e => { e.unactivated = false; return e; }).ToList();
			var unactivated = canHaveUnactivated ? subStats.Where(e => e.unactivated).ToList() : new List<SubStat>();
			UnactivatedSubStats = unactivated.Count > 0 ? unactivated : null;
			SetName = string.IsNullOrWhiteSpace(_setName) ? "" : _setName;
			EquippedCharacter = string.IsNullOrWhiteSpace(_equippedCharacter) ? "" : _equippedCharacter;
			Lock = _Lock;
			Id = _id;
		}

		public bool IsValid()
		{
			return HasValidLevel() && HasValidRarity() && HasValidSlot() && HasValidSetName() && HasValidMainStat() && HasValidSubStats() && HasValidEquippedCharacter();
		}

		public bool HasValidLevel()
		{
			return 0 <= Level && Level <= 20;
		}

		public bool HasValidRarity()
		{
			return 1 <= Rarity && Rarity <= 5;
		}

		public bool HasValidSlot()
		{
			return ModelValidator.IsValidSlot(GearSlot);
		}

		public bool HasValidSetName()
		{
			return ModelValidator.IsValidSetName(SetName);
		}

		public bool HasValidMainStat()
		{
			return ModelValidator.IsValidStat(MainStat);
		}

		public bool HasValidSubStats()
		{
			bool valid = true;

			SubStats.Concat(UnactivatedSubStats ?? new List<SubStat>()).ToList().ForEach(s =>
			{
                if (!string.IsNullOrWhiteSpace(s.stat) &&
                    (!ModelValidator.IsValidStat(s.stat) || s.value == (decimal)(-1.0)))
                {
                    valid = false;
                }
            });

			return valid;
		}

		public bool HasValidEquippedCharacter()
		{
			return string.IsNullOrWhiteSpace(EquippedCharacter) || ModelValidator.IsValidCharacter(EquippedCharacter);
		}

		[Serializable]
		public struct SubStat
		{
			[JsonProperty("key")]
			[DefaultValue("")]
			public string stat;

			[JsonProperty("value")]
			[DefaultValue(-1)]
			public decimal value;

			/// <summary>Set by the scraper when the substat is shown greyed out as "(Unactivated)".</summary>
			[JsonIgnore]
			public bool unactivated;

			public override string ToString()
			{
				return stat is null
					? "NULL"
					: stat.Contains("_") ? $"{stat} + {value}%" : $"{stat} + {value}";
			}
		}

		public override bool Equals(object obj) => this.Equals(obj as Artifact);

		public bool Equals(Artifact artifact)
		{
			if (artifact is null)
			{
				return false;
			}

			if (Object.ReferenceEquals(this, artifact))
			{
				return true;
			}

			if (GetType() != artifact.GetType())
			{
				return false;
			}

			return GearSlot == artifact.GearSlot
				&& Rarity == artifact.Rarity
				&& MainStat == artifact.MainStat
				&& Level == artifact.Level
				&& SubStats == artifact.SubStats
				&& SetName == artifact.SetName
				&& EquippedCharacter == artifact.EquippedCharacter
				&& Lock == artifact.Lock;
		}

		public override string ToString()
		{
			string output = $"Artifact ID: {Id}\n"
				+ $"Slot: {GearSlot}\n"
				+ $"Set: {SetName}\n"
				+ $"Rarity: {Rarity}\n"
				+ $"Level: {Level}\n"
				+ $"Main Stat: {MainStat}\n";

			SubStats.ForEach(s => output += $"Substat {SubStats.IndexOf(s)+1}: {s}\n");
			UnactivatedSubStats?.ForEach(s => output += $"Unactivated substat: {s}\n");

			output += $"Locked: {Lock}\n";

			if (!string.IsNullOrWhiteSpace(EquippedCharacter)) output += $"Equipped character: {EquippedCharacter}\n";
			return output;
		}

		public override int GetHashCode() => (GearSlot, Rarity, MainStat, Level, SubStats, SetName, EquippedCharacter, Lock).GetHashCode();

		public static bool operator ==(Artifact lhs, Artifact rhs)
		{
			if (lhs is null)
			{
				if (rhs is null)
				{
					return true;
				}

				return false;
			}

			return lhs.Equals(rhs);
		}

		public static bool operator !=(Artifact lhs, Artifact rhs) => !( lhs == rhs );
	}
}
