using UnityEngine;

namespace company.BettingOnColors.Chips
{
    public class Chip : MonoBehaviour
    {
        public int chipIndex { get; set; }
        public int stackIndex { get; set; }

        public System.Action onClick;
        public System.Action onHoverOff;
        public System.Action<int, int> onHover;

        private Renderer _renderer;

        private void OnMouseEnter()
        {
            onHover?.Invoke(chipIndex, stackIndex);
        }

        private void OnMouseExit()
        {
            onHoverOff?.Invoke();
        }

        private void OnMouseOver()
        {
            if (Input.GetMouseButtonDown(0))
            {
                onClick?.Invoke();
            }
        }

        public void Highlight()
        {
            transform.localScale = new Vector3(1f, 0.1f, 1f);
        }

        public void HighlightOff()
        {
            transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
        }

        public void SetColor(Color color)
        {
            if (!_renderer)
            {
                _renderer = GetComponent<Renderer>();
            }
            _renderer.material.color = color;
        }
    }
}