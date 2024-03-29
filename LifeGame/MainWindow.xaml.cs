﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LifeGame
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Life Game;
        GraphicsController GC;
        Timer ScrollTimer;
        AutoResetEvent autoResetEvent;
        InfoWindow InfoWindow;

        public MainWindow()
        {
            InitializeComponent();
            autoResetEvent = new AutoResetEvent(false);
            Game = new Life(500, 500, true);
            DataObject.AddPastingHandler(CyclesInput, OnCancelCommand);
            DataObject.AddPastingHandler(DelayCyclesTextbox, OnCancelCommand);
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GameControllerCreateOrUpdate(Game);
        }

        private void GameControllerCreateOrUpdate(Life game)
        {
            int width = (int)GridMain.ColumnDefinitions[1].ActualWidth;
            int height = (int)GridMain.RowDefinitions[1].ActualHeight;
            if (GC == null)
            {
                GC = new GraphicsController(GameField, game, new Size(width, height), 1);
                ZoomValue.Text = GC.Scale.ToString();
            }
            else GC.Size = (width, height);
            UpdateUIGameFieldPosition();
        }

        private void OnCancelCommand(object sender, DataObjectEventArgs e)
        {
            e.CancelCommand();
        }


        //
        #region Game Field UI Events

        (int x, int y) _lastCell = (-1, -1);
        public (int x, int y) lastCell {
            get { return _lastCell; }
            set
            {
                if (value == (-1, -1))
                    _ = 1;
                _lastCell = value;
            }
        }
        DateTime lastRedraw;
        Point startDragPoint;
        (int x, int y) changes;

        private void PaintPixelIfCoordsChanged((int x, int y) coords, bool setAsAlive)
        {
            if (lastCell != coords)
            {                
                lastCell = coords;
                GC.PaintPixel(coords.x, coords.y, setAsAlive);
            }
        }

        private void GameField_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var s = sender as Image;
                var cell = GC.ToCell(e.GetPosition(GameField).X,
                    e.GetPosition(GameField).Y,
                    s.ActualWidth, s.ActualHeight);
                PaintPixelIfCoordsChanged(cell, true);
            }
            else if (e.ChangedButton == MouseButton.Right) {
                var s = sender as Image;
                var cell = GC.ToCell(e.GetPosition(GameField).X,
                    e.GetPosition(GameField).Y,
                    s.ActualWidth, s.ActualHeight);
                PaintPixelIfCoordsChanged(cell, false);
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                startDragPoint = e.GetPosition(GameField);

                var pos = e.GetPosition(GameField);
                pos = new Point(pos.X - DragMarkerIcon.ActualWidth / 2, pos.Y - DragMarkerIcon.ActualHeight / 2);
                DragMarkerIcon.Opacity = 1;
                Canvas.SetLeft(DragMarkerIcon, pos.X);
                Canvas.SetTop(DragMarkerIcon, pos.Y);
            }
        }

        private void GameField_MouseMove(object sender, MouseEventArgs e)
        {
            DEBUG.Text = $"{lastCell.x} {lastCell.y}";
            if (lastCell != (-1, -1))
            {
                if (e.MouseDevice.LeftButton == MouseButtonState.Pressed && e.MouseDevice.RightButton == MouseButtonState.Pressed)
                {
                    lastCell = (-1, -1);
                }
                else if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
                {
                    var s = sender as Image;
                    var cell = GC.ToCell(e.GetPosition(GameField).X,
                        e.GetPosition(GameField).Y,
                        s.ActualWidth, s.ActualHeight);
                    PaintPixelIfCoordsChanged(cell, true);
                }
                else if (e.MouseDevice.RightButton == MouseButtonState.Pressed)
                {
                    var s = sender as Image;
                    var cell = GC.ToCell(e.GetPosition(GameField).X,
                        e.GetPosition(GameField).Y,
                        s.ActualWidth, s.ActualHeight);
                    PaintPixelIfCoordsChanged(cell, false);
                }
            }            
            else if (e.MouseDevice.MiddleButton == MouseButtonState.Pressed)
            {
                if ((DateTime.Now - lastRedraw).Milliseconds > 2)
                {
                    lastRedraw = DateTime.Now;

                    var point = e.MouseDevice.GetPosition(GameField);
                    var change = point - startDragPoint;
                    (int x, int y) changes = ((int)change.X / 4, (int)change.Y / 4);
                    if (changes.x != this.changes.x || changes.y != this.changes.y)
                    {
                        this.changes = changes;
                        if (ScrollTimer == null) ScrollTimer = new Timer(TimerTick, autoResetEvent, 0, 10);
                    }
                    //GC.Position = pos;
                }
            }
        }
        void TimerTick(object state)
        {
            Dispatcher.Invoke(() =>
            {
                GC.Position = (GC.Position.x + changes.x, GC.Position.y + changes.y);
                UpdateUIGameFieldPosition();
            });
        }
        private void GameField_MouseLeave(object sender, MouseEventArgs e)
        {
            lastCell = (-1, -1);
            DragMarkerIcon.Opacity = 0;
            ScrollTimer?.Dispose();
            ScrollTimer = null;   
        }
        private void GameField_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
            {
                lastCell = (-1, -1);
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                DragMarkerIcon.Opacity = 0;
                ScrollTimer?.Dispose();
                ScrollTimer = null;
            }
        }
        private void GameField_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DragMarkerCanvas.Height = GameField.ActualHeight;
            DragMarkerCanvas.Width = GameField.ActualWidth;
        }

        #endregion


        //
        #region Screen position
        private void Left_Click(object sender, RoutedEventArgs e)
        {
            var pos = GC.Position;
            GC.Position = (pos.x - 1, pos.y);
            UpdateUIGameFieldPosition();
        }
        private void Right_Click(object sender, RoutedEventArgs e)
        {
            var pos = GC.Position;
            GC.Position = (pos.x + 1, pos.y);
            UpdateUIGameFieldPosition();
        }
        private void Up_Click(object sender, RoutedEventArgs e)
        {
            var pos = GC.Position;
            GC.Position = (pos.x, pos.y - 1);
            UpdateUIGameFieldPosition();
        }
        private void Down_Click(object sender, RoutedEventArgs e)
        {
            var pos = GC.Position;
            GC.Position = (pos.x, pos.y + 1);
            UpdateUIGameFieldPosition();
        }
        private void UpdateUIGameFieldPosition()
        {
            var (x, y) = GC.Position;
            CellXPosition.Text = x.ToString();
            CellYPosition.Text = y.ToString();
        }

        #endregion


        //
        #region ScaleUI
        private void ZoomValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ZoomValueUpdateScale();
        }
        private void ZoomValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ZoomValueUpdateScale();
            }
        }
        private void GameField_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            UpdateScale(GC.Scale + e.Delta / 120);
        }
        private void ZoomValueUpdateScale()
        {
            try
            {
                var value = int.Parse(ZoomValue.Text);
                UpdateScale(value);
                if (value != GC.Scale) GC.Scale = value;
            }
            catch (Exception)
            {
            }
        }
        private void IncZoom_Click(object sender, RoutedEventArgs e)
        {
            UpdateScale(GC.Scale + 1);
        }
        private void DecZoom_Click(object sender, RoutedEventArgs e)
        {
            UpdateScale(GC.Scale - 1);
        }

        private void UpdateScale (int value)
        {
            GC.Scale = value;
            ZoomValue.Text = GC.Scale.ToString();
        }
        #endregion


        //
        #region Game flow

        int DelayMs = 1;
        bool running = false;
        private void CyclesInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                int.Parse(e.Text);
            }
            catch (Exception)
            {
                e.Handled = true;
            }
            if (CyclesInput.Text.Length >= 7 && CyclesInput.SelectionLength < 1) e.Handled = true;
        }

        private void StartPauseButtonClick(object sender, RoutedEventArgs e)
        {
            running = !running;
            if (running)
            {
                int cycles = 0;
                try
                {
                    cycles = int.Parse(CyclesInput.Text);
                }
                catch (Exception) { }

                if (cycles > 0) RunGameLoop(cycles);
                else RunInifiniteGameLoop();
            }

        }
        async void RunInifiniteGameLoop()
        {
            UpdateDelay();
            FlipRunButtonIcon();
            await Task.Factory.StartNew(() =>
            {
                while (running)
                {
                    Dispatcher.Invoke(() => Game.Tick());
                    Thread.Sleep(DelayMs);
                }
            });
            FlipRunButtonIcon();
        }

        async void RunGameLoop(int amount)
        {
            UpdateDelay();
            FlipRunButtonIcon();
            var t = Task.Factory.StartNew(() =>
            {
                while (running && amount-- > 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Game.Tick();
                        CyclesInput.Text = amount.ToString();
                    });
                    Thread.Sleep(DelayMs);
                }
            });
            await t;
            if (running) running = false; // if loop stopped because of amount == 0
            FlipRunButtonIcon();
        }

        void FlipRunButtonIcon()
        {
            if (PauseIcon.Opacity != 1)
            {
                PauseIcon.Opacity = 1;
                StartIcon.Opacity = 0;
            }
            else
            {
                PauseIcon.Opacity = 0;
                StartIcon.Opacity = 1;
            }
        }


        #endregion


        //
        #region Game speed

        private void DelayInc_Click(object sender, RoutedEventArgs e)
        {
            ++DelayMs;
            EnsureDelayBounds();
            DelayCyclesTextbox.Text = DelayMs.ToString();
        }

        private void DelayDec_Click(object sender, RoutedEventArgs e)
        {
            --DelayMs;
            EnsureDelayBounds();
            DelayCyclesTextbox.Text = DelayMs.ToString();
            
        }

        private void DelayCyclesTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                int.Parse(e.Text);
            }
            catch (Exception)
            {
                e.Handled = true;
            }
            if (DelayCyclesTextbox.Text.Length >= 4 && DelayCyclesTextbox.SelectionLength < 1) e.Handled = true;
        }
        private void DelayCyclesTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDelay();
        }


        void UpdateDelay()
        {
            try
            {
                DelayMs = int.Parse(DelayCyclesTextbox.Text);
                EnsureDelayBounds();
            }
            catch (Exception)
            {
                if (CyclesInput != null) DelayCyclesTextbox.Text = DelayMs.ToString();
            }
        }



        void EnsureDelayBounds()
        {
            int max = 1000;
            int min = 1;
            if (DelayMs > max) DelayMs = max;
            if (DelayMs < min) DelayMs = min;
        }



        #endregion



        public void SetupNewGame(int width, int height, bool wrap)
        {
            Game = new Life(width, height, wrap);
            GC = null;
            GameControllerCreateOrUpdate(Game);
        }


        #region Menu
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void MenuItem_Reset_Click(object sender, RoutedEventArgs e)
        {
            Game = new Life(Game.Width, Game.Height, Game.Wrapping);
            GC = null;
            GameControllerCreateOrUpdate(Game);
        }

        private void MenuItem_New_Click(object sender, RoutedEventArgs e)
        {
            new CreateNewGame().Show();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (InfoWindow == null) InfoWindow = new InfoWindow();
            else if (!InfoWindow.IsLoaded) InfoWindow = new InfoWindow();

            if (InfoWindow.IsLoaded) InfoWindow.Activate();
            else InfoWindow.Show();
        }
        #endregion
    }
}
