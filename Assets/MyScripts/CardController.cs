using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardController : MonoBehaviour {

    #region Singleton
    private static CardController m_Instance;
    public static CardController Instance
    {
        get { return m_Instance; }
    }
    private CardController()
    {
    }

    void Awake()
    {
        m_Instance = this;
    }
    #endregion Singleton


    #region Attributes
    public bool IsCardPresent = false;

    public Animator MenuAnimator;
    private GameObject m_CardPackObj;
    private List<GameObject> m_CardObjLit = new List<GameObject>();
    private List<Vector3> m_CardDisplayPositions = new List<Vector3>();
    private List<Sprite> m_CardSpriteList = new List<Sprite>();
    private List<int> m_CardStarCountList = new List<int>();
    private Sprite[] m_CardSprites;
    private List<GameObject> m_IdleStarParticles = new List<GameObject>();
    private List<GameObject> m_SpecialParticles = new List<GameObject>();
    private int m_ResetCompleteCardCount = 0;
    #endregion Attributes

    // Use this for initialization
    void Start () {

        m_CardSprites = Resources.LoadAll<Sprite>("Card Art");

        m_CardDisplayPositions.Clear();
        m_CardDisplayPositions.Add(new Vector3(-480, -242, 0));
        m_CardDisplayPositions.Add(new Vector3(-445, 297, 0));
        m_CardDisplayPositions.Add(new Vector3(455, 251, 0));
        m_CardDisplayPositions.Add(new Vector3(439, -323, 0));
        m_CardDisplayPositions.Add(new Vector3(0, 0, 0));


    }

    /// <summary>
    /// Initiates the card sequence.
    /// </summary>
    /// <returns>The card sequence.</returns>
    IEnumerator InitiateCardSequence()
    {
        MenuAnimator.SetInteger("MenuAnimation", -1);

        m_CardSpriteList.Clear();
        m_CardStarCountList.Clear();
        foreach (Sprite sp in m_CardSprites)
        {
            m_CardSpriteList.Add(sp);
            m_CardStarCountList.Add(Random.Range(1, 5));
        }

        if (m_CardPackObj)
        {
            m_CardPackObj.SetActive(true);
        }
        else
        {
            m_CardPackObj = (GameObject)Instantiate(Resources.Load("CardPack", typeof(GameObject)), new Vector3(420, 315, 0), Quaternion.identity);
        }
        m_CardPackObj.GetComponent<Animator>().SetInteger("direction", 1);
        iTween.MoveTo(m_CardPackObj, iTween.Hash("path", iTweenPath.GetPath("CardEntering"), "Time", 2, "easetype", iTween.EaseType.linear));

        yield return new WaitForSeconds(3);

        for (int i = 0; i < 5; i++)
        {
            m_CardPackObj.transform.GetChild(i).gameObject.GetComponent<Card>().MoveCardToEndPosition(m_CardDisplayPositions[i]);
            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(1);

        MenuAnimator.SetInteger("MenuAnimation", 2);
    }

    /// <summary>
    /// Resets the card sequence.
    /// </summary>
    /// <returns>The card sequence.</returns>
    IEnumerator ResetCardSequence()
    {

        m_CardPackObj.GetComponent<Animator>().SetInteger("direction", -1);
        iTween.MoveTo(m_CardPackObj, iTween.Hash("path", iTweenPath.GetPathReversed("CardEntering"), "Time", 2, "easetype", iTween.EaseType.linear));
        yield return new WaitForSeconds(3);
        m_CardPackObj.SetActive(false);
        MenuAnimator.SetInteger("MenuAnimation", 1);
    }

    /// <summary>
    /// Gets the random card data.
    /// </summary>
    /// <returns>The random card data.</returns>
    public object[] GetRandomCardData()
    {
        int m_RandomNo = Random.Range(0, m_CardSpriteList.Count - 1);

        object[] m_Obj = new object[] { m_CardSpriteList[m_RandomNo], m_CardStarCountList[m_RandomNo] };
        m_CardSpriteList.RemoveAt(m_RandomNo);
        m_CardStarCountList.RemoveAt(m_RandomNo);
        return m_Obj;
    }

    /// <summary>
    /// Gets the idle star particle to play with individiual cards
    /// </summary>
    /// <returns>The idle star particle.</returns>
    /// <param name="_name">Name.</param>
    public GameObject GetIdleStarParticle(string _name)
    {
        if(m_IdleStarParticles.Count > 0)
        {
            GameObject m_StartPS = m_IdleStarParticles[0];
            m_IdleStarParticles.RemoveAt(0);
            //m_StartPS.SetActive(true);
            return m_StartPS;

        }
        else
        {
           return (GameObject)Instantiate(Resources.Load("CardParticleEffects/StarPS", typeof(GameObject)));
        }
    }

    /// <summary>
    /// Resets the individual star particle.
    /// </summary>
    /// <param name="_starParticle">Star particle.</param>
    public void ResetStarParticle(GameObject _starParticle)
    {
        m_IdleStarParticles.Add(_starParticle);
        // _starParticle.SetActive(false);
        _starParticle.GetComponent<ParticleSystem>().Stop();
    }

    /// <summary>
    /// Gets the special particle to be played when the card is revealed
    /// </summary>
    /// <returns>The special particle.</returns>
    /// <param name="_requestParticleType">Request particle type.</param>
    public GameObject GetSpecialParticle(string _requestParticleType)
    {
        GameObject m_ReqParticle = null;
        foreach(GameObject m_SPS in m_SpecialParticles)
        {
            if(m_SPS.name == _requestParticleType)
            {
                m_ReqParticle = m_SPS;
                break;
            }
        }

        if(m_ReqParticle)
        {
            m_SpecialParticles.Remove(m_ReqParticle); 
            m_ReqParticle.SetActive(true);
            return m_ReqParticle;
        }
        else
        {
            m_ReqParticle = (GameObject)Instantiate(Resources.Load("CardParticleEffects/"+ _requestParticleType, typeof(GameObject)));
            m_ReqParticle.name = _requestParticleType;
            return m_ReqParticle;
        }
    }


    /// <summary>
    /// Resets the special particle that was used for revealing cards
    /// </summary>
    /// <param name="_specialParticle">Special particle.</param>
    public void ResetSpecialParticle(GameObject _specialParticle)
    {
        _specialParticle.transform.SetParent(null);
        m_SpecialParticles.Add(_specialParticle);
        _specialParticle.SetActive(false);
        ParticleSystem[] m_PS = _specialParticle.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in m_PS)
        {
            ps.Stop();
        }
    }


    /// <summary>
    /// Method to execute when Individiuals cards completes their resetting.
    /// </summary>
    public void IndividiualCardResetComplete()
    {
        m_ResetCompleteCardCount++;
        if (m_ResetCompleteCardCount > 4)
        {
            StartCoroutine(ResetCardSequence());
        }
    }

    #region UI
    public void StartButtonPress()
    {
        StartCoroutine(InitiateCardSequence());
    }

    public void RestartButtonPress()
    {
        MenuAnimator.SetInteger("MenuAnimation", 3);
        m_ResetCompleteCardCount = 0;
        for (int i = 0; i < 5; i++)
        {
            m_CardPackObj.transform.GetChild(i).gameObject.GetComponent<Card>().Reset();
        }
        IsCardPresent = false;
    }
    #endregion UI
}
