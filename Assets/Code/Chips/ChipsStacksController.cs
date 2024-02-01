using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using company.BettingOnColors.Utility;

namespace company.BettingOnColors.Chips
{
    public class ChipsStacksController : MonoBehaviour
    {
        [SerializeField] private GameObject chipPrefab;
        [SerializeField] private float _chipHeight = .22f;
        [SerializeField] private Text stackSelectionText;
        [SerializeField] private Transform stacksOrigin;
        public bool allowInteraction = true;

        public int[] chips { get; private set; }
        public ChipsStack[] stacks { get; private set; }
        public int[] selectionMask { get; private set; }
        public bool receiving { get; internal set; } = false;

        public System.Action<int[]> onSelectChips;

        private int _numStacks;
        private int[] _updatedChips;
        private int[] _selectedChips;
        private float _chipsFlySpeed;
        private bool _sending = false;
        private int _chipsPoolInitialNum;
        private Vector3 _offsetVector;
        private readonly static Vector3 _offsetStart = Vector3.zero;

        private Queue<IEnumerator> _sentChipsQueue = new Queue<IEnumerator>();

        void Start()
        {
            StartCoroutine(HandleQueue());
        }

        public void SetSelectionMask(int[] mask)
        {
            for (int i = 0; i < selectionMask.Length; i++)
            {
                selectionMask[i] = mask[i];
            }
        }

        public void RoundReset()
        {
            _sending = false;

            _updatedChips = IntArrayUtility.Zeros(_numStacks);
            _selectedChips = IntArrayUtility.Zeros(_numStacks);
            selectionMask = IntArrayUtility.Zeros(_numStacks);
        }

        private Vector3[] StacksPositions(Transform origin, int numStacks, float scale = 1f)
        {
            Vector3[] result = new Vector3[numStacks];
            var startPos = origin.position - origin.right * (numStacks * scale * 0.5f - scale * 0.5f);
            for (int i = 0; i < numStacks; i++)
            {
                result[i] = startPos;
                startPos += origin.right * scale;
            }
            return result;
        }

        public void InitStacks(GameplaySettings settings, bool useEvents, int? initialNumChips = null)
        {
            if (!chipPrefab.GetComponent<Chip>())
            {
                Debug.LogError("Chip prefab must have the 'Chip' component attached to it");
                return;
            }

            if (initialNumChips == null)
            {
                initialNumChips = settings.initialChipsPerStack;
            }
            _numStacks = settings.numberOfStacks;
            _chipsPoolInitialNum = settings.initialChipsPerStack * 2;
            _chipsFlySpeed = settings.chipsFlySpeed;
            _offsetVector = new Vector3(0, _chipHeight, 0);

            _updatedChips = IntArrayUtility.Zeros(_numStacks);
            _selectedChips = IntArrayUtility.Zeros(_numStacks);
            selectionMask = IntArrayUtility.Zeros(_numStacks);

            stacks = new ChipsStack[_numStacks];
            chips = new int[_numStacks];
            var positions = StacksPositions(stacksOrigin, _numStacks);
            for (int i = 0; i < _numStacks; i++)
            {
                stacks[i] = new ChipsStack(positions[i], settings.stacksColors[i]);
                chips[i] = initialNumChips.Value;
                InitStack(stacks[i], i, initialNumChips.Value, useEvents);
            }
        }

        private void InitStack(ChipsStack stack, int stackIndex, int numChips, bool useEvents)
        {
            var offset = _offsetStart;
            for (int i = 0; i < _chipsPoolInitialNum; i++)
            {
                var go = Instantiate(chipPrefab, stack.position + offset, Quaternion.identity);
                go.transform.parent = stacksOrigin;
                go.SetActive(i < numChips);
                var chip = go.GetComponent<Chip>();
                chip.chipIndex = i;
                chip.stackIndex = stackIndex;
                if (useEvents)
                {
                    chip.onHover += UpdateChipsHighlight;
                    chip.onHoverOff += ClearUpdatedChips;
                    chip.onClick += AddSelection;
                }
                chip.SetColor(stack.stackColor);

                stack.chipsGameObjects.Add(chip);

                offset += _offsetVector;
            }
        }

        private void ClearUpdatedChips()
        {
            if (!allowInteraction)
            {
                return;
            }
            for (int stackIndex = 0; stackIndex < _updatedChips.Length; stackIndex++)
            {
                var numUpdatedChips = _updatedChips[stackIndex];
                var chipsGameObjects = stacks[stackIndex].chipsGameObjects;
                for (int i = numUpdatedChips; i < chipsGameObjects.Count; i++)
                {
                    chipsGameObjects[i].HighlightOff();
                }
            }
            _updatedChips = IntArrayUtility.Zeros(_numStacks);
            stackSelectionText.text = null;
        }

