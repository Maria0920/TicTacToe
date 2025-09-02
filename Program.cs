using System;
using System.Collections.Generic;
using System.IO;

public abstract class Game
{
    // The main entry point of the program. It starts the game by displaying the game selection menu.
    public static void Main(string[] args)
    {
        DisplayMenu();
    }


    public static void DisplayMenu()
    {
        Console.WriteLine("Welcome to Game World!");
        Console.WriteLine("Please select a game:");
        Console.WriteLine("1. Treble Cross");
        Console.WriteLine("2. Reversi");
        Console.WriteLine("3. Exit");
        HandleMenuSelection();
    }

    private static void HandleMenuSelection()
    {
        int choice;
        while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > 3)
        {
            Console.WriteLine("Invalid input. Please enter a valid option.");
        }

        switch (choice)
        {
            case 1:
                new TrebleCross().StartSetup();
                break;
            case 2:
                new Reversi().Message();
                break;
            case 3:
                Console.WriteLine("Exiting the game. Goodbye!");
                Environment.Exit(0);
                break;
        }
    }
}

// Reversi game class inheriting from Game, displays a placeholder message.
public class Reversi : Game
{
    public void Message()
    {
        Console.WriteLine("Reversi is coming soon! Choose another option.");
        DisplayMenu();
    }
}

// Treble Cross game class that extends the Game base class.
public class TrebleCross : Game
{
    private GameMode mode;
    private Board board;
    private List<Player> players;
    private int currentPlayerIndex;
    private GameHistory gameHistory;


    public void StartSetup()
    {
        Console.WriteLine("Choose the mode:");
        Console.WriteLine("1. Start New Game");
        Console.WriteLine("2. Load Game");

        int modeChoice;
        while (!int.TryParse(Console.ReadLine(), out modeChoice) || (modeChoice != 1 && modeChoice != 2))
        {
            Console.WriteLine("Invalid input. Please enter a valid mode.");
        }

        if (modeChoice == 1)
        {
            StartNewGame();
        }
        else if (modeChoice == 2)
        {
            LoadGame();
        }
    }


    private void StartNewGame()
    {
        Console.WriteLine("Choose the mode:");
        Console.WriteLine("1. Human vs Human");
        Console.WriteLine("2. Human vs Computer");

        int modeChoice;
        while (!int.TryParse(Console.ReadLine(), out modeChoice) || (modeChoice != 1 && modeChoice != 2))
        {
            Console.WriteLine("Invalid input. Please enter a valid mode.");
        }

        mode = modeChoice == 1 ? GameMode.HumanVsHuman : GameMode.HumanVsComputer;
        SetupGame();
        Start();
    }


    private void LoadGame()
    {
        Console.WriteLine("Select the game to load by entering the file number:");
        List<int> savedGames = GetSavedGames();
        foreach (int fileNumber in savedGames)
        {
            Console.WriteLine($"File {fileNumber}");
        }

        int fileChoice;
        while (!int.TryParse(Console.ReadLine(), out fileChoice) || !savedGames.Contains(fileChoice))
        {
            Console.WriteLine("Invalid input. Please enter a valid file number.");
        }


        (char[] loadedBoardCells, int loadedPlayerIndex) = GameLoader.LoadGame(fileChoice);
        if (loadedBoardCells != null)
        {
            board = new Board(loadedBoardCells.Length);
            board.Cells = loadedBoardCells;
            currentPlayerIndex = loadedPlayerIndex;
            mode = GameMode.HumanVsHuman;
            players = new List<Player> { new HumanPlayer('X', ConsoleColor.Red), new HumanPlayer('X', ConsoleColor.Blue) };
            gameHistory = new GameHistory();

            Start();
        }
        else
        {
            Console.WriteLine("Error loading the game.");
        }
    }

