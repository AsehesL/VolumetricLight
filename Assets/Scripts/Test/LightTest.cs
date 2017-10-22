using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTest : MonoBehaviour
{

    public VolumetricLight light;

    public Texture2D[] cookies;

    private int m_CurrentCookieIndex;

    void OnGUI()
    {
        light.directional = GUI.Toggle(new Rect(0, 0, Screen.width*0.1f, Screen.height*0.05f), light.directional, "平行光");
        Color col = light.color;
        float h, s, v;
        Color.RGBToHSV(col, out h, out s, out v);
        GUI.Label(new Rect(0, Screen.height*0.05f, Screen.width*0.1f, Screen.height*0.05f), "颜色（HSV）");
        h = GUI.HorizontalSlider(new Rect(0, Screen.height*0.1f, Screen.width*0.1f, Screen.height*0.05f), h, 0, 1f);
        s = GUI.HorizontalSlider(new Rect(0, Screen.height * 0.15f, Screen.width * 0.1f, Screen.height * 0.05f), s, 0, 1f);
        v = GUI.HorizontalSlider(new Rect(0, Screen.height * 0.2f, Screen.width * 0.1f, Screen.height * 0.05f), v, 0, 1f);
        col = Color.HSVToRGB(h, s, v);
        light.color = col;
        GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width * 0.1f, Screen.height * 0.05f), "亮度");
        light.intensity = GUI.HorizontalSlider(new Rect(0, Screen.height * 0.3f, Screen.width * 0.1f, Screen.height * 0.05f), light.intensity, 0, 10f);
        if (GUI.Button(new Rect(0, Screen.height*0.35f, Screen.width*0.1f, Screen.height*0.05f), "切换Cookie"))
        {
            if (light.cookie == null)
            {
                m_CurrentCookieIndex = 0;
                light.cookie = cookies[m_CurrentCookieIndex];
            }
            else
            {
                m_CurrentCookieIndex++;
                if (m_CurrentCookieIndex >= cookies.Length)
                {
                    light.cookie = null;
                }
                else
                {
                    light.cookie = cookies[m_CurrentCookieIndex];
                }
            }
        }
    }
}
