using UnityEngine;
using RoadCrossing.Types;

namespace RoadCrossing
{
	/// <summary>
	/// A code for a block that can be touched by the player (can be a rock, a wall, an enemy, a coin, etc.
	/// Defines characteristics of a block (what it is, what player can do to it) and its interaction with the player (nothing, kill him, etc)
	/// </summary>
	public class RCGBlock : MonoBehaviour
	{
		// The tag of the object that can touch this block
		public string touchTargetTag = "Player";

		// An array of functions that run when this block is touched by the target
		public TouchFunction[] touchFunctions;

		// Remove this object after a ceratin amount of touches
		public int removeAfterTouches = 0;
		internal bool isRemovable = false;

		// The animation that plays when this object is touched
		public AnimationClip hitAnimation;

		// The sound that plays when this object is touched
		public AudioClip soundHit;
		public string soundSourceTag = "GameController";

		// The effect that is created at the location of this object when it is destroyed
		public Transform deathEffect;

		/// <summary>
		/// define what kind of object it is (toucheble / untouchable)
		/// </summary>
		void Start()
		{
			// If removeAfterTouches is higher than 0, make this object removable after one or more touches
			if (removeAfterTouches > 0)
				isRemovable = true;
		}

		/// <summary>
		/// Is executed when this obstacle touches another object with a trigger collider
		/// </summary>
		void OnTriggerEnter(Collider other)
		{
			// Check if the object that was touched has the correct tag
			if (other.tag == touchTargetTag)
			{
				// Go through the list of functions and runs them on the correct targets
				foreach (var touchFunction in touchFunctions)
				{
					// Check that we have a target tag and function name before running
					if (touchFunction.functionName != string.Empty)
					{
						// If the targetTag is "TouchTarget", it means that we apply the function on the object that ouched this lock
						if (touchFunction.targetTag == "TouchTarget")
						{
							// Run the function
							other.SendMessage(touchFunction.functionName, transform);
						}
						else if (touchFunction.targetTag != string.Empty)    // Otherwise, apply the function on the target tag set in this touch function
						{
							// Run the function
							GameObject.FindGameObjectWithTag(touchFunction.targetTag).SendMessage(touchFunction.functionName, touchFunction.functionParameter);
						}
					}
				}

				// If there is an animation, play it
				if (GetComponent<Animation>() && hitAnimation)
				{
					// Stop the animation
					GetComponent<Animation>().Stop();

					// Play the animation
					GetComponent<Animation>().Play(hitAnimation.name);
				}

				// If this object is removable, count down the touches and then remove it
				if (isRemovable == true)
				{
					// Reduce the number of times this object was touched by the target
					removeAfterTouches--;

					if (removeAfterTouches <= 0)
					{
						if (deathEffect)
							Instantiate(deathEffect, transform.position, Quaternion.identity);

						Destroy(gameObject);
					}
				}

				// If there is a sound source and a sound assigned, play it
				if (soundSourceTag != string.Empty && soundHit)
					GameObject.FindGameObjectWithTag(soundSourceTag).GetComponent<AudioSource>().PlayOneShot(soundHit);
			}
		}
	}
}