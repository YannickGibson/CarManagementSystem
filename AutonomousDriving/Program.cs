using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace AutonomousDriving
{
    enum Track { Nothing, Bridge, Tunnel}
    enum Weather { Normal, Rainy, Icy}
    class Program
    {
        static Random rnd = new Random();
        static void Main(string[] args)//each char is 100m!!
        {
            MeteorologicCentrum mCentrum = new MeteorologicCentrum();
            DriveControl control = new DriveControl(numOfCars: 25);
            mCentrum.OnWeatherChanged += control.WeatherHasChanged;
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
                    Tesla t = new Tesla(rnd.Next(1000, 5000));
                    t.OnTrackChanged += this.TrackChanged;
                    cars.Add(t);
                }
            }
            public IEnumerable<int>Run()
            {
                stopwatch.Start();
                while (true)
                {
                    foreach (var car in cars)
                    {
                        if (car.Path.Length - 1 <= car.PositionIndex)
                        {
                            car.Completed = true;
                        }
                        else
                        {
                            float timeWithInterval = 0;

                            float addingInterval = 0;

                            if (weather == Weather.Normal)
                                addingInterval = car.NormalInterval;
                            else if (weather == Weather.Rainy)
                                addingInterval = car.RainyInterval;
                            else if (weather == Weather.Icy)
                                addingInterval = car.IcyInterval;

                            addingInterval /= car.TrackSpeedMultiplier;
                            car.CurrentInterval = (int)addingInterval;

                            timeWithInterval = car.LastMovedAt + addingInterval;




                            if (stopwatch.ElapsedMilliseconds > timeWithInterval)
                            {
                                car.Move(stopwatch.ElapsedMilliseconds);
                            }
                        }
                    }

                    yield return 0;
                }
            }

            public float GetSpeedOf(int ind)
            {
                float interval = cars[ind].CurrentInterval / cars[ind].TrackSpeedMultiplier;
                double speedPerSecond = 1000 / interval;
                double speedPerHour = speedPerSecond / 3600;
                double speed = speedPerHour * 100_000;//speeding onesecond realtime 100k times
                return (float)speedPerSecond;
            }

            public void TrackChanged(Tesla car)
            {
                if (car.CurrentTrack == Track.Nothing)
                {
                    car.LightsOn = false;
                    car.TrackSpeedMultiplier = 1f;
                }
                else if (car.CurrentTrack == Track.Bridge)
                {
                    car.LightsOn = false;
                    car.TrackSpeedMultiplier = 0.8f;
                }
                else if (car.CurrentTrack == Track.Tunnel)
                {
                    car.LightsOn = true;
                    car.TrackSpeedMultiplier = 5f;
                }
            }
            public void UpdateRoad()
            {
                ConsoleColor currColor = new ConsoleColor();
                if (weather == Weather.Normal)
                    currColor = ConsoleColor.Black;
                else if (weather == Weather.Rainy)
                    currColor = ConsoleColor.DarkBlue;
                else// if (weather == Weather.Icy)
                    currColor = ConsoleColor.DarkGray;
                Console.BackgroundColor = currColor;
                string s = "";
                for (int i = 0; i < cars.Count; i++)
                {
                    s += cars[i].GetPath();
                }
                Console.Clear();
                string temp = "";//for performance reasons
                int carIndex = 0;
                int carIndexTemp = -1;
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if (carIndexTemp == -1 || carIndex != carIndexTemp && ( s[i - 1] == '\n' || s[i - 1] == '\r'))
                    {
                        carIndexTemp = carIndex;
                        temp += $"{GetSpeedOf(carIndex): 0.0}/s ";
                    }
                    if (c == 'X')
                    {
                        Console.Write(temp);
                        temp = "";
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.Write(c);
                        Console.BackgroundColor = currColor;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else if (i -1  > 0 && s[i - 1] == 'X')
                    {
                        if(cars[carIndex].LightsOn == true)
                        {
                            Console.BackgroundColor = ConsoleColor.Yellow;
                            Console.Write(c);
                            Console.BackgroundColor = currColor;
                        }
                        carIndex++;
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
            public Track[] Path { get; set; }
            public int PositionIndex { get; set; }
            public bool Completed { get; set; } = false;
            public long LastMovedAt { get; private set; }

            public int CurrentInterval { get; set; }
            public int NormalInterval { get; set; }
            public int RainyInterval { get; set; } = 4000;
            public int IcyInterval { get; set; } = 6000;

            public Track CurrentTrack { get; set; }

            public delegate void TrackChanged(Tesla t);

            public event TrackChanged OnTrackChanged;
            
            public bool LightsOn { get; set; }
            public float TrackSpeedMultiplier { get; set; } = 1;
            public Tesla(int interval, int positionIndex = 0)
            {
                CurrentTrack = Track.Nothing;
                this.PositionIndex = positionIndex;//
                this.NormalInterval = interval;


                Path = new Track[rnd.Next(50, 70)];
                for (int i = 0; i < Path.Length; i++)
                {
                    int nothingChance = rnd.Next(0, 10);
                    if(nothingChance == 0)
                    {
                        Path[i] = (Track)rnd.Next(1, 3);
                        if (i != 0)//makes the tunnel/bridge longer if can
                        {
                            while (rnd.Next(0, 5) != 0)//4 in 5 chance to continue 
                            {
                                if (i < Path.Length - 1)
                                {
                                    i++;
                                    Path[i] = Path[i - 1];
                                }
                                else
                                {
                                    break;
                                }

                            }
                        }
                        

                    }
                    else
                    {
                        Path[i] = Track.Nothing;
                    }
                }
            }
            public void Move(long elapsedMilliseconds)
            {
                this.PositionIndex++;
                LastMovedAt = elapsedMilliseconds;
                if (OnTrackChanged != null)
                {
                    if (Path.Length > PositionIndex + 1 && Path[PositionIndex + 1] != CurrentTrack)
                    {
                        CurrentTrack = Path[PositionIndex + 1];
                        OnTrackChanged.Invoke(this);
                    }
                }
            }
            public string GetPath()
            {
                string s = "";
                for (int i = 0; i < Path.Length; i++)
                {
                    if (i == PositionIndex)
                        s += "X";
                    if (Path[i] == Track.Nothing)
                        s += "-";
                    else if (Path[i] == Track.Bridge)
                        s += "M";
                    else if (Path[i] == Track.Tunnel)
                        s += "_";
                }
                return s + "\n";
            }
        }
        class TrackChangedArgs : EventArgs
        {
            public Tesla tesla { get; set; }
            public TrackChangedArgs(Tesla tesla)
            {
                this.tesla = tesla;
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
