using SDL2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

static partial class Engine
{
    private static HashSet<MouseButton> MouseButtonsDown = new HashSet<MouseButton>();
    private static HashSet<MouseButton> MouseButtonsHeld = new HashSet<MouseButton>();
    private static HashSet<MouseButton> MouseButtonsUp = new HashSet<MouseButton>();
    private static HashSet<Key> KeysDown = new HashSet<Key>();
    private static HashSet<Key> KeysDownAutorepeat = new HashSet<Key>();
    private static HashSet<Key> KeysHeld = new HashSet<Key>();
    private static HashSet<Key> KeysUp = new HashSet<Key>();
    private static Dictionary<int, Gamepad> GamepadsByID = new Dictionary<int, Gamepad>();
    private static Gamepad[] GamepadsByPlayer = new Gamepad[64];

    /// <summary>
    /// The current position of the mouse cursor (in pixels).
    /// </summary>
    public static Vector2 MousePosition { get; private set; }

    /// <summary>
    /// The change in position of the mouse cursor this frame (in pixels).
    /// </summary>
    public static Vector2 MouseMotion { get; private set; }

    /// <summary>
    /// The amount the mouse wheel has been scrolled this frame (in scroll units).
    /// </summary>
    public static float MouseScroll { get; private set; }

    /// <summary>
    /// The textual representation of the keys that were pressed this frame.
    /// </summary>
    public static string TypedText { get; private set; }

