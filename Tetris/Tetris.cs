using System.Timers;

namespace Tetris
{
    //Finding and removing lines can be optimized
    //general optimization
    //difficulty curve
    //if you move the second, the update is called, blocks can enter eachother - maybe add an instruction queue - or change the way it updates all together
    public class Tetris : IDisposable
    {
        const int SINGLE_POINTS = 40;
        const int DOUBLE_POINTS = 100;
        const int TRIPLE_POINTS = 300;
        const int TETRIS_POINTS = 1200;
        const int MAX_TICK = 40;

        public event Action? OnGridUpdated;

        System.Timers.Timer timer;
        public Tetris(int width = 10, int heigth = 20)
        {
            timer = new(5);
            timer.AutoReset = true;
            timer.Elapsed += Tick;
            Width = width;
            Heigth = heigth;
            Reset();
            timer.Start();
        }

        private Queue<Input> inputQueue = new Queue<Input>();
        private void Control(Input input)
        {
            if(GameState == GameState.Active)
            switch (input)
            {
                case Input.Rotate:
                    Rotate();
                    break;
                case Input.MoveLeft:
                    Move(-1, true);
                    break;
                case Input.MoveRight:
                    Move(1, true);
                    break;
                case Input.Place:
                    Move(-100, false);
                    break;
                case Input.MoveDown:
                    Move(-1, false);
                    break;
                default:
                    break;
            }
        }

        public void SendInput(Input input) => inputQueue.Enqueue(input);

        int SinceLastTick = 0;

        public void Update()
        {
            if (GameState != GameState.Active)
                return;

            if(inputQueue.Count > 0)
            {
                while (inputQueue.Count > 0)
                {
                    Control(inputQueue.Dequeue());
                }

                OnGridUpdated?.Invoke();
            }
        }

        private void Tick(object? source, ElapsedEventArgs e)
        {
            SinceLastTick++;

            if (SinceLastTick >= MAX_TICK - LinesCleared / 2)
            {
                inputQueue.Enqueue(Input.MoveDown);
                SinceLastTick = 0;
            }
        }

        public void Reset()
        {
            Cells = new CellType[Width, Heigth];
            SpawnPiece();
            Points = 0;
            OnGridUpdated?.Invoke();
            inputQueue.Clear();
            GameState = GameState.Active;
        }

        private void Move(int move, bool horizontal)
        {
            Console.WriteLine("Moving");

            int moveX = 0;
            int moveY = 0;

            int moveCount = (int)MathF.Abs(move);
            move /= moveCount;

            if (horizontal)
                moveX = move;
            else moveY = move;
                
            for (int i = 0; i < moveCount; i++)
            {
                if (Cast(ActivePieceType, ActiveRotation, ActiveX + moveX, ActiveY + moveY))
                {
                    Console.WriteLine("Collission detected");

                    if (!horizontal)
                    {
                        LockActivePiece();
                        SpawnPiece();
                        CheckAndRemoveLines();
                        inputQueue.Clear();
                        return;
                    }
                }
                else
                {
                    if (horizontal)
                        ActiveX += move;
                    else ActiveY += move;
                }
            }
        }

        private bool Rotate()
        {
            int newRotation = ActiveRotation - 1;

            if (newRotation < 0)
                newRotation = pieces[ActivePieceType].Length -1;

            if (!Cast(ActivePieceType, newRotation, ActiveX, ActiveY))
            {
                ActiveRotation = newRotation;
                return true;
            }

            return false;
        }


        //Checks if move is possible
        private bool Cast(int piecetype, int rotation, int Xpos, int Ypos)
        {
            bool[] piece = pieces[piecetype][rotation];

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    //check if out of bounds (sides or bottom)
                    if(y+Ypos < 0 || x+Xpos<0 || x+Xpos >= Width)
                    {
                        if (piece[4 * y + x])
                            return true;

                        continue;
                    }

                    if(y+Ypos < Heigth)
                    {
                        if (piece[4 * y + x] && Cells[Xpos + x, Ypos + y] != CellType.Empty)
                            return true;
                    }
                    //check if cell is occupied
                }
            }

