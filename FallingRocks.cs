/*
 * Falling Rocks Game, written as a homework assignment for 'Intro to Programming' course, October 2015.
 *
 * OVERVIEW
 * This is my interpretation of the game, which deviates somewhat from the one
 * pictured in the actual homework assignment. I've removed some elements that
 * I considered non-essential, like rocks of different color and symbol
 * representations. I also changed the appearance of the playable character.
 * Some gameplay additions that I made:
 * - background colors are chosen to represent sky and ground;
 * - player has health instead of the game ending in a single hit;
 * - the game becomes progressively faster and harder;
 * - a special type of obstacle that moves twice as fast and always spawns
 *   above the player's current position.
 *
 * HOW IT WORKS, IN SHORT
 * Most methods will be documented where they appear, this is just a quick
 * summary. The process runs inside an endless 'while' loop that handles both
 * input and logic processing. Instead of using an event to achieve concurrent
 * user controls and frame progression, the 'Console.KeyAvailable' property is
 * emplyed, since it doesn't block the thread, waiting for input. To make it
 * possible to have enemies with different movement speed and to allow the
 * player to move more than once in a single tick, the loop moves as fast as
 * the CPU allows and enemy movement is based on milliseconds elapsed since
 * last draw. The player and enemies are class-based with built in properties
 * for position and appearance, as well as methods for movement. Enemy
 * spawnrate and movespeed is based on elapsed playing time. Scoring depends
 * on the speed of enemies (you get more points for faster enemies) and is
 * awarded for each enemy seperately. Display and collision detection are
 * handled by an array of 'char' elements that represents the play area.
 *
 * HOW IT CAN BE IMPROVED
 * A secondary screen buffer array for keeping color information will allow
 * for greater fidelity.
 * The global variables are a bit of a mess, the result of feature creep. If
 * those are packed in a class along with the rendering functions and arrays,
 * the solution will be a lot neater.
 * Simple sound using 'Console.Beep()'.
 * I was considering health pickups at one point, but i don't expect the game
 * has enough hook to keep anyone play for longer than 1 minute, hence the
 * the difficulty ramp peaking at 90 seconds.
 * A drawback of using objects for the enemies is we have to allocate memory
 * for each of them. Right now this is handled with an array of fixed size,
 * filled in a rolling fashion (we start over once the end reached and the
 * first ID is tagged as dead).
 */

using System;
using System.Threading;
using System.Diagnostics;   // We need these two for timers functionality

/*
 * Initially the player characteer and enemies only differed in the rocks'
 * ability to move vertically and disappear off the screen. At this point,
 * inheriting the two classes made perfect sense, but as time went on, it was
 * apparent I wold have been better off with an abstract class for movement and
 * representation and two seperate classes inheriting it for enemies and
 * playable character.
 */
class ScreenElement
{
	// Size, position and appearance
	protected byte width;
	protected byte left;
	protected byte top;
	protected string representation;

	// Property members
	public byte Left
	{
		get
		{
			return left;
		}
		set
		{
			left = value;
		}
	}
	public byte Top
	{
		get
		{
			return top;
		}
		set
		{
			top = value;
		}
	}
	public byte Width
	{
		get
		{
			return width;
		}
	}

	// Constructors
	public ScreenElement()                                              // Default
	{
		width = 1;
		left = 0;
		top = 0;
		representation = "@";
	}
	public ScreenElement(string representation)                         // Only appearance
	{
		this.representation = representation;
		width = (byte)(representation.Length);
		left = 0;
		top = 0;
	}
	public ScreenElement(string representation, byte left)              // Definition and horizontal position
	{
		this.representation = representation;
		width = (byte)(representation.Length);
		this.left = left;
		top = 0;
	}
	public ScreenElement(string representation, byte left, byte top)    // Definition and position
	{
		this.representation = representation;
		width = (byte)(representation.Length);
		this.left = left;
		this.top = top;
	}

	// Movement
	public void MoveLeft()
	{
		if (left > 0)	// We make sure not to leave the playable area
		{
			left--;
		}
	}
	public void MoveRight()
	{
		if (left + width <= FallingRocks.boardWidth - 1)    // We make sure not to leave the playable area
		{
			left++;
		}
	}

	/*
	 * It makes sense to handle collision in a seperate place, to avoid having
	 * to pass elapsed time to this method
	 */
	public void Draw(TimeSpan frameLast)
	{
		// Commit element to board
		for (int i = 0; i < width; i++)
		{
			// Collision detecion
			if (FallingRocks.board[left + i, top] != ' ')
			{
				// Player is invulnerable for half a second after taking a hit
				// (otherwise the game ends isntantly, considering the speed of updates)
				if (frameLast.TotalMilliseconds > FallingRocks.lastHurt + 500)
				{
					FallingRocks.lastHurt = frameLast.TotalMilliseconds;
					FallingRocks.health--;
				}
			}
			FallingRocks.board[left+i, top] = representation[i];
		}
	}
}

