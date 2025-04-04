﻿using UnityEngine;
using System;

namespace RoadCrossing.Types
{
	[Serializable]
	public class ShopItem
	{
		// The button of the item.
		public Transform itemButton;

		// Is the item locked or not? 0 = locked, 1 = unlocked
		public int lockState = 0;

		// How many coins we need to unlock this item
		public int costToUnlock = 100;

		// The player prefs record for this item
		public string playerPrefsName = "FroggyUnlock";
	}
}