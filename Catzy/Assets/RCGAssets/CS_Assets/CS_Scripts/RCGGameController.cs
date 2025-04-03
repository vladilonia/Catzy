
using UnityEngine.SceneManagement;

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using RoadCrossing.Types;

namespace RoadCrossing
{
	/// <summary>
	/// This script controls the game, starting it, following game progress, and finishing it with game over.
	/// It also creates level lanes as the player progresses.
	/// </summary>
	public class RCGGameController : MonoBehaviour
	{
        public int numOfPlayers;

		// The player
		public Transform[] playerObjects1;
        public Transform[] playerObjects2;
        public int currentPlayer1;
        public int currentPlayer2;

        // Player lives. Death –> -1 life. 0 lifes = game over.
        public int lives = 1;
		public Transform livesText;

		// Respawn things: respawn time, respawn skin
		public float respawnTime = 1.2f;
		public Transform respawnObject1;
        public Transform respawnObject2;
        static Vector3 targetPosition;

		// The camera that follows the player
		public Transform cameraObject1;
        public Transform cameraObject2;

        // Is active only if swipes aren't used
        public Transform moveButtonsObject;

		// Swipe or click/tap?
		public bool swipeControls = false;

		// The start and end positions of the touches for swipe control
		internal Vector3 swipeStart;
		internal Vector3 swipeEnd;

		// How far we need to swipe before detecting movement
		public float swipeDistance = 10;

		// How long to wait before the swipe command is cancelled
		public float swipeTimeout = 1;
		internal float swipeTimeoutCount;

		// An array of activated powerups
		public Powerup[] powerups;

		public bool stopPowerupsOnDeath = false;

		// An array of lanes that randomly appear as the player moves forward
		public Lane[] lanes;
		internal Lane[] lanesList;

		// A lane that appears after the player passes the set number of lanes for victory. This is used in randomly-generated levels only.
		public Transform victoryLane;

		// When the player passes this number of lanes in a level, we win. This is used in randomly-generated levels only.
		public int lanesToVictory = 0;

		// The number of lanes we passed so far. This is used to check if we reached the victory lane. Only for randomly-generated levels.
		internal int lanesCreated = 0;

		// An array of the objects that can be dropped
		public ObjectDrop[] objectDrops;
		internal Transform[] objectDropList;

		// Should objects be dropped in sequence (one after the other) rather than randomly?
		public bool dropInSequence = true;

		// The index of the current drop, if we are using dropInSequence
		internal int currentDrop = -1;

		// The offset left and right on the lane
		public int objectDropOffset = 4;

		// How many lanes to create before starting the game
		public int precreateLanes = 20;
		public float nextLanePosition = 1;

		// The score and score text of the player
		public int score = 0;
		public Transform scoreText;
		internal int highScore = 0;
		internal int scoreMultiplier = 1;

		// The player prefs record of the total coins we have (not high score, but total coins we collected in all games)
		public string coinsPlayerPrefs = "Coins";

		// The overall game speed
		public float gameSpeed = 1;

		// How many points the player needs to collect before leveling up
		public int levelUpEveryCoins = 5;
		internal int increaseCount = 0;

		// This is the speed of the camera that keeps advancing on the player and kills him instantly if it reaches him
		public Transform deathLineObject;
		internal float deathLineTargetPosX;
		public float deathLineSpeed = 1;
		public float deathLineSpeedIncrease = 1;
		public float deathLineSpeedMax = 1.5f;

		// Various canvases for the UI
		public Transform gameCanvas;
		public Transform pauseCanvas;
		public Transform gameOverCanvas;
		public Transform victoryCanvas;

		// Is the game over?
		internal bool isGameOver = false;

		// The level of the main menu that can be loaded after the game ends
		public string mainMenuLevelName = "MainMenu";

		// Various sounds
		public AudioClip soundLevelUp;
		public AudioClip soundGameOver;
		public AudioClip soundVictory;

		// The tag of the sound source
		public string soundSourceTag = "GameController";
		public string confirmButton = "Submit";

		// The button that pauses the game. Clicking on the pause button in the UI also pauses the game
		public string pauseButton = "Cancel";
		internal bool isPaused = false;
		internal int index = 0;

