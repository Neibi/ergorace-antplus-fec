﻿using System;
using System.Threading.Tasks;

namespace ErgoRaceWin
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly object lockObject = new object();
        private DateTime clock;
        private int heartRate;
        private int cadence;
        private double gradient, previousGradient;
        private double speed;
        private bool ergMode = false;
        private int targetPower = 25;
        private int bikeTargetPower = 25;
        private int currentBikePower;
        private int frontGear = 1;
        private int rearGear = 1;
        private Direction keyPadDirection = Direction.None;
        private readonly bool stopRequested = false;
        private Task clockLoopTask;
        private Task keyPadLoopTask;
        private int[] chainRings = { 39, 53 }; // Tor
        // private int[] chainRings = { 24, 34 }; // Nina
        private int[] sprockets = { 40, 35, 31, 27, 24, 21, 19, 17, 15, 13, 11 };

        public MainViewModel()
        {
            clockLoopTask = Task.Run(() => ClockLoop());
            keyPadLoopTask = Task.Run(() => KeyPadLoop());
        }

        private async Task ClockLoop()
        {
            while (!stopRequested)
            {
                Clock = DateTime.Now;
                await Task.Delay(1000);
            }
        }

        private async Task KeyPadLoop()
        {
            var lastDirection = Direction.None;
            int streak = 0;
            const int repeatDelay = 20;

            while (!stopRequested)
            {
                if (!ergMode)
                {
                    if (keyPadDirection == lastDirection)
                        streak++;
                    else
                        streak = 0;

                    if (keyPadDirection == Direction.Up)
                    {
                        if (streak == 0 || streak > repeatDelay)
                            TargetPower += 5;
                    }
                    else if (keyPadDirection == Direction.Down)
                    {
                        if (streak == 0 || streak > repeatDelay)
                            TargetPower -= 5;
                    }
                    else if (keyPadDirection == Direction.Left)
                    {
                        if (streak == 0)
                            ShiftDown();
                    }
                    else if (keyPadDirection == Direction.Right)
                    {
                        if (streak == 0)
                            ShiftUp();
                    }

                    if (TargetPower < 0)
                        TargetPower = 0;

                    lastDirection = keyPadDirection;
                }

                await Task.Delay(20);
            }
        }

        void ShiftDown()
        {
            if (RearGear > 2)
                RearGear--;
            else if (RearGear > 1 && FrontGear == 1)
                RearGear--;
            else if (FrontGear > 1)
            {
                FrontGear--;

                if (RearGear < 10)
                    RearGear++;
            }
        }

        void ShiftUp()
        {
            if (RearGear < 10)
                RearGear++;
            else if (RearGear < 11 && FrontGear == 2)
                RearGear++;
            else if (FrontGear == 1)
            {
                FrontGear++;

                if (RearGear > 2)
                    RearGear--;
            }
        }

        public DateTime Clock
        {
            get => clock;
            set
            {
                lock (lockObject)
                {
                    clock = value;
                }

                RaisePropertyChangedEvent("Clock");
            }
        }

        public int HeartRate
        {
            get => heartRate;
            set
            {
                lock (lockObject)
                {
                    heartRate = value;
                }

                RaisePropertyChangedEvent("HeartRate");
            }
        }

        public int Cadence
        {
            get => cadence;
            set
            {
                lock (lockObject)
                {
                    cadence = value;
                }

                RaisePropertyChangedEvent("Cadence");
                Calculate(true);
            }
        }

        public double Gradient
        {
            get => gradient;
            set
            {
                lock (lockObject)
                {
                    previousGradient = gradient;
                    gradient = value;
                }

                RaisePropertyChangedEvent("Gradient");
                Calculate(true);
            }
        }

        public double Speed
        {
            get => speed;
            set
            {
                lock (lockObject)
                {
                    speed = value;
                }

                RaisePropertyChangedEvent("Speed");
            }
        }

        public int FrontGear
        {
            get => frontGear;
            set
            {
                lock (lockObject)
                {
                    frontGear = value;
                }

                RaisePropertyChangedEvent("FrontGear");
                Calculate(false);
            }
        }

        public int RearGear
        {
            get => rearGear;
            set
            {
                lock (lockObject)
                {
                    rearGear = value;
                }

                RaisePropertyChangedEvent("RearGear");
                Calculate(false);
            }
        }

        public int ChainRing => chainRings[FrontGear - 1];
        public int Sprocket => sprockets[RearGear - 1];

        private void Calculate(bool doAutoShift)
        {
            const int minCadence = 70;
            const int maxCadence = 95;

            var power = (int)Math.Round(BikeCalculator.CalculatePower(Cadence, Gradient, ChainRing, Sprocket));

            if (!ergMode)
                TargetPower = power;

            Speed = BikeCalculator.CalculateSpeed(Cadence, ChainRing, Sprocket) * 3600.0 / 1000.0;

            var minPower = BikeCalculator.CalculatePower(minCadence, Gradient, ChainRing, Sprocket);
            var maxPower = BikeCalculator.CalculatePower(maxCadence, Gradient, ChainRing, Sprocket);

            if (!doAutoShift)
                return;

            if (CurrentBikePower < minPower)
                ShiftDown();
            else if (CurrentBikePower > maxPower)
                ShiftUp();
        }

        public bool ErgMode
        {
            get => ergMode;
            set
            {
                lock (lockObject)
                {
                    ergMode = value;
                }

                RaisePropertyChangedEvent("ErgMode");
            }
        }

        public int TargetPower
        {
            get => targetPower;
            set
            {
                lock (lockObject)
                {
                    targetPower = value;
                }

                RaisePropertyChangedEvent("TargetPower");
            }
        }

        public int BikeTargetPower
        {
            get => bikeTargetPower;
            set
            {
                lock (lockObject)
                {
                    bikeTargetPower = value;
                }

                RaisePropertyChangedEvent("BikeTargetPower");
            }
        }

        public int CurrentBikePower
        {
            get => currentBikePower;
            set
            {
                lock (lockObject)
                {
                    currentBikePower = value;
                }

                RaisePropertyChangedEvent("CurrentBikePower");
            }
        }

        public Direction KeyPadDirection
        {
            get => keyPadDirection;
            set
            {
                lock (lockObject)
                {
                    keyPadDirection = value;
                }

                RaisePropertyChangedEvent("KeyPadDirection");
            }
        }
    }
}