using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.Demo.OutfitSystem
{
    public class OutfitDemoUI : MonoBehaviour
    {
        private static readonly string[] MixLabels = { "Lv1", "Lv2", "Lv3", "Lv4", "Lv5" };

        [SerializeField] private CharacterOutfitController m_characterController;
        [SerializeField] private TextMeshProUGUI m_txtOutfitInfo;
        [SerializeField] private TextMeshProUGUI m_txtStats;

        [Header("Full Set Buttons (5 Ranks)")]
        [SerializeField] private Button[] m_outfitButtons;

        [Header("Mix-and-Match Buttons")]
        [SerializeField] private Button m_btnCycleTorso;
        [SerializeField] private Button m_btnCycleArms;
        [SerializeField] private Button m_btnCycleLegs;

        private int m_torsoMixIndex;
        private int m_armsMixIndex;
        private int m_legsMixIndex;

        private void Start()
        {
            if (m_outfitButtons != null)
            {
                for (int i = 0; i < m_outfitButtons.Length; i++)
                {
                    int capturedIndex = i;
                    if (m_outfitButtons[i] != null)
                    {
                        m_outfitButtons[i].onClick.AddListener(() => SelectOutfit(capturedIndex));
                    }
                }
            }

            if (m_btnCycleTorso != null)
            {
                m_btnCycleTorso.onClick.AddListener(CycleTorsoPart);
            }

            if (m_btnCycleArms != null)
            {
                m_btnCycleArms.onClick.AddListener(CycleArmsPart);
            }

            if (m_btnCycleLegs != null)
            {
                m_btnCycleLegs.onClick.AddListener(CycleLegsPart);
            }

            if (m_characterController != null)
            {
                User.EnsureLoaded();
                string savedId = User.data.current_outfit_id;
                int index = 0;
                for (int i = 0; i < m_characterController.Outfits.Count; i++)
                {
                    if (m_characterController.Outfits[i].OutfitId == savedId)
                    {
                        index = i;
                        break;
                    }
                }
                m_characterController.EquipOutfitByIndex(index);
                m_torsoMixIndex = m_armsMixIndex = m_legsMixIndex = index;
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (m_outfitButtons != null)
            {
                foreach (Button btn in m_outfitButtons)
                {
                    if (btn != null) btn.onClick.RemoveAllListeners();
                }
            }

            if (m_btnCycleTorso != null) m_btnCycleTorso.onClick.RemoveAllListeners();
            if (m_btnCycleArms != null) m_btnCycleArms.onClick.RemoveAllListeners();
            if (m_btnCycleLegs != null) m_btnCycleLegs.onClick.RemoveAllListeners();
        }

        private void SelectOutfit(int index)
        {
            if (m_characterController == null)
            {
                return;
            }

            m_characterController.EquipOutfitByIndex(index);
            User.EnsureLoaded();
            User.data.current_outfit_id = m_characterController.CurrentOutfit.OutfitId;
            User.Save();
            m_torsoMixIndex = index % MixLabels.Length;
            m_armsMixIndex = index % MixLabels.Length;
            m_legsMixIndex = index % MixLabels.Length;
            RefreshUI();
        }

        private void CycleTorsoPart()
        {
            if (m_characterController == null)
            {
                return;
            }

            m_torsoMixIndex = (m_torsoMixIndex + 1) % MixLabels.Length;
            string label = MixLabels[m_torsoMixIndex];
            m_characterController.EquipPart("Torso", label);

            if (m_txtOutfitInfo != null)
            {
                m_txtOutfitInfo.text = $"Mix & Match: Áo -> {label}";
            }
        }

        private void CycleArmsPart()
        {
            if (m_characterController == null)
            {
                return;
            }

            m_armsMixIndex = (m_armsMixIndex + 1) % MixLabels.Length;
            string label = MixLabels[m_armsMixIndex];
            m_characterController.EquipPart("LeftArm", label);
            m_characterController.EquipPart("RightArm", label);

            if (m_txtOutfitInfo != null)
            {
                m_txtOutfitInfo.text = $"Mix & Match: Tay -> {label}";
            }
        }

        private void CycleLegsPart()
        {
            if (m_characterController == null)
            {
                return;
            }

            m_legsMixIndex = (m_legsMixIndex + 1) % MixLabels.Length;
            string label = MixLabels[m_legsMixIndex];
            m_characterController.EquipPart("LeftLeg", label);
            m_characterController.EquipPart("RightLeg", label);

            if (m_txtOutfitInfo != null)
            {
                m_txtOutfitInfo.text = $"Mix & Match: Quần/Giày -> {label}";
            }
        }

        private void RefreshUI()
        {
            if (m_characterController == null || m_characterController.CurrentOutfit == null)
            {
                return;
            }

            OutfitData outfit = m_characterController.CurrentOutfit;
            if (m_txtOutfitInfo != null)
            {
                m_txtOutfitInfo.text = $"{outfit.DisplayName} ({outfit.RankTitle})";
            }

            if (m_txtStats != null)
            {
                m_txtStats.text = $"HP +{outfit.BonusHealth}   |   DEF +{outfit.BonusDefence}   |   Price: {outfit.PriceGold}G";
            }
        }

        public void SetupReferences(
            CharacterOutfitController controller,
            TextMeshProUGUI txtOutfitInfo,
            TextMeshProUGUI txtStats,
            Button[] outfitButtons,
            Button btnCycleTorso,
            Button btnCycleArms,
            Button btnCycleLegs)
        {
            m_characterController = controller;
            m_txtOutfitInfo = txtOutfitInfo;
            m_txtStats = txtStats;
            m_outfitButtons = outfitButtons;
            m_btnCycleTorso = btnCycleTorso;
            m_btnCycleArms = btnCycleArms;
            m_btnCycleLegs = btnCycleLegs;
        }
    }
}
