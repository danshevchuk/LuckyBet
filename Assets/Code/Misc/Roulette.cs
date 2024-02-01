using System.Collections.Generic;
using UnityEngine;

namespace company.BettingOnColors.Misc
{
    public class Roulette : MonoBehaviour
    {
        public List<Renderer> renderers = new List<Renderer>();

        // Start is called before the first frame update
        void Start()
        {
            DisplayColor(BettingColor.None);
        }

        public void DisplayColor(BettingColor color){
            switch(color){
                case BettingColor.None:{
                    ApplyColor(Color.gray);
                    break;
                }
                case BettingColor.Red:{
                    ApplyColor(Color.red);
                    break;
                }
                case BettingColor.Green:{
                    ApplyColor(Color.green);
                    break;
                }
            }
        }

        private void ApplyColor(Color color){
            foreach(var r in renderers){
                r.material.color = color;
            }
        }
    }
}