		void Start()
		{
			// Update the score and lives without changing them
			ChangeScore(0);
			StartCoroutine(ChangeLives(0));

			// Hide unnecessary canvases
			if (gameOverCanvas) gameOverCanvas.gameObject.SetActive(false);
			if (victoryCanvas) gameOverCanvas.gameObject.SetActive(false);

			// Get the highscore for the player
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			highScore = PlayerPrefs.GetInt(SceneManager.GetActiveScene().name + "_HighScore", 0);
#else
			highScore = PlayerPrefs.GetInt(Application.loadedLevelName + "_HighScore", 0);
#endif

			// Calculate the chances for the objects to drop
			int totalLanes = 0;
			int totalLanesIndex = 0;

			// Calculate the total number of drops with their chances
			for (index = 0; index < lanes.Length; index++)
			{
				totalLanes += lanes[index].laneChance;
			}

			// Create a new list of the objects that can be dropped
			lanesList = new Lane[totalLanes];

			// Go through the list again and fill out each type of drop based on its drop chance
			for (index = 0; index < lanes.Length; index++)
			{
				int laneChanceCount = 0;

				while (laneChanceCount < lanes[index].laneChance)
				{
					lanesList[totalLanesIndex] = lanes[index];

					laneChanceCount++;

					totalLanesIndex++;
				}
			}

			// Calculate the chances for the objects to drop
			int totalDrops = 0;
			int totalDropsIndex = 0;

			// Calculate the total number of drops with their chances
			for (index = 0; index < objectDrops.Length; index++)
			{
				totalDrops += objectDrops[index].dropChance;
			}

			// Create a new list of the objects that can be dropped
			objectDropList = new Transform[totalDrops];

			// Go through the list again and fill out each type of drop based on its drop chance
			for (index = 0; index < objectDrops.Length; index++)
			{
				int dropChanceCount = 0;

				while (dropChanceCount < objectDrops[index].dropChance)
				{
					objectDropList[totalDropsIndex] = objectDrops[index].droppedObject;

					dropChanceCount++;

					totalDropsIndex++;
				}
			}

			// Get the currently selected player from PlayerPrefs
			currentPlayer1 = PlayerPrefs.GetInt("currentPlayer1", currentPlayer1);

			// Set the current player object
			SetPlayer1(currentPlayer1);
			if(numOfPlayers > 1)
			{
				SetPlayer2(1);
			}

			// If the player object is not already assigned, Assign it from the "Player" tag
			if (cameraObject1 == null)
				cameraObject1 = GameObject.FindGameObjectWithTag("MainCamera").transform;
			if (numOfPlayers > 1)
			{
				if (cameraObject2 == null)
					cameraObject2 = GameObject.FindGameObjectWithTag("SecondCamera").transform;
			}

            //Create a few lanes at the start of the game
            if (lanesList.Length > 0)
			{
				// Count the number of lanes to create at the start of the game
				for (index = 0; index < precreateLanes; index++)
				{
					// Create a lane only if we have an endless game, or (in case we have a victory condition) as long as we didn't reach the number of lanes to victory
					if (victoryLane == null || (victoryLane && lanesToVictory > 0 && lanesCreated <= lanesToVictory)) CreateLane();
				}
			}

			// Go through all the powerups and reset their timers
			for (index = 0; index < powerups.Length; index++)
			{
				// Set the maximum duration of the powerup
				powerups[index].durationMax = powerups[index].duration;

				// Reset the duration counter
				powerups[index].duration = 0;

				// Deactivate the icon of the powerup
				powerups[index].icon.gameObject.SetActive(false);
			}

			// If swipe controls are on, deactivate button controls
			if (swipeControls == true && moveButtonsObject) moveButtonsObject.gameObject.SetActive(false);

			// Register the current death line position
			if (deathLineObject) deathLineTargetPosX = deathLineObject.position.x;

			// Pause the game at the start
			Pause();

			// These warnings appear if you set one of the attributes needed for a win condition in a randomly generated level, but don't set the rest
			if (victoryLane && lanesToVictory <= 0) Debug.LogWarning("If you want the victory lane to appear you must set the number of lanes to victory higher than 0");
			if (victoryLane == null && lanesToVictory > 0) Debug.LogWarning("You must assign a victory lane which will appear after you passed the number of lanes to victory", victoryLane);
			if (victoryLane && lanesToVictory > 0 && victoryCanvas == null) Debug.LogWarning("You must set a victory canvas from the scene that will appear when you win the game (similar to how the game over canvas is set)");
		}

