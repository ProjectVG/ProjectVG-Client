#nullable enable
using System;

namespace ProjectVG.Domain.Chat.Model
{

    [Serializable]
    public class CostInfo
    {

        public float UsedCost { get; set; } = 0f;
        
        public float RemainingCost { get; set; } = 0f;

        public CostInfo() { }

        public CostInfo(float usedCost, float remainingCost)
        {
            UsedCost = usedCost;
            RemainingCost = remainingCost;
        }

        public bool HasCostInfo() => UsedCost > 0 || RemainingCost > 0;
        

        public override string ToString()
        {
            return $"Used: {UsedCost} Token, Remaining: {RemainingCost} Token";
        }
    }
}