    // Sets up the game environment by initializing the board, players, and other necessary components.
    private void SetupGame()
    {
        Console.Write("Enter the size of the board: ");
        int size;
        while (!int.TryParse(Console.ReadLine(), out size) || size <= 0)
        {
            Console.WriteLine("Invalid input. Please enter a valid board size.");
        }

        board = new Board(size);
        players = new List<Player>();
        if (mode == GameMode.HumanVsHuman)
        {
            players.Add(new HumanPlayer('X', ConsoleColor.Red));
            players.Add(new HumanPlayer('X', ConsoleColor.Blue));
        }
        else if (mode == GameMode.HumanVsComputer)
        {
            players.Add(new HumanPlayer('X', ConsoleColor.Red));
            players.Add(new ComputerPlayer('X', ConsoleColor.Blue));
        }
        currentPlayerIndex = 0;
        gameHistory = new GameHistory();
    }

    // Starts the game loop, handling player moves, game saving, undo/redo, and checking for game end conditions.
    public void Start()
    {
        bool gameOver = false;

        do
        {
            Console.Clear();
            board.Display();
            Console.WriteLine($"\nPlayer {(currentPlayerIndex + 1)}'s turn:");

            int move = players[currentPlayerIndex].GetMove(board);
            char[] previousBoard = (char[])board.Cells.Clone(); // Store previous board state for undo/redo

            // Handle special commands (save, undo, redo, exit) and validate moves.
            switch (move)
            {
                case -1: // Save
                    int fileNumber = GetNextAvailableFileNumber();
                    if (GameLoader.SaveGame(board.Cells, currentPlayerIndex, fileNumber))
                    {
                        Console.WriteLine($"Game saved successfully with file number {fileNumber}.");
                        Console.WriteLine("Do you want to continue playing? (y/n)");
                        if (Console.ReadLine().ToLower() != "y")
                        {
                            gameOver = true;
                        }
                    }
                    continue;
                case -2: // Undo
                    (char[] undoBoard, int undoPlayerIndex) = gameHistory.Undo(mode);
                    if (undoBoard != null)
                    {
                        board.Cells = undoBoard;
                        currentPlayerIndex = undoPlayerIndex;
                    }
                    continue;
                case -3: // Redo
                    var (redoBoard, redoPlayerIndex) = gameHistory.Redo(mode);
                    if (redoBoard != null)
                    {
                        board.Cells = redoBoard;
                        currentPlayerIndex = redoPlayerIndex;
                        continue; // Continue the loop without prompting for input again
                    }
                    else if (redoPlayerIndex == -2) // Check for the distinct value indicating no redo moves available
                    {
                        Console.WriteLine("No moves available to redo.");
                        continue;
                    }
                    break;
                case -4: // Exit
                    Console.WriteLine("Exiting the game. Returning to the main menu.");
                    Game.DisplayMenu();
                    return;
                default:
                    gameHistory.Push(previousBoard, currentPlayerIndex, mode);

                    if (board.MakeMove(move, players[currentPlayerIndex].Symbol))
                    {
                        if (board.IsWin(players[currentPlayerIndex].Symbol))
                        {
                            gameOver = true;
                            Console.Clear();
                            board.Display();
                            Console.WriteLine($"Player {(currentPlayerIndex + 1)} won!");
                        }
                        else if (board.IsDraw())
                        {
                            gameOver = true;
                            Console.Clear();
                            board.Display();
                            Console.WriteLine("It's a draw!");
                        }
                        else
                        {
                            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid move. Please try again.");
                    }
                    break;
            }

        } while (!gameOver);

        Console.WriteLine("Press any key to return to the main screen...");
        Console.ReadKey();
        Game.DisplayMenu(); // Returning to the main menu after exiting the game
    }

    // Retrieves the next available file number for saving the game.
    private int GetNextAvailableFileNumber()
    {
        int fileNumber = 1;
        List<int> savedGames = GetSavedGames();
        while (savedGames.Contains(fileNumber))
        {
            fileNumber++;
        }
        return fileNumber;
    }

    // Retrieves a list of saved game file numbers.
    private List<int> GetSavedGames()
    {
        List<int> savedGames = new List<int>();
        for (int i = 1; i <= 10; i++) // Assuming there are 10 saved games
        {
            string fileName = $"saved_game_{i}.txt";
            if (File.Exists(fileName))
            {
                savedGames.Add(i);
            }
        }
        return savedGames;
    }
}

// Enumeration to distinguish between different game modes.
public enum GameMode
{
    HumanVsHuman,
    HumanVsComputer
}

// Board class manages the game board, handling display and move execution.
public class Board
{
    public char[] Cells { get; set; } // Array of cells representing the board.