		/// <summary>
		/// Update is called every frame, if the MonoBehaviour is enabled.
		/// </summary>
		void Update()
		{
			// If the game is over, listen for the Restart and MainMenu buttons
			if (isGameOver == true)
			{
				// The jump button restarts the game
				if (Input.GetButtonDown(confirmButton))
				{
					Restart();
				}

				// The pause button goes to the main menu
				if (Input.GetButtonDown(pauseButton))
				{
					MainMenu();
				}
			}
			else
			{
				// Toggle pause/unpause in the game
				if (Input.GetButtonDown(pauseButton))
				{
					if (isPaused == true)
						Unpause();
					else
						Pause();
				}

				// Using swipe controls to move the player
				if (swipeControls == true)
				{
					if (swipeTimeoutCount > 0) swipeTimeoutCount -= Time.deltaTime;

					// Check touches on the screen
					foreach (Touch touch in Input.touches)
					{
						// Check the touch position at the beginning
						if (touch.phase == TouchPhase.Began)
						{
							swipeStart = touch.position;
							swipeEnd = touch.position;

							swipeTimeoutCount = swipeTimeout;
						}

						// Check swiping motion
						if (touch.phase == TouchPhase.Moved)
						{
							swipeEnd = touch.position;
						}

						// Check the touch position at the end, and move the player accordingly
						if (touch.phase == TouchPhase.Ended && swipeTimeoutCount > 0)
						{
							if ((swipeStart.x - swipeEnd.x) > swipeDistance && (swipeStart.y - swipeEnd.y) < -swipeDistance) // Swipe left
							{
								MovePlayer1("left");
							}
							else if ((swipeStart.x - swipeEnd.x) < -swipeDistance && (swipeStart.y - swipeEnd.y) > swipeDistance) // Swipe right
							{
								MovePlayer1("right");
							}
							else if ((swipeStart.y - swipeEnd.y) < -swipeDistance && (swipeStart.x - swipeEnd.x) < -swipeDistance) // Swipe up
							{
								MovePlayer1("forward");
							}
							else if ((swipeStart.y - swipeEnd.y) > swipeDistance && (swipeStart.x - swipeEnd.x) > swipeDistance) // Swipe down
							{
								MovePlayer1("backward");
							}
						}
					}
				}
			}

			// If the camera moved forward enough, create another lane
			if (lanesList.Length > 0 && ((nextLanePosition - cameraObject1.position.x < precreateLanes) || (nextLanePosition - cameraObject2.position.x < precreateLanes)))
			{
				// Create a lane only if we have an endless game, or (in case we have a victory condition) as long as we didn't reach the number of lanes to victory
				if (victoryLane == null || (victoryLane && lanesToVictory > 0 && lanesCreated <= lanesToVictory)) CreateLane();
			}

			if (cameraObject1)
			{
                // Make the camera chase the player in all directions
                if (playerObjects1[currentPlayer1] && playerObjects1[currentPlayer1].gameObject.activeSelf == true)
					cameraObject1.position = new Vector3(Mathf.Lerp(cameraObject1.position.x, playerObjects1[currentPlayer1].position.x, Time.deltaTime * 3), 0, Mathf.Lerp(cameraObject1.position.z, playerObjects1[currentPlayer1].position.z, Time.deltaTime * 3));
				else if (respawnObject1 && respawnObject1.gameObject.activeSelf == true)
					cameraObject1.position = new Vector3(Mathf.Lerp(cameraObject1.position.x, respawnObject1.position.x, Time.deltaTime * 3), 0, Mathf.Lerp(cameraObject1.position.z, respawnObject1.position.z, Time.deltaTime * 3));	
			}

			if (numOfPlayers > 1)
			{
				if (cameraObject2)
				{
					// Make the camera chase the player in all directions
					if (playerObjects2[currentPlayer2] && playerObjects2[currentPlayer2].gameObject.activeSelf == true)
					{
						cameraObject2.position = new Vector3(Mathf.Lerp(cameraObject2.position.x, playerObjects2[currentPlayer2].position.x, Time.deltaTime * 3), 0, Mathf.Lerp(cameraObject2.position.z, playerObjects2[currentPlayer2].position.z, Time.deltaTime * 3));
					}
					else if (respawnObject2 && respawnObject2.gameObject.activeSelf == true)
						cameraObject2.position = new Vector3(Mathf.Lerp(cameraObject2.position.x, respawnObject2.position.x, Time.deltaTime * 3), 0, Mathf.Lerp(cameraObject2.position.z, respawnObject2.position.z, Time.deltaTime * 3));
				}
			}

            if (deathLineObject)
            {
				if(numOfPlayers == 1)
				{
                    if (cameraObject1)
					{
                        if (cameraObject1.position.x > deathLineTargetPosX)
                            deathLineTargetPosX = cameraObject1.position.x;
                    }

                }
				else if (numOfPlayers > 1)
				{
                    if (cameraObject1 && cameraObject2)
                    {
						if (cameraObject1.position.x >= cameraObject2.position.x)
						{
                            if (cameraObject1.position.x > deathLineTargetPosX)
                                deathLineTargetPosX = cameraObject1.position.x;
                        }

                        if (cameraObject1.position.x < cameraObject2.position.x)
                        {
                            if (cameraObject2.position.x > deathLineTargetPosX)
                                deathLineTargetPosX = cameraObject2.position.x;
                        }
                    }
                }


				if (isGameOver == false)
					deathLineTargetPosX += deathLineSpeed * Time.deltaTime;

                Vector3 newVector3 = new Vector3(deathLineTargetPosX, deathLineObject.position.y, deathLineObject.position.z);

                deathLineObject.position = Vector3.Lerp(deathLineObject.position, newVector3, Time.deltaTime * 0.5f);
            }
        }


