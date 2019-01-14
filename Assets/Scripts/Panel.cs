using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace game
{
    public partial class Panel : MonoBehaviour
    {
        [SerializeField]
        float m_screenOffset = 0.1f;

        private Vector2 scrollPostionGM;
        private int m_optionIndex = 0;
        private string[] m_optionItems = { "更新工具" };
        private float m_buttonOffset;

        [HideInInspector]
        public Action loadCallback;
        [HideInInspector]
        public Action updateCallback;
        [HideInInspector]
        public Action preLoadCallback;

        void Start()
        {
            Application.targetFrameRate = 30;
            m_buttonOffset = Screen.height * 0.02f * 2.5f;
            OnInitPageAction();
        }

        private System.Action[] m_actions;
        void OnInitPageAction()
        {
            m_actions = new System.Action[]
            {
                    DrawDown,
             };
        }

        void OnGUI()
        {
            var s = new GUIStyle(GUI.skin.button);
            s.fontSize = GUI.skin.textField.fontSize;
            GUI.skin.button.fontSize = (int)((Screen.width * 0.02f + Screen.height * 0.02f) / 2);

            var r = new Rect(new Vector2(Screen.width * m_screenOffset, 50), new Vector2(Screen.width - Screen.width * m_screenOffset * 2, Screen.height - 100));
            using (var a = new GUILayout.AreaScope(r, "", s))
            {
                m_optionIndex = GUILayout.Toolbar(m_optionIndex, m_optionItems, GUILayout.Height(m_buttonOffset));
                using (var scrollViewScope = new GUILayout.ScrollViewScope(scrollPostionGM))
                {
                    scrollPostionGM = scrollViewScope.scrollPosition;
                    m_actions[m_optionIndex]();
                }
            }
        }

        void DrawDown()
        {
            if (loadCallback != null &&GUILayout.Button("加载预制体", GUILayout.Height(m_buttonOffset)))
            {
                loadCallback?.Invoke();
            }

            if (updateCallback != null && GUILayout.Button("启动更新", GUILayout.Height(m_buttonOffset)))
            {
                updateCallback?.Invoke();
            }

            if (preLoadCallback != null && GUILayout.Button("加载新版本预制体", GUILayout.Height(m_buttonOffset)))
            {
                preLoadCallback?.Invoke();
            }
        }
    }
}

