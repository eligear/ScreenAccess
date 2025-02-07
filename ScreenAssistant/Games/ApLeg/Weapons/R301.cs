﻿using TiqSoft.ScreenAssistant.Games.ApLeg.Weapons.Base;

namespace TiqSoft.ScreenAssistant.Games.ApLeg.Weapons
{
    internal sealed class R301 : UniqueLogicWeapon
    {
        public R301(string name, double burstSeconds, string recognizedName, int numOfMods) 
            : base(name, burstSeconds, recognizedName, numOfMods)
        {
        }

        public override double AdjustMouse(int shotNumber)
        {
            this.AdjustmentCoefficient = CalculateAdjustment(shotNumber, 15);
            var hAdj = shotNumber > 10 ? 3 : -1;
            var horizontalOffset = hAdj * (Rnd.NextDouble() * 1 + 1);
            if (this.AdjustmentCoefficient < 0.001)
            {
                horizontalOffset = 0;
            }
            var verticalOffset = Rnd.NextDouble() + 4d;
            this.MoveMouse(horizontalOffset, verticalOffset);

            return this.GetAdjustmentTime(1d);
        }
    }
}