		// Create game lane
		void CreateLane()
		{
			// If we have a victory lane and we passed the needed number of lanes, create the victory lane.
			if (victoryLane && lanesToVictory > 0 && lanesCreated >= lanesToVictory)
			{
				Instantiate(victoryLane, new Vector3(nextLanePosition, 0, 0), Quaternion.identity);
			}
			else //Othewise, create a random lane from the list of available lanes
			{
				// Choose a random lane from the list
				int randomLane = Mathf.FloorToInt(Random.Range(0, lanesList.Length));

				// Create a random lane from the list of available lanes
				Transform newLane = Instantiate(lanesList[randomLane].laneObject, new Vector3(nextLanePosition, 0, 0), Quaternion.identity) as Transform;

				if (Random.value < lanesList[randomLane].itemChance)
				{
					if (dropInSequence == true)
					{
						if (currentDrop < objectDropList.Length - 1) currentDrop++;
						else currentDrop = 0;
					}
					else
					{
						currentDrop = Mathf.FloorToInt(Random.Range(0, objectDropList.Length));
					}

					Transform newObject = Instantiate(objectDropList[currentDrop]) as Transform;

					Vector3 newVector = new Vector3();

					newVector = newLane.position;

					newVector.z += Mathf.Round(Random.Range(-objectDropOffset, objectDropOffset));

					newObject.position = newVector;
				}

				// Go to the next lane position
				nextLanePosition += lanesList[randomLane].laneWidth;
			}

			lanesCreated++;
		}

		/// <summary>
		/// Updates the players score, considers whether the player has also leveld up
		/// </summary>
		/// <param name="changeValue">Value to add to the current score.</param>
		void ChangeScore(int changeValue)
		{
			// Change the score
			score += changeValue * scoreMultiplier;

			// Update the score text
			if (scoreText)
				scoreText.GetComponent<Text>().text = score.ToString();

			// Increase the counter to the next level
			increaseCount += changeValue;

			// If we reached the required number of points, level up!
			if (increaseCount >= levelUpEveryCoins)
			{
				increaseCount -= levelUpEveryCoins;

				LevelUp();
			}
		}

		/// <summary>
		/// Sets the score multiplier (When the player picks up coins he gets 1X,2X,etc score)
		/// </summary>
		/// <param name="changeValue">Value to set</param>
		void SetScoreMultiplier(int setValue)
		{
			// Set the score multiplier
			scoreMultiplier = setValue;
		}

