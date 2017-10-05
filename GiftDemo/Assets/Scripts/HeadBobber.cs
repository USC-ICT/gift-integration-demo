using UnityEngine;
using System.Collections;

public class HeadBobber : MonoBehaviour
{
    #region Variables
    public string m_HorizontalAxisName = "AltHorizontal";
    public string m_VerticalAxisName = "AltVertical";
    public float m_BobbingSpeed = 0.08f;
    public float m_BobbingAmount = 0.03f;
    public float m_MidPoint = 2.0f;
    private float timer = 0.0f;
    #endregion

    #region Functions
    void Update()
    {
        float waveslice = 0.0f;
        float horizontal = Input.GetAxis(m_HorizontalAxisName);
        float vertical = Input.GetAxis(m_VerticalAxisName);

        Vector3 cSharpConversion = transform.localPosition;

        if (Mathf.Abs(horizontal) == 0 && Mathf.Abs(vertical) == 0)
        {
            timer = 0.0f;
        }
        else
        {
            waveslice = Mathf.Sin(timer);
            timer = timer + m_BobbingSpeed;
            if (timer > Mathf.PI * 2)
            {
                timer = timer - (Mathf.PI * 2);
            }
        }
        if (waveslice != 0)
        {
            float translateChange = waveslice * m_BobbingAmount;
            float totalAxes = Mathf.Abs(horizontal) + Mathf.Abs(vertical);
            totalAxes = Mathf.Clamp(totalAxes, 0.0f, 1.0f);
            translateChange = totalAxes * translateChange;
            cSharpConversion.y = m_MidPoint + translateChange;
        }
        else
        {
            cSharpConversion.y = m_MidPoint;
        }

        transform.localPosition = cSharpConversion;
    }
    #endregion
}