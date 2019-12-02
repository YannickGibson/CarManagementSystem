using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace AutonomousDriving
{
    enum Obstacle { Nothing, Bridge, Tunnel}
    enum Weather { Normal, Rainy, Icy}
    class Program
    {
        static Random rnd = new Random();
        static void Main(string[] args)
        {
            MeteorologicCentrum mCentrum = new MeteorologicCentrum();
            DriveControl control = new DriveControl(numOfCars: 25);
            mCentrum.OnWeatherChanged += control.WeatherHasChanged;
            control.UpdateRoad();
            while (true)
            {
                foreach (var v in control.Run())
                {
                    control.UpdateRoad();
                    if (rnd.Next(0, 5) == 0)
                        mCentrum.TriggerWeatherChange();
                    Thread.Sleep(50);
                }
            }
        }
        class DriveControl
        {
            Stopwatch stopwatch = new Stopwatch();
            List<Tesla> cars = new List<Tesla>();
            Weather weather;

            public DriveControl(int numOfCars = 100)
            {
                for (int i = 0; i < numOfCars; i++)
                {
                    cars.Add(new Tesla(rnd.Next(1000,5000)));
                }
            }
            public IEnumerable<int>Run()
            {
                stopwatch.Start();
                while (true)
                {
                    foreach (var car in cars)
                    {
                        car.StayOrMove(stopwatch.ElapsedMilliseconds, weather);
                    }
                    
                    yield return 0;
                }
            }
            public void UpdateRoad()
            {
                if (weather == Weather.Normal)
                    Console.BackgroundColor = ConsoleColor.Black;
                else if (weather == Weather.Rainy)
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                else if (weather == Weather.Icy)
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                
                string s = "";
                for (int i = 0; i < cars.Count; i++)
                {
                    s += cars[i].GetPath();
                }
                Console.Clear();
                string temp = "";//for performance reasons
                foreach (var c in s)
                {
                    if (c == 'X')
                    {
                        Console.Write(temp);
                        temp = "";
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(c);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    else
                        temp += c;
                }
                Console.WriteLine(temp);
            }
            public void WeatherHasChanged(MeteorologicCentrum m, WeatherArgs warg)
            {
                weather = warg.weather;
            }

        }
        class Tesla
        {
            Obstacle[] path;
            public int PositionIndex { get; set; }
            public bool Completed { get; private set; } = false;
            public long TimeElapsed { get; private set; }

            public int Interval { get; set; }
            private int RainyInterval = 4000;
            private int IcyInterval = 6000;

            public Tesla(int interval, int positionIndex = 0)
            {
                this.PositionIndex = positionIndex;
                this.Interval = interval;
                path = new Obstacle[rnd.Next(50, 70)];
                for (int i = 0; i < path.Length; i++)
                {
                    int nothingChance = rnd.Next(0, 10);
                    if(nothingChance == 0)
                    {
                        path[i] = (Obstacle)rnd.Next(1, 3);
                    }
                    else
                    {
                        path[i] = Obstacle.Nothing;
                    }
                }
            }

            public void StayOrMove(long elapsedMilliseconds,Weather weather)
            {
                
                if(path.Length -1 <= PositionIndex)
                {
                    Completed = true;
                    return;
                }

                long timeToReach = 0; 
                if (weather == Weather.Normal)
                    timeToReach = TimeElapsed + Interval;
                else if (weather == Weather.Rainy)
                    timeToReach = TimeElapsed + RainyInterval;
                else if (weather == Weather.Icy)
                    timeToReach = TimeElapsed + IcyInterval;

                if (path[PositionIndex] == Obstacle.Bridge)
                    timeToReach -= (timeToReach - TimeElapsed)/3;// speed -30%
                else if (path[PositionIndex] == Obstacle.Tunnel)
                    timeToReach += (timeToReach - TimeElapsed) / 2;// speed +50%

                if (elapsedMilliseconds > timeToReach)
                {
                    this.PositionIndex++;
                    TimeElapsed = elapsedMilliseconds;
                }


            }
            public string GetPath()
            {
                string s = "";
                for (int i = 0; i < path.Length; i++)
                {
                    if (i == PositionIndex)
                        s += "X";
                    if (path[i] == Obstacle.Nothing)
                        s += "-";
                    else if (path[i] == Obstacle.Bridge)
                        s += "M";
                    else if (path[i] == Obstacle.Tunnel)
                        s += "U";
                }
                return s + "\n";
            }
        }
        class MeteorologicCentrum
        {
            public event OnWeatherChangedHandler OnWeatherChanged;

            public delegate void OnWeatherChangedHandler(MeteorologicCentrum m, WeatherArgs e);

            public void TriggerWeatherChange()
            {
                if(OnWeatherChanged != null)
                {
                    Weather newWeather = (Weather)(rnd.Next
                            (
                                0,
                                Enum.GetNames(typeof(Weather)).Length
                            ));
                    OnWeatherChanged(
                        this, 
                        new WeatherArgs(newWeather)
                    );
                }

            }

        }
        class WeatherArgs : EventArgs
        {
            public Weather weather { get; set; }
            public WeatherArgs(Weather weather)
            {
                this.weather = weather;
            }
        }
    }
}
