using UnityEngine.SceneManagement;

using UnityEngine;

namespace RoadCrossing
{
	/// <summary>
	/// Includes functions for loading levels and URLs. It's intended for use with UI Buttons
	/// </summary>
	public class RCGLoadLevel : MonoBehaviour
	{
		/// <summary>
		/// Loads the URL.
		/// </summary>
		/// <param name="urlName">URL/URI</param>
		public void LoadURL(string urlName)
		{
			Application.OpenURL(urlName);
		}

		/// <summary>
		/// Loads the level.
		/// </summary>
		/// <param name="levelName">Level name.</param>
		public void LoadLevel(string levelName)
		{
			SceneManager.LoadScene(levelName);
		}

		/// <summary>
		/// Restarts the current level.
		/// </summary>
		public void RestartLevel()
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
	}
}