class Enemy : ScreenElement
{
	// Additional fields we need for vertically moving elements
	float z;
	float rate;
	bool alive = false;
	public static byte nOfEnemies;	// This information is used to keep the array from overflowing
	public static int lastID;		// We have to keep track of which element we have to assign next

	public bool isAlive
	{
		get { return alive; }
	}

	// Constructor
	public Enemy()
	{
		alive = false;	// Initialize dead element
	}
	public Enemy(string representation, byte left, byte top, float rate)
	{
		nOfEnemies++;
		this.alive = true;
		this.representation = representation;
		width = (byte)(representation.Length);
		this.left = left;
		this.top = top;
		this.rate = rate;
		z = 0f;
	}

	// Movement
	public void Fall(double elapsed)
	{
		z += (float)(elapsed / rate);
		// Rocks linger for 1 tick on the ground
		byte limit = (byte)(Math.Floor(z));
		if (top < FallingRocks.boardHeight - 2)
		{
			top = (byte)(Math.Floor(z));
		}
		else if (top == FallingRocks.boardHeight - 2 && limit <= FallingRocks.boardHeight)
		{
			top = (byte)(FallingRocks.boardHeight - 2);
		}
		else
		{
			nOfEnemies--;
			alive = false;  // Destroy if ground is reached
			FallingRocks.score += (int)((1000 - 3 * rate) / 20f);	// Award points
		}
		/*
		 * The following check exists because element progression is based on time,
		 * which means it's possible the object has warped way past the end of the
		 * playing area by the time we update it. In most situations this wont occur,
		 * however if the computer stutters for some reason, or if we are debugging
		 * (no, the timers aren't paused on a breakpoint; yes, this was a very non-
		 * obvious bug to track), it may cause an IndexOutOfRange exception.
		 */
		if (top > FallingRocks.boardHeight - 2)
		{
			nOfEnemies--;
			alive = false;
			FallingRocks.score += (int)((1000 - 3 * rate) / 20f);	// Award points
		}
	}

	// Display element
	new public void Draw()
	{
		if (alive)
		{
			// Commit element to board
			for (int i = 0; i < width; i++)
			{
				FallingRocks.board[left + i, top] = representation[i];
			}
		}
	}
}

class FallingRocks
{
	/*
	 * Global game parameters; playable area and diffculty can be tweaked here
	 * without having to change anything else (in theory), since all other
	 * definitions are based on this, nothing is hard-coded.
	 */
	public static int score = 0;
	public static int boardWidth = 30;
	public static int boardHeight = 15;
	public static int health = 3;
	public static char[,] board = new char[boardWidth, boardHeight-1];
	public static int maxRocks = 30;
	static Enemy[] rocks = new Enemy[maxRocks];
	static Enemy seeker = new Enemy();
	public static double lastHurt = 0.0;

	static void ClearBoard()
	{
		for (int i = 0; i < boardHeight - 1; i++)
		{
			for (int j = 0; j < boardWidth; j++)
			{
				board[j, i] = ' ';
			}
		}
	}

	static void DrawBoard()
	{
		Console.BackgroundColor = ConsoleColor.Cyan;
		Console.ForegroundColor = ConsoleColor.Black;
		Console.SetCursorPosition(0, 0);
		for (int i = 0; i < boardHeight - 1; i++)
		{
			for (int j = 0; j < boardWidth; j++)
			{
				Console.Write(board[j, i]);
			}
		}
	}

	static void DrawGround()
	{
		Console.SetCursorPosition(0, boardHeight - 1);
		Console.BackgroundColor = ConsoleColor.Green;
		Console.Write(new string(' ', boardWidth));
	}

	static void DrawScore()
	{
		Console.SetCursorPosition(2, boardHeight);
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write("SCORE {0}",score);
		Console.CursorLeft = boardWidth - 6;
		Console.ForegroundColor = ConsoleColor.Red;
		for (int i = 0; i < 3; i++)
		{
			if (health > i) { Console.Write("\x03"); }
			else { Console.Write(" "); }
		}
	}

	static void SpawnRock(double elapsed)
	{
		if (Enemy.nOfEnemies < maxRocks - 1)			// Can only spawn more if not using all IDs
		{
			int nextID = Enemy.lastID + 1;
			if (nextID >= maxRocks) { nextID = 0; }     // Make sure we don't go out of range
			if (rocks[nextID].isAlive == false)         // Only spawn another enemy if the oldest one is cleared
			{
				Random position = new Random();         // Initialize pseudorandom generator
				float rate = (float)elapsed * 0.004f;   // Game gets faster with time elapsed,
				if (rate > 240) { rate = 240; }         // reaching max rate at 60 seconds
				string representation = new string('@', position.Next(4));
				// We have to make sure the width of the new element doesn't go out of range
				rocks[nextID] = new Enemy(representation, (byte)(position.Next(boardWidth-1-representation.Length)), 0, 300 - rate);
				// This will not execute if the array is full, meaning we'll
				// be back to the same ID on next spawn call
				Enemy.lastID = nextID;
			}
		}
	}
	static void SpawnSeeker(double elapsed, byte left)
	{
		float rate = (float)elapsed * 0.002f;				// Seeker moves twice as fast as regular rocks
		if (rate > 120f) { rate = 120f; }
		seeker = new Enemy("\x1E", left, 0, 150f - rate);	// Seeker always spawns above player
	}

