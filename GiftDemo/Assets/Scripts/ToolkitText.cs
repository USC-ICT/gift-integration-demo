using UnityEngine;
using System.Collections;

static public class ToolkitText
{
    static public string[] TipText = new string[]
    {
        "Press the \'M\' key to toggle using the microphone; note the color of the mic icon on the lower right.",
        "When the microphone is is on, press and hold the left mouse button and start talking. Release the button to send the speech.",
        "The keyboard keys \'W\' and \'S\' move the camera forwards and backwards, \'A\' and \'D\' move the left and right, and "
            + "\'Q'\' and \'E\' move up and down.",
        "Press the \'J\' key to toggle free mouse look on and off.",
		"Press the \'C\' key to bring up the debug menu. You can load different levels from here.",
        "Press the \'X\' key to reset the camera to its starting position.",
        "Press the \'Z\' key to show or hide your framerate.",
        "Press the \'O\' key to show or hide the dialog text that you write or say to Brad.",
        "Press the \'I\' key to show or hide the subtitle text that Brad responds to you with.",
        "Press the \'P\' key to show or hide the entire user interface",
        "You can make Brad walk. Hit T to make Brad walk forward, G to step back, and F and H to make me turn left and right."
         + " Full instructions are in the documentation.",
        "Hit ~ to show the debug console and then type \'?\' to see the available commands",
    };

    static public string[] QuestionsToBrad = new string[]
    {
        "Hi Brad",
        "What's up?",
        "How are you doing today?",
        "How old are you?",
        "What is a virtual human?",
        "Who are you?",
        "What can you tell me about the Virtual Human Toolkit?",
        "Where can I find more information?",
    };
}