    // Constructor initializes the board cells.
    public Board(int size)
    {
        Cells = new char[size];
        for (int i = 0; i < size; i++)
        {
            Cells[i] = ' ';
        }
    }

    // Displays the current state of the board and game instructions.
    public void Display()
    {
        Console.WriteLine("\nBoard:");
        for (int i = 0; i < Cells.Length; i++)
        {
            Console.Write($"[{(Cells[i] != ' ' ? Cells[i].ToString() : (i + 1).ToString())}]");
        }
        Console.WriteLine();
        HelpSystem.DisplayHelp();
    }

    // Attempts to make a move on the board; returns false if the move is invalid.
    public bool MakeMove(int position, char symbol)
    {
        if (position < 1 || position > Cells.Length || Cells[position - 1] != ' ')
        {
            return false;
        }

        Cells[position - 1] = symbol;
        return true;
    }

    // Checks for a winning condition based on consecutive symbols.
    public bool IsWin(char symbol)
    {
        return CheckHorizontal(symbol);
    }

    // Checks for a horizontal win by examining sequences of three cells.
    private bool CheckHorizontal(char symbol)
    {
        for (int i = 0; i <= Cells.Length - 3; i++)
        {
            if (Cells[i] == symbol && Cells[i] == Cells[i + 1] && Cells[i + 1] == Cells[i + 2])
            {
                return true;
            }
        }
        return false;
    }

    // Checks if all cells are filled, indicating a draw if no win has been declared.
    public bool IsDraw()
    {
        foreach (char cell in Cells)
        {
            if (cell == ' ')
                return false;
        }
        return true;
    }
}


public abstract class Player
{
    public char Symbol { get; set; }
    public ConsoleColor Color { get; set; }
    public string Type { get; set; }

    // Abstract method that derived classes must implement to determine how they select moves.
    public abstract int GetMove(Board board);
}

// HumanPlayer class derived from Player, handles human player interactions.
public class HumanPlayer : Player
{

    public HumanPlayer(char symbol, ConsoleColor color)
    {
        Symbol = symbol;
        Color = color;
        Type = "Human";
    }

    // Processes human player input, allowing for game control commands or move selection.
    public override int GetMove(Board board)
    {
        Console.WriteLine($"Enter your move (1-{board.Cells.Length}), (s) Save, (u) Undo, (r) Redo, (e) Exit:");
        string input = Console.ReadLine().ToLower();

        if (input == "s")
        {
            return -1; // Save
        }
        else if (input == "u")
        {
            return -2; // Undo
        }
        else if (input == "r")
        {
            return -3; // Redo
        }
        else if (input == "e")
        {
            return -4; // Exit
        }
        else if (int.TryParse(input, out int position))
        {
            return position;
        }
        else
        {
            Console.WriteLine("Invalid move. Please try again.");
            return GetMove(board);
        }
    }
}

// ComputerPlayer class derived from Player, simulates computer-controlled moves.
public class ComputerPlayer : Player
{

    public ComputerPlayer(char symbol, ConsoleColor color)
    {
        Symbol = symbol;
        Color = color;
        Type = "Computer";
    }

    // Generates a random move within the bounds of the board, representing the computer's strategy.
    public override int GetMove(Board board)
    {
        Random random = new Random();
        return random.Next(1, board.Cells.Length + 1);
    }
}

// Piece class to represent a game piece, currently unused.
public class Piece
{
    public char Symbol { get; private set; }
    public ConsoleColor Color { get; private set; }

    public Piece(char symbol, ConsoleColor color)
    {
        Symbol = symbol;
        Color = color;
    }
}

// GameHistory class to manage game history, supporting undo and redo functionality.
public class GameHistory
{
    private Stack<(char[] board, int currentPlayerIndex)> humanVsHumanPreviousMoves; // Stack for human vs human move history.
    private Stack<(char[] board, int currentPlayerIndex)> humanVsComputerPreviousMoves; // Stack for human vs computer move history.
    private Stack<(char[] board, int currentPlayerIndex)> humanVsHumanRedoMoves; // Stack for redoing moves in human vs human.
    private Stack<(char[] board, int currentPlayerIndex)> humanVsComputerRedoMoves; // Stack for redoing moves in human vs computer.

