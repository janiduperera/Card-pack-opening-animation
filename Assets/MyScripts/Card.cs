using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    private Transform m_Transform;
    private Vector3 m_TargetPosition;
    private float m_EularAngelZ;
    private Animator m_Animator;
    private bool m_CardSpriteSet = false;
    private Image m_CardArt;
    private Image[] m_StarsArt;
    private Text m_CardNameTxt;
    private Image m_CardNameBg;
    private Image[] m_CardBg;
    private float m_CardAngle;
    private GameObject m_SpecialParticleSystem;
    private GameObject m_StarParticleSystem;
    private Collider m_Collider;

    enum CardState
    {
        Default,
        MovingToEndPoint,
        Idle,
        BringInForward,
        BringInForwardEnd,
        DismissForward,
        Reset
    };

    private CardState m_CardState = CardState.Default;

    void Awake()
    {
        m_Transform = transform;
        m_Animator = GetComponent<Animator>();
        m_Collider = GetComponent<Collider>();

        m_CardArt = m_Transform.Find("CardPrefab").Find("root").Find("card-art").gameObject.GetComponent<Image>();
        m_StarsArt = m_Transform.Find("CardPrefab").Find("root").Find("stars").GetComponentsInChildren<Image>();
        m_CardNameTxt = m_Transform.Find("CardPrefab").Find("root").Find("name-bg").Find("name").gameObject.GetComponent<Text>();
        m_CardNameBg = m_Transform.Find("CardPrefab").Find("root").Find("name-bg").gameObject.GetComponent<Image>();
        m_CardBg = m_Transform.Find("CardPrefab").Find("root").Find("bg").GetComponentsInChildren<Image>();
    }

    // Update is called once per frame
    void Update() {


        if (m_CardState == CardState.MovingToEndPoint) // Card moving to its idle position from the initial pack. 
        {
            m_Transform.localPosition = Vector3.Slerp(m_Transform.localPosition, m_TargetPosition, 3 * Time.deltaTime);

            if ((m_Transform.localPosition - m_TargetPosition).magnitude < 1)
            {
                SetCardState(CardState.Idle);
                m_Animator.SetInteger("CardAnimState", 1);
                SetStarParticleSystem();
            }
        }
        else if (m_CardState == CardState.BringInForward) // Card moving to revealing position from the idl position.
        {
            m_Transform.localPosition = Vector3.Slerp(m_Transform.localPosition, new Vector3(0, 0, -1), 3 * Time.deltaTime);
            m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, Quaternion.Euler(new Vector3(0, -180, 0)), 3 * Time.deltaTime);
            m_Transform.localScale = Vector3.Lerp(m_Transform.localScale, new Vector3(3, 3, 1), 3 * Time.deltaTime);

            // get the angle Y axis positive value
            m_CardAngle = (m_Transform.localEulerAngles.y < 0) ? 360 + m_Transform.localEulerAngles.y : m_Transform.localEulerAngles.y;

            if (!m_CardSpriteSet && m_CardAngle > 89.8f) // Set Card Data and the Special particle effect in the middle
            {
                m_CardSpriteSet = true;
                SetCardArt();
                SetCardSpecialParticleEffect();
            }


            if ((m_Transform.localScale - new Vector3(3, 3, 1)).magnitude < 1 && (m_Transform.localPosition - new Vector3(0, 0, -1)).magnitude < 1 && m_CardAngle > 178)
            {
                SetCardState(CardState.BringInForwardEnd);
                m_Collider.enabled = true;
            }
        }
        else if (m_CardState == CardState.DismissForward) // Card going back to the idle position from the revealing position
        {

            m_Transform.localPosition = Vector3.Slerp(m_Transform.localPosition, m_TargetPosition, 3 * Time.deltaTime);
            m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, Quaternion.Euler(new Vector3(0, 0, m_EularAngelZ)), 3 * Time.deltaTime);
            m_Transform.localScale = Vector3.Lerp(m_Transform.localScale, new Vector3(1, 1, 1), 3 * Time.deltaTime);

            // get the angle Y axis positive value
            m_CardAngle = (m_Transform.localEulerAngles.y < 0) ? 360 + m_Transform.localEulerAngles.y : m_Transform.localEulerAngles.y;

            if (m_CardSpriteSet && m_CardAngle < 89.8f) // Removing the card data and the special particle effect
            {
                m_CardSpriteSet = false;
                ResetCardArt();
                ResetCardSpecialParticleEffect();
            }

            if ((m_Transform.localScale - Vector3.one).magnitude < 1 && (m_Transform.localPosition - m_TargetPosition).magnitude < 1 && m_CardAngle < 2)
            {
                m_Transform.localScale = Vector3.one;
                m_Animator.enabled = true;
                CardController.Instance.IsCardPresent = false;
                SetCardState(CardState.Idle);
                m_Animator.SetInteger("CardAnimState", 1);
                SetStarParticleSystem();
                m_Collider.enabled = true;
            }
        }
        else if (m_CardState == CardState.Reset) // Reset all the individiaul cards to the pack position. 
        {
            m_Transform.localPosition = Vector3.Slerp(m_Transform.localPosition, Vector3.zero, 3 * Time.deltaTime);

            if ((m_Transform.localPosition - Vector3.zero).magnitude < 1)
            {
                SetCardState(CardState.Default);
                CardController.Instance.IndividiualCardResetComplete();
            }
        }
    }

    void OnMouseEnter()
    {
        if(m_CardState == CardState.Idle)
        {
            m_Animator.SetInteger("CardAnimState", 2);
        }
    }

    void OnMouseDown()
    {
        if (CardController.Instance.IsCardPresent)
        {
            if (m_CardState == CardState.BringInForwardEnd)
            {
                m_Collider.enabled = false;
                SetCardState(CardState.DismissForward);
            }

            return;
        }

        if (m_CardState == CardState.Idle)
        {
            m_Collider.enabled = false;
            SetCardState(CardState.BringInForward);
            m_Animator.SetInteger("CardAnimState", 0);
            m_Animator.Play("Default");
            m_Animator.enabled = false;
            CardController.Instance.IsCardPresent = true;
            ResetStarParticleSystem();
        }

    }

    void OnMouseExit()
    {
        if (m_CardState == CardState.Idle)
        {
            m_Animator.SetInteger("CardAnimState", 3);
        }
    }

    /// <summary>
    /// Sets the card art.
    /// </summary>
    void SetCardArt()
    {
        object[] m_CardDataObj = CardController.Instance.GetRandomCardData();

        // Setting the Card Art
        m_CardArt.sprite = m_CardDataObj[0] as Sprite;

        // Setting the Stars Images
        Sprite m_StarSprite = (Resources.LoadAll<Sprite>("Stars Art"))[Random.Range(0, 2)];
        int m_StarsCount = System.Convert.ToInt32(m_CardDataObj[1]);
        for (int i = 0; i < m_StarsArt.Length; i++)
        {
            if (m_StarsCount > 0)
            {
                m_StarsArt[i].sprite = m_StarSprite;
                m_StarsCount--;
            }
            else
            {
                m_StarsArt[i].sprite = Resources.Load<Sprite>("Stars Art/card_star_grey");
            }
        }

        // Setting the Card name
        m_CardNameTxt.text = m_CardArt.sprite.name;

        // Setting the Card name blue title
        m_CardNameBg.sprite = Resources.Load<Sprite>("card_title");

        // Setting the Card background image
        Sprite m_CardBackgroundSprite = (Resources.LoadAll<Sprite>("Card Background"))[Random.Range(0, 2)];
        for (int i = 0; i < m_CardBg.Length; i++)
        {
            m_CardBg[i].sprite = m_CardBackgroundSprite;
        }

        m_Transform.Find("CardPrefab").localScale = new Vector3(-1, 1, 1);

    }

    /// <summary>
    /// Resets the card art.
    /// </summary>
    void ResetCardArt()
    {
        m_CardArt.sprite = Resources.Load<Sprite>("card_inactive");

        for (int i = 0; i < m_StarsArt.Length; i++)
        {
            m_StarsArt[i].sprite = Resources.Load<Sprite>("Stars Art/card_star_grey");
        }

        m_CardNameTxt.text = "";

        m_CardNameBg.sprite = Resources.Load<Sprite>("card_title_inactive");

        for (int i = 0; i < m_CardBg.Length; i++)
        {
            m_CardBg[i].sprite = Resources.Load<Sprite>("Card Background/card_bg_white");
        }

        m_Transform.Find("CardPrefab").localScale = new Vector3(1, 1, 1);
    }

    /// <summary>
    /// Sets the card special particle effect.
    /// </summary>
    void SetCardSpecialParticleEffect()
    {
        int m_RandomNo = Random.Range(1, 120);
        if(m_RandomNo < 30)
        {
            m_SpecialParticleSystem = CardController.Instance.GetSpecialParticle("FX border Card Blue");
        }
        else if(m_RandomNo < 60)
        {
            m_SpecialParticleSystem = CardController.Instance.GetSpecialParticle("FX border Card Gold");
        }
        else if (m_RandomNo < 90)
        {
            m_SpecialParticleSystem = CardController.Instance.GetSpecialParticle("FX border Card Purple");
        }
        else
        {
            m_SpecialParticleSystem = CardController.Instance.GetSpecialParticle("FX border Card Special");
        }

        m_SpecialParticleSystem.transform.SetParent(m_Transform);
        m_SpecialParticleSystem.transform.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));
        m_SpecialParticleSystem.transform.localPosition = new Vector3(0, 0, -5);


        ParticleSystem[] m_PS = m_SpecialParticleSystem.GetComponentsInChildren<ParticleSystem>();
        foreach(ParticleSystem ps in m_PS)
        {
            ps.Play();
        }
    }

    /// <summary>
    /// Resets the card special particle effect.
    /// </summary>
    void ResetCardSpecialParticleEffect()
    {
        if (m_SpecialParticleSystem)
        {
            CardController.Instance.ResetSpecialParticle(m_SpecialParticleSystem);
        }
    }

    /// <summary>
    /// Sets the state of the card.
    /// </summary>
    /// <param name="_state">State.</param>
    void SetCardState(CardState _state)
    {
        m_CardState = _state;
    }

    /// <summary>
    /// Sets the individual star particle system.
    /// </summary>
    void SetStarParticleSystem()
    {
        m_StarParticleSystem = CardController.Instance.GetIdleStarParticle(name);
        m_StarParticleSystem.transform.SetParent(m_Transform);
        m_StarParticleSystem.transform.localRotation = Quaternion.Euler(Vector3.zero);
        m_StarParticleSystem.transform.localPosition = new Vector3(0, -100, -1);
        m_StarParticleSystem.transform.SetParent(null);
        m_StarParticleSystem.GetComponent<ParticleSystem>().Play();
    }

    /// <summary>
    /// Resets the star particle system.
    /// </summary>
    void ResetStarParticleSystem()
    {
        if(m_StarParticleSystem)
        {
            CardController.Instance.ResetStarParticle(m_StarParticleSystem);
            m_StarParticleSystem = null;
        }
    }

    /// <summary>
    /// Moves the card to end position will be called by the Cardcontroller.cs
    /// </summary>
    /// <param name="_endPosition">End position.</param>
    public void MoveCardToEndPosition(Vector3 _endPosition)
    {
        SetCardState(CardState.MovingToEndPoint);
        m_TargetPosition = _endPosition;
        m_EularAngelZ = m_Transform.eulerAngles.z;
    }

    /// <summary>
    /// Reset this instance. Will be called by the Cardcontroller.cs
    /// </summary>
    public void Reset()
    {
        m_Animator.SetInteger("CardAnimState", 0);
        m_Animator.Play("Default");
        //m_Animator.enabled = false;
        m_Transform.localRotation = Quaternion.Euler(new Vector3(0, 0, m_EularAngelZ));
        m_Transform.localPosition = m_TargetPosition;
        m_Transform.localScale = Vector3.one;
        m_CardSpriteSet = false;
        ResetCardArt();
        ResetCardSpecialParticleEffect();
        ResetStarParticleSystem();
        SetCardState(CardState.Reset);
        m_Collider.enabled = true;
    }
}
