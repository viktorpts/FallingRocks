# FallingRocks

Falling Rocks Game, written as a homework assignment for 'Intro to Programming' course, October 2015.

# Overview
This is my interpretation of the game, which deviates somewhat from the one
pictured in the actual homework assignment. I've removed some elements that
I considered non-essential, like rocks of different color and symbol
representations. I also changed the appearance of the playable character.
Some gameplay additions that I made:
- background colors are chosen to represent sky and ground;
- player has health instead of the game ending in a single hit;
- the game becomes progressively faster and harder;
- a special type of obstacle that moves twice as fast and always spawns
  above the player's current position.

# How it works, in short
Most methods will be documented where they appear, this is just a quick
summary. The process runs inside an endless 'while' loop that handles both
input and logic processing. Instead of using an event to achieve concurrent
user controls and frame progression, the 'Console.KeyAvailable' property is
emplyed, since it doesn't block the thread, waiting for input. To make it
possible to have enemies with different movement speed and to allow the
player to move more than once in a single tick, the loop moves as fast as
the CPU allows and enemy movement is based on milliseconds elapsed since
last draw. The player and enemies are class-based with built in properties
for position and appearance, as well as methods for movement. Enemy
spawnrate and movespeed is based on elapsed playing time. Scoring depends
on the speed of enemies (you get more points for faster enemies) and is
awarded for each enemy seperately. Display and collision detection are
handled by an array of 'char' elements that represents the play area.

# How it can be improved
* A secondary screen buffer array for keeping color information will allow
for greater fidelity.
* The global variables are a bit of a mess, the result of feature creep. If
those are packed in a class along with the rendering functions and arrays,
the solution will be a lot neater.
* Simple sound using 'Console.Beep()'.
* I was considering health pickups at one point, but i don't expect the game
has enough hook to keep anyone play for longer than 1 minute, hence the
the difficulty ramp peaking at 90 seconds.
* A drawback of using objects for the enemies is we have to allocate memory
for each of them. Right now this is handled with an array of fixed size,
filled in a rolling fashion (we start over once the end reached and the
first ID is tagged as dead).
