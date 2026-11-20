using NTGH.Demo.OutfitSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D.Animation;

// The Home canvas also contains the backdrop. Draw each rigid sprite on its
// animated bone so the character shares canvas ordering with that backdrop.
public class HomeOutfitView : MonoBehaviour
{
    [SerializeField] private CharacterOutfitController m_prefab;
    private CharacterOutfitController m_character;
    private SpriteResolver[] m_resolvers;
    private Image[] m_parts;
    private Transform[] m_bones;
    public OutfitProgression Progression { get; private set; }

    private void Awake()
    {
        m_character = Instantiate(m_prefab, transform, false);
        m_resolvers = m_character.GetComponentsInChildren<SpriteResolver>();
        System.Array.Sort(m_resolvers, (a, b) => Renderer(a).sortingOrder.CompareTo(Renderer(b).sortingOrder));
        m_parts = new Image[m_resolvers.Length];
        m_bones = new Transform[m_parts.Length];

        // Use one canvas sibling per part for the same draw order as the rig.
        for (int i = 0; i < m_resolvers.Length; i++)
        {
            var resolver = m_resolvers[i];
            Renderer(resolver).forceRenderingOff = true;
            string boneName = resolver.GetCategory() == "Torso" ? "Spine_Bone" : resolver.GetCategory() + "_Bone";
            m_bones[i] = System.Array.Find(resolver.GetComponent<SpriteSkin>().boneTransforms,
                bone => bone.name == boneName);
            var part = new GameObject(m_resolvers[i].name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            part.transform.SetParent(transform, false);
            m_parts[i] = part.GetComponent<Image>();
            m_parts[i].raycastTarget = false;
        }

        Progression = new OutfitProgression(m_character.Outfits);
        Progression.RankChanged += ApplyRank;
        ApplyRank(Progression.CurrentRank);
        OnRectTransformDimensionsChange();
    }

    private static SpriteRenderer Renderer(SpriteResolver resolver) => resolver.GetComponent<SpriteRenderer>();

    private void ApplyRank(int rank)
    {
        m_character.EquipOutfitByIndex(rank - 1);

        for (int i = 0; i < m_parts.Length; i++)
        {
            var resolver = m_resolvers[i];
            resolver.ResolveSpriteToSpriteRenderer();
            Sprite sprite = Renderer(resolver).sprite;
            Image part = m_parts[i];
            part.sprite = sprite;
            part.rectTransform.pivot = sprite.pivot / sprite.rect.size;
            part.rectTransform.sizeDelta = sprite.rect.size / sprite.pixelsPerUnit;
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!m_character) return;
        Rect rect = ((RectTransform)transform).rect;
        float scale = Mathf.Min(rect.width / 2.6f, rect.height / 5.1f);
        m_character.transform.localScale = Vector3.one * scale;
        m_character.transform.localPosition = new Vector3(-0.18f, 0.25f) * scale;
    }

    private void LateUpdate()
    {
        // Bone positions are in world space. Scale in the portrait's local space
        // so CanvasScaler applies exactly once.
        for (int i = 0; i < m_parts.Length; i++)
        {
            Transform bone = m_bones[i];
            var part = m_parts[i].rectTransform;
            part.SetPositionAndRotation(bone.position, bone.rotation);
            Matrix4x4 relative = transform.worldToLocalMatrix * bone.localToWorldMatrix;
            part.localScale = new Vector3(relative.GetColumn(0).magnitude, relative.GetColumn(1).magnitude, 1f);
        }
    }

    private void OnDestroy()
    {
        if (Progression != null)
            Progression.RankChanged -= ApplyRank;
    }
}