		/// <summary>
		/// Levels up and increases the difficulty of the game.
		/// </summary>
		void LevelUp()
		{
			// Increase the speed of the death line (the moving fog), but never above the maximum allowed value
			if (deathLineSpeed + deathLineSpeedIncrease < deathLineSpeedMax)
				deathLineSpeed += deathLineSpeedIncrease;

			// If there is a source and a sound, play it from the source
			if (soundSourceTag != string.Empty && soundLevelUp)
				GameObject.FindGameObjectWithTag(soundSourceTag).GetComponent<AudioSource>().PlayOneShot(soundLevelUp);
		}

		/// <summary>
		/// Pauses the game.
		/// </summary>
		void Pause()
		{
			isPaused = true;

			// Set timescale to 0, preventing anything from moving
			Time.timeScale = 0;

			// Show the pause screen and hide the game screen
			if (pauseCanvas)
				pauseCanvas.gameObject.SetActive(true);

			if (gameCanvas)
				gameCanvas.gameObject.SetActive(false);
		}

		/// <summary>
		/// Unpauses the game
		/// </summary>
		void Unpause()
		{
			isPaused = false;

			// Set timescale back to the current game speed
			Time.timeScale = gameSpeed;

			// Hide the pause screen and show the game screen
			if (pauseCanvas)
				pauseCanvas.gameObject.SetActive(false);

			if (gameCanvas)
				gameCanvas.gameObject.SetActive(true);
		}

		/// <summary>
		/// Changes the number of lives the player has. If 0, game over function is called
		/// </summary>
		/// <param name="changeValue">Value to change in lives number.</param>
		IEnumerator ChangeLives(int changeValue)
		{
			// Change the number of lives the player has
			lives += changeValue;

			// Update the lives text
			if (livesText) livesText.GetComponent<Text>().text = lives.ToString();

			// If we ran out of lives, run the game over function
			if (lives <= 0) StartCoroutine(GameOver(0.5f));
			else if (playerObjects1[currentPlayer1] && changeValue < 0)
			{
				// Stop all powerups
				if (stopPowerupsOnDeath == true)
				{
					// Go through all the powerups and nullify their timers, making them end
					for (index = 0; index < powerups.Length; index++)
					{
						// Set the duration of the powerup to 0
						powerups[index].duration = 0;
					}
				}

				// Show the respawn object, allowing it to move
				if (respawnObject1)
				{
					respawnObject1.gameObject.SetActive(true);

					respawnObject1.position = playerObjects1[currentPlayer1].position;

					respawnObject1.rotation = playerObjects1[currentPlayer1].rotation;

					respawnObject1.SendMessage("Spawn");
				}

				yield return new WaitForSeconds(respawnTime);

				// Activate the player object
				if (playerObjects1[currentPlayer1].gameObject.activeSelf == false)
				{
					playerObjects1[currentPlayer1].gameObject.SetActive(true);

					// Respawn the player object
					playerObjects1[currentPlayer1].SendMessage("Spawn");

					// If there is a respawn object, place the player at its position, and hide the respawn object
					if (respawnObject1)
					{
						targetPosition = respawnObject1.position;

						playerObjects1[currentPlayer1].position = targetPosition;

						playerObjects1[currentPlayer1].rotation = respawnObject1.rotation;

						respawnObject1.gameObject.SetActive(false);
					}
				}

                // Activate the player object
                if (playerObjects2[currentPlayer2].gameObject.activeSelf == false)
                {
                    playerObjects2[currentPlayer2].gameObject.SetActive(true);

                    // Respawn the player object
                    playerObjects2[currentPlayer2].SendMessage("Spawn");

                    // If there is a respawn object, place the player at its position, and hide the respawn object
                    if (respawnObject2)
                    {
                        targetPosition = respawnObject2.position;

                        playerObjects2[currentPlayer2].position = targetPosition;

                        playerObjects2[currentPlayer2].rotation = respawnObject2.rotation;

                        respawnObject2.gameObject.SetActive(false);
                    }
                }
            }
		}