            return false;
        }


        //locks the active piece in place and check if you lost
        private void LockActivePiece()
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if(y + ActiveY >= Heigth)
                    {
                        if (ActivePiece[4 * y + x])
                        {
                            GameState = GameState.Lost;
                            continue;
                        }
                    }

                    if (y + ActiveY >= 0 && y + ActiveY < Heigth && x + ActiveX >= 0 && x + ActiveX < Width)
                    {
                        if (ActivePiece[4 * y + x])
                        {
                            Cells[x+ActiveX, y+ActiveY] = (CellType)ActivePieceType+1;
                        }
                    }
                }
            }

            SpawnPiece();
        }

        private void SpawnPiece()
        {
            ActiveX = Width / 2 - 2;
            ActiveY = Heigth;
            ActiveRotation = 0;
            ActivePieceType = new Random().Next(0,7);
        }

        //checks if any lines are complete and removes them and moves down the rest
        private void CheckAndRemoveLines()
        {
            List<int> toRemove = new List<int>();

            for (int y = 0; y < Heigth; y++)
            {
                if (toRemove.Count == 4)
                    break;

                bool shouldRemove = true;

                for (int x = 0; x < Width; x++)
                {
                    if (Cells[x, y] == CellType.Empty)
                    {
                        shouldRemove = false;
                        break;
                    }
                }

                if (shouldRemove)
                    toRemove.Add(y);
            }

            foreach (var index in toRemove)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cells[x, index] = CellType.Empty;
                }
            }


            toRemove.Reverse();
            foreach (var index in toRemove)
            {
                for (int y = index; y < Heigth-1; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Cells[x, y] = Cells[x, y+1];
                    }
                }
            }

            switch (toRemove.Count)
            {
                case 1:
                    Points += SINGLE_POINTS;
                    break;
                case 2:
                    Points += DOUBLE_POINTS;
                    break;
                case 3:
                    Points += TRIPLE_POINTS;
                    break;
                case 4:
                    Points += TETRIS_POINTS;
                    break;
                default:
                    break;
            }
        }

        public void Dispose()
        {
            timer.Dispose();
            GC.SuppressFinalize(this);
        }

        public CellType[,] Cells { get; private set; }
        public CellType[,] CellsWithActive
        {
            get
            {
                var _return = new CellType[Width, Heigth];

                for (int y = 0; y < Heigth; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        _return[x, y] = Cells[x, y];
                    }
                }

                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        if (y + ActiveY >= 0 && y + ActiveY < Heigth && x + ActiveX >= 0 && x + ActiveX < Width)
                        {
                            if (ActivePiece[4 * y + x])
                                _return[x + ActiveX, y + ActiveY] = (CellType)Enum.ToObject(typeof(CellType), ActivePieceType + 1);
                        }
                    }
                }

                return _return;
            }
        }

        public int Width { get; init; }
        public int Heigth { get; init; }
        public int ActiveX { get; private set; }
        public int ActiveY { get; private set; }

        public int Points { get; private set; }

        public int LinesCleared { get; private set; }
        public int TickRate { get; private set; }

        public int ActivePieceType { get; private set; }
        public int ActiveRotation { get; private set; }

        public bool[] ActivePiece => pieces[ActivePieceType][ActiveRotation];

        public GameState GameState { get; private set; }

        private static readonly bool[][][] pieces = new bool[][][]
       {
            //I
            new bool[][]
            {
                new bool[]
                {
                    false,false,false,false,
                    false,false,false,false,
                    true,true,true,true,
                    false,false,false,false
                },
                new bool[]
                {
                    false,false,true,false,
                    false,false,true,false,
                    false,false,true,false,
                    false,false,true,false
                }
            },

            //O
            new bool[][]
            {
                new bool[]
                {
                    false,false,false,false,
                    false,true,true,false,
                    false,true,true,false,
                    false,false,false,false
                },
            },

            //J
            new bool[][]
            {
                new bool[]
                {
                    false,false,false,false,
                    false,false,false,false,
                    true,true,true,false,
                    false,false,true,false
                },
                new bool[]
                {
                    false,false,false,false,
                    false,true,false,false,
                    false,true,false,false,
                    true,true,false,false
                },

                new bool[]
                {
                    false,false,false,false,
                    true,false,false,false,
                    true,true,true,false,
                    false,false,false,false
                },

                new bool[]
                {
                    false,false,false,false,
                    false,true,true,false,
                    false,true,false,false,
                    false,true,false,false
                }

            },

            //L
            new bool[][]
            {
                new bool[]
                {
                    false,false,false,false,
                    false,false,true,false,
                    true,true,true,false,
                    false,false,false,false
                },
                new bool[]
                {
                    false,false,false,false,
                    false,true,false,false,
                    false,true,false,false,
                    false,true,true,false
                },

                new bool[]
                {
                    false,false,false,false,
                    false,false,false,false,
                    true,true,true,false,
                    true,false,false,false
                },

                new bool[]
                {
                    false,false,false,false,
                    true,true,false,false,
                    false,true,false,false,
                    false,true,false,false
                }

            },
            
            //S
            new bool[][]
            {
                new bool[]
                {
                    false,false,false,false,
                    false,false,false,false,
                    false,true,true,false,
                    true,true,false,false
                },
                new bool[]
                {
                    false,false,false,false,
                    false,true,false,false,
                    false,true,true,false,
                    false,false,true,false
                }
            },

            //Z
            new bool[][]
            {
                new bool[]
                {
                    false,false,false,false,
                    false,false,false,false,
                    true,true,false,false,
                    false,true,true,false
                },
                new bool[]
                {
                    false,false,false,false,
                    false,false,true,false,
                    false,true,true,false,
                    false,true,false,false
                }
            },

            //T
            new bool[][]
            {
                new bool[]
                {
                    false,false,false,false,
                    false,true,false,false,
                    true,true,true,false,
                    false,false,false,false
                },
                new bool[]
                {
                    false,false,false,false,
                    false,true,false,false,
                    false,true,true,false,
                    false,true,false,false
                },
                new bool[]
                {
                    false,false,false,false,
                    false,false,false,false,
                    true,true,true,false,
                    false,true,false,false
                },
                new bool[]
                {
                    false,false,false,false,
                    false,true,false,false,
                    true,true,false,false,
                    false,true,false,false
                }
            },
       };
    }

    public enum CellType
    {
        Empty,
        I,
        O,
        J,
        L,
        S,
        Z,
        T
    }

    public enum Input
    {
        None,
        Rotate,
        MoveLeft,
        MoveRight,
        Place,
        MoveDown
    }

    public enum GameState
    {
        None,
        Lost,
        Won,
        Active
    }
}