    private static void PollEvents()
    {
        // ======================================================================================
        // Reset per-frame input flags
        // ======================================================================================

        MouseButtonsDown.Clear();
        MouseButtonsUp.Clear();
        MouseScroll = 0;

        KeysDown.Clear();
        KeysDownAutorepeat.Clear();
        KeysUp.Clear();
        TypedText = "";
        
        foreach (Gamepad gamepad in GamepadsByID.Values)
        {
            gamepad.ButtonsDown.Clear();
            gamepad.ButtonsUp.Clear();
        }

        // ======================================================================================
        // Poll for new events from SDL
        // ======================================================================================

        SDL.SDL_Event evt;
        while (SDL.SDL_PollEvent(out evt) != 0)
        {
            if (evt.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
            {
                MousePosition = new Vector2(evt.motion.x, evt.motion.y);
                MouseMotion = new Vector2(evt.motion.xrel, evt.motion.yrel);
            }
            else if (evt.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
            {
                MouseButton button = (MouseButton)evt.button.button;
                MouseButtonsDown.Add(button);
                MouseButtonsHeld.Add(button);
            }
            else if (evt.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
            {
                MouseButton button = (MouseButton)evt.button.button;
                MouseButtonsUp.Add(button);
                MouseButtonsHeld.Remove(button);
            }
            else if (evt.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
            {
                MouseScroll = evt.wheel.y;
            }
            else if (evt.type == SDL.SDL_EventType.SDL_KEYDOWN)
            {
                if (evt.key.repeat == 0)
                {
                    KeysDown.Add((Key)evt.key.keysym.sym);
                    KeysHeld.Add((Key)evt.key.keysym.sym);
                }
                else
                {
                    KeysDownAutorepeat.Add((Key)evt.key.keysym.sym);
                }
            }
            else if (evt.type == SDL.SDL_EventType.SDL_KEYUP)
            {
                KeysUp.Add((Key)evt.key.keysym.sym);
                KeysHeld.Remove((Key)evt.key.keysym.sym);
            }
            else if (evt.type == SDL.SDL_EventType.SDL_TEXTINPUT)
            {
                // Convert the byte array into a C# string:
                string text = null;
                unsafe
                {
                    byte[] bufferCopy = new byte[SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE];
                    Marshal.Copy((IntPtr)evt.text.text, bufferCopy, 0, bufferCopy.Length);
                    text = Encoding.UTF8.GetString(bufferCopy);
                }

                TypedText += text[0];
            }
            else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED)
            {
                // Create the gamepad object:
                IntPtr handle = SDL.SDL_GameControllerOpen(evt.cdevice.which);
                Gamepad gamepad = new Gamepad(handle);

                // All other gamepad events use the "joystick instance ID" to identify the gamepad:
                int id = SDL.SDL_JoystickInstanceID(SDL.SDL_GameControllerGetJoystick(handle));
                GamepadsByID[id] = gamepad;

                // Use the lowest available player number for this gamepad:
                for (int i = 0; i < GamepadsByPlayer.Length; i++)
                {
                    if (GamepadsByPlayer[i] == null)
                    {
                        GamepadsByPlayer[i] = gamepad;
                        break;
                    }
                }
            }
            else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED)
            {
                Gamepad gamepad;
                if (GamepadsByID.TryGetValue(evt.cdevice.which, out gamepad))
                {
                    // Destroy the gamepad:
                    SDL.SDL_GameControllerClose(gamepad.Handle);
                    GamepadsByID.Remove(evt.cdevice.which);

                    // Free up this player number for use by the next gamepad attached:
                    for (int i = 0; i < GamepadsByPlayer.Length; i++)
                    {
                        if (GamepadsByPlayer[i] == gamepad)
                        {
                            GamepadsByPlayer[i] = null;
                            break;
                        }
                    }
                }
            }
            else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION)
            {
                Gamepad gamepad;
                if (GamepadsByID.TryGetValue(evt.caxis.which, out gamepad))
                {
                    float value = ((float)evt.caxis.axisValue) / short.MaxValue;
                    switch ((SDL.SDL_GameControllerAxis)evt.caxis.axis)
                    {
                        case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX: gamepad.LeftStick.X = value; break;
                        case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY: gamepad.LeftStick.Y = value; break;
                        case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX: gamepad.RightStick.X = value; break;
                        case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY: gamepad.RightStick.Y = value; break;
                        case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT: gamepad.Triggers.X = value; break;
                        case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT: gamepad.Triggers.Y = value; break;
                    }
                }
            }
            else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN)
            {
                Gamepad gamepad;
                if (GamepadsByID.TryGetValue(evt.cbutton.which, out gamepad))
                {
                    GamepadButton button = (GamepadButton)evt.cbutton.button;
                    gamepad.ButtonsDown.Add(button);
                    gamepad.ButtonsHeld.Add(button);
                }
            }
            else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP)
            {
                Gamepad gamepad;
                if (GamepadsByID.TryGetValue(evt.cbutton.which, out gamepad))
                {
                    GamepadButton button = (GamepadButton)evt.cbutton.button;
                    gamepad.ButtonsUp.Add(button);
                    gamepad.ButtonsHeld.Remove(button);
                }
            }
            else if (evt.type == SDL.SDL_EventType.SDL_QUIT)
            {
                Environment.Exit(0);
            }
        }
    }

    // ======================================================================================
    // Mouse input
    // ======================================================================================

    /// <summary>
    /// Sets the mouse mode, which controls the visibility and lock state of the cursor.
    /// </summary>
    /// <param name="mode">The mouse mode to set.</param>
    public static void SetMouseMode(MouseMode mode)
    {
        switch (mode)
        {
            case MouseMode.Visible:
                SDL.SDL_SetRelativeMouseMode(SDL.SDL_bool.SDL_FALSE);
                SDL.SDL_ShowCursor(1);
                break;
            case MouseMode.Hidden:
                SDL.SDL_SetRelativeMouseMode(SDL.SDL_bool.SDL_FALSE);
                SDL.SDL_ShowCursor(0);
                break;
            case MouseMode.Locked:
                SDL.SDL_SetRelativeMouseMode(SDL.SDL_bool.SDL_TRUE);
                break;
        }
    }

    /// <summary>
    /// Returns true if a mouse button was pressed down this frame.
    /// </summary>
    /// <param name="button">The mouse button to query.</param>
    public static bool GetMouseButtonDown(MouseButton button)
    {
        return MouseButtonsDown.Contains(button);
    }

    /// <summary>
    /// Returns true if a mouse button was held during this frame.
    /// </summary>
    /// <param name="button">The mouse button to query.</param>
    public static bool GetMouseButtonHeld(MouseButton button)
    {
        return MouseButtonsHeld.Contains(button);
    }

    /// <summary>
    /// Returns true if a mouse button was released this frame.
    /// </summary>
    /// <param name="button"></param>
    public static bool GetMouseButtonUp(MouseButton button)
    {
        return MouseButtonsUp.Contains(button);
    }

    // ======================================================================================
    // Keyboard input
    // ======================================================================================

    /// <summary>
    /// Returns true if a key was pressed down this frame.
    /// </summary>
    /// <param name="key">The key to query.</param>
    /// <param name="allowAutorepeat">Whether or not key-down events generated by autorepeat will be included.</param>
    public static bool GetKeyDown(Key key, bool allowAutorepeat = false)
    {
        if (allowAutorepeat && KeysDownAutorepeat.Contains(key))
        {
            return true;
        }

        return KeysDown.Contains(key);
    }

    /// <summary>
    /// Returns true if a key was held during this frame.
    /// </summary>
    /// <param name="key">The key to query.</param>
    public static bool GetKeyHeld(Key key)
    {
        return KeysHeld.Contains(key);
    }

    /// <summary>
    /// Returns true if a key was released this frame.
    /// </summary>
    /// <param name="key">The key to query.</param>
    public static bool GetKeyUp(Key key)
    {
        return KeysUp.Contains(key);
    }

    // ======================================================================================
    // Gamepad input
    // ======================================================================================

    /// <summary>
    /// Returns true if a player's gamepad is connected.
    /// </summary>
    /// <param name="player">The player to query, starting with 0.</param>
    public static bool GetGamepadConnected(int player)
    {
        return player >= 0 && player < GamepadsByPlayer.Length && GamepadsByPlayer[player] != null;
    }

    /// <summary>
    /// Reads the analog values of the specified axis on a player's gamepad.
    /// </summary>
    /// <param name="player">The player to query, starting with 0.</param>
    /// <param name="axis">The gamepad axis to query.</param>
    public static Vector2 GetGamepadAxis(int player, GamepadAxis axis)
    {
        if (GetGamepadConnected(player))
        {
            switch (axis)
            {
                case GamepadAxis.LeftStick: return GamepadsByPlayer[player].LeftStick;
                case GamepadAxis.RightStick: return GamepadsByPlayer[player].RightStick;
                case GamepadAxis.Triggers: return GamepadsByPlayer[player].Triggers;
                default: throw new Exception("Unhandled case.");
            }
        }
        else
        {
            return Vector2.Zero;
        }
    }

    /// <summary>
    /// Returns true if a gamepad button was pressed down this frame.
    /// </summary>
    /// <param name="player">The player to query, starting with 0.</param>
    /// <param name="button">The gamepad button to query.</param>
    /// <returns></returns>
    public static bool GetGamepadButtonDown(int player, GamepadButton button)
    {
        if (GetGamepadConnected(player))
        {
            return GamepadsByPlayer[player].ButtonsDown.Contains(button);
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true if a gamepad button was held during this frame.
    /// </summary>
    /// <param name="player">The player to query, starting with 0.</param>
    /// <param name="button">The gamepad button to query.</param>
    /// <returns></returns>
    public static bool GetGamepadButtonHeld(int player, GamepadButton button)
    {
        if (GetGamepadConnected(player))
        {
            return GamepadsByPlayer[player].ButtonsHeld.Contains(button);
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true if a gamepad button was released this frame.
    /// </summary>
    /// <param name="player">The player to query, starting with 0.</param>
    /// <param name="button">The gamepad button to query.</param>
    /// <returns></returns>
    public static bool GetGamepadButtonUp(int player, GamepadButton button)
    {
        if (GetGamepadConnected(player))
        {
            return GamepadsByPlayer[player].ButtonsUp.Contains(button);
        }
        else
        {
            return false;
        }
    }

    private class Gamepad
    {
        public readonly IntPtr Handle;
        public readonly HashSet<GamepadButton> ButtonsDown = new HashSet<GamepadButton>();
        public readonly HashSet<GamepadButton> ButtonsHeld = new HashSet<GamepadButton>();
        public readonly HashSet<GamepadButton> ButtonsUp = new HashSet<GamepadButton>();
        public Vector2 LeftStick = Vector2.Zero;
        public Vector2 RightStick = Vector2.Zero;
        public Vector2 Triggers = Vector2.Zero;

        public Gamepad(IntPtr handle)
        {
            Handle = handle;
        }
    }
}

public enum Key
{
    Return = '\r',
    Escape = 27,
    Backspace = '\b',
    Tab = '\t',
    Space = ' ',
    Exclamation = '!',
    DoubleQuote = '"',
    Hash = '#',
    Percent = '%',
    Dollar = '$',
    Ampersand = '&',
    SingleQuote = '\'',
    LeftParen = '(',
    RightParen = ')',
    Asterisk = '*',
    Plus = '+',
    Comma = ',',
    Minus = '-',
    Period = '.',
    Slash = '/',
    NumRow0 = '0',
    NumRow1 = '1',
    NumRow2 = '2',
    NumRow3 = '3',
    NumRow4 = '4',
    NumRow5 = '5',
    NumRow6 = '6',
    NumRow7 = '7',
    NumRow8 = '8',
    NumRow9 = '9',
    Colon = ':',
    Semicolon = ';',
    Less = '<',
    Equals = '=',
    Greater = '>',
    Question = '?',
    At = '@',
    LeftBracket = '[',
    Backslash = '\\',
    RightBracket = ']',
    Caret = '^',
    Underscore = '_',
    Backquote = '`',
    A = 'a',
    B = 'b',
    C = 'c',
    D = 'd',
    E = 'e',
    F = 'f',
    G = 'g',
    H = 'h',
    I = 'i',
    J = 'j',
    K = 'k',
    L = 'l',
    M = 'm',
    N = 'n',
    O = 'o',
    P = 'p',
    Q = 'q',
    R = 'r',
    S = 's',
    T = 't',
    U = 'u',
    V = 'v',
    W = 'w',
    X = 'x',
    Y = 'y',
    Z = 'z',
    CapsLock = SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK | SDL.SDLK_SCANCODE_MASK,
    F1 = SDL.SDL_Scancode.SDL_SCANCODE_F1 | SDL.SDLK_SCANCODE_MASK,
    F2 = SDL.SDL_Scancode.SDL_SCANCODE_F2 | SDL.SDLK_SCANCODE_MASK,
    F3 = SDL.SDL_Scancode.SDL_SCANCODE_F3 | SDL.SDLK_SCANCODE_MASK,
    F4 = SDL.SDL_Scancode.SDL_SCANCODE_F4 | SDL.SDLK_SCANCODE_MASK,
    F5 = SDL.SDL_Scancode.SDL_SCANCODE_F5 | SDL.SDLK_SCANCODE_MASK,
    F6 = SDL.SDL_Scancode.SDL_SCANCODE_F6 | SDL.SDLK_SCANCODE_MASK,
    F7 = SDL.SDL_Scancode.SDL_SCANCODE_F7 | SDL.SDLK_SCANCODE_MASK,
    F8 = SDL.SDL_Scancode.SDL_SCANCODE_F8 | SDL.SDLK_SCANCODE_MASK,
    F9 = SDL.SDL_Scancode.SDL_SCANCODE_F9 | SDL.SDLK_SCANCODE_MASK,
    F10 = SDL.SDL_Scancode.SDL_SCANCODE_F10 | SDL.SDLK_SCANCODE_MASK,
    F11 = SDL.SDL_Scancode.SDL_SCANCODE_F11 | SDL.SDLK_SCANCODE_MASK,
    F12 = SDL.SDL_Scancode.SDL_SCANCODE_F12 | SDL.SDLK_SCANCODE_MASK,
    PrintScreen = SDL.SDL_Scancode.SDL_SCANCODE_PRINTSCREEN | SDL.SDLK_SCANCODE_MASK,
    ScrollLock = SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK | SDL.SDLK_SCANCODE_MASK,
    Pause = SDL.SDL_Scancode.SDL_SCANCODE_PAUSE | SDL.SDLK_SCANCODE_MASK,
    Insert = SDL.SDL_Scancode.SDL_SCANCODE_INSERT | SDL.SDLK_SCANCODE_MASK,
    Home = SDL.SDL_Scancode.SDL_SCANCODE_HOME | SDL.SDLK_SCANCODE_MASK,
    PageUp = SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP | SDL.SDLK_SCANCODE_MASK,
    Delete = 127,
    End = SDL.SDL_Scancode.SDL_SCANCODE_END | SDL.SDLK_SCANCODE_MASK,
    PageDown = SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN | SDL.SDLK_SCANCODE_MASK,
    Right = SDL.SDL_Scancode.SDL_SCANCODE_RIGHT | SDL.SDLK_SCANCODE_MASK,
    Left = SDL.SDL_Scancode.SDL_SCANCODE_LEFT | SDL.SDLK_SCANCODE_MASK,
    Down = SDL.SDL_Scancode.SDL_SCANCODE_DOWN | SDL.SDLK_SCANCODE_MASK,
    Up = SDL.SDL_Scancode.SDL_SCANCODE_UP | SDL.SDLK_SCANCODE_MASK,
    NumpadDivide = SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE | SDL.SDLK_SCANCODE_MASK,
    NumpadMultiply = SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY | SDL.SDLK_SCANCODE_MASK,
    NumpadMinus = SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS | SDL.SDLK_SCANCODE_MASK,
    NumpadPlus = SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS | SDL.SDLK_SCANCODE_MASK,
    NumpadEnter = SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER | SDL.SDLK_SCANCODE_MASK,
    Numpad1 = SDL.SDL_Scancode.SDL_SCANCODE_KP_1 | SDL.SDLK_SCANCODE_MASK,
    Numpad2 = SDL.SDL_Scancode.SDL_SCANCODE_KP_2 | SDL.SDLK_SCANCODE_MASK,
    Numpad3 = SDL.SDL_Scancode.SDL_SCANCODE_KP_3 | SDL.SDLK_SCANCODE_MASK,
    Numpad4 = SDL.SDL_Scancode.SDL_SCANCODE_KP_4 | SDL.SDLK_SCANCODE_MASK,
    Numpad5 = SDL.SDL_Scancode.SDL_SCANCODE_KP_5 | SDL.SDLK_SCANCODE_MASK,
    Numpad6 = SDL.SDL_Scancode.SDL_SCANCODE_KP_6 | SDL.SDLK_SCANCODE_MASK,
    Numpad7 = SDL.SDL_Scancode.SDL_SCANCODE_KP_7 | SDL.SDLK_SCANCODE_MASK,
    Numpad8 = SDL.SDL_Scancode.SDL_SCANCODE_KP_8 | SDL.SDLK_SCANCODE_MASK,
    Numpad9 = SDL.SDL_Scancode.SDL_SCANCODE_KP_9 | SDL.SDLK_SCANCODE_MASK,
    Numpad0 = SDL.SDL_Scancode.SDL_SCANCODE_KP_0 | SDL.SDLK_SCANCODE_MASK,
    NumpadPeriod = SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD | SDL.SDLK_SCANCODE_MASK,
    LeftControl = SDL.SDL_Scancode.SDL_SCANCODE_LCTRL | SDL.SDLK_SCANCODE_MASK,
    LeftShift = SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT | SDL.SDLK_SCANCODE_MASK,
    LeftAlt = SDL.SDL_Scancode.SDL_SCANCODE_LALT | SDL.SDLK_SCANCODE_MASK,
    RightControl = SDL.SDL_Scancode.SDL_SCANCODE_RCTRL | SDL.SDLK_SCANCODE_MASK,
    RightShift = SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT | SDL.SDLK_SCANCODE_MASK,
    RightAlt = SDL.SDL_Scancode.SDL_SCANCODE_RALT | SDL.SDLK_SCANCODE_MASK,
}

public enum MouseMode
{
    /// <summary>
    /// Show the cursor and allow it to move freely. The default mode.
    /// </summary>
    Visible,

    /// <summary>
    /// Hide the cursor and allow it to move freely. Useful for when you want to draw your own mouse cursor.
    /// </summary>
    Hidden,

    /// <summary>
    /// Hide the cursor and lock it to the center of the screen so that only MouseMotion is relevant. Useful for games like first-person shooters.
    /// </summary>
    Locked,
}

public enum MouseButton
{
    Left = 1,
    Middle = 2,
    Right = 3,
}

public enum GamepadButton
{
    A = 0,
    B,
    X,
    Y,
    Back,
    Guide,
    Start,
    LeftStick,
    RightStick,
    LeftShoulder,
    RightShoulder,
    Up,
    Down,
    Left,
    Right,
}

public enum GamepadAxis
{
    /// <summary>
    /// The left analog stick, with values from -1 to 1.
    /// </summary>
    LeftStick,

    /// <summary>
    /// The right analog stick, with values from -1 to 1. 
    /// </summary>
    RightStick,

    /// <summary>
    /// The analog triggers, with the left trigger in X, the right trigger in Y, and values from 0 to 1.
    /// </summary>
    Triggers,
}
