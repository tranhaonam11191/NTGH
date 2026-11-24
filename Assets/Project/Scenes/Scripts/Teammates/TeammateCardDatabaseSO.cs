using System.Collections.Generic;
using UnityEngine;

namespace NTGH.Teammates
{
    [CreateAssetMenu(fileName = "TeammateCardDatabase", menuName = "NTGH/Teammates/Card Database")]
    public class TeammateCardDatabaseSO : ScriptableObject
    {
        public const int MaxSquadSlots = 4;

        [Header("Reusable Card Themes")]
        [SerializeField] private TeammateCardThemeSO m_attackTheme;
        [SerializeField] private TeammateCardThemeSO m_supportTheme;

        [Header("All Teammate Cards")]
        [SerializeField] private List<TeammateCardSO> m_cards = new List<TeammateCardSO>();

        public TeammateCardThemeSO AttackTheme => m_attackTheme;
        public TeammateCardThemeSO SupportTheme => m_supportTheme;
        public IReadOnlyList<TeammateCardSO> Cards => m_cards;

        public TeammateCardThemeSO GetTheme(TeammateCardType cardType)
        {
            return cardType == TeammateCardType.Attack ? m_attackTheme : m_supportTheme;
        }

        public TeammateCardSO GetCardById(string id)
        {
            if (string.IsNullOrEmpty(id) || m_cards == null) return null;
            for (int i = 0; i < m_cards.Count; i++)
            {
                var card = m_cards[i];
                if (card != null && card.Id == id)
                {
                    return card;
                }
            }
            return null;
        }

        public List<TeammateCardSO> GetDefaultUnlockedCards()
        {
            var result = new List<TeammateCardSO>();
            if (m_cards == null) return result;
            for (int i = 0; i < m_cards.Count; i++)
            {
                if (m_cards[i] != null && m_cards[i].UnlockedByDefault)
                {
                    result.Add(m_cards[i]);
                }
            }
            return result;
        }

        public void Configure(
            TeammateCardThemeSO attackTheme,
            TeammateCardThemeSO supportTheme,
            List<TeammateCardSO> cards)
        {
            m_attackTheme = attackTheme;
            m_supportTheme = supportTheme;
            m_cards = cards ?? new List<TeammateCardSO>();
        }
    }
}
