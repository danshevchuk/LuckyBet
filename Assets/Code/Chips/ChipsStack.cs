using System.Collections.Generic;
using UnityEngine;

namespace company.BettingOnColors.Chips
{
    /// <summary>
    /// Contains the information needed to render and animate objects
    /// </summary>
    public class ChipsStack
    {
        public List<Chip> chipsGameObjects = new List<Chip>();
        public Color stackColor { get; private set; }
        public Vector3 position { get; set; }

        public ChipsStack(Vector3 position, Color color)
        {
            this.position = position;
            stackColor = color;
        }
    }
}