		/// <summary>
		/// Handles when the game is over.
		/// </summary>
		/// <returns>Yields for a period of time to allow execution to continue then continues through the game over text/gui display</returns>
		/// <param name="delay">The delay of the yield in seconds</param>
		IEnumerator GameOver(float delay)
		{
			// Go through all the powerups and nullify their timers, making them end
			for (index = 0; index < powerups.Length; index++)
			{
				// Set the duration of the powerup to 0
				powerups[index].duration = 0;
			}

			yield return new WaitForSeconds(delay);

			isGameOver = true;

			// Remove the pause and game screens
			if (pauseCanvas)
				Destroy(pauseCanvas.gameObject);

			if (gameCanvas)
				Destroy(gameCanvas.gameObject);

			// Get the number of coins we have
			int totalCoins = PlayerPrefs.GetInt(coinsPlayerPrefs, 0);

			// Add to the number of coins we collected in this game
			totalCoins += score;

			// Record the number of coins we have
			PlayerPrefs.SetInt(coinsPlayerPrefs, totalCoins);

			// Show the game over screen
			if (gameOverCanvas)
			{
				// Show the game over screen
				gameOverCanvas.gameObject.SetActive(true);

				// Write the score text
				gameOverCanvas.Find("TextScore").GetComponent<Text>().text = "SCORE " + score.ToString();

				// Check if we got a high score
				if (score > highScore)
				{
					highScore = score;

					// Register the new high score
					PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "_HighScore", score);
				}

				// Write the high sscore text
				gameOverCanvas.Find("TextHighScore").GetComponent<Text>().text = "HIGH SCORE " + highScore.ToString();
			}

			// If there is a source and a sound, play it from the source
			if (soundSourceTag != string.Empty && soundGameOver)
				GameObject.FindGameObjectWithTag(soundSourceTag).GetComponent<AudioSource>().PlayOneShot(soundGameOver);
		}

		/// <summary>
		/// Reloads the current loaded level.
		/// </summary>
		void Restart()
		{
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
#else
			Application.LoadLevel(Application.loadedLevelName);
#endif
		}

		/// <summary>
		/// Loads and returns the user/player to the main menu.
		/// </summary>
		void MainMenu()
		{
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene(mainMenuLevelName);
#else
			Application.LoadLevel(mainMenuLevelName);
#endif
		}

		/// <summary>
		/// Activates the selected player, while deactivating all the others
		/// </summary>
		/// <param name="playerNumber">The number of the player to be activated</param>
		void SetPlayer1(int playerNumber)
		{
			// Hide the respawn object
			if (respawnObject1) respawnObject1.gameObject.SetActive(false);

			// Go through all the players, and hide each one except the current player
			for (index = 0; index < playerObjects1.Length; index++)
			{
				if (index != playerNumber)
					playerObjects1[index].gameObject.SetActive(false);
				else
					playerObjects1[index].gameObject.SetActive(true);
			}
		}

