using System.Collections.Generic;
using UnityEngine;

namespace ARBlackjack.Interaction
{

    public class PlayerChipDisplay : MonoBehaviour
    {
        [System.Serializable]
        public struct ChipEntry
        {
            public int denomination;
            public GameObject prefab;
        }

        private class Chip
        {
            public int Denomination;
            public GameObject Go;
        }

        [Header("Chip Prefabs (one per denomination)")]
        [SerializeField] private ChipEntry[] _chipEntries;

        [Header("Layout")]
        [SerializeField] private float _denominationSpacing = 0.08f;
        [SerializeField] private float _stackSpacing       = 0.04f;
        [SerializeField] private float _chipHeight         = 0.008f;
        [SerializeField] private float _chipBaseHeight     = 0.06f;
        [SerializeField] private int   _maxPerStack        = 10;
        [Tooltip("Offset from the betting zone toward the player's cards and the wood area.")]
        [SerializeField] private Vector3 _tableOffset;
        [SerializeField] private Vector3 _winningsOffset = new(0f, 0f, 0.05f);

        private readonly List<Chip> _bet = new();
        private readonly List<Chip> _winnings = new();
        private readonly Dictionary<int, int> _denomIndex = new();
        private bool _settled;
        private void Awake()
        {
            for (int i = 0; i < _chipEntries.Length; i++)
                _denomIndex[_chipEntries[i].denomination] = i;
        }


        public void SetAmount(int amount)
        {
            if (_settled)
            {
                if (amount <= 0) Clear();
                return;
            }

            var wanted = Decompose(amount);

            int keep = 0;
            while (keep < _bet.Count && keep < wanted.Count && _bet[keep].Denomination == wanted[keep])
                keep++;
            for (int i = _bet.Count - 1; i >= keep; i--)
            {
                DestroyChip(_bet[i]);
                _bet.RemoveAt(i);
            }
            for (int i = keep; i < wanted.Count; i++)
            {
                var chip = Spawn(wanted[i], SlotPosition(_bet, wanted[i], Vector3.zero));
                if (chip != null) _bet.Add(chip);
            }
        }

        public void AddWinnings(int amount)
        {
            _settled = true;
            foreach (int denomination in Decompose(amount))
            {
                var chip = Spawn(denomination, SlotPosition(_winnings, denomination, _winningsOffset));
                if (chip != null) _winnings.Add(chip);
            }
        }

        public void TakeBet()
        {
            _settled = true;
            foreach (var chip in _bet) DestroyChip(chip);
            _bet.Clear();
        }

        public void Clear()
        {
            foreach (var chip in _bet) DestroyChip(chip);
            foreach (var chip in _winnings) DestroyChip(chip);
            _bet.Clear();
            _winnings.Clear();
            _settled = false;
        }

        private static List<int> Decompose(int amount)
        {
            var chips = new List<int>();
            int remaining = Mathf.Max(0, amount);
            for (int i = ChipSelector.Denominations.Length - 1; i >= 0; i--)
            {
                int denomination = ChipSelector.Denominations[i];
                while (remaining >= denomination)
                {
                    chips.Add(denomination);
                    remaining -= denomination;
                }
            }
            return chips;
        }

        private Vector3 SlotPosition(List<Chip> pile, int denomination, Vector3 offset)
        {
            int n = 0;
            foreach (var c in pile) if (c.Denomination == denomination) n++;
            return ChipWorldPos(denomination, n / _maxPerStack, n % _maxPerStack)
                 + transform.right * offset.x + transform.up * offset.y + transform.forward * offset.z;
        }

        private Chip Spawn(int denomination, Vector3 worldPos)
        {
            var prefab = GetPrefab(denomination);
            if (prefab == null) return null;

            var go = Instantiate(prefab, transform, false);
            go.transform.position = worldPos;
            go.transform.rotation = transform.rotation;
            ChipPivot.Recenter(go);
            go.transform.position += transform.up * _chipBaseHeight;
            return new Chip { Denomination = denomination, Go = go };
        }

        private Vector3 ChipWorldPos(int denomination, int stackIndex, int chipIndex)
        {
            if (!_denomIndex.TryGetValue(denomination, out int di)) return transform.position;

            float x = di * _denominationSpacing + stackIndex * _stackSpacing;
            float y = chipIndex * _chipHeight;

            return transform.position + transform.right * (_tableOffset.x + x)
                 + transform.up * (_tableOffset.y + y)
                 + transform.forward * _tableOffset.z;
        }

        private GameObject GetPrefab(int denomination)
        {
            if (!_denomIndex.TryGetValue(denomination, out int i)) return null;
            return _chipEntries[i].prefab;
        }

        private static void DestroyChip(Chip chip)
        {
            if (chip.Go != null) Destroy(chip.Go);
        }
    }
}