	static void UpdateRocks(double elapsed)
	{
		for (int i = 0; i < maxRocks; i++)
		{
			if (rocks[i].isAlive)   // Only update rocks that are alive & defined
			{
				rocks[i].Fall(elapsed);
				rocks[i].Draw();
			}
		}
		if (seeker.isAlive)
		{
			seeker.Fall(elapsed);
			seeker.Draw();
		}
	}

	static void DrawEndscreen()
	{
		string hiscore = " Your score is " + score + " ";
		int width = hiscore.Length;
		int left = (boardWidth - width) / 2;
		int top = (boardHeight - 4) / 2;
        Console.SetCursorPosition(left, top);
		Console.Write(new string(' ', width));
		Console.SetCursorPosition(left, top + 1);
		int leftPad = (width - 9) / 2;
		Console.Write(new string(' ', leftPad) + "GAME OVER" + new string(' ', width - 9 - leftPad));
		Console.SetCursorPosition(left, top + 2);
		Console.Write(hiscore);
		Console.SetCursorPosition(left, top + 3);
		Console.Write(new string(' ', width));
		// Ensure window stays alive for atleast a second, since it's likely the user
		// will keep pressing buttons shortly after losing their last hitpoint
		Thread.Sleep(1000);
		Console.ReadKey();
	}

	static void Main()
	{
		// Setup playable area
		Console.Title = "Falling Rocks";
		Console.CursorVisible = false;
		Console.WindowHeight = boardHeight + 2;
		Console.BufferHeight = boardHeight + 2;
		Console.WindowWidth = boardWidth;
		Console.BufferWidth = boardWidth;

		// First draw
		Console.BackgroundColor = ConsoleColor.Black;
		Console.Clear();
		ClearBoard();
		DrawBoard();
		DrawGround();
		DrawScore();

		// Initialize screen elements
		ScreenElement dwarf = new ScreenElement("\x1B\x02\x1A", (byte)(boardWidth / 2), (byte)(boardHeight - 2));
		Enemy.lastID = maxRocks;		// Set lastID to the end of the array, on next spawn call the index will roll over
		for (int i = 0; i < maxRocks; i++)
		{
			rocks[i] = new Enemy();		// Fill array with dead elements
		}

		// Initialize clock
		Stopwatch timer = new Stopwatch();
		timer.Start();
		TimeSpan frameLast = new TimeSpan(0);
		TimeSpan frameThis = new TimeSpan();
		double lastSpawn = 0.0;		// Last time an enemy was spawned
		int lastSeeker = 0;			// Last score a seeker was spawned

		// Game loop
		ConsoleKeyInfo pressedKey;
		bool escapePressed = false;
		while (true)
		{
			frameThis = timer.Elapsed;

			// Process input
			if (Console.KeyAvailable)
			{
				pressedKey = Console.ReadKey(true);
				switch (pressedKey.Key)
				{
					case ConsoleKey.LeftArrow:
						dwarf.MoveLeft();
						break;
					case ConsoleKey.RightArrow:
						dwarf.MoveRight();
						break;
					case ConsoleKey.Escape:
						escapePressed = true;
						break;
					default:
						break;
				}
				// Escape is used to terminate
				if (escapePressed)
				{
					break;
				}
			}
			// Process enemies
			double elapsed = frameThis.TotalMilliseconds - frameLast.TotalMilliseconds;	// Calculate elapsed time
			if (frameThis.TotalMilliseconds > lastSpawn + (500 - frameLast.TotalMilliseconds * 0.0055))
			{												// Rocks spawn at an increasing rate,
				SpawnRock(frameThis.TotalMilliseconds);		// reaching peak at 90 seconds
				lastSpawn = frameThis.TotalMilliseconds;    // (spawning every tick, if possible)
			}
			// Seeker spawnrate peaks at 90 seconds, which theretically should mean regular intervals,
			// however enemy movement speed is out of phase (peaking at 60 seconds), meaning fewer
			// seekers up to 60 seconds and more seekers later.
			if (score - lastSeeker > (100 + frameLast.TotalMilliseconds / 225))
			{
				lastSeeker = score;
				SpawnSeeker(frameThis.TotalMilliseconds, (byte)(dwarf.Left + 1));
			}
			UpdateRocks(elapsed);	// Make 'em fall
			frameLast = frameThis;

			// Commit elements and render board
			dwarf.Draw(frameLast);   // Character is comitted last for collision detecion
			DrawBoard();
			DrawScore();
			ClearBoard();

			// Check alive state
			if (health < 1)
			{
				break;
			}
		}
		// Exit
		DrawEndscreen();	// Display final score; this will trigger when exiting with Escape
	}
}