		/// <summary>
		/// same as SetPlayer1 but for the second player
		/// </summary>
		/// <param name="playerNumber"></param>
        void SetPlayer2(int playerNumber)
        {
            // Hide the respawn object
            if (respawnObject2) respawnObject2.gameObject.SetActive(false);


            // Go through all the players, and hide each one except the current player
            for (index = 0; index < playerObjects2.Length; index++)
            {
                if (index != playerNumber)
                    playerObjects2[index].gameObject.SetActive(false);
                else
                    playerObjects2[index].gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Send a move command to the current player
        /// </summary>
        /// <param name="moveDirection">The direction the player should move in</param>
        void MovePlayer1(string moveDirection)
		{
			// If there is a current player, send a move message with a direction
			if (playerObjects1[currentPlayer1] && playerObjects1[currentPlayer1].gameObject.activeSelf == true) playerObjects1[currentPlayer1].SendMessage("Move", moveDirection);
			else if (respawnObject1 && respawnObject1.gameObject.activeSelf == true) respawnObject1.SendMessage("Move", moveDirection);
		}

        void MovePlayer2(string moveDirection)
        {
            // If there is a current player, send a move message with a direction
            if (playerObjects2[currentPlayer1] && playerObjects2[currentPlayer1].gameObject.activeSelf == true) playerObjects2[currentPlayer1].SendMessage("Move", moveDirection);
            else if (respawnObject2 && respawnObject2.gameObject.activeSelf == true) respawnObject2.SendMessage("Move", moveDirection);
        }

        /// <summary>
        /// Changes the speed of the game (Time.timeScale)
        /// </summary>
        /// <param name="setValue">The new speed of the game</param>
        void SetGameSpeed(float setValue)
		{
			gameSpeed = setValue;

			// Set the overall speed of the scene
			Time.timeScale = gameSpeed;

			// Toggle between a low pitch for the slowmotion time, and normal pitch when the slowmotion ends
			if (GetComponent<AudioSource>().pitch == 1) GetComponent<AudioSource>().pitch = 0.5f;
			else GetComponent<AudioSource>().pitch = 1;
		}

		/// <summary>
		/// Changes the speed of the player
		/// </summary>
		/// <param name="setValue">The new speed of the player</param>
		void SetPlayer1Speed(float setValue)
		{
			if (playerObjects1[currentPlayer1] && playerObjects1[currentPlayer1].gameObject.activeSelf == true) playerObjects1[currentPlayer1].SendMessage("SetPlayer1Speed", setValue);
			else if (respawnObject1 && respawnObject1.gameObject.activeSelf == true) respawnObject1.SendMessage("SetPlayer1Speed", setValue);
		}

        void SetPlayer2Speed(float setValue)
        {
            if (playerObjects2[currentPlayer1] && playerObjects1[currentPlayer1].gameObject.activeSelf == true) playerObjects2[currentPlayer1].SendMessage("SetPlayer2Speed", setValue);
            else if (respawnObject2 && respawnObject2.gameObject.activeSelf == true) respawnObject2.SendMessage("SetPlayer2Speed", setValue);
        }

        /// <summary>
        /// Resets the position of the death line to the camera
        /// </summary>
        void ResetDeathLine()
		{
			if (deathLineObject && cameraObject1) deathLineTargetPosX = cameraObject1.position.x;
		}

		/// <summary>
		/// Activates a power up from a list of available power ups
		/// </summary>
		/// <param name="setValue">The index numebr of the powerup to activate</param>
		IEnumerator ActivatePowerup(int powerupIndex)
		{
			// If there is already a similar powerup running, refill its duration timer
			if (powerups[powerupIndex].duration > 0)
			{
				// Refil the duration of the powerup to maximum
				powerups[powerupIndex].duration = powerups[powerupIndex].durationMax;
			}
			else // Otherwise, activate the power up functions
			{
				// Activate the powerup icon
				if (powerups[powerupIndex].icon) powerups[powerupIndex].icon.gameObject.SetActive(true);

				// Run up to two start functions from the gamecontroller
				if (powerups[powerupIndex].startFunctionA != string.Empty) SendMessage(powerups[powerupIndex].startFunctionA, powerups[powerupIndex].startParamaterA);
				if (powerups[powerupIndex].startFunctionB != string.Empty) SendMessage(powerups[powerupIndex].startFunctionB, powerups[powerupIndex].startParamaterB);

				// Fill the duration timer to maximum
				powerups[powerupIndex].duration = powerups[powerupIndex].durationMax;

				// Count down the duration of the powerup
				while (powerups[powerupIndex].duration > 0)
				{
					yield return new WaitForSeconds(Time.deltaTime);

					powerups[powerupIndex].duration -= Time.deltaTime;

					// Animate the powerup timer graphic using fill amount
					if (powerups[powerupIndex].icon) powerups[powerupIndex].icon.Find("FillAmount").GetComponent<Image>().fillAmount = powerups[powerupIndex].duration / powerups[powerupIndex].durationMax;
				}

				// Run up to two end functions from the gamecontroller
				if (powerups[powerupIndex].endFunctionA != string.Empty) SendMessage(powerups[powerupIndex].endFunctionA, powerups[powerupIndex].endParamaterA);
				if (powerups[powerupIndex].endFunctionB != string.Empty) SendMessage(powerups[powerupIndex].endFunctionB, powerups[powerupIndex].endParamaterB);

				// Deactivate the powerup icon
				if (powerups[powerupIndex].icon) powerups[powerupIndex].icon.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Draws the position of the next lane in the editor.
		/// </summary>
		public void OnDrawGizmos()
		{
			// Draw the position of the next lane in red
			Gizmos.color = Color.red;
			Gizmos.DrawLine(new Vector3(nextLanePosition, 0, -10), new Vector3(nextLanePosition, 0, 10));
		}
	}
}