    // Constructor initializes the history stacks.
    public GameHistory()
    {
        humanVsHumanPreviousMoves = new Stack<(char[], int)>();
        humanVsComputerPreviousMoves = new Stack<(char[], int)>();
        humanVsHumanRedoMoves = new Stack<(char[], int)>();
        humanVsComputerRedoMoves = new Stack<(char[], int)>();
    }

    // Pushes the current game state onto the appropriate history stack based on the game mode.
    public void Push(char[] board, int currentPlayerIndex, GameMode mode)
    {
        if (mode == GameMode.HumanVsHuman)
        {
            humanVsHumanPreviousMoves.Push((board, currentPlayerIndex));
            humanVsHumanRedoMoves.Clear();
        }
        else if (mode == GameMode.HumanVsComputer)
        {
            humanVsComputerPreviousMoves.Push((board, currentPlayerIndex));
            humanVsComputerRedoMoves.Clear();
        }
    }

    public (char[] board, int currentPlayerIndex) Undo(GameMode mode)
    {
        if (mode == GameMode.HumanVsHuman)
        {
            if (humanVsHumanPreviousMoves.Count == 0)
                return (null, -1);

            var previousMove = humanVsHumanPreviousMoves.Pop();
            humanVsHumanRedoMoves.Push(previousMove);
            return previousMove;
        }
        else if (mode == GameMode.HumanVsComputer)
        {
            if (humanVsComputerPreviousMoves.Count < 2)
                return (null, -1);

            var previousMove = humanVsComputerPreviousMoves.Pop();
            var secondPreviousMove = humanVsComputerPreviousMoves.Pop();
            humanVsComputerRedoMoves.Push(previousMove);
            humanVsComputerRedoMoves.Push(secondPreviousMove);
            return secondPreviousMove;
        }
        return (null, -1);
    }


    public (char[] board, int currentPlayerIndex) Redo(GameMode mode)
    {
        Stack<(char[], int)> previousMoves;
        Stack<(char[], int)> redoMoves;

        if (mode == GameMode.HumanVsHuman)
        {
            previousMoves = humanVsHumanPreviousMoves;
            redoMoves = humanVsHumanRedoMoves;
        }
        else if (mode == GameMode.HumanVsComputer)
        {
            previousMoves = humanVsComputerPreviousMoves;
            redoMoves = humanVsComputerRedoMoves;
        }
        else
        {
            // Unsupported game mode
            return (null, -1);
        }

        if (redoMoves.Count == 0)
            return (null, -2);

        var redoMove = redoMoves.Pop();
        previousMoves.Push(redoMove);

        // Check if there's a next redo move available
        if (redoMoves.Count > 0)
        {

            var nextRedoMove = redoMoves.Peek();
            return nextRedoMove;
        }
        else
        {

            return redoMove;
        }
    }
}

// GameLoader class manages the file-based saving and loading of game states.
public class GameLoader
{

    public static bool SaveGame(char[] boardCells, int currentPlayerIndex, int fileNumber)
    {
        try
        {
            File.WriteAllLines($"saved_game_{fileNumber}.txt", new string[] { new string(boardCells), currentPlayerIndex.ToString() });
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving game: {ex.Message}");
            return false;
        }
    }

    // Loads a game state from a file, returning the board state and current player index.
    public static (char[] boardCells, int currentPlayerIndex) LoadGame(int fileNumber)
    {
        int currentPlayerIndex = 0;
        string fileName = $"saved_game_{fileNumber}.txt";
        try
        {
            string[] lines = File.ReadAllLines(fileName);
            if (lines.Length > 1)
            {
                currentPlayerIndex = int.Parse(lines[1]);
                return (lines[0].ToCharArray(), currentPlayerIndex);
            }
            else
            {
                Console.WriteLine("No saved game found.");
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("No saved game found.");
        }
        return (null, -1);
    }
}


public class HelpSystem
{
    // Displays help information for saving, undoing, redoing, and exiting the game.
    public static void DisplayHelp()
    {
        Console.WriteLine($"Instructions: (s) Save | (u) Undo | (r) Redo | (e) Exit");
    }
}