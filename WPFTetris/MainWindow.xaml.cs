using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Tetris;

namespace WPFTetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Tetris.Tetris tetris;

        public MainWindow()
        {
            InitializeComponent();
            timer = new System.Timers.Timer(5);
            timer.Elapsed += Update;
            timer.AutoReset = true;
            tetris = new();
            tetris.OnGridUpdated += () => Dispatcher.Invoke(() => DrawGame());
            cells = new Rectangle[tetris.Width, tetris.Heigth];
            timer.Start();
            InitGrid();
        }

        int tick = 0;
        int moveCount = 0;
        bool leftArrowDown = false;
        bool rightArrowDown = false;
        const int MOVE_CD_FIRST = 20;
        const int MOVE_CD = 5;
        int lastMove = -50;
        const int ROTA_CD = 25;
        int lastRotate = -50;
        int lastPlace = 0;
        const int PLACE_CD = 15;


        System.Timers.Timer timer;
        private void Update(object? source, ElapsedEventArgs e)
        {
            TickLoop();

            if (placing == true)
                if (tick > lastPlace + PLACE_CD)
                {
                    tetris.SendInput(Input.Place);
                    lastPlace = tick;
                }

            if(moveCount == 0)
                if (Moving == 1)
                {
                    tetris.SendInput(Input.MoveRight);
                    lastMove = tick;
                    moveCount++;
                }
                else if (Moving == -1)
                {
                    tetris.SendInput(Input.MoveLeft);
                    lastMove = tick;
                    moveCount++;
                }

            if (tick > lastMove + (moveCount > 1 ? MOVE_CD : MOVE_CD_FIRST) && moveCount > 0)
                if (Moving == 1)
                {
                    tetris.SendInput(Input.MoveRight);
                    lastMove = tick;
                    moveCount++;
                }
                else if (Moving == -1)
                {
                    tetris.SendInput(Input.MoveLeft);
                    lastMove = tick;
                    moveCount++;
                }

            if (tick > lastRotate + ROTA_CD)
                if (rotate)
                {
                    tetris.SendInput(Input.Rotate);
                    lastRotate = tick;
                }

            tick++;
        }

           
        private void TickLoop()
        {
            tetris.Update();
            if (tetris.GameState == GameState.Lost)
            {
                tetris.Reset();
            }
        }

        private void InitGrid()
        {
            TetrisGrid.Height = tetris.Heigth;
            TetrisGrid.Width = tetris.Width;

            for (int x = 0; x < tetris.Width; x++)
            {
                TetrisGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int y = 0; y < tetris.Heigth; y++)
            {
                TetrisGrid.RowDefinitions.Add(new RowDefinition());
            }

            for (int x = 0; x < tetris.Width; x++)
                for (int y = 0; y < tetris.Heigth; y++)
                {
                    Rectangle rectangle = new Rectangle();
                    rectangle.Margin = new Thickness(0.05);
                    Grid.SetColumn(rectangle, x);
                    Grid.SetRow(rectangle, y);
                    cells[x, y] = rectangle;
                    TetrisGrid.Children.Add(rectangle);
                }
        }

        Rectangle[,] cells;
        private void DrawGame()
        {
            PointCounter.Text = tetris.Points.ToString();

            for (int y = 0; y < tetris.Heigth; y++)
            {
                for (int x = 0; x < tetris.Width; x++)
                {
                    Brush cellFill = Brushes.WhiteSmoke;
                    switch (tetris.CellsWithActive[x, tetris.Heigth - y - 1])
                    {
                        case CellType.Empty:
                            break;
                        case CellType.I:
                            cellFill = Brushes.Cyan;
                            break;
                        case CellType.O:
                            cellFill = Brushes.Yellow;
                            break;
                        case CellType.L:
                            cellFill = Brushes.Blue;
                            break;
                        case CellType.J:
                            cellFill = Brushes.Orange;
                            break;
                        case CellType.S:
                            cellFill = Brushes.Green;
                            break;
                        case CellType.Z:
                            cellFill = Brushes.Red;
                            break;
                        case CellType.T:
                            cellFill = Brushes.Purple;
                            break;
                        default:
                            break;
                    }

                    cells[x,y].Fill = cellFill;
                }
            }
        }

        private int Moving;
        private bool rotate;
        private bool placing;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    if (!rightArrowDown)
                    {
                        Moving++;
                        moveCount = 0;
                        rightArrowDown = true;
                    }
                    break;
                case Key.Left:
                    if (!leftArrowDown)
                    {
                        Moving--;
                        moveCount = 0;
                        leftArrowDown = true;
                    }
                    break;
                case Key.Up:
                    rotate = true;
                    break;
                case Key.Space:
                    placing = true;
                    break;
                default:
                    break;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    Moving--;
                    rightArrowDown = false;
                    break;
                case Key.Left:
                    Moving++;
                    leftArrowDown = false;
                    break;
                case Key.Up:
                    rotate = false;
                    lastRotate = tick - ROTA_CD;
                    break;
                case Key.Space:
                    lastPlace = tick - PLACE_CD;
                    placing = false;
                    break;
                default:
                    break;
            }

            base.OnKeyUp(e);
        }
    }
}