        private void UpdateChipsHighlight(int chipIndex, int stackIndex)
        {
            if (!allowInteraction)
            {
                return;
            }
            ClearUpdatedChips();

            var chipsGameObjects = stacks[stackIndex].chipsGameObjects;
            for (int i = chipIndex; i < chipsGameObjects.Count; i++)
            {
                if (i >= selectionMask[stackIndex])
                {
                    chipsGameObjects[i].Highlight();
                }
            }
            _updatedChips[stackIndex] = chips[stackIndex] - chipIndex;

            UpdateStackSelectionText(stackIndex, (chips[stackIndex] - chipIndex).ToString());
        }

        private void UpdateStackSelectionText(int stackIndex, string newText)
        {
            stackSelectionText.text = newText;
            var pos = stacks[stackIndex].position;
            stackSelectionText.transform.position = new Vector3(pos.x, pos.y + chips[stackIndex] * (_chipHeight * 1.15f), pos.z);
        }

        private void AddSelection()
        {
            if (!allowInteraction)
            {
                return;
            }
            if (_sending || receiving)
            {
                return;
            }
            _selectedChips = IntArrayUtility.Zeros(_numStacks);
            for (int stackIndex = 0; stackIndex < _updatedChips.Length; stackIndex++)
            {
                if (_updatedChips[stackIndex] >= selectionMask[stackIndex])
                {
                    _selectedChips[stackIndex] = _updatedChips[stackIndex];
                }
            }

            onSelectChips?.Invoke(_selectedChips);
        }

        public void SendChips(ChipsStacksController targetController, int[] chips, System.Action<int[], int[]> onStateChanged = null)
        {
            _sentChipsQueue.Enqueue(SendChipsEnum(chips, targetController, onStateChanged));
        }

        private IEnumerator HandleQueue()
        {
            while (true)
            {
                if (_sentChipsQueue.Count > 0)
                {
                    yield return StartCoroutine(_sentChipsQueue.Dequeue());
                }
                yield return null;
            }
        }

        public IEnumerator SendChipsEnum(int[] chips, ChipsStacksController other, System.Action<int[], int[]> onStateChanged = null)
        {
            if (_sending)
            {
                yield return null;
            }
            _sending = true;
            other.receiving = true;
            for (int i = 0; i < _numStacks; i++)
            {
                if (this.chips[i] == 0)
                {
                    continue;
                }
                int kStartNorm = this.chips[i] - 1;
                var k_end = Mathf.Max(-1, kStartNorm - chips[i]);
                for (int k = kStartNorm; k > k_end; k--)
                {
                    yield return StartCoroutine(FlyChipEnum(stacks[i].chipsGameObjects[k], other, i));
                }
            }
            _sending = false;
            other.receiving = false;

            onStateChanged?.Invoke(this.chips, other.chips);
        }

        private IEnumerator FlyChipEnum(Chip chipGo, ChipsStacksController otherController, int targetStackIndex)
        {
            var progress = 0f;
            var startPos = chipGo.transform.position;
            var targetPos = otherController.stacks[targetStackIndex].position
                + new Vector3(0, _chipHeight * (otherController.chips[targetStackIndex]), 0);
            
            while (progress < 1)
            {
                chipGo.transform.position = Vector3.Lerp(startPos, targetPos, progress);
                progress += _chipsFlySpeed * Time.deltaTime;
                yield return null;
            }

            otherController.AddChips(1, targetStackIndex);
            SubtractChips(1, targetStackIndex);
            chipGo.transform.position = startPos;
            chipGo.gameObject.SetActive(false);
        }

        private void AddChips(int numChips, int stackIndex)
        {
            if (numChips == 0)
            {
                return;
            }

            int stackChips = chips[stackIndex];
            for (int j = stackChips; j < Mathf.Min(_chipsPoolInitialNum, stackChips + numChips); j++)
            {
                stacks[stackIndex].chipsGameObjects[j].gameObject.SetActive(true);
                chips[stackIndex] += 1;
            }
        }

        private void SubtractChips(int numChips, int stackIndex)
        {
            if (numChips == 0)
            {
                return;
            }

            int stackChips = chips[stackIndex];
            for (int j = stackChips - 1; j >= Mathf.Max(0, stackChips - numChips); j--)
            {
                stacks[stackIndex].chipsGameObjects[j].gameObject.SetActive(false);
                chips[stackIndex] -= 1;
            }
        }
    }
}